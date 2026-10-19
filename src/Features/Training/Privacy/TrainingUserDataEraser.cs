namespace ShapeUp.Features.Training.Privacy;

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Infrastructure.Mongo;
using ShapeUp.Shared.Privacy;

/// <summary>
/// Plans and sessions of the user are deleted; templates they created are deleted. Plans a trainer wrote for someone else
/// stay with that person (the trainer reference is cleared). Collections are addressed as raw documents, so the eraser does not depend on the document classes.
/// </summary>
public class TrainingUserDataEraser(IMongoClient client, IOptions<TrainingMongoOptions> options) : IUserDataEraser
{
    public string Module => "Training";

    public async Task EraseAsync(int userId, string? email, CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var db = client.GetDatabase(opts.DatabaseName);
        var f = Builders<BsonDocument>.Filter;

        var plans = db.GetCollection<BsonDocument>(opts.WorkoutPlansCollectionName);
        await plans.DeleteManyAsync(f.Eq("TargetUserId", userId), cancellationToken);
        await plans.UpdateManyAsync(f.Eq("TrainerUserId", userId), Builders<BsonDocument>.Update.Set("TrainerUserId", BsonNull.Value), cancellationToken: cancellationToken);
        await plans.UpdateManyAsync(f.Eq("CreatedByUserId", userId), Builders<BsonDocument>.Update.Set("CreatedByUserId", 0), cancellationToken: cancellationToken);

        var sessions = db.GetCollection<BsonDocument>(opts.WorkoutSessionsCollectionName);
        await sessions.DeleteManyAsync(f.Eq("TargetUserId", userId), cancellationToken);
        await sessions.UpdateManyAsync(f.Eq("TrainerUserId", userId), Builders<BsonDocument>.Update.Set("TrainerUserId", BsonNull.Value), cancellationToken: cancellationToken);
        await sessions.UpdateManyAsync(f.Eq("ExecutedByUserId", userId), Builders<BsonDocument>.Update.Set("ExecutedByUserId", 0), cancellationToken: cancellationToken);

        await db.GetCollection<BsonDocument>(opts.WorkoutTemplatesCollectionName).DeleteManyAsync(f.Eq("CreatedByUserId", userId), cancellationToken);
    }
}
