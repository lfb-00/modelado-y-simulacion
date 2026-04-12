import math, re
import numpy as np

def preparar_funcion(f_str):
    s = f_str.strip()
    s = s.replace("math.", "")
    s = s.replace("ln(", "log(")
    s = s.replace("^", "**")
    _FUNCIONES = ['sinh', 'cosh', 'tanh', 'asin', 'acos', 'atan',
                  'sin', 'cos', 'tan', 'exp', 'log10', 'log2', 'log',
                  'sqrt', 'abs', 'pow', 'pi']
    placeholders = {}
    for i, func in enumerate(_FUNCIONES):
        ph = "@@{}@@".format(i)
        placeholders[ph] = func
        s = s.replace(func, ph)
    s = re.sub(r'(\d)([a-zA-Z@\(])', r'\1*\2', s)
    s = re.sub(r'\)([a-zA-Z0-9@\(])', r')*\1', s)
    s = re.sub(r'([a-zA-Z0-9])(\()', r'\1*\2', s)
    for ph, func in placeholders.items():
        s = s.replace(ph + "*(", func + "(")
        s = s.replace(ph, func)
    return s

_EVAL_NS = {
    "__builtins__": {},
    "sin": math.sin, "cos": math.cos, "tan": math.tan,
    "asin": math.asin, "acos": math.acos, "atan": math.atan,
    "sinh": math.sinh, "cosh": math.cosh, "tanh": math.tanh,
    "exp": math.exp, "log": math.log, "log10": math.log10, "log2": math.log2,
    "sqrt": math.sqrt, "abs": abs, "pow": pow,
    "pi": math.pi, "e": math.e, "math": math,
}

def evaluar_funcion(f_str, x_val):
    return float(eval(f_str, {**_EVAL_NS, "x": x_val}))

# Test trapecio
f_str = preparar_funcion("sin(x)")
print("Prepared:", f_str)
a, b, n = 0, math.pi, 10
h = (b - a) / n
xs = [a + i * h for i in range(n + 1)]
ys = [evaluar_funcion(f_str, xi) for xi in xs]
suma = ys[0] + ys[-1] + sum(2 * ys[i] for i in range(1, n))
trap = (h / 2) * suma
print("Trapecio n=10 sin(x) [0,pi]:", trap)

# Test simpson 1/3
s = ys[0] + ys[-1]
for i in range(1, n):
    if i % 2 != 0:
        s += 4 * ys[i]
    else:
        s += 2 * ys[i]
s13 = (h / 3) * s
print("Simpson 1/3 n=10:", s13)

# Test simpson 3/8
n3 = 9
h3 = (b - a) / n3
xs3 = [a + i * h3 for i in range(n3 + 1)]
ys3 = [evaluar_funcion(f_str, xi) for xi in xs3]
s38 = ys3[0] + ys3[-1]
for i in range(1, n3):
    if i % 3 == 0:
        s38 += 2 * ys3[i]
    else:
        s38 += 3 * ys3[i]
s38_result = (3 * h3 / 8) * s38
print("Simpson 3/8 n=9:", s38_result)
print("Expected: ~2.0")
