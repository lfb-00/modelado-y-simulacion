using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dotnet_webapp.Models;
using dotnet_webapp.Services;

namespace dotnet_webapp.Pages;

public class NewtonCotesModel : PageModel
{
    private readonly ILogger<NewtonCotesModel> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public NewtonCotesModel(ILogger<NewtonCotesModel> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    [BindProperty]
    public string SelectedRule { get; set; } = "trapezoidal";

    [BindProperty]
    public string Function { get; set; } = "sin(x) + x^2";

    [BindProperty]
    public double A { get; set; } = 0;

    [BindProperty]
    public double B { get; set; } = 2;

    [BindProperty]
    public int N { get; set; } = 6;

    public string ResultMessage { get; set; } = string.Empty;
    public string SummaryMessage { get; set; } = string.Empty;
    public bool HasResult { get; set; }
    public double Approximation { get; set; }
    public double StepSize { get; set; }
    public string RuleDisplayName { get; set; } = string.Empty;
    public string FormulaLatex { get; set; } = string.Empty;
    public string ErrorBoundLatex { get; set; } = string.Empty;
    public List<double> ChartXValues { get; set; } = new();
    public List<double> ChartYValues { get; set; } = new();
    public List<double> NodeChartXValues { get; set; } = new();
    public List<double> NodeChartYValues { get; set; } = new();
    public List<NewtonCotesNodeEntry> Nodes { get; set; } = new();

    public void OnGet()
    {
    }

    public void OnPost()
    {
        HasResult = false;
        ResultMessage = string.Empty;
        SummaryMessage = string.Empty;

        if (!NormalizeInputs())
        {
            return;
        }

        if (!ValidateInput())
        {
            return;
        }

        try
        {
            var service = new NewtonCotesService(_loggerFactory);
            service.Compute(Function, SelectedRule, A, B, N);

            Approximation = service.Approximation;
            StepSize = service.StepSize;
            RuleDisplayName = service.RuleDisplayName;
            FormulaLatex = service.FormulaLatex;
            ErrorBoundLatex = service.ErrorBoundLatex;
            ChartXValues = service.ChartXValues;
            ChartYValues = service.ChartYValues;
            NodeChartXValues = service.NodeChartXValues;
            NodeChartYValues = service.NodeChartYValues;
            Nodes = service.Nodes;
            HasResult = true;

            ResultMessage = $"Integracion completada con {RuleDisplayName.ToLowerInvariant()}.";
            SummaryMessage = $"Integral aproximada = {Approximation.ToString("0.0000000000", CultureInfo.InvariantCulture)} con h = {StepSize.ToString("0.000000", CultureInfo.InvariantCulture)} y n = {N}.";
        }
        catch (ArgumentException ex)
        {
            ResultMessage = "Error: " + ex.Message;
            _logger.LogWarning(ex, "Validacion fallida en Newton-Cotes");
        }
        catch (Exception ex)
        {
            ResultMessage = "Error al integrar: " + ex.Message;
            _logger.LogError(ex, "Error inesperado en Newton-Cotes");
        }
    }

    private bool NormalizeInputs()
    {
        if (!TryParseFlexibleDouble("A", out var a) || !TryParseFlexibleDouble("B", out var b))
        {
            ResultMessage = "Intervalo invalido. Usa valores como 0.5 o 0,5.";
            return false;
        }

        if (!int.TryParse(Request.Form["N"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) &&
            !int.TryParse(Request.Form["N"], NumberStyles.Integer, CultureInfo.CurrentCulture, out n))
        {
            ResultMessage = "n invalido. Ingresa un numero entero de subintervalos.";
            return false;
        }

        A = a;
        B = b;
        N = n;
        return true;
    }

    private bool TryParseFlexibleDouble(string key, out double value)
    {
        value = 0;
        string raw = Request.Form[key].ToString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        const NumberStyles strictFloat = NumberStyles.AllowLeadingWhite |
                                         NumberStyles.AllowTrailingWhite |
                                         NumberStyles.AllowLeadingSign |
                                         NumberStyles.AllowDecimalPoint |
                                         NumberStyles.AllowExponent;

        if (double.TryParse(raw, strictFloat, CultureInfo.InvariantCulture, out value) ||
            double.TryParse(raw, strictFloat, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        try
        {
            var parser = new FunctionParser(raw);
            value = parser.Evaluate(0);
            return double.IsFinite(value);
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Function))
        {
            ResultMessage = "Ingresa una funcion.";
            return false;
        }

        if (A >= B)
        {
            ResultMessage = "Debe cumplirse a < b.";
            return false;
        }

        if (N <= 0)
        {
            ResultMessage = "n debe ser mayor que 0.";
            return false;
        }

        return true;
    }
}