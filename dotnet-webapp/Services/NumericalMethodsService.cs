using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

/// <summary>
/// Servicio con métodos numéricos para encontrar raíces
/// </summary>
public class NumericalMethodsService
{
    private readonly ILogger<NumericalMethodsService> _logger;
    private readonly string _function;
    private readonly string _selectedAlgorithm;
    private readonly double _tolerance;
    private readonly int _maxIterations;

    public NumericalMethodsService(ILoggerFactory loggerFactory, string function, string selectedAlgorithm, double tolerance, int maxIterations)
    {
        _logger = loggerFactory.CreateLogger<NumericalMethodsService>();
        _function = function;
        _selectedAlgorithm = selectedAlgorithm;
        _tolerance = tolerance;
        _maxIterations = maxIterations;
    }

    public List<StepEntry> Steps { get; set; } = new();
    public double? RootX { get; set; }
    public double? RootY { get; set; }
    public string ResultMessage { get; set; } = string.Empty;
    public string SummaryMessage { get; set; } = string.Empty;

    private double F(double x)
    {
        if (_selectedAlgorithm == "fixed-point")
        {
            throw new InvalidOperationException("Use G(x) para punto fijo.");
        }
        return new FunctionParser(_function).Evaluate(x);
    }

    private double G(double x)
    {
        if (_selectedAlgorithm != "fixed-point")
        {
            throw new InvalidOperationException("Use F(x) para el método seleccionado.");
        }
        return new FunctionParser(_function).Evaluate(x);
    }

    private double Derivative(double x)
    {
        const double h = 1e-6;
        double f1 = new FunctionParser(_function).Evaluate(x + h);
        double f2 = new FunctionParser(_function).Evaluate(x - h);
        return (f1 - f2) / (2 * h);
    }

    public void ComputeBisection(double a, double b)
    {
        if (a > b)
        {
            (a, b) = (b, a);
        }

        double fa = F(a);
        double fb = F(b);

        if (Math.Abs(fa) < _tolerance)
        {
            RootX = a;
            RootY = fa;
            ResultMessage = $"Raíz aproximada: {a:F10} (método bisección)";
            SummaryMessage = "La raíz coincide con el extremo izquierdo del intervalo.";
            return;
        }

        if (Math.Abs(fb) < _tolerance)
        {
            RootX = b;
            RootY = fb;
            ResultMessage = $"Raíz aproximada: {b:F10} (método bisección)";
            SummaryMessage = "La raíz coincide con el extremo derecho del intervalo.";
            return;
        }

        if (fa * fb > 0)
        {
            ResultMessage = "El intervalo [a, b] debe contener un cambio de signo.";
            _logger.LogError("Bisección fallida: f({A})={FA} y f({B})={FB} tienen el mismo signo", a, fa, b, fb);
            return;
        }

        _logger.LogInformation("Iniciando Bisección: f({A})={FA}, f({B})={FB}", a, fa, b, fb);

        for (int i = 1; i <= _maxIterations; i++)
        {
            double c = (a + b) / 2.0;
            double fc = F(c);
            double error = (b - a) / 2.0;

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

            if (Math.Abs(fc) < _tolerance || error < _tolerance)
            {
                ResultMessage = $"Raíz aproximada: {c:F10} (método bisección)";
                SummaryMessage = $"Iteraciones: {i}, f(c) = {fc:E2}";
                RootX = c;
                RootY = fc;
                _logger.LogInformation("Bisección convergió en {Iterations} iteraciones con raíz={Root}", i, c);
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
        }

        ResultMessage = $"No se encontró convergencia después de {_maxIterations} iteraciones.";
        _logger.LogWarning("Bisección no convergió después de {MaxIterations} iteraciones", _maxIterations);
    }

    public void ComputeNewton(double x0)
    {
        double x = x0;

        _logger.LogInformation("Iniciando Newton-Raphson con x0={X0}, función={Function}", x, _function);

        for (int i = 1; i <= _maxIterations; i++)
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

            if (error < _tolerance)
            {
                ResultMessage = $"Raíz aproximada: {next:F10} (método Newton-Raphson)";
                SummaryMessage = $"Iteraciones: {i}, f(x) = {fx:E2}";
                RootX = next;
                RootY = F(next);
                _logger.LogInformation("Newton-Raphson convergió en {Iterations} iteraciones con raíz={Root}", i, next);
                return;
            }

            x = next;
        }

        ResultMessage = $"No se encontró convergencia después de {_maxIterations} iteraciones.";
        _logger.LogWarning("Newton-Raphson no convergió después de {MaxIterations} iteraciones", _maxIterations);
    }

    public void ComputeFixedPoint(double x0)
    {
        double x = x0;

        _logger.LogInformation("Iniciando Punto Fijo con x0={X0}, g(x)={Function}", x, _function);

        for (int i = 1; i <= _maxIterations; i++)
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

            if (error < _tolerance)
            {
                ResultMessage = $"Raíz aproximada: {next:F10} (método de punto fijo)";
                SummaryMessage = $"Iteraciones: {i}, error final = {error:E2}";
                RootX = next;
                RootY = next;
                _logger.LogInformation("Punto Fijo convergió en {Iterations} iteraciones con raíz={Root}", i, next);
                return;
            }

            x = next;
        }

        ResultMessage = $"No se encontró convergencia después de {_maxIterations} iteraciones.";
        _logger.LogWarning("Punto Fijo no convergió después de {MaxIterations} iteraciones", _maxIterations);
    }
}
