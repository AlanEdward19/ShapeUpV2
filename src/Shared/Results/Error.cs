namespace ShapeUp.Shared.Results;

public sealed record Error(string Code, string Message, int StatusCode);
