def punto_fijo(g, x0, tolerancia=1e-6, max_iter=100):
    """
    g: función g(x)
    x0: valor inicial
    tolerancia: criterio de parada
    max_iter: máximo número de iteraciones
    """
    
    x = x0
    
    for i in range(max_iter):
        x_nuevo = g(x)
        error = abs(x_nuevo - x)
        
        print(f"Iteración {i+1}: x = {x_nuevo}, error = {error}")
        
        if error < tolerancia:
            print("\nRaíz aproximada:", x_nuevo)
            return x_nuevo
        
        x = x_nuevo
    
    print("\nNo convergió en el número máximo de iteraciones")
    return None


# Ejemplo
# Resolver x^2 - x - 1 = 0
# Reescribimos como x = sqrt(x + 1)

import math

def g(x):
    return math.cos(x)
    "return (math.pi+0.5*math.sin(x/2))"
    "e a la menos x"
    "return math.cos(x)"
    "return ((2/5) * math.exp(x**2))" "e a la x al cuadrado"
    "return math.sqrt(x + 1)"


# valor inicial
x0 = 1

punto_fijo(g, x0)