using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.WorkoutTemplates.AssignWorkoutTemplate;

namespace UnitTests.Domains.Training.WorkoutTemplates;

public class AssignWorkoutTemplateHandlerTests
{
    // --- workout-schedule-dashboard: AssignedWeekdays default on assign (WSD-01) ---

    [Fact]
    public async Task HandleAsync_WhenValid_CreatesPlanWithEmptyAssignedWeekdays()
    {
        var templateRepository = new Mock<IWorkoutTemplateRepository>();
        templateRepository
            .Setup(x => x.GetByIdAsync("tpl-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutTemplateDocument
            {
                Id = "tpl-1",
                CreatedByUserId = 10,
                Name = "Template A",
                DurationInWeeks = 4,
                Phase = "Hypertrophy",
                Difficulty = Difficulty.Intermediate,
                Blocks =
                [
                    new BlockDocumentValueObject
                    {
                        Type = BlockType.Straight,
                        Exercises =
                        [
                            new BlockExerciseDocumentValueObject
                            {
                                ExerciseId = 1,
                                ExerciseName = "Bench",
                                Sets = []
                            }
                        ]
                    }
                ]
            });

        WorkoutPlanDocument? capturedPlan = null;
        var planRepository = new Mock<IWorkoutPlanRepository>();
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) =>
            {
                plan.Id = "plan-1";
                capturedPlan = plan;
            })
            .Returns(Task.CompletedTask);

        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(10, 22, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new AssignWorkoutTemplateHandler(
            templateRepository.Object,
            planRepository.Object,
            accessPolicy.Object,
            new AssignWorkoutTemplateCommandValidator());

        var result = await sut.HandleAsync(
            new AssignWorkoutTemplateCommand("tpl-1", 22, "Assigned Plan"),
            10,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(capturedPlan!.AssignedWeekdays);
        Assert.Empty(result.Value!.AssignedWeekdays);
    }
}
