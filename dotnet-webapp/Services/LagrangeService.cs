namespace dotnet_webapp.Services;

/// <summary>
/// Servicio para interpolación polinomial de Lagrange.
/// </summary>
public class LagrangeService
{
    private readonly ILogger<LagrangeService> _logger;

    public List<(double X, double Y, double PX, double LocalError)> Nodes { get; private set; } = new();

    public List<double> PointsChartX { get; private set; } = new();
    public List<double> PointsChartY { get; private set; } = new();
    public List<double> PolynomialChartX { get; private set; } = new();
    public List<double> PolynomialChartY { get; private set; } = new();

    public double GlobalError { get; private set; }

    public LagrangeService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<LagrangeService>();
    }

    /// <summary>
    /// Calcula la interpolación de Lagrange a partir de puntos (x, y) provistos por el usuario.
    /// </summary>
    public void Compute(IReadOnlyList<(double X, double Y)> points)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));

        int n = points.Count;
        if (n < 2)
            throw new ArgumentException("Se necesitan al menos 2 nodos de interpolación.");

        var nodeXs = new double[n];
        var nodeYs = new double[n];
        for (int i = 0; i < n; i++)
        {
            nodeXs[i] = points[i].X;
            nodeYs[i] = points[i].Y;
        }

        ValidateDistinctX(nodeXs);

        Nodes = new List<(double, double, double, double)>();
        double maxLocalError = 0;
        for (int i = 0; i < n; i++)
        {
            double px = EvaluatePolynomial(nodeXs[i], nodeXs, nodeYs);
            double localError = Math.Abs(nodeYs[i] - px);
            if (localError > maxLocalError)
            {
                maxLocalError = localError;
            }
            Nodes.Add((nodeXs[i], nodeYs[i], px, localError));
        }

        GlobalError = maxLocalError;

        PointsChartX.Clear();
        PointsChartY.Clear();
        for (int i = 0; i < n; i++)
        {
            PointsChartX.Add(nodeXs[i]);
            PointsChartY.Add(nodeYs[i]);
        }

        const int chartPoints = 400;
        PolynomialChartX.Clear();
        PolynomialChartY.Clear();

        double minX = nodeXs.Min();
        double maxX = nodeXs.Max();
        double span = Math.Max(maxX - minX, 1e-6);
        double plotMinX = minX - span * 0.1;
        double plotMaxX = maxX + span * 0.1;

        for (int i = 0; i < chartPoints; i++)
        {
            double x = plotMinX + (plotMaxX - plotMinX) * i / (chartPoints - 1);
            double pVal;

            try
            {
                pVal = EvaluatePolynomial(x, nodeXs, nodeYs);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error evaluando en x={X}", x);
                continue;
            }

            if (!double.IsFinite(pVal))
                continue;

            PolynomialChartX.Add(x);
            PolynomialChartY.Add(pVal);
        }

        _logger.LogInformation("Interpolación de Lagrange completada: {N} puntos, error global(local)= {Err:E4}", n, GlobalError);
    }

    private static void ValidateDistinctX(double[] xs)
    {
        var seen = new HashSet<double>();
        foreach (double x in xs)
        {
            if (!seen.Add(x))
            {
                throw new ArgumentException("Los puntos deben tener coordenadas X distintas para interpolar con Lagrange.");
            }
        }
    }

    private static double EvaluatePolynomial(double x, double[] xs, double[] ys)
    {
        int n = xs.Length;
        double result = 0.0;

        for (int i = 0; i < n; i++)
        {
            double term = ys[i];
            for (int j = 0; j < n; j++)
            {
                if (j != i)
                    term *= (x - xs[j]) / (xs[i] - xs[j]);
            }
            result += term;
        }

        return result;
    }
}
