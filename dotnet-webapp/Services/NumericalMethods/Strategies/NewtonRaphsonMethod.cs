using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

internal sealed class NewtonRaphsonMethod : INumericalMethod
{
    public string MethodKey => "newton";

    public MethodResult Run(MethodContext context, double first, double? second)
    {
        double x = first;
        var result = new MethodResult();

        context.Logger.LogInformation("Iniciando Newton-Raphson con x0={X0}, función={Function}", x, context.Function);

        for (int i = 1; i <= context.MaxIterations; i++)
        {
            double fx = context.EvaluateF(x);
            double dfx = context.EvaluateDerivative(x);

            if (Math.Abs(dfx) < 1e-12)
            {
                result.ResultMessage = "Derivada demasiado pequeña. Cambie la semilla inicial.";
                context.Logger.LogError("Newton-Raphson fallido: derivada muy pequeña ({DFX}) en x={X}", dfx, x);
                return result;
            }

            double next = x - fx / dfx;
            double error = Math.Abs(next - x);

            result.Steps.Add(new StepEntry
            {
                Iteration = i,
                X = x,
                FX = fx,
                DFX = dfx,
                Error = error
            });

            context.Logger.LogDebug("Iteración {Iteration}: x={X}, f(x)={FX}, f'(x)={DFX}, error={Error}", i, x, fx, dfx, error);

            if (error < context.Tolerance)
            {
                result.ResultMessage = $"Raíz aproximada: {next:F10} (método Newton-Raphson)";
                result.SummaryMessage = $"Iteraciones: {i}, f(x) = {fx:E2}";
                result.RootX = next;
                result.RootY = context.EvaluateF(next);
                context.Logger.LogInformation("Newton-Raphson convergió en {Iterations} iteraciones con raíz={Root}", i, next);
                return result;
            }

            x = next;
        }

        result.ResultMessage = $"No se encontró convergencia después de {context.MaxIterations} iteraciones.";
        context.Logger.LogWarning("Newton-Raphson no convergió después de {MaxIterations} iteraciones", context.MaxIterations);
        return result;
    }
}
