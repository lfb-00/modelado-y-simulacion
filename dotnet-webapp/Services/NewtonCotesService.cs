using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

public class NewtonCotesService
{
    private readonly ILogger<NewtonCotesService> _logger;

    public List<NewtonCotesNodeEntry> Nodes { get; private set; } = new();
    public List<double> ChartXValues { get; private set; } = new();
    public List<double> ChartYValues { get; private set; } = new();
    public List<double> NodeChartXValues { get; private set; } = new();
    public List<double> NodeChartYValues { get; private set; } = new();
    public double Approximation { get; private set; }
    public double StepSize { get; private set; }
    public string RuleDisplayName { get; private set; } = string.Empty;
    public string FormulaLatex { get; private set; } = string.Empty;
    public string ErrorBoundLatex { get; private set; } = string.Empty;

    public NewtonCotesService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<NewtonCotesService>();
    }

    public void Compute(string function, string rule, double a, double b, int subintervals)
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

        if (subintervals <= 0)
        {
            throw new ArgumentException("n debe ser mayor que 0.");
        }

        ValidateRule(rule, subintervals);

        var parser = new FunctionParser(function);

        StepSize = (b - a) / subintervals;
        Nodes = new List<NewtonCotesNodeEntry>();
        NodeChartXValues = new List<double>();
        NodeChartYValues = new List<double>();

        double weightedSum = 0;
        for (int i = 0; i <= subintervals; i++)
        {
            double x = a + i * StepSize;
            double fx = parser.Evaluate(x);
            if (!double.IsFinite(fx))
            {
                fx = ApproximateLimit(parser, x, StepSize);
                if (!double.IsFinite(fx))
                {
                    throw new ArgumentException($"La funcion no pudo evaluarse correctamente en x = {x:0.######} (indeterminacion no resuelta).");
                }
                _logger.LogInformation("Indeterminacion en x={X} resuelta por limite numerico: f(x)≈{FX}", x, fx);
            }

            int coefficient = GetCoefficient(rule, i, subintervals);
            weightedSum += coefficient * fx;

            Nodes.Add(new NewtonCotesNodeEntry
            {
                Index = i,
                X = x,
                FX = fx,
                Coefficient = coefficient,
                WeightedValue = coefficient * fx,
                PartialWeightedSum = weightedSum
            });

            NodeChartXValues.Add(x);
            NodeChartYValues.Add(fx);
        }

        Approximation = GetRuleFactor(rule, StepSize) * weightedSum;
        BuildFormulas(rule, a, b, subintervals);
        BuildChartSeries(parser, a, b);

        _logger.LogInformation("Newton-Cotes completado con regla {Rule}, intervalo [{A}, {B}], n={N}, aproximacion={Approximation:E6}", rule, a, b, subintervals, Approximation);
    }

    /// <summary>
    /// Approximates the limit of f(x) at a point where it is indeterminate (NaN/Infinity)
    /// by evaluating from both sides with decreasing step sizes.
    /// </summary>
    private static double ApproximateLimit(FunctionParser parser, double x, double h)
    {
        double[] deltas = { 1e-8, 1e-10, 1e-12 };
        double sumLeft = 0, sumRight = 0;
        int countLeft = 0, countRight = 0;

        foreach (var d in deltas)
        {
            double fLeft = parser.Evaluate(x - d);
            if (double.IsFinite(fLeft)) { sumLeft += fLeft; countLeft++; }

            double fRight = parser.Evaluate(x + d);
            if (double.IsFinite(fRight)) { sumRight += fRight; countRight++; }
        }

        int total = countLeft + countRight;
        if (total == 0) return double.NaN;

        return (sumLeft + sumRight) / total;
    }

    private static void ValidateRule(string rule, int subintervals)
    {
        switch (rule)
        {
            case "trapezoidal":
                if (subintervals < 1)
                {
                    throw new ArgumentException("La regla del trapecio requiere n >= 1.");
                }
                break;
            case "simpson-13":
                if (subintervals < 2 || subintervals % 2 != 0)
                {
                    throw new ArgumentException("Simpson 1/3 compuesto requiere n par y n >= 2.");
                }
                break;
            case "simpson-38":
                if (subintervals < 3 || subintervals % 3 != 0)
                {
                    throw new ArgumentException("Simpson 3/8 compuesto requiere n multiplo de 3 y n >= 3.");
                }
                break;
            default:
                throw new ArgumentException("Selecciona una regla de Newton-Cotes valida.");
        }
    }

    private static int GetCoefficient(string rule, int index, int subintervals)
    {
        if (index == 0 || index == subintervals)
        {
            return 1;
        }

        return rule switch
        {
            "trapezoidal" => 2,
            "simpson-13" => index % 2 == 0 ? 2 : 4,
            "simpson-38" => index % 3 == 0 ? 2 : 3,
            _ => throw new ArgumentException("Regla de Newton-Cotes no soportada.")
        };
    }

    private static double GetRuleFactor(string rule, double h)
    {
        return rule switch
        {
            "trapezoidal" => h / 2.0,
            "simpson-13" => h / 3.0,
            "simpson-38" => 3.0 * h / 8.0,
            _ => throw new ArgumentException("Regla de Newton-Cotes no soportada.")
        };
    }

    private void BuildFormulas(string rule, double a, double b, int subintervals)
    {
        string h = "\\frac{b-a}{n}";

        switch (rule)
        {
            case "trapezoidal":
                RuleDisplayName = "Regla del trapecio compuesta";
                FormulaLatex = $"\\int_{{{a:0.######}}}^{{{b:0.######}}} f(x)\\,dx \\approx \\frac{{h}}{{2}}\\left[f(x_0)+2\\sum_{{i=1}}^{{{subintervals - 1}}} f(x_i)+f(x_{{{subintervals}}})\\right],\\quad h={h}";
                ErrorBoundLatex = $"|E| \\le \\frac{{(b-a)^3}}{{12n^2}}\\max_{{x\\in[a,b]}} |f''(x)|,\\quad \\frac{{(b-a)^3}}{{12n^2}} = {Math.Pow(b - a, 3) / (12.0 * subintervals * subintervals):0.######}";
                break;
            case "simpson-13":
                RuleDisplayName = "Regla de Simpson 1/3 compuesta";
                FormulaLatex = $"\\int_{{{a:0.######}}}^{{{b:0.######}}} f(x)\\,dx \\approx \\frac{{h}}{{3}}\\left[f(x_0)+4\\sum_{{i\\,impar}} f(x_i)+2\\sum_{{i\\,par,\\,0<i<{subintervals}}} f(x_i)+f(x_{{{subintervals}}})\\right],\\quad h={h}";
                ErrorBoundLatex = $"|E| \\le \\frac{{(b-a)^5}}{{180n^4}}\\max_{{x\\in[a,b]}} |f^{{(4)}}(x)|,\\quad \\frac{{(b-a)^5}}{{180n^4}} = {Math.Pow(b - a, 5) / (180.0 * Math.Pow(subintervals, 4)):0.######}";
                break;
            case "simpson-38":
                RuleDisplayName = "Regla de Simpson 3/8 compuesta";
                FormulaLatex = $"\\int_{{{a:0.######}}}^{{{b:0.######}}} f(x)\\,dx \\approx \\frac{{3h}}{{8}}\\left[f(x_0)+3\\sum_{{i\\not\\equiv 0\\,(3)}} f(x_i)+2\\sum_{{i\\equiv 0\\,(3),\\,0<i<{subintervals}}} f(x_i)+f(x_{{{subintervals}}})\\right],\\quad h={h}";
                ErrorBoundLatex = $"|E| \\le \\frac{{(b-a)^5}}{{80n^4}}\\max_{{x\\in[a,b]}} |f^{{(4)}}(x)|,\\quad \\frac{{(b-a)^5}}{{80n^4}} = {Math.Pow(b - a, 5) / (80.0 * Math.Pow(subintervals, 4)):0.######}";
                break;
        }
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
                y = ApproximateLimit(parser, x, (b - a) / (chartPoints - 1));
                if (!double.IsFinite(y))
                {
                    continue;
                }
            }

            ChartXValues.Add(x);
            ChartYValues.Add(y);
        }
    }
}