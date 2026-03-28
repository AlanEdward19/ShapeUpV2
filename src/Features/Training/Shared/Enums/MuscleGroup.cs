namespace ShapeUp.Features.Training.Shared.Enums;

/// <summary>
/// Represents muscle groups and individual muscles using bit flags.
/// This allows for flexible combination of multiple muscles and group hierarchies.
/// Example: Chest includes UpperChest, MiddleChest, and LowerChest
/// </summary>
[Flags]
public enum MuscleGroup : long
{
    // Chest Muscles
    MiddleChest = 1L << 0,
    UpperChest = 1L << 1,
    LowerChest = 1L << 2,
    Chest = MiddleChest | UpperChest | LowerChest,

    // Arm Muscles
    Triceps = 1L << 3,
    Biceps = 1L << 4,
    Forearms = 1L << 5,
    Arms = Triceps | Biceps | Forearms,

    // Shoulder Muscles
    DeltoidAnterior = 1L << 6,
    DeltoidLateral = 1L << 7,
    DeltoidPosterior = 1L << 8,
    Shoulders = DeltoidAnterior | DeltoidLateral | DeltoidPosterior,

    // Back Muscles
    Traps = 1L << 9,
    UpperBack = 1L << 10,
    MiddleBack = 1L << 11,
    LowerBack = 1L << 12,
    Lats = 1L << 13,
    Back = Traps | UpperBack | MiddleBack | LowerBack | Lats,

    // Core/Abs Muscles
    AbsUpper = 1L << 14,
    AbsLower = 1L << 15,
    AbsObliques = 1L << 16,
    Abs = AbsUpper | AbsLower | AbsObliques,

    // Leg Muscles
    Quadriceps = 1L << 17,
    Hamstrings = 1L << 18,
    Glutes = 1L << 19,
    Calves = 1L << 20,
    HipFlexors = 1L << 21,
    Legs = Quadriceps | Hamstrings | Glutes | Calves | HipFlexors,

    // Composite Groups
    FullBody = Chest | Arms | Shoulders | Back | Abs | Legs,
    
    // Additional common muscles
    LatissimusDorsi = Lats,
    Pectoralis = Chest,
    Rhomboid = MiddleBack,
    Serratus = MiddleBack,
    PectoralisMajor = MiddleChest,
    PectoralisMinor = UpperChest,
}

