using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

public class MonteCarloIntegrationService
{
    private readonly ILogger<MonteCarloIntegrationService> _logger;
    public int Dimensions { get; private set; }
    public double DomainVolume { get; private set; }

    public double Approximation { get; private set; }
    public double MeanValue { get; private set; }
    public double StandardDeviation { get; private set; }
    public double StandardError { get; private set; }
    public string FunctionLatex { get; private set; } = string.Empty;
    public double ZCritical95 { get; private set; }
    public double Confidence95Lower { get; private set; }
    public double Confidence95Upper { get; private set; }
    public int EffectiveSeed { get; private set; }

    public List<double> ChartXValues { get; private set; } = new();
    public List<double> ChartYValues { get; private set; } = new();
    public List<double> InsideAreaXValues { get; private set; } = new();
    public List<double> InsideAreaYValues { get; private set; } = new();
    public List<double> OutsideAreaXValues { get; private set; } = new();
    public List<double> OutsideAreaYValues { get; private set; } = new();
    public List<MonteCarloSampleEntry> Samples { get; private set; } = new();

    public MonteCarloIntegrationService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MonteCarloIntegrationService>();
    }

    public void Compute(
        string function,
        int dimensions,
        double ax,
        double bx,
        double? ay,
        double? by,
        double? az,
        double? bz,
        int sampleCount,
        int? seed)
    {
        if (string.IsNullOrWhiteSpace(function))
        {
            throw new ArgumentException("Ingresa una funcion valida.");
        }

        if (dimensions is < 1 or > 3)
        {
            throw new ArgumentException("La dimension debe ser 1, 2 o 3.");
        }

        if (!double.IsFinite(ax) || !double.IsFinite(bx))
        {
            throw new ArgumentException("Los extremos del intervalo deben ser finitos.");
        }

        if (ax >= bx)
        {
            throw new ArgumentException("Debe cumplirse ax < bx.");
        }

        if (dimensions >= 2)
        {
            if (!ay.HasValue || !by.HasValue || !double.IsFinite(ay.Value) || !double.IsFinite(by.Value))
            {
                throw new ArgumentException("Para integrales dobles y triples, ay y by deben ser finitos.");
            }

            if (ay.Value >= by.Value)
            {
                throw new ArgumentException("Debe cumplirse ay < by.");
            }
        }

        if (dimensions == 3)
        {
            if (!az.HasValue || !bz.HasValue || !double.IsFinite(az.Value) || !double.IsFinite(bz.Value))
            {
                throw new ArgumentException("Para integrales triples, az y bz deben ser finitos.");
            }

            if (az.Value >= bz.Value)
            {
                throw new ArgumentException("Debe cumplirse az < bz.");
            }
        }

        if (sampleCount <= 1)
        {
            throw new ArgumentException("La cantidad de muestras debe ser mayor que 1.");
        }

        var parser = new FunctionParser(function);
        Dimensions = dimensions;
        EffectiveSeed = seed ?? Random.Shared.Next(int.MinValue, int.MaxValue);
        var random = new Random(EffectiveSeed);

        if (dimensions == 1)
        {
            BuildChartSeries(parser, ax, bx);
        }
        else
        {
            ChartXValues = new List<double>();
            ChartYValues = new List<double>();
        }

        double minCurveY = ChartYValues.Count > 0 ? ChartYValues.Min() : 0;
        double maxCurveY = ChartYValues.Count > 0 ? ChartYValues.Max() : 0;
        double boxYMin = Math.Min(minCurveY, 0);
        double boxYMax = Math.Max(maxCurveY, 0);
        if (Math.Abs(boxYMax - boxYMin) < 1e-12)
        {
            boxYMin -= 1;
            boxYMax += 1;
        }

        double intervalLengthX = bx - ax;
        double intervalLengthY = dimensions >= 2 ? by!.Value - ay!.Value : 1;
        double intervalLengthZ = dimensions == 3 ? bz!.Value - az!.Value : 1;
        DomainVolume = intervalLengthX * intervalLengthY * intervalLengthZ;
        double sum = 0;
        double sumSquares = 0;

        const int maxTableRows = 200;
        const int maxScatterPoints = 1500;
        int scatterStride = Math.Max(1, sampleCount / maxScatterPoints);

        Samples = new List<MonteCarloSampleEntry>(Math.Min(sampleCount, maxTableRows));
        InsideAreaXValues = new List<double>(Math.Min(sampleCount, maxScatterPoints));
        InsideAreaYValues = new List<double>(Math.Min(sampleCount, maxScatterPoints));
        OutsideAreaXValues = new List<double>(Math.Min(sampleCount, maxScatterPoints));
        OutsideAreaYValues = new List<double>(Math.Min(sampleCount, maxScatterPoints));

        for (int i = 1; i <= sampleCount; i++)
        {
            double x = ax + random.NextDouble() * intervalLengthX;
            double y = dimensions >= 2 ? ay!.Value + random.NextDouble() * intervalLengthY : 0;
            double z = dimensions == 3 ? az!.Value + random.NextDouble() * intervalLengthZ : 0;

            double fx = parser.Evaluate(x, y, z);
            if (!double.IsFinite(fx))
            {
                throw new ArgumentException($"La funcion no pudo evaluarse correctamente en el punto muestreado.");
            }

            sum += fx;
            sumSquares += fx * fx;

            if (i <= maxTableRows)
            {
                double partialMean = sum / i;
                Samples.Add(new MonteCarloSampleEntry
                {
                    Index = i,
                    X = x,
                    Y = dimensions >= 2 ? y : null,
                    Z = dimensions == 3 ? z : null,
                    FX = fx,
                    PartialMean = partialMean,
                    PartialIntegralEstimate = DomainVolume * partialMean
                });
            }

            if (dimensions == 1 && i % scatterStride == 0)
            {
                double yProbe = boxYMin + random.NextDouble() * (boxYMax - boxYMin);
                bool isInsideArea = (fx >= 0 && yProbe >= 0 && yProbe <= fx) ||
                                    (fx < 0 && yProbe <= 0 && yProbe >= fx);

                if (isInsideArea)
                {
                    InsideAreaXValues.Add(x);
                    InsideAreaYValues.Add(yProbe);
                }
                else
                {
                    OutsideAreaXValues.Add(x);
                    OutsideAreaYValues.Add(yProbe);
                }
            }
        }

        MeanValue = sum / sampleCount;
        Approximation = DomainVolume * MeanValue;

        double variance = (sumSquares - sampleCount * MeanValue * MeanValue) / (sampleCount - 1);
        variance = Math.Max(variance, 0);

        StandardDeviation = Math.Sqrt(variance);
        StandardError = DomainVolume * Math.Sqrt(variance / sampleCount);
        ZCritical95 = InverseNormalCdf(0.975);
        double margin95 = ZCritical95 * StandardError;
        Confidence95Lower = Approximation - margin95;
        Confidence95Upper = Approximation + margin95;

        FunctionLatex = ToLatex(function);

        _logger.LogInformation(
            "Monte Carlo completado: dim={Dimensions}, dominio volumen={Volume:E6}, muestras={N}, semilla={Seed}, aproximacion={Approximation:E6}, error_est={StandardError:E6}",
            dimensions,
            DomainVolume,
            sampleCount,
            EffectiveSeed,
            Approximation,
            StandardError);
    }

    // Rational approximation of the inverse standard normal CDF (Beasley-Springer-Moro).
    // Accurate to ~1e-9 for p in (0, 1).
    private static double InverseNormalCdf(double p)
    {
        if (p <= 0 || p >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(p), "p must be in (0, 1).");
        }

        // Coefficients for the rational approximation
        double[] a = [ -3.969683028665376e+01,  2.209460984245205e+02,
                        -2.759285104469687e+02,  1.383577518672690e+02,
                        -3.066479806614716e+01,  2.506628277459239e+00 ];
        double[] b = [ -5.447609879822406e+01,  1.615858368580409e+02,
                        -1.556989798598866e+02,  6.680131188771972e+01,
                        -1.328068155288572e+01 ];
        double[] c = [ -7.784894002430293e-03, -3.223964580411365e-01,
                        -2.400758277161838e+00, -2.549732539343734e+00,
                         4.374664141464968e+00,  2.938163982698783e+00 ];
        double[] d = [  7.784695709041462e-03,  3.224671290700398e-01,
                         2.445134137142996e+00,  3.754408661907416e+00 ];

        const double pLow  = 0.02425;
        const double pHigh = 0.97575;

        double q, r, x;

        if (p < pLow)
        {
            q = Math.Sqrt(-2.0 * Math.Log(p));
            x = (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1.0);
        }
        else if (p <= pHigh)
        {
            q = p - 0.5;
            r = q * q;
            x = (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q /
                (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1.0);
        }
        else
        {
            q = Math.Sqrt(-2.0 * Math.Log(1.0 - p));
            x = -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                 ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1.0);
        }

        return x;
    }

    private static string ToLatex(string expr)
    {
        string s = expr.Trim()
            .Replace("**", "^")
            .Replace("arcsin", "\\arcsin")
            .Replace("arccos", "\\arccos")
            .Replace("arctan", "\\arctan")
            .Replace("sinh", "\\sinh")
            .Replace("cosh", "\\cosh")
            .Replace("tanh", "\\tanh")
            .Replace("sin", "\\sin")
            .Replace("cos", "\\cos")
            .Replace("tan", "\\tan")
            .Replace("exp", "\\exp")
            .Replace("log", "\\log")
            .Replace("ln", "\\ln")
            .Replace("sqrt", "\\sqrt")
            .Replace("pi", "\\pi")
            .Replace("*", "\\cdot ");

        // Wrap exponent tokens: a^b  ->  a^{b}  (handles single char or parenthesised block)
        s = System.Text.RegularExpressions.Regex.Replace(
            s,
            @"\^([^{(\s])|(\()(.+?)(\))",
            m => m.Groups[1].Success ? $"^{{{m.Groups[1].Value}}}" : m.Value);
        s = System.Text.RegularExpressions.Regex.Replace(
            s,
            @"\^(\([^)]+\))",
            m => $"^{{{m.Groups[1].Value}}}");

        return $"f(x) = {s}";
    }

    private void BuildChartSeries(FunctionParser parser, double a, double b)
    {
        ChartXValues = new List<double>();
        ChartYValues = new List<double>();

        const int chartPoints = 401;
        for (int i = 0; i < chartPoints; i++)
        {
            double x = a + (b - a) * i / (chartPoints - 1);
            double y;

            try
            {
                y = parser.Evaluate(x);
            }
            catch
            {
                continue;
            }

            if (!double.IsFinite(y))
            {
                continue;
            }

            ChartXValues.Add(x);
            ChartYValues.Add(y);
        }
    }
}
