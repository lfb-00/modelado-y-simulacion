using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

internal sealed class MethodResult
{
    public List<StepEntry> Steps { get; } = new();
    public double? RootX { get; set; }
    public double? RootY { get; set; }
    public string ResultMessage { get; set; } = string.Empty;
    public string SummaryMessage { get; set; } = string.Empty;
}
