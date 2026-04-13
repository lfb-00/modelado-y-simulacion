using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

internal sealed class FixedPointMethod : INumericalMethod
{
    public string MethodKey => "fixed-point";

    public MethodResult Run(MethodContext context, double first, double? second)
    {
        double x = first;
        var result = new MethodResult();

        context.Logger.LogInformation("Iniciando Punto Fijo con x0={X0}, g(x)={Function}", x, context.Function);

        for (int i = 1; i <= context.MaxIterations; i++)
        {
            double next = context.EvaluateG(x);
            double error = Math.Abs(next - x);

            result.Steps.Add(new StepEntry
            {
                Iteration = i,
                X = x,
                G = next,
                Error = error,
                Residual = error
            });

            context.Logger.LogDebug("Iteración {Iteration}: x={X}, g(x)={G}, error={Error}", i, x, next, error);

            if (error < context.Tolerance)
            {
                result.ResultMessage = $"Raíz aproximada: {next:F10} (método de punto fijo)";
                result.SummaryMessage = $"Iteraciones: {i}, error final = {error:E2}";
                result.RootX = next;
                result.RootY = next;
                context.Logger.LogInformation("Punto Fijo convergió en {Iterations} iteraciones con raíz={Root}", i, next);
                return result;
            }

            x = next;
        }

        result.ResultMessage = $"No se encontró convergencia después de {context.MaxIterations} iteraciones.";
        context.Logger.LogWarning("Punto Fijo no convergió después de {MaxIterations} iteraciones", context.MaxIterations);
        return result;
    }
}
