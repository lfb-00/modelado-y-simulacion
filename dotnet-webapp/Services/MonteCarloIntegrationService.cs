using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

public class MonteCarloIntegrationService
{
    private readonly ILogger<MonteCarloIntegrationService> _logger;

    public double Approximation { get; private set; }
    public double MeanValue { get; private set; }
    public double StandardDeviation { get; private set; }
    public double StandardError { get; private set; }
    public string FunctionLatex { get; private set; } = string.Empty;
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

    public void Compute(string function, double a, double b, int sampleCount, int? seed)
    {
        if (string.IsNullOrWhiteSpace(function))
        {
            throw new ArgumentException("Ingresa una funcion valida.");
        }

        if (!double.IsFinite(a) || !double.IsFinite(b))
        {
            throw new ArgumentException("Los extremos del intervalo deben ser finitos.");
        }

        if (a >= b)
        {
            throw new ArgumentException("Debe cumplirse a < b.");
        }

        if (sampleCount <= 1)
        {
            throw new ArgumentException("La cantidad de muestras debe ser mayor que 1.");
        }

        var parser = new FunctionParser(function);
        EffectiveSeed = seed ?? Random.Shared.Next(int.MinValue, int.MaxValue);
        var random = new Random(EffectiveSeed);

        BuildChartSeries(parser, a, b);
        double minCurveY = ChartYValues.Count > 0 ? ChartYValues.Min() : 0;
        double maxCurveY = ChartYValues.Count > 0 ? ChartYValues.Max() : 0;
        double boxYMin = Math.Min(minCurveY, 0);
        double boxYMax = Math.Max(maxCurveY, 0);
        if (Math.Abs(boxYMax - boxYMin) < 1e-12)
        {
            boxYMin -= 1;
            boxYMax += 1;
        }

        double intervalLength = b - a;
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
            double x = a + random.NextDouble() * intervalLength;
            double fx = parser.Evaluate(x);
            if (!double.IsFinite(fx))
            {
                throw new ArgumentException($"La funcion no pudo evaluarse correctamente en x = {x:0.######}.");
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
                        FX = fx,
                        PartialMean = partialMean,
                        PartialIntegralEstimate = intervalLength * partialMean
                    });
                }

                if (i % scatterStride == 0)
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
        Approximation = intervalLength * MeanValue;

        double variance = (sumSquares - sampleCount * MeanValue * MeanValue) / (sampleCount - 1);
        variance = Math.Max(variance, 0);

        StandardDeviation = Math.Sqrt(variance);
        StandardError = intervalLength * Math.Sqrt(variance / sampleCount);
        double margin95 = 1.96 * StandardError;
        Confidence95Lower = Approximation - margin95;
        Confidence95Upper = Approximation + margin95;

        FunctionLatex = ToLatex(function);

        _logger.LogInformation(
            "Monte Carlo completado: intervalo [{A}, {B}], muestras={N}, semilla={Seed}, aproximacion={Approximation:E6}, error_est={StandardError:E6}",
            a,
            b,
            sampleCount,
            EffectiveSeed,
            Approximation,
            StandardError);
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
