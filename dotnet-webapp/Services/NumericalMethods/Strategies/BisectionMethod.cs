using dotnet_webapp.Models;

namespace dotnet_webapp.Services;

internal sealed class BisectionMethod : INumericalMethod
{
    public string MethodKey => "bisection";

    public MethodResult Run(MethodContext context, double first, double? second)
    {
        if (!second.HasValue)
        {
            throw new ArgumentException("El método de bisección requiere dos valores: a y b.");
        }

        double a = first;
        double b = second.Value;
        var result = new MethodResult();

        if (a > b)
        {
            (a, b) = (b, a);
        }

        double fa = context.EvaluateF(a);
        double fb = context.EvaluateF(b);

        if (Math.Abs(fa) < context.Tolerance)
        {
            result.RootX = a;
            result.RootY = fa;
            result.ResultMessage = $"Raíz aproximada: {a:F10} (método bisección)";
            result.SummaryMessage = "La raíz coincide con el extremo izquierdo del intervalo.";
            return result;
        }

        if (Math.Abs(fb) < context.Tolerance)
        {
            result.RootX = b;
            result.RootY = fb;
            result.ResultMessage = $"Raíz aproximada: {b:F10} (método bisección)";
            result.SummaryMessage = "La raíz coincide con el extremo derecho del intervalo.";
            return result;
        }

        if (fa * fb > 0)
        {
            result.ResultMessage = "El intervalo [a, b] debe contener un cambio de signo.";
            context.Logger.LogError("Bisección fallida: f({A})={FA} y f({B})={FB} tienen el mismo signo", a, fa, b, fb);
            return result;
        }

        context.Logger.LogInformation("Iniciando Bisección: f({A})={FA}, f({B})={FB}", a, fa, b, fb);

        for (int i = 1; i <= context.MaxIterations; i++)
        {
            double c = (a + b) / 2.0;
            double fc = context.EvaluateF(c);
            double error = (b - a) / 2.0;

            result.Steps.Add(new StepEntry
            {
                Iteration = i,
                A = a,
                B = b,
                Mid = c,
                FA = fa,
                FB = fb,
                FC = fc,
                Error = error,
                Residual = Math.Abs(fc)
            });

            context.Logger.LogDebug("Iteración {Iteration}: c={C}, f(c)={FC}, error={Error}", i, c, fc, error);

            if (Math.Abs(fc) < context.Tolerance || error < context.Tolerance)
            {
                result.ResultMessage = $"Raíz aproximada: {c:F10} (método bisección)";
                result.SummaryMessage = $"Iteraciones: {i}, f(c) = {fc:E2}";
                result.RootX = c;
                result.RootY = fc;
                context.Logger.LogInformation("Bisección convergió en {Iterations} iteraciones con raíz={Root}", i, c);
                return result;
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

        result.ResultMessage = $"No se encontró convergencia después de {context.MaxIterations} iteraciones.";
        context.Logger.LogWarning("Bisección no convergió después de {MaxIterations} iteraciones", context.MaxIterations);
        return result;
    }
}
