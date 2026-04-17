namespace dotnet_webapp.Models;

public class NewtonCotesNodeEntry
{
    public int Index { get; set; }
    public double X { get; set; }
    public double FX { get; set; }
    public int Coefficient { get; set; }
    public double WeightedValue { get; set; }
    public double PartialWeightedSum { get; set; }
}