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
    public int Dimensions { get; set; } = 1;

    [BindProperty]
    public double A { get; set; } = 0;

    [BindProperty]
    public double B { get; set; } = 2;

    [BindProperty]
    public double AY { get; set; } = 0;

    [BindProperty]
    public double BY { get; set; } = 1;

    [BindProperty]
    public double AZ { get; set; } = 0;

    [BindProperty]
    public double BZ { get; set; } = 1;

    [BindProperty]
    public int? SamplesCount { get; set; } = 5000;

    [BindProperty]
    public double? ConfidenceLevel { get; set; } = 95;

    [BindProperty]
    public double? ZCriticalInput { get; set; }

    [BindProperty]
    public double? MaxError { get; set; }

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
    public double ZCritical { get; set; }
    public double ConfidenceLower { get; set; }
    public double ConfidenceUpper { get; set; }
    public int EffectiveSeed { get; set; }
    public double DomainVolume { get; set; }
    public bool ToleranceMet { get; set; }
    public int EffectiveSamplesUsed { get; set; }
    public int? EstimatedNForTolerance { get; set; }

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
            service.Compute(Function, Dimensions, A, B, AY, BY, AZ, BZ, SamplesCount, Seed, ConfidenceLevel, ZCriticalInput, MaxError);

            Approximation = service.Approximation;
            MeanValue = service.MeanValue;
            StandardDeviation = service.StandardDeviation;
            StandardError = service.StandardError;
            FunctionLatex = service.FunctionLatex;
            ZCritical = service.ZCritical;
            ConfidenceLevel = service.ConfidenceLevel;
            ConfidenceLower = service.ConfidenceLower;
            ConfidenceUpper = service.ConfidenceUpper;
            EffectiveSeed = service.EffectiveSeed;
            DomainVolume = service.DomainVolume;
            ToleranceMet = service.ToleranceMet;
            EffectiveSamplesUsed = service.EffectiveSamplesUsed;
            EstimatedNForTolerance = service.EstimatedNForTolerance;
            SamplesCount = service.EffectiveSamplesUsed;
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
                $"Integral {Dimensions}D aproximada = {Approximation.ToString("0.0000000000", CultureInfo.InvariantCulture)} con N = {EffectiveSamplesUsed}, volumen = {DomainVolume.ToString("0.000000", CultureInfo.InvariantCulture)} y semilla = {EffectiveSeed}.";
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
        if (!int.TryParse(Request.Form["Dimensions"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dimensions) &&
            !int.TryParse(Request.Form["Dimensions"], NumberStyles.Integer, CultureInfo.CurrentCulture, out dimensions))
        {
            ResultMessage = "Dimension invalida. Debe ser 1, 2 o 3.";
            return false;
        }

        if (!TryParseFlexibleDouble("A", out var a) || !TryParseFlexibleDouble("B", out var b))
        {
            ResultMessage = "Intervalo invalido. Usa valores como 0.5 o 0,5.";
            return false;
        }

        double ay = AY;
        double by = BY;
        double az = AZ;
        double bz = BZ;

        if (dimensions >= 2)
        {
            if (!TryParseFlexibleDouble("AY", out ay) || !TryParseFlexibleDouble("BY", out by))
            {
                ResultMessage = "Intervalo en y invalido. Usa valores como 0.5 o 0,5.";
                return false;
            }
        }

        if (dimensions == 3)
        {
            if (!TryParseFlexibleDouble("AZ", out az) || !TryParseFlexibleDouble("BZ", out bz))
            {
                ResultMessage = "Intervalo en z invalido. Usa valores como 0.5 o 0,5.";
                return false;
            }
        }

        if (!int.TryParse(Request.Form["SamplesCount"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var sampleCount) &&
            !int.TryParse(Request.Form["SamplesCount"], NumberStyles.Integer, CultureInfo.CurrentCulture, out sampleCount))
        {
            sampleCount = -1; // sentinel for missing
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

        if (!TryParseFlexibleDouble("ConfidenceLevel", out var confidenceLevel))
        {
            confidenceLevel = double.NaN;
        }

        if (!TryParseFlexibleDouble("ZCriticalInput", out var zCriticalInput))
        {
            zCriticalInput = double.NaN;
        }

        if (!TryParseFlexibleDouble("MaxError", out var maxError))
        {
            maxError = double.NaN;
        }

        Dimensions = dimensions;
        A = a;
        B = b;
        AY = ay;
        BY = by;
        AZ = az;
        BZ = bz;
        SamplesCount = sampleCount > 0 ? sampleCount : null;
        ConfidenceLevel = double.IsNaN(confidenceLevel) ? null : confidenceLevel;
        ZCriticalInput = double.IsNaN(zCriticalInput) ? null : zCriticalInput;
        MaxError = double.IsNaN(maxError) ? null : maxError;
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

        if (double.TryParse(raw, strictFloat, CultureInfo.InvariantCulture, out value) ||
            double.TryParse(raw, strictFloat, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        // Allow constant expressions like pi, 2*pi, pi/2, etc.
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
            ResultMessage = "Debe cumplirse ax < bx.";
            return false;
        }

        if (Dimensions is < 1 or > 3)
        {
            ResultMessage = "La dimension debe ser 1, 2 o 3.";
            return false;
        }

        if (Dimensions >= 2 && AY >= BY)
        {
            ResultMessage = "Debe cumplirse ay < by.";
            return false;
        }

        if (Dimensions == 3 && AZ >= BZ)
        {
            ResultMessage = "Debe cumplirse az < bz.";
            return false;
        }

        if (!SamplesCount.HasValue && !MaxError.HasValue)
        {
            ResultMessage = "Ingresa la cantidad de muestras o el error maximo permitido.";
            return false;
        }

        if (SamplesCount.HasValue && SamplesCount <= 1)
        {
            ResultMessage = "La cantidad de muestras debe ser mayor que 1.";
            return false;
        }

        if (!ConfidenceLevel.HasValue && !ZCriticalInput.HasValue)
        {
            ResultMessage = "Ingresa el nivel de confianza o el valor z critico.";
            return false;
        }

        if (ConfidenceLevel.HasValue && (ConfidenceLevel <= 0 || ConfidenceLevel >= 100))
        {
            ResultMessage = "El nivel de confianza debe estar entre 0 y 100 (exclusivo).";
            return false;
        }

        if (ZCriticalInput.HasValue && ZCriticalInput <= 0)
        {
            ResultMessage = "El valor z critico debe ser positivo.";
            return false;
        }

        if (MaxError.HasValue && MaxError <= 0)
        {
            ResultMessage = "El error maximo debe ser positivo.";
            return false;
        }

        return true;
    }
}
