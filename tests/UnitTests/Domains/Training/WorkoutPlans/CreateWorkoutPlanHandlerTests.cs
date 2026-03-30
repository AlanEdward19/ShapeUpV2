using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.WorkoutPlans.CreateWorkoutPlan;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace UnitTests.Domains.Training.WorkoutPlans;

public class CreateWorkoutPlanHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenActorCannotCreateForTarget_ReturnsForbidden()
    {
        var planRepository = new Mock<IWorkoutPlanRepository>();
        var exerciseRepository = new Mock<IExerciseRepository>();
        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(11, 22, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new CreateWorkoutPlanHandler(planRepository.Object, exerciseRepository.Object, accessPolicy.Object, new CreateWorkoutPlanCommandValidator());

        var command = new CreateWorkoutPlanCommand(
            22,
            "Plan A",
            null,
            4,
            "Hypertrophy",
            Difficulty.Intermediate,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, 8, 90)])]);

        var result = await sut.HandleAsync(command, 11, ["training:workout-plans:create"], CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenValid_CreatesWorkoutPlan()
    {
        var planRepository = new Mock<IWorkoutPlanRepository>();
        WorkoutPlanDocument? capturedPlan = null;
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) =>
            {
                plan.Id = "plan-1";
                capturedPlan = plan;
            })
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });

        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(10, 10, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new CreateWorkoutPlanHandler(planRepository.Object, exerciseRepository.Object, accessPolicy.Object, new CreateWorkoutPlanCommandValidator());

        var command = new CreateWorkoutPlanCommand(
            10,
            " Push Day ",
            " notes ",
            4,
            " Strength ",
            Difficulty.Hard,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(8, 80, LoadUnit.Kg, SetType.Working, Technique.Straight, 8, 120)])]);

        var result = await sut.HandleAsync(command, 10, ["training:workout-plans:create"], CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedPlan);
        Assert.Equal("Push Day", capturedPlan!.Name);
        Assert.Equal("notes", capturedPlan.Notes);
        Assert.Equal("plan-1", result.Value!.PlanId);
    }

}



