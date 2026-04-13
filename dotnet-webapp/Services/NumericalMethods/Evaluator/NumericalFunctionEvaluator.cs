namespace dotnet_webapp.Services;

internal sealed class NumericalFunctionEvaluator
{
    private readonly string _function;

    public NumericalFunctionEvaluator(string function)
    {
        _function = function;
    }

    public double Evaluate(double x) => new FunctionParser(_function).Evaluate(x);

    public double Derivative(double x)
    {
        const double h = 1e-6;
        double f1 = Evaluate(x + h);
        double f2 = Evaluate(x - h);
        return (f1 - f2) / (2 * h);
    }
}
