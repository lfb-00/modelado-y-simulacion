import customtkinter as ctk
from tkinter import messagebox
import math
import re

# --- Importar módulos propios ---
from sympy import symbols, Rational, simplify, expand, latex

# --- Configuracion de apariencia ---
ctk.set_appearance_mode("dark")
ctk.set_default_color_theme("blue")

# ==================================================================
#  PREPROCESAMIENTO DE FUNCIONES
# ==================================================================

def preparar_funcion(f_str):
    """
    Preprocesa la cadena de la funcion para que sea evaluable.
    Acepta notacion natural: sin(x), cos(x), exp(x), ln(x), sqrt(x), pi, e
    Tambien acepta: math.sin(x), math.exp(x), etc.
    Acepta ^ como potencia.
    Soporta multiplicacion implicita: 2x -> 2*x, 3sin(x) -> 3*sin(x)
    """
    s = f_str.strip()
    # Quitar prefijo "math." si el usuario lo puso
    s = s.replace("math.", "")
    # Reemplazar ln(x) -> log(x) (en Python log = ln)
    s = s.replace("ln(", "log(")
    # Reemplazar ^ por ** (notacion matematica comun)
    s = s.replace("^", "**")

    # --- Multiplicacion implicita ---
    # Proteger nombres de funciones reemplazandolos temporalmente
    _FUNCIONES = ['sinh', 'cosh', 'tanh', 'asin', 'acos', 'atan',
                  'sin', 'cos', 'tan', 'exp', 'log10', 'log2', 'log',
                  'sqrt', 'abs', 'pow', 'pi']
    placeholders = {}
    for i, func in enumerate(_FUNCIONES):
        ph = "@@{}@@".format(i)
        placeholders[ph] = func
        s = s.replace(func, ph)

    # numero seguido de letra o (: 2x -> 2*x, 2( -> 2*(
    s = re.sub(r'(\, r'\1*\2', s)
    # ) seguido de letra, nd)([a-zA-Z@\(])'umero o (: )(  -> )*(, )x -> )*x, )2 -> )*2
    s = re.sub(r'\)([a-zA-Z0-9@\(])', r')*\1', s)
    # x seguido de (: x( -> x*(  (variable por parentesis)
    s = re.sub(r'([a-zA-Z0-9])(\()', r'\1*\2', s)

    # Restaurar nombres de funciones
    for ph, func in placeholders.items():
        # El placeholder pudo recibir un * insertado: @@0@@*( -> restaurar a func(
        s = s.replace(ph + "*(", func + "(")
        s = s.replace(ph, func)

    return s

# Namespace seguro para eval (sin necesidad de escribir "math.")
_EVAL_NS = {
    "__builtins__": {},
    "sin": math.sin, "cos": math.cos, "tan": math.tan,
    "asin": math.asin, "acos": math.acos, "atan": math.atan,
    "sinh": math.sinh, "cosh": math.cosh, "tanh": math.tanh,
    "exp": math.exp, "log": math.log, "log10": math.log10, "log2": math.log2,
    "sqrt": math.sqrt, "abs": abs, "pow": pow,
    "pi": math.pi, "e": math.e,
    "math": math,
}

def evaluar_funcion(f_str, x_val):
    """Evalua una funcion f(x) preprocesada en un valor numerico x_val."""
    return float(eval(f_str, {**_EVAL_NS, "x": x_val}))


# ==================================================================
#  FUNCIONES DE LOS METODOS NUMERICOS
# ==================================================================

def metodo_biseccion(f_str, a, b, tolerancia=1e-3, max_iter=100):
    """Metodo de biseccion. Retorna (raiz, texto_tabla)"""
    f_str = preparar_funcion(f_str)

    def f(val):
        return evaluar_funcion(f_str, val)

    fa = f(a)
    fb = f(b)

    if fa * fb > 0:
        return None, "Error: f(a) y f(b) deben tener signos opuestos.\n  f({}) = {}\n  f({}) = {}".format(a, fa, b, fb)

    lineas = []
    lineas.append("{:<6} {:<14} {:<14} {:<14} {:<14} {:<14} {:<14}".format(
        "Iter", "a", "b", "c (medio)", "f(a)", "f(c)", "Error"))
    lineas.append("-" * 90)

    for i in range(1, max_iter + 1):
        c = (a + b) / 2
        fc = f(c)
        error = abs(b - a) / 2

        lineas.append("{:<6} {:<14.8f} {:<14.8f} {:<14.8f} {:<14.8f} {:<14.8f} {:<14.8f}".format(
            i, a, b, c, fa, fc, error))

        if abs(fc) < 1e-15 or error < tolerancia:
            lineas.append("\nRaiz aproximada: {:.10f}".format(c))
            lineas.append("Iteraciones: {}".format(i))
            lineas.append("f(c) = {:.2e}".format(fc))
            return c, "\n".join(lineas)

        if fa * fc < 0:
            b = c
            fb = fc
        else:
            a = c
            fa = fc

    lineas.append("\nNo convergio en el numero maximo de iteraciones")
    return None, "\n".join(lineas)


def metodo_newton_raphson(f_str, df_str, x0, tolerancia=1e-6, max_iter=100):
    """Newton-Raphson. Retorna (raiz, texto_tabla)"""
    f_str = preparar_funcion(f_str)
    df_str = preparar_funcion(df_str)

    def f(val):
        return evaluar_funcion(f_str, val)

    def df(val):
        return evaluar_funcion(df_str, val)

    x = x0
    lineas = []
    lineas.append("{:<6} {:<16} {:<16} {:<16} {:<16} {:<16} {:<16}".format(
        "Iter", "x_n", "x_n+1", "f(x_n)", "f'(x_n)", "Err Abs", "Err Rel"))
    lineas.append("-" * 105)

    for i in range(1, max_iter + 1):
        fx = f(x)
        dfx = df(x)

        if dfx == 0:
            lineas.append("\nDerivada cero, no se puede continuar.")
            return None, "\n".join(lineas)

        x_nuevo = x - fx / dfx
        error_abs = abs(x_nuevo - x)
        error_rel = error_abs / abs(x_nuevo) if x_nuevo != 0 else 0

        lineas.append("{:<6} {:<16.8f} {:<16.8f} {:<16.8f} {:<16.8f} {:<16.8f} {:<16.8f}".format(
            i, x, x_nuevo, fx, dfx, error_abs, error_rel))

        if error_abs < tolerancia:
            lineas.append("\nRaiz aproximada: {:.10f}".format(x_nuevo))
            lineas.append("Iteraciones: {}".format(i))
            return x_nuevo, "\n".join(lineas)

        x = x_nuevo

    lineas.append("\nNo convergio en el numero maximo de iteraciones")
    return None, "\n".join(lineas)


def metodo_punto_fijo(g_str, x0, tolerancia=1e-6, max_iter=100):
    """Punto fijo. Retorna (raiz, texto_tabla)"""
    g_str = preparar_funcion(g_str)

    def g(val):
        return evaluar_funcion(g_str, val)

    x = x0
    lineas = []
    lineas.append("{:<8} {:<20} {:<20} {:<20}".format(
        "Iter", "x_n", "x_n+1 = g(x_n)", "Error"))
    lineas.append("-" * 70)

    for i in range(1, max_iter + 1):
        try:
            x_nuevo = g(x)
        except Exception as ex:
            lineas.append("\nError al evaluar g({}): {}".format(x, ex))
            return None, "\n".join(lineas)

        error = abs(x_nuevo - x)
        lineas.append("{:<8} {:<20.10f} {:<20.10f} {:<20.10f}".format(
            i, x, x_nuevo, error))

        if error < tolerancia:
            lineas.append("\nRaiz aproximada: {:.10f}".format(x_nuevo))
            lineas.append("Iteraciones: {}".format(i))
            return x_nuevo, "\n".join(lineas)

        x = x_nuevo

    lineas.append("\nNo convergio en el numero maximo de iteraciones")
    return None, "\n".join(lineas)


def metodo_lagrange(puntos):
    """Lagrange para n puntos. Retorna (polinomio_sympy, latex_str, texto)"""
    x = symbols('x')
    n = len(puntos)

    coords = [(Rational(p[0]), Rational(p[1])) for p in puntos]

    P = 0
    for i in range(n):
        xi, yi = coords[i]
        Li = 1
        for j in range(n):
            if j != i:
                xj = coords[j][0]
                Li *= (x - xj) / (xi - xj)
        P += yi * Li

    P_simplificado = expand(simplify(P))
    P_latex = latex(P_simplificado)

    lineas = []
    lineas.append("Puntos ingresados:")
    for idx, (xi, yi) in enumerate(coords):
        lineas.append("  P{} = ({}, {})".format(idx, xi, yi))
    lineas.append("")
    lineas.append("Polinomios base de Lagrange:")
    for i in range(n):
        xi, yi = coords[i]
        Li = 1
        for j in range(n):
            if j != i:
                xj = coords[j][0]
                Li *= (x - xj) / (xi - xj)
        Li_exp = expand(simplify(Li))
        lineas.append("  L{}(x) = {}".format(i, Li_exp))
    lineas.append("")
    lineas.append("Polinomio interpolante P(x) = {}".format(P_simplificado))
    lineas.append("\nLaTeX: {}".format(P_latex))

    return P_simplificado, P_latex, "\n".join(lineas)


# ==================================================================
#  APLICACION PRINCIPAL
# ==================================================================

class App(ctk.CTk):
    def __init__(self):
        super().__init__()
        self.title("Metodos Numericos - Modelado y Simulacion")
        self.geometry("1050x700")
        self.minsize(900, 600)

        # -- Layout principal --
        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)

        # ======= SIDEBAR =======
        self.sidebar = ctk.CTkFrame(self, width=220, corner_radius=0)
        self.sidebar.grid(row=0, column=0, sticky="nsew")
        self.sidebar.grid_rowconfigure(7, weight=1)

        self.logo_label = ctk.CTkLabel(
            self.sidebar, text="Metodos\nNumericos",
            font=ctk.CTkFont(size=22, weight="bold")
        )
        self.logo_label.grid(row=0, column=0, padx=20, pady=(25, 5))

        self.subtitle = ctk.CTkLabel(
            self.sidebar, text="Modelado y Simulacion",
            font=ctk.CTkFont(size=12), text_color="gray"
        )
        self.subtitle.grid(row=1, column=0, padx=20, pady=(0, 25))

        # Botones del sidebar
        btn_style = {"font": ctk.CTkFont(size=14), "height": 40, "corner_radius": 8}

        self.btn_biseccion = ctk.CTkButton(
            self.sidebar, text="Biseccion",
            command=self.mostrar_biseccion, **btn_style
        )
        self.btn_biseccion.grid(row=2, column=0, padx=15, pady=6, sticky="ew")

        self.btn_newton = ctk.CTkButton(
            self.sidebar, text="Newton-Raphson",
            command=self.mostrar_newton, **btn_style
        )
        self.btn_newton.grid(row=3, column=0, padx=15, pady=6, sticky="ew")

        self.btn_punto_fijo = ctk.CTkButton(
            self.sidebar, text="Punto Fijo",
            command=self.mostrar_punto_fijo, **btn_style
        )
        self.btn_punto_fijo.grid(row=4, column=0, padx=15, pady=6, sticky="ew")

        self.btn_lagrange = ctk.CTkButton(
            self.sidebar, text="Lagrange",
            command=self.mostrar_lagrange, **btn_style
        )
        self.btn_lagrange.grid(row=5, column=0, padx=15, pady=6, sticky="ew")

        # Tema
        self.tema_label = ctk.CTkLabel(self.sidebar, text="Apariencia:", anchor="w")
        self.tema_label.grid(row=8, column=0, padx=20, pady=(10, 0))
        self.tema_menu = ctk.CTkOptionMenu(
            self.sidebar, values=["Dark", "Light", "System"],
            command=self.cambiar_tema
        )
        self.tema_menu.grid(row=9, column=0, padx=20, pady=(5, 20), sticky="ew")

        # ======= MAIN FRAME =======
        self.main_frame = ctk.CTkFrame(self, corner_radius=10)
        self.main_frame.grid(row=0, column=1, padx=15, pady=15, sticky="nsew")
        self.main_frame.grid_columnconfigure(0, weight=1)
        self.main_frame.grid_rowconfigure(2, weight=1)

        # Titulo del metodo actual
        self.titulo_metodo = ctk.CTkLabel(
            self.main_frame, text="Seleccione un metodo numerico",
            font=ctk.CTkFont(size=20, weight="bold")
        )
        self.titulo_metodo.grid(row=0, column=0, padx=20, pady=(15, 5), sticky="w")

        # Frame de inputs (dinamico)
        self.input_frame = ctk.CTkFrame(self.main_frame)
        self.input_frame.grid(row=1, column=0, padx=15, pady=10, sticky="ew")
        self.input_frame.grid_columnconfigure(1, weight=1)

        # Area de resultados
        self.resultado_text = ctk.CTkTextbox(
            self.main_frame, font=ctk.CTkFont(family="Consolas", size=13),
            wrap="none"
        )
        self.resultado_text.grid(row=2, column=0, padx=15, pady=(5, 15), sticky="nsew")
        self.resultado_text.insert("1.0",
            "  Bienvenido al sistema de Metodos Numericos.\n\n"
            "  Seleccione un metodo del panel izquierdo para comenzar.\n\n"
            "  Metodos disponibles:\n"
            "    - Biseccion: Encontrar raices en un intervalo [a, b]\n"
            "    - Newton-Raphson: Encontrar raices con derivada\n"
            "    - Punto Fijo: Iteracion de punto fijo x = g(x)\n"
            "    - Lagrange: Interpolacion polinomica con n puntos\n\n"
            "  --- Funciones soportadas ---\n"
            "    sin(x), cos(x), tan(x), exp(x), log(x), ln(x),\n"
            "    sqrt(x), abs(x), pi, e\n"
            "    Potencias: x**2  o  x^2\n"
            "    No necesita escribir 'math.' delante.\n"
        )
        self.resultado_text.configure(state="disabled")

        # Referencia a entries de Lagrange
        self.lagrange_entries = []
        self.num_puntos_lagrange = 3

    # --- Utilidades ---
    def cambiar_tema(self, modo):
        ctk.set_appearance_mode(modo)

    def limpiar_inputs(self):
        for widget in self.input_frame.winfo_children():
            widget.destroy()
        self.lagrange_entries = []

    def mostrar_resultado(self, texto):
        self.resultado_text.configure(state="normal")
        self.resultado_text.delete("1.0", "end")
        self.resultado_text.insert("1.0", texto)
        self.resultado_text.configure(state="disabled")

    def crear_entry(self, frame, row, label_text, placeholder, col_label=0, col_entry=1, width=280):
        label = ctk.CTkLabel(frame, text=label_text, font=ctk.CTkFont(size=13))
        label.grid(row=row, column=col_label, padx=(10, 5), pady=6, sticky="e")
        entry = ctk.CTkEntry(frame, placeholder_text=placeholder, width=width,
                             font=ctk.CTkFont(size=13))
        entry.grid(row=row, column=col_entry, padx=(5, 10), pady=6, sticky="w")
        return entry

    def crear_boton_calcular(self, frame, row, command, colspan=2):
        btn = ctk.CTkButton(
            frame, text="Calcular", command=command,
            font=ctk.CTkFont(size=14, weight="bold"),
            height=38, corner_radius=8, fg_color="#2563EB", hover_color="#1D4ED8"
        )
        btn.grid(row=row, column=0, columnspan=colspan, pady=15, padx=10)

    # ======================================
    #  BISECCION
    # ======================================
    def mostrar_biseccion(self):
        self.limpiar_inputs()
        self.titulo_metodo.configure(text="Metodo de Biseccion")

        self.entry_func_bis = self.crear_entry(
            self.input_frame, 0, "f(x) =", "Ej: x^3 - x - 2")
        self.entry_a = self.crear_entry(
            self.input_frame, 1, "a =", "Limite inferior (ej: 1)")
        self.entry_b = self.crear_entry(
            self.input_frame, 2, "b =", "Limite superior (ej: 2)")
        self.entry_tol_bis = self.crear_entry(
            self.input_frame, 3, "Tolerancia =", "Ej: 0.001")
        self.entry_iter_bis = self.crear_entry(
            self.input_frame, 4, "Max iter =", "100")

        self.crear_boton_calcular(self.input_frame, 5, self.ejecutar_biseccion)

    def ejecutar_biseccion(self):
        try:
            f_str = self.entry_func_bis.get().strip()
            if not f_str:
                messagebox.showwarning("Advertencia", "Debe ingresar f(x)")
                return
            a = float(self.entry_a.get())
            b = float(self.entry_b.get())
            tol_str = self.entry_tol_bis.get().strip()
            tol = float(tol_str) if tol_str else 1e-3
            iter_str = self.entry_iter_bis.get().strip()
            max_iter = int(iter_str) if iter_str else 100

            raiz, texto = metodo_biseccion(f_str, a, b, tol, max_iter)
            self.mostrar_resultado(texto)
        except Exception as e:
            f_procesada = preparar_funcion(f_str) if f_str else ""
            messagebox.showerror("Error",
                "Error al ejecutar Biseccion:\n{}\n\n"
                "f(x) procesada: {}".format(e, f_procesada))

    # ======================================
    #  NEWTON-RAPHSON
    # ======================================
    def mostrar_newton(self):
        self.limpiar_inputs()
        self.titulo_metodo.configure(text="Metodo de Newton-Raphson")

        self.entry_func_nr = self.crear_entry(
            self.input_frame, 0, "f(x) =", "Ej: exp(x) + x^2 - 4")
        self.entry_dfunc_nr = self.crear_entry(
            self.input_frame, 1, "f'(x) =", "Ej: exp(x) + 2*x")
        self.entry_x0_nr = self.crear_entry(
            self.input_frame, 2, "x0 =", "Valor inicial (ej: 0.5)")
        self.entry_tol_nr = self.crear_entry(
            self.input_frame, 3, "Tolerancia =", "Ej: 0.000001")
        self.entry_iter_nr = self.crear_entry(
            self.input_frame, 4, "Max iter =", "100")

        self.crear_boton_calcular(self.input_frame, 5, self.ejecutar_newton)

    def ejecutar_newton(self):
        try:
            f_str = self.entry_func_nr.get().strip()
            df_str = self.entry_dfunc_nr.get().strip()

            if not f_str or not df_str:
                messagebox.showwarning("Advertencia", "Debe ingresar f(x) y f'(x)")
                return

            x0_str = self.entry_x0_nr.get().strip()
            if not x0_str:
                messagebox.showwarning("Advertencia", "Debe ingresar el valor inicial x0")
                return

            x0 = float(x0_str)
            tol_str = self.entry_tol_nr.get().strip()
            tol = float(tol_str) if tol_str else 1e-6
            iter_str = self.entry_iter_nr.get().strip()
            max_iter = int(iter_str) if iter_str else 100

            raiz, texto = metodo_newton_raphson(f_str, df_str, x0, tol, max_iter)
            self.mostrar_resultado(texto)
        except Exception as e:
            # Mostrar la funcion procesada para ayudar a depurar
            f_procesada = preparar_funcion(f_str) if f_str else ""
            df_procesada = preparar_funcion(df_str) if df_str else ""
            messagebox.showerror("Error",
                "Error al ejecutar Newton-Raphson:\n{}\n\n"
                "f(x) procesada: {}\n"
                "f'(x) procesada: {}".format(e, f_procesada, df_procesada))

    # ======================================
    #  PUNTO FIJO
    # ======================================
    def mostrar_punto_fijo(self):
        self.limpiar_inputs()
        self.titulo_metodo.configure(text="Metodo de Punto Fijo")

        self.entry_func_pf = self.crear_entry(
            self.input_frame, 0, "g(x) =", "Ej: cos(x)")
        self.entry_x0_pf = self.crear_entry(
            self.input_frame, 1, "x0 =", "Valor inicial (ej: 1)")
        self.entry_tol_pf = self.crear_entry(
            self.input_frame, 2, "Tolerancia =", "Ej: 0.000001")
        self.entry_iter_pf = self.crear_entry(
            self.input_frame, 3, "Max iter =", "100")

        self.crear_boton_calcular(self.input_frame, 4, self.ejecutar_punto_fijo)

    def ejecutar_punto_fijo(self):
        try:
            g_str = self.entry_func_pf.get().strip()
            x0 = float(self.entry_x0_pf.get())
            tol_str = self.entry_tol_pf.get().strip()
            tol = float(tol_str) if tol_str else 1e-6
            iter_str = self.entry_iter_pf.get().strip()
            max_iter = int(iter_str) if iter_str else 100

            raiz, texto = metodo_punto_fijo(g_str, x0, tol, max_iter)
            self.mostrar_resultado(texto)
        except Exception as e:
            messagebox.showerror("Error", "Error al ejecutar Punto Fijo:\n{}".format(e))

    # ======================================
    #  LAGRANGE
    # ======================================
    def mostrar_lagrange(self):
        self.limpiar_inputs()
        self.titulo_metodo.configure(text="Interpolacion de Lagrange")

        # Selector de cantidad de puntos
        top_frame = ctk.CTkFrame(self.input_frame, fg_color="transparent")
        top_frame.grid(row=0, column=0, columnspan=4, pady=(5, 10), sticky="w", padx=10)

        ctk.CTkLabel(top_frame, text="Cantidad de puntos:", font=ctk.CTkFont(size=13)).pack(side="left", padx=(0, 10))
        self.spin_puntos = ctk.CTkOptionMenu(
            top_frame, values=["2", "3", "4", "5", "6", "7"],
            command=self.actualizar_puntos_lagrange, width=70
        )
        self.spin_puntos.set("3")
        self.spin_puntos.pack(side="left")

        # Frame para los puntos
        self.puntos_frame = ctk.CTkFrame(self.input_frame, fg_color="transparent")
        self.puntos_frame.grid(row=1, column=0, columnspan=4, sticky="ew", padx=10)

        self._crear_entries_lagrange(3)

        self.crear_boton_calcular(self.input_frame, 2, self.ejecutar_lagrange, colspan=4)

    def actualizar_puntos_lagrange(self, valor):
        n = int(valor)
        self.num_puntos_lagrange = n
        for widget in self.puntos_frame.winfo_children():
            widget.destroy()
        self.lagrange_entries = []
        self._crear_entries_lagrange(n)

    def _crear_entries_lagrange(self, n):
        self.lagrange_entries = []
        for i in range(n):
            ctk.CTkLabel(self.puntos_frame, text="P{}:".format(i),
                         font=ctk.CTkFont(size=13, weight="bold")).grid(
                row=i, column=0, padx=(5, 3), pady=4, sticky="e")
            ctk.CTkLabel(self.puntos_frame, text="x =", font=ctk.CTkFont(size=13)).grid(
                row=i, column=1, padx=(3, 2), pady=4)
            ex = ctk.CTkEntry(self.puntos_frame, width=90, placeholder_text="x",
                              font=ctk.CTkFont(size=13))
            ex.grid(row=i, column=2, padx=3, pady=4)
            ctk.CTkLabel(self.puntos_frame, text="y =", font=ctk.CTkFont(size=13)).grid(
                row=i, column=3, padx=(10, 2), pady=4)
            ey = ctk.CTkEntry(self.puntos_frame, width=90, placeholder_text="y",
                              font=ctk.CTkFont(size=13))
            ey.grid(row=i, column=4, padx=3, pady=4)
            self.lagrange_entries.append((ex, ey))

    def ejecutar_lagrange(self):
        try:
            puntos = []
            for i, (ex, ey) in enumerate(self.lagrange_entries):
                xi = ex.get().strip()
                yi = ey.get().strip()
                if not xi or not yi:
                    messagebox.showwarning("Advertencia",
                                           "Complete los valores del punto P{}".format(i))
                    return
                puntos.append((float(xi), float(yi)))

            poly, poly_latex, texto = metodo_lagrange(puntos)
            self.mostrar_resultado(texto)
        except Exception as e:
            messagebox.showerror("Error", "Error al ejecutar Lagrange:\n{}".format(e))


# ==================================================================
#  MAIN
# ==================================================================
if __name__ == "__main__":
    app = App()
    app.mainloop()
