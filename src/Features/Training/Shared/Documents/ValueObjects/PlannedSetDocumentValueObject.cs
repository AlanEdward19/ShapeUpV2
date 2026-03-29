namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class PlannedSetDocumentValueObject
{
    public int Repetitions { get; set; }
    public decimal Load { get; set; }
    public string LoadUnit { get; set; } = "kg";
    public string SetType { get; set; } = "working";
    public int Rpe { get; set; }
    public int RestSeconds { get; set; }
}

