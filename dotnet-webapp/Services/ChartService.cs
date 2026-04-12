namespace dotnet_webapp.Services;

/// <summary>
/// Servicio para generar datos de gráficos
/// </summary>
public class ChartService
{
    private readonly string _function;
    private readonly string _selectedAlgorithm;
    private readonly ILogger<ChartService> _logger;

    public List<double> ChartXValues { get; set; } = new();
    public List<double> ChartYValues { get; set; } = new();

    public ChartService(ILoggerFactory loggerFactory, string function, string selectedAlgorithm)
    {
        _function = function;
        _selectedAlgorithm = selectedAlgorithm;
        _logger = loggerFactory.CreateLogger<ChartService>();
    }

    /// <summary>
    /// Genera datos del gráfico en un rango especificado
    /// </summary>
    public void GenerateChart(double xMin, double xMax)
    {
        ChartXValues.Clear();
        ChartYValues.Clear();

        int points = 200;
        for (int i = 0; i < points; i++)
        {
            double x = xMin + (xMax - xMin) * i / (points - 1);
            try
            {
                double y = EvaluateFunction(x);
                if (!double.IsNaN(y) && !double.IsInfinity(y))
                {
                    ChartXValues.Add(x);
                    ChartYValues.Add(y);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error evaluando función en x={X}", x);
                // Ignorar puntos que causan error
            }
        }

        _logger.LogInformation("Gráfico generado con {Points} puntos", ChartXValues.Count);
    }

    /// <summary>
    /// Evalúa la función según el algoritmo seleccionado
    /// </summary>
    private double EvaluateFunction(double x)
    {
        if (_selectedAlgorithm == "fixed-point")
        {
            return new FunctionParser(_function).Evaluate(x);
        }
        return new FunctionParser(_function).Evaluate(x);
    }
}
