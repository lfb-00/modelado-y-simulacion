import math


def f(x: float) -> float:
    """Polinomio a evaluar. Modificar según se necesite."""
    return x**3 - x - 2


def biseccion(a: float, b: float, tolerancia: float = 1e-3, max_iter: int = 100):
    """
    Método de bisección para encontrar raíces de f(x) = 0.
    a, b: extremos del intervalo [a, b]
    tolerancia: criterio de parada (10^-3 por defecto)
    max_iter: máximo número de iteraciones
    """

    fa = f(a)
    fb = f(b)

    if fa * fb > 0:
        print("Error: f(a) y f(b) deben tener signos opuestos.")
        print(f"  f({a}) = {fa}")
        print(f"  f({b}) = {fb}")
        return None

    print("{:<5} {:<15} {:<15} {:<15} {:<15} {:<15} {:<15}".format(
        "Iter", "a", "b", "c (punto medio)", "f(a)", "f(c)", "Error"
    ))
    print("-" * 100)

    for i in range(1, max_iter + 1):
        c = (a + b) / 2
        fc = f(c)
        error = abs(b - a) / 2

        print("{:<5} {:<15.8f} {:<15.8f} {:<15.8f} {:<15.8f} {:<15.8f} {:<15.8f}".format(
            i, a, b, c, fa, fc, error
        ))

        if abs(fc) < 1e-15 or error < tolerancia:
            print(f"\nRaíz aproximada: {c}")
            print(f"Iteraciones realizadas: {i}")
            print(f"f(c) = {fc}")
            return c

        if fa * fc < 0:
            b = c
            fb = fc
        else:
            a = c
            fa = fc

    print("\nNo convergió en el número máximo de iteraciones")
    return None


# --- Ejemplo de uso ---
# Polinomio: f(x) = x^3 - x - 2
# Intervalo [1, 2], tolerancia 10^-3

print("Método de Bisección")
print(f"Polinomio: f(x) = x^3 - x - 2")
print(f"Tolerancia: 1e-3\n")

biseccion(1, 2, tolerancia=1e-3)
