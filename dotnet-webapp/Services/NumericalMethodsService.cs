using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

/// <summary>
/// Servicio con métodos numéricos para encontrar raíces
/// </summary>
public class NumericalMethodsService
{
    private readonly ILogger<NumericalMethodsService> _logger;
    private readonly string _selectedAlgorithm;
    private readonly NumericalFunctionEvaluator _evaluator;
    private readonly MethodContext _context;

    public NumericalMethodsService(ILoggerFactory loggerFactory, string function, string selectedAlgorithm, double tolerance, int maxIterations)
    {
        _logger = loggerFactory.CreateLogger<NumericalMethodsService>();
        _selectedAlgorithm = selectedAlgorithm;
        _evaluator = new NumericalFunctionEvaluator(function);

        _context = new MethodContext
        {
            Function = function,
            Tolerance = tolerance,
            MaxIterations = maxIterations,
            EvaluateF = _evaluator.Evaluate,
            EvaluateG = _evaluator.Evaluate,
            EvaluateDerivative = _evaluator.Derivative,
            Logger = _logger
        };
    }

    public List<StepEntry> Steps { get; set; } = new();
    public double? RootX { get; set; }
    public double? RootY { get; set; }
    public string ResultMessage { get; set; } = string.Empty;
    public string SummaryMessage { get; set; } = string.Empty;

    public void ComputeBisection(double a, double b)
    {
        Execute(new BisectionMethod(), a, b);
    }

    public void ComputeNewton(double x0)
    {
        Execute(new NewtonRaphsonMethod(), x0, null);
    }

    public void ComputeFixedPoint(double x0)
    {
        Execute(new FixedPointMethod(), x0, null);
    }

    private void Execute(INumericalMethod method, double first, double? second)
    {
        if (method.MethodKey == "fixed-point" && _selectedAlgorithm != "fixed-point")
        {
            throw new InvalidOperationException("Use F(x) para el método seleccionado.");
        }

        if (method.MethodKey != "fixed-point" && _selectedAlgorithm == "fixed-point")
        {
            throw new InvalidOperationException("Use G(x) para punto fijo.");
        }

        MethodResult result = method.Run(_context, first, second);
        Steps = result.Steps;
        RootX = result.RootX;
        RootY = result.RootY;
        ResultMessage = result.ResultMessage;
        SummaryMessage = result.SummaryMessage;
    }
}
