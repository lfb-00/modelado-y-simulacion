using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

internal sealed class FixedPointAitkenMethod : INumericalMethod
{
    public string MethodKey => "fixed-point-aitken";

    public MethodResult Run(MethodContext context, double first, double? second)
    {
        double x = first;
        var result = new MethodResult();

        context.Logger.LogInformation("Iniciando Punto Fijo Aitken con x0={X0}, g(x)={Function}", x, context.Function);

        for (int i = 1; i <= context.MaxIterations; i++)
        {
            double gx = context.EvaluateG(x);
            double ggx = context.EvaluateG(gx);
            double denominator = ggx - (2.0 * gx) + x;

            if (Math.Abs(denominator) < 1e-14)
            {
                result.ResultMessage = "Aitken no puede continuar: denominador cercano a cero en la aceleración.";
                result.SummaryMessage = $"Iteración detenida: {i}. Pruebe otra semilla inicial.";
                context.Logger.LogWarning("Aitken detenido en iteración {Iteration}: denominador={Denominator}", i, denominator);
                return result;
            }

            double delta = gx - x;
            double xAitken = x - (delta * delta) / denominator;
            double error = Math.Abs(xAitken - x);
            double residual = Math.Abs(context.EvaluateG(xAitken) - xAitken);

            result.Steps.Add(new StepEntry
            {
                Iteration = i,
                X = x,
                G = gx,
                GG = ggx,
                AitkenX = xAitken,
                Error = error,
                Residual = residual
            });

            context.Logger.LogDebug(
                "Iteración {Iteration}: x={X}, g(x)={GX}, g(g(x))={GGX}, xAitken={XAitken}, error={Error}, residual={Residual}",
                i,
                x,
                gx,
                ggx,
                xAitken,
                error,
                residual);

            if (error < context.Tolerance || residual < context.Tolerance)
            {
                result.ResultMessage = $"Raíz aproximada: {xAitken:F10} (punto fijo con aceleración de Aitken)";
                result.SummaryMessage = $"Iteraciones: {i}, error final = {error:E2}, residual = {residual:E2}";
                result.RootX = xAitken;
                result.RootY = xAitken;
                context.Logger.LogInformation("Aitken convergió en {Iterations} iteraciones con raíz={Root}", i, xAitken);
                return result;
            }

            x = xAitken;
        }

        result.ResultMessage = $"No se encontró convergencia después de {context.MaxIterations} iteraciones (Aitken).";
        context.Logger.LogWarning("Aitken no convergió después de {MaxIterations} iteraciones", context.MaxIterations);
        return result;
    }
}
