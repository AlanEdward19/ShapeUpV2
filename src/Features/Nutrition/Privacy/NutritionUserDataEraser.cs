namespace ShapeUp.Features.Nutrition.Privacy;

using Infrastructure.Data;
using Infrastructure.Mongo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ShapeUp.Shared.Privacy;

/// <summary>
/// Everything the user logged (diary, weight, water, meals, fasting, goals, plans) is deleted, including health data a
/// nutritionist recorded about them. What the user wrote for others (comments, templates) is deleted too. Records that
/// belong to someone else but carry the user as author (a client's measurement, a plan prescribed to a client) stay with
/// that person, with the author cleared. Foods in the shared catalog stay, anonymized.
/// </summary>
public class NutritionUserDataEraser(NutritionDbContext db, IMongoClient mongo, IOptions<NutritionMongoOptions> options) : IUserDataEraser
{
    public string Module => "Nutrition";

    public async Task EraseAsync(int userId, string? email, CancellationToken cancellationToken)
    {
        // SQL. Diary entries go with their day (cascade).
        await db.DiaryDays.Where(d => d.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.UserMeals.Where(m => m.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GoalEvaluations.Where(g => g.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.FastingAgendas.Where(f => f.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.FastingOverrides.Where(f => f.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.WeightRegisters.Where(w => w.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.WeightTargets.Where(w => w.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Profiles.Where(p => p.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Measurements.Where(m => m.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Measurements.Where(m => m.RecordedByUserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.RecordedByUserId, 0), cancellationToken);
        await db.DiaryComments.Where(c => c.ClientUserId == userId || c.AuthorUserId == userId).ExecuteDeleteAsync(cancellationToken);

        // Mongo.
        var opts = options.Value;
        var database = mongo.GetDatabase(opts.DatabaseName);
        var f = Builders<BsonDocument>.Filter;
        var u = Builders<BsonDocument>.Update;

        await database.GetCollection<BsonDocument>(opts.HydrationDaysCollectionName).DeleteManyAsync(f.Eq("UserId", userId), cancellationToken);
        await database.GetCollection<BsonDocument>(opts.WeightRegistersCollectionName).DeleteManyAsync(f.Eq("UserId", userId), cancellationToken);
        await database.GetCollection<BsonDocument>(opts.WeightTargetsCollectionName).DeleteManyAsync(f.Eq("UserId", userId), cancellationToken);
        await database.GetCollection<BsonDocument>(opts.FoodOverridesCollectionName).DeleteManyAsync(f.Eq("UserId", userId), cancellationToken);
        await database.GetCollection<BsonDocument>(opts.FoodModerationRequestsCollectionName).DeleteManyAsync(f.Eq("RequestedByUserId", userId), cancellationToken);
        await database.GetCollection<BsonDocument>(opts.MealPlanTemplatesCollectionName).DeleteManyAsync(f.Eq("CreatedByUserId", userId), cancellationToken);

        var plans = database.GetCollection<BsonDocument>(opts.MealPlansCollectionName);
        await plans.DeleteManyAsync(f.Eq("UserId", userId), cancellationToken);
        await plans.UpdateManyAsync(f.Eq("PrescribedByUserId", userId), u.Set("PrescribedByUserId", BsonNull.Value), cancellationToken: cancellationToken);

        await database.GetCollection<BsonDocument>(opts.FoodsCollectionName)
            .UpdateManyAsync(f.Eq("CreatedByUserId", userId), u.Set("CreatedByUserId", 0), cancellationToken: cancellationToken);
    }
}
