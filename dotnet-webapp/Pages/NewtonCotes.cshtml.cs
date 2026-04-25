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

    [BindProperty]
    public string Xi { get; set; } = string.Empty;

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

    // Truncation error at ξ
    public double? XiValue { get; set; }
    public double? TruncationErrorValue { get; set; }
    public string TruncationErrorDetailLatex { get; set; } = string.Empty;
    public bool HasTruncationError { get; set; }

    // Input function rendered as LaTeX
    public string FunctionInputLatex { get; set; } = string.Empty;
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

            FunctionInputLatex = ConvertFunctionToLatex(Function);
            ComputeTruncationError();

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

    private void ComputeTruncationError()
    {
        HasTruncationError = false;
        if (string.IsNullOrWhiteSpace(Xi)) return;

        try
        {
            double xi;
            if (!double.TryParse(Xi, NumberStyles.Float, CultureInfo.InvariantCulture, out xi) &&
                !double.TryParse(Xi, NumberStyles.Float, CultureInfo.CurrentCulture, out xi))
            {
                xi = new FunctionParser(Xi).Evaluate(0);
            }
            if (!double.IsFinite(xi)) return;

            XiValue = xi;
            double h = StepSize;
            var parser = new FunctionParser(Function);

            string fmt(double v) => v.ToString("0.########", CultureInfo.InvariantCulture);
            string fmtE(double v) => v.ToString("E6", CultureInfo.InvariantCulture);

            if (SelectedRule == "trapezoidal")
            {
                // E = -((b-a)/12) * h^2 * f''(ξ)
                double fpp = NumericalDerivative(parser, xi, 2);
                double errorFactor = -(B - A) / 12.0 * h * h;
                TruncationErrorValue = errorFactor * fpp;

                TruncationErrorDetailLatex = $"E_T = -\\frac{{(b-a)}}{{12}}\\,h^2\\,f''(\\xi)" +
                    $" = -\\frac{{{fmt(B - A)}}}{{12}} \\cdot {fmt(h)}^2 \\cdot ({fmt(fpp)})" +
                    $" = {fmtE(TruncationErrorValue.Value)}";
                TruncationErrorDetailLatex += $", \\qquad f''({fmt(xi)}) \\approx {fmt(fpp)}";
            }
            else if (SelectedRule == "simpson-13")
            {
                // E = -((b-a)/180) * h^4 * f⁽⁴⁾(ξ)
                double f4 = NumericalDerivative(parser, xi, 4);
                double errorFactor = -(B - A) / 180.0 * Math.Pow(h, 4);
                TruncationErrorValue = errorFactor * f4;

                TruncationErrorDetailLatex = $"E_S = -\\frac{{(b-a)}}{{180}}\\,h^4\\,f^{{(4)}}(\\xi)" +
                    $" = -\\frac{{{fmt(B - A)}}}{{180}} \\cdot {fmt(h)}^4 \\cdot ({fmt(f4)})" +
                    $" = {fmtE(TruncationErrorValue.Value)}";
                TruncationErrorDetailLatex += $", \\qquad f^{{(4)}}({fmt(xi)}) \\approx {fmt(f4)}";
            }
            else if (SelectedRule == "simpson-38")
            {
                // E = -((b-a)/80) * h^4 * f⁽⁴⁾(ξ)
                double f4 = NumericalDerivative(parser, xi, 4);
                double errorFactor = -(B - A) / 80.0 * Math.Pow(h, 4);
                TruncationErrorValue = errorFactor * f4;

                TruncationErrorDetailLatex = $"E_{{3/8}} = -\\frac{{(b-a)}}{{80}}\\,h^4\\,f^{{(4)}}(\\xi)" +
                    $" = -\\frac{{{fmt(B - A)}}}{{80}} \\cdot {fmt(h)}^4 \\cdot ({fmt(f4)})" +
                    $" = {fmtE(TruncationErrorValue.Value)}";
                TruncationErrorDetailLatex += $", \\qquad f^{{(4)}}({fmt(xi)}) \\approx {fmt(f4)}";
            }

            HasTruncationError = true;
        }
        catch { }
    }

    private static double NumericalDerivative(FunctionParser parser, double x, int order)
    {
        const double h = 1e-3;

        if (order == 1)
            return (parser.Evaluate(x + h) - parser.Evaluate(x - h)) / (2 * h);

        if (order == 2)
            return (parser.Evaluate(x + h) - 2 * parser.Evaluate(x) + parser.Evaluate(x - h)) / (h * h);

        if (order == 3)
            return (parser.Evaluate(x + 2 * h) - 2 * parser.Evaluate(x + h) + 2 * parser.Evaluate(x - h) - parser.Evaluate(x - 2 * h)) / (2 * h * h * h);

        if (order == 4)
            return (parser.Evaluate(x + 2 * h) - 4 * parser.Evaluate(x + h) + 6 * parser.Evaluate(x) - 4 * parser.Evaluate(x - h) + parser.Evaluate(x - 2 * h)) / (h * h * h * h);

        return 0;
    }

    private static string ConvertFunctionToLatex(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string s = input.Trim();

        // Replace ** with ^
        s = s.Replace("**", "^");

        // Named functions → \func{...}
        foreach (var fn in new[] { "arcsin", "arccos", "arctan", "sinh", "cosh", "tanh",
                                    "sin", "cos", "tan", "sqrt", "log", "exp", "abs", "ln" })
        {
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @$"\b{fn}\b",
                $@"\{fn}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // sqrt → \sqrt  (already handled, but ensure bracket style)
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\\sqrt\(([^)]+)\)", @"\sqrt{$1}");

        // pi → \pi, but not inside function names
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\bpi\b", @"\pi");

        // Standalone e (not part of exp) → e
        // Keep as-is since 'e' alone is fine in LaTeX

        // Convert fraction patterns like (expr)/(expr) → \frac{expr}{expr}
        // Simple case: (...)/(...)
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\(([^)]+)\)/\(([^)]+)\)", @"\frac{$1}{$2}");

        // x^(expr) → x^{expr}
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\^(\([^)]+\))", m =>
        {
            string inner = m.Groups[1].Value;
            // Remove outer parens and wrap in braces
            inner = inner.Substring(1, inner.Length - 2);
            return "^{" + inner + "}";
        });

        // Simple a*b → a \cdot b (but not \func cases)
        s = System.Text.RegularExpressions.Regex.Replace(s, @"(?<!\\)(\w)\*(\w)", @"$1 \cdot $2");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\)\*", @") \cdot ");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\*\(", @" \cdot (");

        return "f(x) = " + s;
    }
}