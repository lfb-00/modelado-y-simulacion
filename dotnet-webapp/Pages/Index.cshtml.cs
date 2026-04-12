using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace dotnet_webapp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
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

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public string ResultMessage { get; set; } = string.Empty;
    public string SummaryMessage { get; set; } = string.Empty;
    public List<StepEntry> Steps { get; set; } = new();
    public List<double> ChartXValues { get; set; } = new();
    public List<double> ChartYValues { get; set; } = new();
    public double? RootX { get; set; }
    public double? RootY { get; set; }

    public void OnGet()
    {
        ResultMessage = string.Empty;
        SummaryMessage = string.Empty;
        Steps.Clear();
        ChartXValues.Clear();
        ChartYValues.Clear();
        RootX = null;
        RootY = null;
    }

    public void OnPost()
    {
        ResultMessage = string.Empty;
        SummaryMessage = string.Empty;
        Steps = new List<StepEntry>();

        _logger.LogInformation("Inicio de cálculo - Algoritmo: {Algorithm}, Tolerancia: {Tolerance}, MaxIteraciones: {MaxIterations}", SelectedAlgorithm, Tolerance, MaxIterations);

        if (Tolerance <= 0)
        {
            ResultMessage = "La tolerancia debe ser mayor que 0.";
            _logger.LogWarning("Tolerancia inválida: {Tolerance}", Tolerance);
            return;
        }

        if (MaxIterations <= 0)
        {
            ResultMessage = "El número máximo de iteraciones debe ser mayor que 0.";
            _logger.LogWarning("MaxIteraciones inválido: {MaxIterations}", MaxIterations);
            return;
        }

        try
        {
            switch (SelectedAlgorithm)
            {
                case "bisection":
                    _logger.LogInformation("Ejecutando Bisección con intervalo [{A}, {B}]", A, B);
                    ComputeBisection();
                    break;
                case "newton":
                    _logger.LogInformation("Ejecutando Newton-Raphson con x0={X0}", X0);
                    ComputeNewton();
                    break;
                case "fixed-point":
                    _logger.LogInformation("Ejecutando Punto Fijo con x0={X0}", X0);
                    ComputeFixedPoint();
                    break;
                default:
                    ResultMessage = "Seleccione un método válido.";
                    _logger.LogError("Algoritmo inválido: {Algorithm}", SelectedAlgorithm);
                    break;
            }
        }
        catch (ArgumentException ex)
        {
            ResultMessage = "Error en la función: " + ex.Message;
            _logger.LogError(ex, "Error en la evaluación de la función");
        }
        catch (Exception ex)
        {
            ResultMessage = "Error al evaluar la función: " + ex.Message;
            _logger.LogError(ex, "Error al evaluar la función");
        }

        _logger.LogInformation("Cálculo completado - Resultado: {Result}", ResultMessage);
    }

    private double F(double x)
    {
        if (SelectedAlgorithm == "fixed-point")
        {
            throw new InvalidOperationException("Use G(x) para punto fijo.");
        }

        return new FunctionParser(Function).Evaluate(x);
    }

    private double G(double x)
    {
        if (SelectedAlgorithm != "fixed-point")
        {
            throw new InvalidOperationException("Use F(x) para el método seleccionado.");
        }

        return new FunctionParser(Function).Evaluate(x);
    }

    private double Derivative(double x)
    {
        const double h = 1e-6;
        double f1 = new FunctionParser(Function).Evaluate(x + h);
        double f2 = new FunctionParser(Function).Evaluate(x - h);
        return (f1 - f2) / (2 * h);
    }

    private void ComputeBisection()
    {
        double a = A;
        double b = B;
        double fa = F(a);
        double fb = F(b);

        if (fa * fb > 0)
        {
            ResultMessage = "El intervalo [a, b] debe contener un cambio de signo.";
            _logger.LogError("Bisección fallida: f({A})={FA} y f({B})={FB} tienen el mismo signo", a, fa, b, fb);
            return;
        }

        _logger.LogInformation("Iniciando Bisección: f({A})={FA}, f({B})={FB}", a, fa, b, fb);

        double previousC = double.NaN;
        for (int i = 1; i <= MaxIterations; i++)
        {
            double c = (a + b) / 2;
            double fc = F(c);
            double error = i == 1 ? Math.Abs(b - a) : Math.Abs(c - previousC);

            Steps.Add(new StepEntry
            {
                Iteration = i,
                A = a,
                B = b,
                Mid = c,
                FA = fa,
                FB = fb,
                FC = fc,
                Error = error
            });

            _logger.LogDebug("Iteración {Iteration}: c={C}, f(c)={FC}, error={Error}", i, c, fc, error);

            if (Math.Abs(fc) < Tolerance || (b - a) / 2 < Tolerance)
            {
                ResultMessage = $"Raíz aproximada: {c:F10} (método bisección)";
                SummaryMessage = $"Iteraciones: {i}, f(c) = {fc:E2}";
                RootX = c;
                RootY = fc;
                _logger.LogInformation("Bisección convergió en {Iterations} iteraciones con raíz={Root}", i, c);
                GenerateChart(-30 - 0.5, 30 + 0.5);
                return;
            }

            if (fa * fc < 0)
            {
                b = c;
                fb = fc;
            }
            else
            {
                a = c;
                fa = fc;
            }

            previousC = c;
        }

        ResultMessage = $"No se encontró convergencia después de {MaxIterations} iteraciones.";
        _logger.LogWarning("Bisección no convergió después de {MaxIterations} iteraciones", MaxIterations);
        GenerateChart(A, B);
    }

    private void ComputeNewton()
    {
        double x = X0;

        _logger.LogInformation("Iniciando Newton-Raphson con x0={X0}, función={Function}", x, Function);

        for (int i = 1; i <= MaxIterations; i++)
        {
            double fx = F(x);
            double dfx = Derivative(x);

            if (Math.Abs(dfx) < 1e-12)
            {
                ResultMessage = "Derivada demasiado pequeña. Cambie la semilla inicial.";
                _logger.LogError("Newton-Raphson fallido: derivada muy pequeña ({DFX}) en x={X}", dfx, x);
                return;
            }

            double next = x - fx / dfx;
            double error = Math.Abs(next - x);

            Steps.Add(new StepEntry
            {
                Iteration = i,
                X = x,
                FX = fx,
                DFX = dfx,
                Error = error
            });

            _logger.LogDebug("Iteración {Iteration}: x={X}, f(x)={FX}, f'(x)={DFX}, error={Error}", i, x, fx, dfx, error);

            if (error < Tolerance)
            {
                ResultMessage = $"Raíz aproximada: {next:F10} (método Newton-Raphson)";
                SummaryMessage = $"Iteraciones: {i}, f(x) = {fx:E2}";
                RootX = next;
                RootY = F(next);
                _logger.LogInformation("Newton-Raphson convergió en {Iterations} iteraciones con raíz={Root}", i, next);
                GenerateChart(Math.Min(X0, next) - 1, Math.Max(X0, next) + 1);
                return;
            }

            x = next;
        }

        ResultMessage = $"No se encontró convergencia después de {MaxIterations} iteraciones.";
        _logger.LogWarning("Newton-Raphson no convergió después de {MaxIterations} iteraciones", MaxIterations);
        GenerateChart(X0 - 2, X0 + 2);
    }

    private void ComputeFixedPoint()
    {
        double x = X0;

        _logger.LogInformation("Iniciando Punto Fijo con x0={X0}, g(x)={Function}", x, Function);

        for (int i = 1; i <= MaxIterations; i++)
        {
            double next = G(x);
            double error = Math.Abs(next - x);

            Steps.Add(new StepEntry
            {
                Iteration = i,
                X = x,
                G = next,
                Error = error
            });

            _logger.LogDebug("Iteración {Iteration}: x={X}, g(x)={G}, error={Error}", i, x, next, error);

            if (error < Tolerance)
            {
                ResultMessage = $"Raíz aproximada: {next:F10} (método de punto fijo)";
                SummaryMessage = $"Iteraciones: {i}, error final = {error:E2}";
                RootX = next;
                RootY = next;
                _logger.LogInformation("Punto Fijo convergió en {Iterations} iteraciones con raíz={Root}", i, next);
                GenerateChart(X0 - 2, X0 + 2);
                return;
            }

            x = next;
        }

        ResultMessage = $"No se encontró convergencia después de {MaxIterations} iteraciones.";
        _logger.LogWarning("Punto Fijo no convergió después de {MaxIterations} iteraciones", MaxIterations);
        GenerateChart(X0 - 2, X0 + 2);
    }

    private void GenerateChart(double xMin, double xMax)
    {
        ChartXValues.Clear();
        ChartYValues.Clear();

        int points = 200;
        for (int i = 0; i < points; i++)
        {
            double x = xMin + (xMax - xMin) * i / (points - 1);
            try
            {
                double y = SelectedAlgorithm == "fixed-point" ? G(x) : F(x);
                if (!double.IsNaN(y) && !double.IsInfinity(y))
                {
                    ChartXValues.Add(x);
                    ChartYValues.Add(y);
                }
            }
            catch
            {
                // Ignorar puntos que causan error
            }
        }
    }

    public class StepEntry
    {
        public int Iteration { get; set; }
        public double? A { get; set; }
        public double? B { get; set; }
        public double? Mid { get; set; }
        public double? FA { get; set; }
        public double? FB { get; set; }
        public double? FC { get; set; }
        public double? X { get; set; }
        public double? FX { get; set; }
        public double? DFX { get; set; }
        public double? G { get; set; }
        public double? Error { get; set; }
    }

    private class FunctionParser
    {
        private readonly string _expression;
        private int _pos;
        private string _text = string.Empty;
        private double _xValue;

        public FunctionParser(string expression)
        {
            _expression = expression ?? string.Empty;
        }

        public double Evaluate(double x)
        {
            _text = _expression.Replace(" ", string.Empty).ToLowerInvariant();
            _pos = 0;
            _xValue = x;
            double result = ParseExpression();
            SkipWhitespace();
            if (_pos < _text.Length)
            {
                throw new ArgumentException($"Símbolo inesperado en la expresión: '{_text[_pos]}'");
            }
            return result;
        }

        private double ParseExpression()
        {
            double value = ParseTerm();
            while (true)
            {
                if (Match('+'))
                {
                    value += ParseTerm();
                }
                else if (Match('-'))
                {
                    value -= ParseTerm();
                }
                else
                {
                    break;
                }
            }
            return value;
        }

        private double ParseTerm()
        {
            double value = ParseFactor();
            while (true)
            {
                if (Match('*'))
                {
                    value *= ParseFactor();
                }
                else if (Match('/'))
                {
                    value /= ParseFactor();
                }
                else
                {
                    break;
                }
            }
            return value;
        }

        private double ParseFactor()
        {
            double value = ParseUnary();
            while (Match('^'))
            {
                value = Math.Pow(value, ParseUnary());
            }
            return value;
        }

        private double ParseUnary()
        {
            if (Match('+'))
            {
                return ParseUnary();
            }
            if (Match('-'))
            {
                return -ParseUnary();
            }
            return ParsePrimary();
        }

        private double ParsePrimary()
        {
            SkipWhitespace();

            if (Match('('))
            {
                double value = ParseExpression();
                if (!Match(')'))
                {
                    throw new ArgumentException("Falta un paréntesis de cierre.");
                }
                return value;
            }

            if (IsLetter(Peek()))
            {
                string name = ParseName();
                if (Match('('))
                {
                    if (name == "pow")
                    {
                        double first = ParseExpression();
                        if (!Match(','))
                        {
                            throw new ArgumentException("Falta la coma en pow(a,b).");
                        }
                        double second = ParseExpression();
                        if (!Match(')'))
                        {
                            throw new ArgumentException("Falta un paréntesis de cierre en pow.");
                        }
                        return Math.Pow(first, second);
                    }

                    double arg = ParseExpression();
                    if (!Match(')'))
                    {
                        throw new ArgumentException("Falta un paréntesis de cierre.");
                    }
                    return EvaluateFunction(name, arg);
                }

                if (name == "x")
                {
                    return _xValue;
                }

                return EvaluateConstant(name);
            }

            return ParseNumber();
        }

        private double ParseNumber()
        {
            SkipWhitespace();
            int start = _pos;
            while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
            {
                _pos++;
            }
            if (start == _pos)
            {
                throw new ArgumentException("Número esperado.");
            }
            string number = _text[start.._pos];
            if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                throw new ArgumentException($"Número inválido: {number}");
            }
            return result;
        }

        private double EvaluateFunction(string name, double arg)
        {
            return name switch
            {
                "sin" => Math.Sin(arg),
                "cos" => Math.Cos(arg),
                "tan" => Math.Tan(arg),
                "exp" => Math.Exp(arg),
                "log" => Math.Log(arg),
                "ln" => Math.Log(arg),
                "sqrt" => Math.Sqrt(arg),
                "abs" => Math.Abs(arg),
                _ => throw new ArgumentException($"Función desconocida: {name}"),
            };
        }

        private double EvaluateConstant(string name)
        {
            return name switch
            {
                "pi" => Math.PI,
                "e" => Math.E,
                _ => throw new ArgumentException($"Constante desconocida: {name}"),
            };
        }

        private string ParseName()
        {
            int start = _pos;
            while (_pos < _text.Length && IsLetter(_text[_pos]))
            {
                _pos++;
            }
            return _text[start.._pos];
        }

        private bool Match(char expected)
        {
            SkipWhitespace();
            if (_pos < _text.Length && _text[_pos] == expected)
            {
                _pos++;
                return true;
            }
            return false;
        }

        private char Peek()
        {
            SkipWhitespace();
            return _pos < _text.Length ? _text[_pos] : '\0';
        }

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
            {
                _pos++;
            }
        }

        private static bool IsLetter(char c) => char.IsLetter(c);
    }
}
