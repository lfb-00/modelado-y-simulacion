import math

def f(x: float) -> float:
    return math.exp(x) + x**2 - 4

def df(x: float) -> float:
    return math.exp(x) + 2*x

def newton_raphson(x0: float, tolerancia: float = 1e-6, max_iter: int = 100) -> float:
    x = x0

    # Encabezado SIN f-string
    print("{:<5} {:<15} {:<15} {:<15} {:<15} {:<15} {:<15}".format(
        "Iter", "x_n", "x_{n+1}", "f(x_n)", "f'(x_n)", "Error Abs", "Error Rel"
    ))
    print("-" * 110)

    for i in range(max_iter):
        fx = f(x)
        dfx = df(x)

        if dfx == 0:
            print("Derivada cero, no se puede continuar")
            return None

        x_nuevo = x - fx / dfx

        error_abs = abs(x_nuevo - x)
        error_rel = error_abs / abs(x_nuevo) if x_nuevo != 0 else 0

        print("{:<5} {:<15.8f} {:<15.8f} {:<15.8f} {:<15.8f} {:<15.8f} {:<15.8f}".format(
            i+1, x, x_nuevo, fx, dfx, error_abs, error_rel
        ))

        if error_abs < tolerancia:
            print("\nRaíz aproximada:", x_nuevo)
            return x_nuevo

        x = x_nuevo

    print("\nNo convergió en el número máximo de iteraciones")
    return None


newton_raphson(0.5)