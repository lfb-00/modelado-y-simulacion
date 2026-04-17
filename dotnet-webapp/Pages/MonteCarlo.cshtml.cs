using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dotnet_webapp.Models;
using dotnet_webapp.Services;

namespace dotnet_webapp.Pages;

public class MonteCarloModel : PageModel
{
    private readonly ILogger<MonteCarloModel> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public MonteCarloModel(ILogger<MonteCarloModel> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    [BindProperty]
    public string Function { get; set; } = "sin(x) + x^2";

    [BindProperty]
    public double A { get; set; } = 0;

    [BindProperty]
    public double B { get; set; } = 2;

    [BindProperty]
    public int SamplesCount { get; set; } = 5000;

    [BindProperty]
    public int? Seed { get; set; }

    public string ResultMessage { get; set; } = string.Empty;
    public string SummaryMessage { get; set; } = string.Empty;
    public bool HasResult { get; set; }

    public double Approximation { get; set; }
    public double MeanValue { get; set; }
    public double StandardDeviation { get; set; }
    public double StandardError { get; set; }
    public string FunctionLatex { get; set; } = string.Empty;
    public double Confidence95Lower { get; set; }
    public double Confidence95Upper { get; set; }
    public int EffectiveSeed { get; set; }

    public List<double> ChartXValues { get; set; } = new();
    public List<double> ChartYValues { get; set; } = new();
    public List<double> InsideAreaXValues { get; set; } = new();
    public List<double> InsideAreaYValues { get; set; } = new();
    public List<double> OutsideAreaXValues { get; set; } = new();
    public List<double> OutsideAreaYValues { get; set; } = new();
    public List<MonteCarloSampleEntry> Samples { get; set; } = new();

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
            var service = new MonteCarloIntegrationService(_loggerFactory);
            service.Compute(Function, A, B, SamplesCount, Seed);

            Approximation = service.Approximation;
            MeanValue = service.MeanValue;
            StandardDeviation = service.StandardDeviation;
            StandardError = service.StandardError;
            FunctionLatex = service.FunctionLatex;
            Confidence95Lower = service.Confidence95Lower;
            Confidence95Upper = service.Confidence95Upper;
            EffectiveSeed = service.EffectiveSeed;
            ChartXValues = service.ChartXValues;
            ChartYValues = service.ChartYValues;
            InsideAreaXValues = service.InsideAreaXValues;
            InsideAreaYValues = service.InsideAreaYValues;
            OutsideAreaXValues = service.OutsideAreaXValues;
            OutsideAreaYValues = service.OutsideAreaYValues;
            Samples = service.Samples;

            HasResult = true;
            ResultMessage = "Integracion Monte Carlo completada.";
            SummaryMessage =
                $"Integral aproximada = {Approximation.ToString("0.0000000000", CultureInfo.InvariantCulture)} con N = {SamplesCount} y semilla = {EffectiveSeed}.";
        }
        catch (ArgumentException ex)
        {
            ResultMessage = "Error: " + ex.Message;
            _logger.LogWarning(ex, "Validacion fallida en Monte Carlo");
        }
        catch (Exception ex)
        {
            ResultMessage = "Error al integrar: " + ex.Message;
            _logger.LogError(ex, "Error inesperado en Monte Carlo");
        }
    }

    private bool NormalizeInputs()
    {
        if (!TryParseFlexibleDouble("A", out var a) || !TryParseFlexibleDouble("B", out var b))
        {
            ResultMessage = "Intervalo invalido. Usa valores como 0.5 o 0,5.";
            return false;
        }

        if (!int.TryParse(Request.Form["SamplesCount"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var sampleCount) &&
            !int.TryParse(Request.Form["SamplesCount"], NumberStyles.Integer, CultureInfo.CurrentCulture, out sampleCount))
        {
            ResultMessage = "Cantidad de muestras invalida. Ingresa un numero entero.";
            return false;
        }

        int? parsedSeed = null;
        string rawSeed = Request.Form["Seed"].ToString().Trim();
        if (!string.IsNullOrWhiteSpace(rawSeed))
        {
            if (!int.TryParse(rawSeed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seedValue) &&
                !int.TryParse(rawSeed, NumberStyles.Integer, CultureInfo.CurrentCulture, out seedValue))
            {
                ResultMessage = "Semilla invalida. Ingresa un numero entero o deja el campo vacio.";
                return false;
            }

            parsedSeed = seedValue;
        }

        A = a;
        B = b;
        SamplesCount = sampleCount;
        Seed = parsedSeed;
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

        return double.TryParse(raw, strictFloat, CultureInfo.InvariantCulture, out value) ||
               double.TryParse(raw, strictFloat, CultureInfo.CurrentCulture, out value);
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

        if (SamplesCount <= 1)
        {
            ResultMessage = "La cantidad de muestras debe ser mayor que 1.";
            return false;
        }

        return true;
    }
}
