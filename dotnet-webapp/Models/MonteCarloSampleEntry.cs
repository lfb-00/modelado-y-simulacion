namespace dotnet_webapp.Models;

public class MonteCarloSampleEntry
{
    public int Index { get; set; }
    public double X { get; set; }
    public double FX { get; set; }
    public double PartialMean { get; set; }
    public double PartialIntegralEstimate { get; set; }
}
