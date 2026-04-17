using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dotnet_webapp.Services;
using System.Globalization;

namespace dotnet_webapp.Pages;

public class LagrangeModel : PageModel
{
    private readonly ILogger<LagrangeModel> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public LagrangeModel(ILogger<LagrangeModel> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    [BindProperty]
    public List<InputPoint> Points { get; set; } = new();

    public string ResultMessage { get; set; } = string.Empty;
    public List<(double X, double Y, double PX, double LocalError)> Nodes { get; set; } = new();
    public List<double> PointsChartX { get; set; } = new();
    public List<double> PointsChartY { get; set; } = new();
    public List<double> FunctionChartX { get; set; } = new();
    public List<double> FunctionChartY { get; set; } = new();
    public List<double> FunctionErrorChartX { get; set; } = new();
    public List<double> FunctionErrorChartY { get; set; } = new();
    public List<double> PolynomialChartX { get; set; } = new();
    public List<double> PolynomialChartY { get; set; } = new();
    public List<double> OutputOverviewChartX { get; set; } = new();
    public List<double> OutputOverviewChartY { get; set; } = new();
    public string InterpolatingFunctionLatex { get; set; } = string.Empty;
    public string InterpolatingFunctionSimplifiedLatex { get; set; } = string.Empty;
    public string InterpolatingFunctionSimplifiedText { get; set; } = string.Empty;
    public double? GlobalError { get; set; }
    public bool HasResult { get; set; }

    public void OnGet()
    {
        EnsureMinimumPoints();
    }

    public void OnPost()
    {
        ResultMessage = string.Empty;
        HasResult = false;

        EnsureMinimumPoints();

        if (!ValidateInputs())
            return;

        try
        {
            var service = new LagrangeService(_loggerFactory);
            var values = Points.Select(p => (p.X, p.Y)).ToList();
            service.Compute(values);

            Nodes = service.Nodes;
            PointsChartX = service.PointsChartX;
            PointsChartY = service.PointsChartY;
            PolynomialChartX = service.PolynomialChartX;
            PolynomialChartY = service.PolynomialChartY;

            BuildOutputFunctionSeriesAndGlobalError();
            BuildInterpolatingLatex();
            BuildSimplifiedOutputFunction();
            BuildOutputOverviewSeries();
            HasResult = true;

            ResultMessage = $"Polinomio interpolante de grado {Points.Count - 1} construido con {Points.Count} puntos.";
            _logger.LogInformation("Lagrange completado con {N} puntos, error global(local)={E:E4}", Points.Count, GlobalError);
        }
        catch (ArgumentException ex)
        {
            ResultMessage = "Error: " + ex.Message;
        }
        catch (Exception ex)
        {
            ResultMessage = "Error al calcular: " + ex.Message;
            _logger.LogError(ex, "Error en interpolación de Lagrange");
        }
    }

    private void EnsureMinimumPoints()
    {
        if (Points.Count >= 3)
        {
            return;
        }

        while (Points.Count < 3)
        {
            Points.Add(new InputPoint
            {
                X = Points.Count,
                Y = 0
            });
        }
    }

    private bool ValidateInputs()
    {
        if (Points.Count < 3)
        {
            ResultMessage = "Ingresá al menos 3 puntos para construir el polinomio.";
            return false;
        }

        if (Points.Count > 30)
        {
            ResultMessage = "Se permiten hasta 30 puntos.";
            return false;
        }

        if (Points.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y)))
        {
            ResultMessage = "Todos los valores de puntos deben ser numéricos válidos.";
            return false;
        }

        return true;
    }

    private void BuildOutputFunctionSeriesAndGlobalError()
    {
        FunctionChartX.Clear();
        FunctionChartY.Clear();
        FunctionErrorChartX.Clear();
        FunctionErrorChartY.Clear();

        var ordered = Points.OrderBy(p => p.X).ToList();
        foreach (var p in ordered)
        {
            FunctionChartX.Add(p.X);
            FunctionChartY.Add(p.Y);
        }

        double maxError = 0;

        int points = Math.Min(PolynomialChartX.Count, PolynomialChartY.Count);
        for (int i = 0; i < points; i++)
        {
            double x = PolynomialChartX[i];
            double p = PolynomialChartY[i];

            if (!TryEvaluatePiecewiseFunction(ordered, x, out double f))
            {
                continue;
            }

            if (!double.IsFinite(f) || !double.IsFinite(p))
            {
                continue;
            }

            FunctionChartX.Add(x);
            FunctionChartY.Add(f);

            double err = Math.Abs(f - p);
            FunctionErrorChartX.Add(x);
            FunctionErrorChartY.Add(err);
            if (err > maxError)
            {
                maxError = err;
            }
        }

        GlobalError = maxError;
    }

    private static bool TryEvaluatePiecewiseFunction(List<InputPoint> orderedPoints, double x, out double y)
    {
        y = 0;
        if (orderedPoints.Count < 2)
        {
            return false;
        }

        if (x < orderedPoints[0].X || x > orderedPoints[^1].X)
        {
            return false;
        }

        for (int i = 0; i < orderedPoints.Count - 1; i++)
        {
            double x0 = orderedPoints[i].X;
            double x1 = orderedPoints[i + 1].X;
            if (x < x0 || x > x1)
            {
                continue;
            }

            double y0 = orderedPoints[i].Y;
            double y1 = orderedPoints[i + 1].Y;
            double t = Math.Abs(x1 - x0) < 1e-12 ? 0 : (x - x0) / (x1 - x0);
            y = y0 + t * (y1 - y0);
            return true;
        }

        return false;
    }

    private void BuildInterpolatingLatex()
    {
        if (Points.Count == 0)
        {
            InterpolatingFunctionLatex = string.Empty;
            return;
        }

        string Format(double v) => v.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);

        var terms = new List<string>();
        for (int i = 0; i < Points.Count; i++)
        {
            var pi = Points[i];
            var factors = new List<string>();
            for (int j = 0; j < Points.Count; j++)
            {
                if (i == j) continue;

                var pj = Points[j];
                factors.Add($"\\frac{{x-({Format(pj.X)})}}{{({Format(pi.X)})-({Format(pj.X)})}}");
            }

            string factorProduct = factors.Count > 0 ? string.Join("", factors) : "1";
            terms.Add($"({Format(pi.Y)}){factorProduct}");
        }

        InterpolatingFunctionLatex = "P(x) = " + string.Join(" + ", terms);
    }

    private void BuildSimplifiedOutputFunction()
    {
        double[] coefficients = ComputeMonomialCoefficients(Points);
        InterpolatingFunctionSimplifiedText = "y = " + BuildPolynomialPlainText(coefficients);
        InterpolatingFunctionSimplifiedLatex = "P(x) = " + BuildPolynomialLatex(coefficients);
    }

    private void BuildOutputOverviewSeries()
    {
        OutputOverviewChartX.Clear();
        OutputOverviewChartY.Clear();

        double[] coefficients = ComputeMonomialCoefficients(Points);
        const int points = 401;
        const double minX = -20;
        const double maxX = 20;

        for (int i = 0; i < points; i++)
        {
            double x = minX + (maxX - minX) * i / (points - 1);
            double y = EvaluateMonomialPolynomial(coefficients, x);

            if (!double.IsFinite(y))
            {
                continue;
            }

            OutputOverviewChartX.Add(x);
            OutputOverviewChartY.Add(y);
        }
    }

    private static double[] ComputeMonomialCoefficients(IReadOnlyList<InputPoint> points)
    {
        int n = points.Count;
        var coefficients = new double[n];

        for (int i = 0; i < n; i++)
        {
            double[] basis = [1.0];
            double denominator = 1.0;

            for (int j = 0; j < n; j++)
            {
                if (i == j)
                {
                    continue;
                }

                denominator *= (points[i].X - points[j].X);
                basis = MultiplyByLinearFactor(basis, -points[j].X, 1.0);
            }

            double scale = points[i].Y / denominator;
            for (int k = 0; k < basis.Length; k++)
            {
                coefficients[k] += scale * basis[k];
            }
        }

        for (int i = 0; i < coefficients.Length; i++)
        {
            if (Math.Abs(coefficients[i]) < 1e-10)
            {
                coefficients[i] = 0;
            }
        }

        return coefficients;
    }

    private static double[] MultiplyByLinearFactor(double[] poly, double constantTerm, double xTerm)
    {
        var result = new double[poly.Length + 1];
        for (int i = 0; i < poly.Length; i++)
        {
            result[i] += poly[i] * constantTerm;
            result[i + 1] += poly[i] * xTerm;
        }

        return result;
    }

    private static double EvaluateMonomialPolynomial(double[] coefficients, double x)
    {
        double value = 0;
        for (int i = coefficients.Length - 1; i >= 0; i--)
        {
            value = value * x + coefficients[i];
        }

        return value;
    }

    private static string BuildPolynomialPlainText(double[] coefficients)
    {
        var terms = new List<string>();

        for (int power = coefficients.Length - 1; power >= 0; power--)
        {
            double c = coefficients[power];
            if (Math.Abs(c) < 1e-10)
            {
                continue;
            }

            string absCoeff = Math.Abs(c).ToString("0.######", CultureInfo.InvariantCulture);
            string sign = c < 0 ? "-" : "+";

            string termBody = power switch
            {
                0 => absCoeff,
                1 when Math.Abs(Math.Abs(c) - 1) < 1e-10 => "x",
                1 => absCoeff + "*x",
                _ when Math.Abs(Math.Abs(c) - 1) < 1e-10 => $"x**{power}",
                _ => $"{absCoeff}*x**{power}"
            };

            terms.Add((terms.Count == 0 && sign == "+") ? termBody : $" {sign} {termBody}");
        }

        return terms.Count == 0 ? "0" : string.Concat(terms);
    }

    private static string BuildPolynomialLatex(double[] coefficients)
    {
        var terms = new List<string>();

        for (int power = coefficients.Length - 1; power >= 0; power--)
        {
            double c = coefficients[power];
            if (Math.Abs(c) < 1e-10)
            {
                continue;
            }

            string absCoeff = Math.Abs(c).ToString("0.######", CultureInfo.InvariantCulture);
            string sign = c < 0 ? "-" : "+";

            string termBody = power switch
            {
                0 => absCoeff,
                1 when Math.Abs(Math.Abs(c) - 1) < 1e-10 => "x",
                1 => absCoeff + "x",
                _ when Math.Abs(Math.Abs(c) - 1) < 1e-10 => $"x^{{{power}}}",
                _ => $"{absCoeff}x^{{{power}}}"
            };

            terms.Add((terms.Count == 0 && sign == "+") ? termBody : $" {sign} {termBody}");
        }

        return terms.Count == 0 ? "0" : string.Concat(terms);
    }

    public sealed class InputPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
