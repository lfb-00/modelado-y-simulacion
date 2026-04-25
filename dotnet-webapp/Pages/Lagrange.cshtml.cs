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

    [BindProperty]
    public string Function { get; set; } = string.Empty;

    [BindProperty]
    public string Xi { get; set; } = string.Empty;

    [BindProperty]
    public string DerivativePoint { get; set; } = string.Empty;

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
    public List<double> OriginalFunctionChartX { get; set; } = new();
    public List<double> OriginalFunctionChartY { get; set; } = new();
    public bool HasOriginalFunction { get; set; }
    public string InterpolatingFunctionLatex { get; set; } = string.Empty;
    public string LagrangeGeneralFormulaLatex { get; set; } = string.Empty;
    public List<string> BasePolynomialsLatex { get; set; } = new();
    public string InterpolatingFunctionSimplifiedLatex { get; set; } = string.Empty;
    public string InterpolatingFunctionSimplifiedText { get; set; } = string.Empty;
    public double? GlobalError { get; set; }
    public bool HasResult { get; set; }

    // Error at ξ
    public double? XiValue { get; set; }
    public double? FunctionAtXi { get; set; }
    public double? PolynomialAtXi { get; set; }
    public double? LocalErrorAtXi { get; set; }
    public double? TheoreticalErrorBound { get; set; }
    public string ErrorAtXiLatex { get; set; } = string.Empty;
    public bool HasErrorAtXi { get; set; }

    // Finite differences
    public List<FiniteDiffEntry> FiniteDifferences { get; set; } = new();
    public bool HasFiniteDifferences { get; set; }

    // Derivative at point
    public double? DerivativePointValue { get; set; }
    public double? DerivativeResult { get; set; }
    public string DerivativeMethodUsed { get; set; } = string.Empty;
    public string DerivativeLatex { get; set; } = string.Empty;
    public bool HasDerivative { get; set; }

    private List<(double X, double Y)> _parsed = new();
    private double[] _monomialCoefficients = Array.Empty<double>();

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
            var values = _parsed.ToList();
            service.Compute(values);

            Nodes = service.Nodes;
            PointsChartX = service.PointsChartX;
            PointsChartY = service.PointsChartY;
            PolynomialChartX = service.PolynomialChartX;
            PolynomialChartY = service.PolynomialChartY;

            BuildOutputFunctionSeriesAndGlobalError();
            BuildLagrangeFormulaDetails();
            BuildInterpolatingLatex();
            BuildSimplifiedOutputFunction();
            BuildOutputOverviewSeries();
            BuildOriginalFunctionSeries();
            ComputeFiniteDifferences();
            ComputeErrorAtXi();
            ComputeDerivativeAtPoint();
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
                X = Points.Count.ToString(),
                Y = "0"
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

        if (!TryParsePoints())
            return false;

        return true;
    }

    private void BuildOutputFunctionSeriesAndGlobalError()
    {
        FunctionChartX.Clear();
        FunctionChartY.Clear();
        FunctionErrorChartX.Clear();
        FunctionErrorChartY.Clear();

        var ordered = _parsed.OrderBy(p => p.X).ToList();
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

    private static bool TryEvaluatePiecewiseFunction(List<(double X, double Y)> orderedPoints, double x, out double y)
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
        if (_parsed.Count == 0)
        {
            InterpolatingFunctionLatex = string.Empty;
            return;
        }

        string Format(double v) => v.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);

        var terms = new List<string>();
        for (int i = 0; i < _parsed.Count; i++)
        {
            var pi = _parsed[i];
            var factors = new List<string>();
            for (int j = 0; j < _parsed.Count; j++)
            {
                if (i == j) continue;

                var pj = _parsed[j];
                factors.Add($"\\frac{{x-({Format(pj.X)})}}{{({Format(pi.X)})-({Format(pj.X)})}}");
            }

            string factorProduct = factors.Count > 0 ? string.Join("", factors) : "1";
            terms.Add($"({Format(pi.Y)}){factorProduct}");
        }

        InterpolatingFunctionLatex = "P(x) = " + string.Join(" + ", terms);
    }

    private void BuildLagrangeFormulaDetails()
    {
        LagrangeGeneralFormulaLatex = "P(x)=\\sum_{i=0}^{n} y_i L_i(x),\\qquad L_i(x)=\\prod_{j=0,\\,j\\ne i}^{n}\\frac{x-x_j}{x_i-x_j}";
        BasePolynomialsLatex.Clear();

        if (_parsed.Count == 0)
        {
            return;
        }

        string Format(double v) => v.ToString("0.######", CultureInfo.InvariantCulture);

        for (int i = 0; i < _parsed.Count; i++)
        {
            var factors = new List<string>();
            for (int j = 0; j < _parsed.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                factors.Add($"\\frac{{x-({Format(_parsed[j].X)})}}{{({Format(_parsed[i].X)})-({Format(_parsed[j].X)})}}");
            }

            string factorProduct = factors.Count > 0 ? string.Join("", factors) : "1";
            BasePolynomialsLatex.Add($"L_{{{i}}}(x) = {factorProduct}");
        }
    }

    private void BuildSimplifiedOutputFunction()
    {
        _monomialCoefficients = ComputeMonomialCoefficients(_parsed);
        InterpolatingFunctionSimplifiedText = "y = " + BuildPolynomialPlainText(_monomialCoefficients);
        InterpolatingFunctionSimplifiedLatex = "P(x) = " + BuildPolynomialLatex(_monomialCoefficients);
    }

    private void BuildOutputOverviewSeries()
    {
        OutputOverviewChartX.Clear();
        OutputOverviewChartY.Clear();

        double[] coefficients = ComputeMonomialCoefficients(_parsed);
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

    private void BuildOriginalFunctionSeries()
    {
        OriginalFunctionChartX.Clear();
        OriginalFunctionChartY.Clear();
        HasOriginalFunction = false;

        if (string.IsNullOrWhiteSpace(Function) || _parsed.Count < 2)
            return;

        FunctionParser fParser;
        try { fParser = new FunctionParser(Function); }
        catch { return; }

        var ordered = _parsed.OrderBy(p => p.X).ToList();
        double minX = ordered[0].X;
        double maxX = ordered[^1].X;
        double range = maxX - minX;
        double plotMin = minX - range * 0.1;
        double plotMax = maxX + range * 0.1;

        const int points = 401;
        for (int i = 0; i < points; i++)
        {
            double x = plotMin + (plotMax - plotMin) * i / (points - 1);
            try
            {
                double y = fParser.Evaluate(x);
                if (!double.IsFinite(y)) continue;
                OriginalFunctionChartX.Add(x);
                OriginalFunctionChartY.Add(y);
            }
            catch { }
        }

        HasOriginalFunction = OriginalFunctionChartX.Count > 0;
    }

    private static double[] ComputeMonomialCoefficients(IReadOnlyList<(double X, double Y)> points)
    {
        int n = points.Count;
        var system = new double[n, n + 1];

        for (int i = 0; i < n; i++)
        {
            double xPower = 1.0;
            for (int j = 0; j < n; j++)
            {
                system[i, j] = xPower;
                xPower *= points[i].X;
            }

            system[i, n] = points[i].Y;
        }

        // Eliminacion gaussiana con pivoteo parcial para mejorar estabilidad numerica.
        for (int col = 0; col < n; col++)
        {
            int pivotRow = col;
            double pivotAbs = Math.Abs(system[col, col]);
            for (int row = col + 1; row < n; row++)
            {
                double candidate = Math.Abs(system[row, col]);
                if (candidate > pivotAbs)
                {
                    pivotAbs = candidate;
                    pivotRow = row;
                }
            }

            if (pivotAbs < 1e-14)
            {
                throw new ArgumentException("No se pudo construir la forma simplificada (matriz singular o mal condicionada).");
            }

            if (pivotRow != col)
            {
                for (int k = 0; k <= n; k++)
                {
                    (system[col, k], system[pivotRow, k]) = (system[pivotRow, k], system[col, k]);
                }
            }

            for (int row = col + 1; row < n; row++)
            {
                double factor = system[row, col] / system[col, col];
                if (Math.Abs(factor) < 1e-18)
                {
                    continue;
                }

                for (int k = col; k <= n; k++)
                {
                    system[row, k] -= factor * system[col, k];
                }
            }
        }

        var coefficients = new double[n];
        for (int row = n - 1; row >= 0; row--)
        {
            double rhs = system[row, n];
            for (int col = row + 1; col < n; col++)
            {
                rhs -= system[row, col] * coefficients[col];
            }

            coefficients[row] = rhs / system[row, row];
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
        public string X { get; set; } = "0";
        public string Y { get; set; } = "0";
    }

    public sealed class FiniteDiffEntry
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double? Forward { get; set; }
        public double? Backward { get; set; }
        public double? Central { get; set; }
    }

    private void ComputeErrorAtXi()
    {
        HasErrorAtXi = false;
        if (string.IsNullOrWhiteSpace(Xi) || string.IsNullOrWhiteSpace(Function))
            return;

        try
        {
            double xi = new FunctionParser(Xi).Evaluate(0);
            if (!double.IsFinite(xi)) return;

            XiValue = xi;

            var fParser = new FunctionParser(Function);
            double fXi = fParser.Evaluate(xi);
            FunctionAtXi = fXi;

            double pXi = EvaluateMonomialPolynomial(_monomialCoefficients, xi);
            PolynomialAtXi = pXi;

            LocalErrorAtXi = Math.Abs(fXi - pXi);

            // Theoretical error bound: |f^(n+1)(ξ)/(n+1)! · ∏(ξ - xᵢ)|
            int n = _parsed.Count - 1; // degree
            int derivOrder = n + 1;

            double productTerm = 1.0;
            for (int i = 0; i < _parsed.Count; i++)
                productTerm *= (xi - _parsed[i].X);

            double fnPlus1 = NumericalDerivative(Function, xi, derivOrder);

            double factorial = 1.0;
            for (int k = 2; k <= derivOrder; k++)
                factorial *= k;

            TheoreticalErrorBound = Math.Abs(fnPlus1 / factorial * productTerm);

            string fmt(double v) => v.ToString("0.########", CultureInfo.InvariantCulture);
            ErrorAtXiLatex = $"|f(\\xi) - P(\\xi)| = |{fmt(fXi)} - {fmt(pXi)}| = {fmt(LocalErrorAtXi.Value)}";
            ErrorAtXiLatex += $", \\qquad \\text{{Cota}} = \\frac{{|f^{{({derivOrder})}}(\\xi)|}}{{({derivOrder})!}} \\prod_{{i=0}}^{{{n}}} |\\xi - x_i| = {TheoreticalErrorBound?.ToString("E6", CultureInfo.InvariantCulture)}";

            HasErrorAtXi = true;
        }
        catch { }
    }

    private void ComputeFiniteDifferences()
    {
        HasFiniteDifferences = false;
        FiniteDifferences.Clear();

        if (_parsed.Count < 2) return;

        var ordered = _parsed.OrderBy(p => p.X).ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            var entry = new FiniteDiffEntry { X = ordered[i].X, Y = ordered[i].Y };

            if (i < ordered.Count - 1)
            {
                double h = ordered[i + 1].X - ordered[i].X;
                if (Math.Abs(h) > 1e-14)
                    entry.Forward = (ordered[i + 1].Y - ordered[i].Y) / h;
            }

            if (i > 0)
            {
                double h = ordered[i].X - ordered[i - 1].X;
                if (Math.Abs(h) > 1e-14)
                    entry.Backward = (ordered[i].Y - ordered[i - 1].Y) / h;
            }

            if (i > 0 && i < ordered.Count - 1)
            {
                double h = ordered[i + 1].X - ordered[i - 1].X;
                if (Math.Abs(h) > 1e-14)
                    entry.Central = (ordered[i + 1].Y - ordered[i - 1].Y) / h;
            }

            FiniteDifferences.Add(entry);
        }

        HasFiniteDifferences = true;
    }

    private void ComputeDerivativeAtPoint()
    {
        HasDerivative = false;
        if (string.IsNullOrWhiteSpace(DerivativePoint)) return;

        try
        {
            double xd = new FunctionParser(DerivativePoint).Evaluate(0);
            if (!double.IsFinite(xd)) return;

            DerivativePointValue = xd;

            // Derivative of the monomial polynomial P'(x)
            double result = 0;
            for (int k = 1; k < _monomialCoefficients.Length; k++)
            {
                result += k * _monomialCoefficients[k] * Math.Pow(xd, k - 1);
            }
            DerivativeResult = result;
            DerivativeMethodUsed = "Derivada analítica del polinomio interpolante";

            // Also try central differences on the tabulated data
            var ordered = _parsed.OrderBy(p => p.X).ToList();
            double? centralDiff = null;
            for (int i = 1; i < ordered.Count - 1; i++)
            {
                double midPrev = (ordered[i - 1].X + ordered[i].X) / 2;
                double midNext = (ordered[i].X + ordered[i + 1].X) / 2;
                if (xd >= midPrev && xd <= midNext)
                {
                    double h2 = ordered[i + 1].X - ordered[i - 1].X;
                    if (Math.Abs(h2) > 1e-14)
                        centralDiff = (ordered[i + 1].Y - ordered[i - 1].Y) / h2;
                    break;
                }
            }

            string fmt(double v) => v.ToString("0.########", CultureInfo.InvariantCulture);

            DerivativeLatex = $"P'({fmt(xd)}) = {fmt(result)}";
            if (centralDiff.HasValue)
            {
                DerivativeLatex += $", \\qquad \\text{{Dif. central (nodos vecinos)}} = {fmt(centralDiff.Value)}";
                DerivativeMethodUsed += $" y diferencia central en nodos vecinos";
            }

            HasDerivative = true;
        }
        catch { }
    }

    private static double NumericalDerivative(string function, double x, int order)
    {
        const double h = 1e-3;
        var parser = new FunctionParser(function);

        if (order == 1)
            return (parser.Evaluate(x + h) - parser.Evaluate(x - h)) / (2 * h);

        if (order == 2)
            return (parser.Evaluate(x + h) - 2 * parser.Evaluate(x) + parser.Evaluate(x - h)) / (h * h);

        if (order == 3)
            return (parser.Evaluate(x + 2 * h) - 2 * parser.Evaluate(x + h) + 2 * parser.Evaluate(x - h) - parser.Evaluate(x - 2 * h)) / (2 * h * h * h);

        if (order == 4)
            return (parser.Evaluate(x + 2 * h) - 4 * parser.Evaluate(x + h) + 6 * parser.Evaluate(x) - 4 * parser.Evaluate(x - h) + parser.Evaluate(x - 2 * h)) / (h * h * h * h);

        // For higher orders, use recursive central differences
        double prev = NumericalDerivative(function, x, order - 1);
        double prevPlus = NumericalDerivative(function, x + h, order - 1);
        double prevMinus = NumericalDerivative(function, x - h, order - 1);
        return (prevPlus - prevMinus) / (2 * h);
    }

    private bool TryParsePoints()
    {
        _parsed.Clear();
        FunctionParser? fParser = null;
        if (!string.IsNullOrWhiteSpace(Function))
        {
            try { fParser = new FunctionParser(Function); }
            catch { fParser = null; }
        }

        for (int i = 0; i < Points.Count; i++)
        {
            var raw = Points[i];
            string xExpr = string.IsNullOrWhiteSpace(raw.X) ? "0" : raw.X.Trim();
            bool yIsBlank = string.IsNullOrWhiteSpace(raw.Y);
            try
            {
                double x = new FunctionParser(xExpr).Evaluate(0);
                if (!double.IsFinite(x))
                {
                    ResultMessage = $"Punto #{i + 1}: el valor de X evaluado no es finito.";
                    return false;
                }

                double y;
                if (yIsBlank && fParser != null)
                {
                    y = fParser.Evaluate(x);
                    // Write back so it displays in the form
                    Points[i].Y = y.ToString("0.##########", CultureInfo.InvariantCulture);
                }
                else
                {
                    string yExpr = yIsBlank ? "0" : raw.Y.Trim();
                    y = new FunctionParser(yExpr).Evaluate(0);
                }

                if (!double.IsFinite(y))
                {
                    ResultMessage = $"Punto #{i + 1}: el valor de Y evaluado no es finito.";
                    return false;
                }
                _parsed.Add((x, y));
            }
            catch (Exception ex)
            {
                ResultMessage = $"Punto #{i + 1}: expresión inválida — {ex.Message}";
                return false;
            }
        }
        return true;
    }
}
