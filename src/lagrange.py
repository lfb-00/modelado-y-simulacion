from sympy import symbols, Rational, simplify, expand, latex

def lagrange_interpolante_3_puntos(p1, p2, p3):
    x = symbols('x')

    # Convertimos a fracciones exactas
    x0, y0 = Rational(p1[0]), Rational(p1[1])
    x1, y1 = Rational(p2[0]), Rational(p2[1])
    x2, y2 = Rational(p3[0]), Rational(p3[1])

    # Polinomios base de Lagrange
    L0 = ((x - x1) * (x - x2)) / ((x0 - x1) * (x0 - x2))
    L1 = ((x - x0) * (x - x2)) / ((x1 - x0) * (x1 - x2))
    L2 = ((x - x0) * (x - x1)) / ((x2 - x0) * (x2 - x1))

    # Polinomio interpolante
    P = simplify(y0 * L0 + y1 * L1 + y2 * L2)
    P_expandido = expand(P)

    return P_expandido, latex(P_expandido)

# Ejemplo de uso
punto1 = (1, 2)
punto2 = (2, 3)
punto3 = (4, 1)

polinomio, polinomio_latex = lagrange_interpolante_3_puntos(punto1, punto2, punto3)

print("Polinomio interpolante de Lagrange:")
print(polinomio)

print("\nPolinomio en LaTeX:")
print(polinomio_latex)