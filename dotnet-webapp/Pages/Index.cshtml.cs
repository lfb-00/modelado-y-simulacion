using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dotnet_webapp.Models;
using dotnet_webapp.Services;

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
    public double? RootX { get; set; }
    public double? RootY { get; set; }

    public void OnGet()
    {
        ClearResults();
    }

    public void OnPost()
    {
        ClearResults();

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
        RootX = null;
        RootY = null;
    }
}
