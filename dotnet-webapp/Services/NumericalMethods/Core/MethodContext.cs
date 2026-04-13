namespace dotnet_webapp.Services;

internal sealed class MethodContext
{
    public required string Function { get; init; }
    public required double Tolerance { get; init; }
    public required int MaxIterations { get; init; }
    public required Func<double, double> EvaluateF { get; init; }
    public required Func<double, double> EvaluateG { get; init; }
    public required Func<double, double> EvaluateDerivative { get; init; }
    public required ILogger Logger { get; init; }
}
