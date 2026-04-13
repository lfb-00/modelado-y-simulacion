namespace dotnet_webapp.Models;

/// <summary>
/// Representa una iteración en la ejecución de un método numérico
/// </summary>
public class StepEntry
{
    /// <summary>
    /// Número de iteración
    /// </summary>
    public int Iteration { get; set; }

    // Bisección
    public double? A { get; set; }
    public double? B { get; set; }
    public double? Mid { get; set; }
    public double? FA { get; set; }
    public double? FB { get; set; }
    public double? FC { get; set; }

    // Newton-Raphson y Punto Fijo
    public double? X { get; set; }
    public double? FX { get; set; }
    public double? DFX { get; set; }
    public double? G { get; set; }
    public double? GG { get; set; }
    public double? AitkenX { get; set; }

    // Común para todos
    public double? Error { get; set; }
    public double? Residual { get; set; }
}
