using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dotnet_webapp.Models;
using dotnet_webapp.Services;
using System.Globalization;

namespace dotnet_webapp.Pages;

/// <summary>
/// Página principal para simulación numérica de métodos de búsqueda de raíces
/// </summary>
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public IndexModel(ILogger<IndexModel> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    [BindProperty]
    public string SelectedAlgorithm { get; set; } = "bisection";

    [BindProperty]
    public string Function { get; set; } = "x^3 - x - 2";

    [BindProperty]
    public double A { get; set; } = 1.0;

    [BindProperty]
    public double B { get; set; } = 2.0;

    [BindProperty]
    public double X0 { get; set; } = 1.5;

    [BindProperty]
    public double Tolerance { get; set; } = 1e-6;

    [BindProperty]
    public int MaxIterations { get; set; } = 50;

    public string ResultMessage { get; set; } = string.Empty;
    public string SummaryMessage { get; set; } = string.Empty;
    public List<StepEntry> Steps { get; set; } = new();
    public List<double> ChartXValues { get; set; } = new();
    public List<double> ChartYValues { get; set; } = new();
    public List<double> OverviewChartXValues { get; set; } = new();
    public List<double> OverviewChartYValues { get; set; } = new();
    public List<int> IterationNumbers { get; set; } = new();
    public List<double> IterationEstimates { get; set; } = new();
    public List<double> IterationErrors { get; set; } = new();
    public List<double> IterationResiduals { get; set; } = new();
    public double? RootX { get; set; }
    public double? RootY { get; set; }
    public string ConvergenceComment { get; set; } = string.Empty;

    public void OnGet()
    {
        ClearResults();
    }

    public void OnPost()
    {
        ClearResults();

        if (!NormalizeNumericInputs())
            return;

        _logger.LogInformation("Inicio de cálculo - Algoritmo: {Algorithm}, Tolerancia: {Tolerance}, MaxIteraciones: {MaxIterations}", 
            SelectedAlgorithm, Tolerance, MaxIterations);

        // Validar entrada
        if (!ValidateInput())
            return;

        try
        {
            ExecuteNumericalMethod();
        }
        catch (ArgumentException ex)
        {
            ResultMessage = "Error en la función: " + ex.Message;
            _logger.LogError(ex, "Error en la evaluación de la función");
        }
        catch (Exception ex)
        {
            ResultMessage = "Error al evaluar la función: " + ex.Message;
            _logger.LogError(ex, "Error inesperado");
        }

        _logger.LogInformation("Cálculo completado - Resultado: {Result}", ResultMessage);
    }

    /// <summary>
    /// Normaliza entradas numéricas para aceptar tanto coma como punto decimal.
    /// Evita que 0.2 se interprete como 2 cuando la cultura usa coma decimal.
    /// </summary>
    private bool NormalizeNumericInputs()
    {
        if (!TryParseFlexibleDouble("Tolerance", out var tolerance))
        {
            ResultMessage = "Tolerancia inválida. Use formato numérico como 1e-6, 0.000001, 0,000001.";
            return false;
        }
        Tolerance = tolerance;

        if (!int.TryParse(Request.Form["MaxIterations"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxIterations) &&
            !int.TryParse(Request.Form["MaxIterations"], NumberStyles.Integer, CultureInfo.CurrentCulture, out maxIterations))
        {
            ResultMessage = "Máximo de iteraciones inválido.";
            return false;
        }
        MaxIterations = maxIterations;

        if (SelectedAlgorithm == "bisection")
        {
            if (!TryParseFlexibleDouble("A", out var a) || !TryParseFlexibleDouble("B", out var b))
            {
                ResultMessage = "Intervalo inválido. Use valores como 0.2 o 0,2.";
                return false;
            }

            A = a;
            B = b;
        }
        else
        {
            if (!TryParseFlexibleDouble("X0", out var x0))
            {
                ResultMessage = "Semilla inicial inválida. Use un valor como 1.5 o 1,5.";
                return false;
            }

            X0 = x0;
        }

        return true;
    }

    private bool TryParseFlexibleDouble(string key, out double value)
    {
        value = 0;
        string raw = Request.Form[key].ToString().Trim();

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        const NumberStyles strictFloat = NumberStyles.AllowLeadingWhite |
                                         NumberStyles.AllowTrailingWhite |
                                         NumberStyles.AllowLeadingSign |
                                         NumberStyles.AllowDecimalPoint |
                                         NumberStyles.AllowExponent;

        return double.TryParse(raw, strictFloat, CultureInfo.InvariantCulture, out value) ||
               double.TryParse(raw, strictFloat, CultureInfo.CurrentCulture, out value);
    }

    /// <summary>
    /// Valida los parámetros de entrada
    /// </summary>
    private bool ValidateInput()
    {
        if (Tolerance <= 0)
        {
            ResultMessage = "La tolerancia debe ser mayor que 0.";
            _logger.LogWarning("Tolerancia inválida: {Tolerance}", Tolerance);
            return false;
        }

        if (MaxIterations <= 0)
        {
            ResultMessage = "El número máximo de iteraciones debe ser mayor que 0.";
            _logger.LogWarning("MaxIteraciones inválido: {MaxIterations}", MaxIterations);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ejecuta el método numérico seleccionado
    /// </summary>
    private void ExecuteNumericalMethod()
    {
        var numericalService = new NumericalMethodsService(_loggerFactory, Function, SelectedAlgorithm, Tolerance, MaxIterations);
        var chartService = new ChartService(_loggerFactory, Function, SelectedAlgorithm);

        switch (SelectedAlgorithm)
        {
            case "bisection":
                _logger.LogInformation("Ejecutando Bisección con intervalo [{A}, {B}]", A, B);
                numericalService.ComputeBisection(A, B);
                if (numericalService.ResultMessage.StartsWith("Raíz"))
                {
                    chartService.GenerateChart(A - 0.5, B + 0.5);
                }
                else
                {
                    chartService.GenerateChart(A, B);
                }
                break;

            case "newton":
                _logger.LogInformation("Ejecutando Newton-Raphson con x0={X0}", X0);
                numericalService.ComputeNewton(X0);
                if (numericalService.ResultMessage.StartsWith("Raíz"))
                {
                    chartService.GenerateChart(Math.Min(X0, numericalService.RootX ?? X0) - 1, Math.Max(X0, numericalService.RootX ?? X0) + 1);
                }
                else
                {
                    chartService.GenerateChart(X0 - 2, X0 + 2);
                }
                break;

            case "fixed-point":
                _logger.LogInformation("Ejecutando Punto Fijo con x0={X0}", X0);
                numericalService.ComputeFixedPoint(X0);
                chartService.GenerateChart(X0 - 2, X0 + 2);
                break;

            case "fixed-point-aitken":
                _logger.LogInformation("Ejecutando Punto Fijo con Aitken con x0={X0}", X0);
                numericalService.ComputeFixedPointAitken(X0);
                chartService.GenerateChart(X0 - 2, X0 + 2);
                break;

            default:
                ResultMessage = "Seleccione un método válido.";
                _logger.LogError("Algoritmo inválido: {Algorithm}", SelectedAlgorithm);
                return;
        }

        // Copiar resultados
        Steps = numericalService.Steps;
        ResultMessage = numericalService.ResultMessage;
        SummaryMessage = numericalService.SummaryMessage;
        RootX = numericalService.RootX;
        RootY = numericalService.RootY;
        ChartXValues = chartService.ChartXValues;
        ChartYValues = chartService.ChartYValues;

        // Generar gráfico de visión general (rango [-30, 30])
        var overviewChartService = new ChartService(_loggerFactory, Function, SelectedAlgorithm);
        overviewChartService.GenerateChart(-30, 30);
        OverviewChartXValues = overviewChartService.ChartXValues;
        OverviewChartYValues = overviewChartService.ChartYValues;

        BuildConvergenceSeries();
        ConvergenceComment = BuildConvergenceComment();
    }

    private void BuildConvergenceSeries()
    {
        IterationNumbers.Clear();
        IterationEstimates.Clear();
        IterationErrors.Clear();
        IterationResiduals.Clear();

        foreach (var step in Steps)
        {
            double? estimate = SelectedAlgorithm switch
            {
                "bisection" => step.Mid,
                "newton" => step.X,
                "fixed-point" => step.G,
                "fixed-point-aitken" => step.AitkenX,
                _ => null
            };

            if (!estimate.HasValue || !step.Error.HasValue)
            {
                continue;
            }

            IterationNumbers.Add(step.Iteration);
            IterationEstimates.Add(estimate.Value);
            IterationErrors.Add(Math.Max(step.Error.Value, 1e-16));

            double residual = step.Residual ?? SelectedAlgorithm switch
            {
                "bisection" => Math.Abs(step.FC ?? 0),
                "newton" => Math.Abs(step.FX ?? 0),
                "fixed-point" => Math.Abs((step.G ?? 0) - (step.X ?? 0)),
                _ => step.Error.Value
            };

            IterationResiduals.Add(Math.Max(residual, 1e-16));
        }
    }

    private string BuildConvergenceComment()
    {
        if (IterationErrors.Count < 3)
        {
            return "Sin suficientes iteraciones para estimar orden de convergencia.";
        }

        double e0 = IterationErrors[^3];
        double e1 = IterationErrors[^2];
        double e2 = IterationErrors[^1];

        if (e0 <= 0 || e1 <= 0 || e2 <= 0)
        {
            return "No se pudo estimar el orden de convergencia por errores no positivos.";
        }

        double denominator = Math.Log(e1 / e0);
        if (Math.Abs(denominator) < 1e-12)
        {
            return "Orden de convergencia no estimable (razón de errores casi constante).";
        }

        double order = Math.Log(e2 / e1) / denominator;
        return $"Orden de convergencia estimado: p ≈ {order:F3}.";
    }

    /// <summary>
    /// Limpia los resultados de una ejecución anterior
    /// </summary>
    private void ClearResults()
    {
        ResultMessage = string.Empty;
        SummaryMessage = string.Empty;
        Steps.Clear();
        ChartXValues.Clear();
        ChartYValues.Clear();
        OverviewChartXValues.Clear();
        OverviewChartYValues.Clear();
        IterationNumbers.Clear();
        IterationEstimates.Clear();
        IterationErrors.Clear();
        IterationResiduals.Clear();
        ConvergenceComment = string.Empty;
        RootX = null;
        RootY = null;
    }
}
