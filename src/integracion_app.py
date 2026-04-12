import customtkinter as ctk
from tkinter import messagebox
import math
import re
import numpy as np

import matplotlib
matplotlib.use("TkAgg")
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg, NavigationToolbar2Tk
from matplotlib.figure import Figure

# --- Configuracion de apariencia ---
ctk.set_appearance_mode("dark")
ctk.set_default_color_theme("blue") //to do agregar cuadratura de gauss.


# ==================================================================
#  PREPROCESAMIENTO DE FUNCIONES
# ==================================================================

def preparar_funcion(f_str):
    """
    Preprocesa la cadena de la funcion para que sea evaluable.
    Acepta notacion natural: sin(x), cos(x), exp(x), ln(x), sqrt(x), pi, e
    Acepta ^ como potencia.
    Soporta multiplicacion implicita: 2x -> 2*x, 3sin(x) -> 3*sin(x)
    """
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
    "pi": math.pi, "e": math.e,
    "math": math,
}


def evaluar_funcion(f_str, x_val):
    """Evalua f(x) preprocesada en x_val."""
    return float(eval(f_str, {**_EVAL_NS, "x": x_val}))


def evaluar_funcion_array(f_str, x_arr):
    """Evalua f(x) preprocesada en un array numpy."""
    return np.array([evaluar_funcion(f_str, float(xi)) for xi in x_arr])


# ==================================================================
#  METODOS DE INTEGRACION NUMERICA  (Newton-Cotes compuestas)
# ==================================================================

def trapecio_compuesto(f_str, a, b, n):
    """
    Regla del Trapecio Compuesta.
    n = numero de subintervalos (>= 1).
    Retorna (resultado, h, puntos_x, puntos_y, tabla_texto).
    """
    f_str = preparar_funcion(f_str)
    h = (b - a) / n
    xs = [a + i * h for i in range(n + 1)]
    ys = [evaluar_funcion(f_str, xi) for xi in xs]

    suma = ys[0] + ys[-1]
    for i in range(1, n):
        suma += 2 * ys[i]
    resultado = (h / 2) * suma

    # Tabla
    lineas = []
    lineas.append("REGLA DEL TRAPECIO COMPUESTA")
    lineas.append("=" * 60)
    lineas.append("  Intervalo: [{}, {}]".format(a, b))
    lineas.append("  n (subintervalos): {}".format(n))
    lineas.append("  h = (b - a) / n = {:.10f}".format(h))
    lineas.append("")
    lineas.append("  {:<6} {:<20} {:<20} {:<12}".format("i", "x_i", "f(x_i)", "Coef"))
    lineas.append("  " + "-" * 58)
    for i in range(n + 1):
        coef = 1 if (i == 0 or i == n) else 2
        lineas.append("  {:<6} {:<20.10f} {:<20.10f} {:<12}".format(i, xs[i], ys[i], coef))

    lineas.append("")
    lineas.append("  Integral ≈ (h/2) * [f(x0) + 2*Σf(xi) + f(xn)]")
    lineas.append("  Integral ≈ {:.10f}".format(resultado))

    # Error estimado: |E| <= (b-a)^3 / (12*n^2) * max|f''(x)|
    lineas.append("")
    lineas.append("  Error de truncamiento:")
    lineas.append("    |E| <= (b-a)^3 / (12*n^2) * max|f''(x)|")
    lineas.append("    Cota sin f'': (b-a)^3 / (12*n^2) = {:.2e}".format(
        (b - a)**3 / (12 * n**2)))

    return resultado, h, xs, ys, "\n".join(lineas)


def simpson_1_3_compuesto(f_str, a, b, n):
    """
    Regla de Simpson 1/3 Compuesta.
    n = numero de subintervalos (debe ser PAR, >= 2).
    """
    if n % 2 != 0:
        return None, 0, [], [], "Error: n debe ser PAR para Simpson 1/3. Se ingreso n = {}".format(n)

    f_str = preparar_funcion(f_str)
    h = (b - a) / n
    xs = [a + i * h for i in range(n + 1)]
    ys = [evaluar_funcion(f_str, xi) for xi in xs]

    suma = ys[0] + ys[-1]
    for i in range(1, n):
        if i % 2 != 0:
            suma += 4 * ys[i]
        else:
            suma += 2 * ys[i]
    resultado = (h / 3) * suma

    lineas = []
    lineas.append("REGLA DE SIMPSON 1/3 COMPUESTA")
    lineas.append("=" * 60)
    lineas.append("  Intervalo: [{}, {}]".format(a, b))
    lineas.append("  n (subintervalos, par): {}".format(n))
    lineas.append("  h = (b - a) / n = {:.10f}".format(h))
    lineas.append("")
    lineas.append("  {:<6} {:<20} {:<20} {:<12}".format("i", "x_i", "f(x_i)", "Coef"))
    lineas.append("  " + "-" * 58)
    for i in range(n + 1):
        if i == 0 or i == n:
            coef = 1
        elif i % 2 != 0:
            coef = 4
        else:
            coef = 2
        lineas.append("  {:<6} {:<20.10f} {:<20.10f} {:<12}".format(i, xs[i], ys[i], coef))

    lineas.append("")
    lineas.append("  Integral ≈ (h/3) * [f(x0) + 4*Σf(x_impar) + 2*Σf(x_par) + f(xn)]")
    lineas.append("  Integral ≈ {:.10f}".format(resultado))

    lineas.append("")
    lineas.append("  Error de truncamiento:")
    lineas.append("    |E| <= (b-a)^5 / (180*n^4) * max|f⁴(x)|")
    lineas.append("    Cota sin f⁴: (b-a)^5 / (180*n^4) = {:.2e}".format(
        (b - a)**5 / (180 * n**4)))

    return resultado, h, xs, ys, "\n".join(lineas)


def simpson_3_8_compuesto(f_str, a, b, n):
    """
    Regla de Simpson 3/8 Compuesta.
    n = numero de subintervalos (debe ser MULTIPLO de 3, >= 3).
    """
    if n % 3 != 0:
        return None, 0, [], [], "Error: n debe ser MULTIPLO DE 3 para Simpson 3/8. Se ingreso n = {}".format(n)

    f_str = preparar_funcion(f_str)
    h = (b - a) / n
    xs = [a + i * h for i in range(n + 1)]
    ys = [evaluar_funcion(f_str, xi) for xi in xs]

    suma = ys[0] + ys[-1]
    for i in range(1, n):
        if i % 3 == 0:
            suma += 2 * ys[i]
        else:
            suma += 3 * ys[i]
    resultado = (3 * h / 8) * suma

    lineas = []
    lineas.append("REGLA DE SIMPSON 3/8 COMPUESTA")
    lineas.append("=" * 60)
    lineas.append("  Intervalo: [{}, {}]".format(a, b))
    lineas.append("  n (subintervalos, multiplo de 3): {}".format(n))
    lineas.append("  h = (b - a) / n = {:.10f}".format(h))
    lineas.append("")
    lineas.append("  {:<6} {:<20} {:<20} {:<12}".format("i", "x_i", "f(x_i)", "Coef"))
    lineas.append("  " + "-" * 58)
    for i in range(n + 1):
        if i == 0 or i == n:
            coef = 1
        elif i % 3 == 0:
            coef = 2
        else:
            coef = 3
        lineas.append("  {:<6} {:<20.10f} {:<20.10f} {:<12}".format(i, xs[i], ys[i], coef))

    lineas.append("")
    lineas.append("  Integral ≈ (3h/8) * [f(x0) + 3*Σf(x_no_mult3) + 2*Σf(x_mult3) + f(xn)]")
    lineas.append("  Integral ≈ {:.10f}".format(resultado))

    lineas.append("")
    lineas.append("  Error de truncamiento:")
    lineas.append("    |E| <= (b-a)^5 / (80*n^4) * max|f⁴(x)|")
    lineas.append("    Cota sin f⁴: (b-a)^5 / (80*n^4) = {:.2e}".format(
        (b - a)**5 / (80 * n**4)))

    return resultado, h, xs, ys, "\n".join(lineas)


# ==================================================================
#  APLICACION PRINCIPAL
# ==================================================================

class IntegracionApp(ctk.CTk):
    def __init__(self):
        super().__init__()
        self.title("Integracion Numerica - Newton-Cotes")
        self.geometry("1200x780")
        self.minsize(1050, 650)

        # Layout principal
        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)

        # ======= SIDEBAR =======
        self.sidebar = ctk.CTkFrame(self, width=230, corner_radius=0)
        self.sidebar.grid(row=0, column=0, sticky="nsew")
        self.sidebar.grid_rowconfigure(6, weight=1)

        self.logo_label = ctk.CTkLabel(
            self.sidebar, text="Integracion\nNumerica",
            font=ctk.CTkFont(size=22, weight="bold")
        )
        self.logo_label.grid(row=0, column=0, padx=20, pady=(25, 5))

        self.subtitle = ctk.CTkLabel(
            self.sidebar, text="Newton-Cotes Compuestas",
            font=ctk.CTkFont(size=12), text_color="gray"
        )
        self.subtitle.grid(row=1, column=0, padx=20, pady=(0, 25))

        btn_style = {"font": ctk.CTkFont(size=14), "height": 42, "corner_radius": 8}

        self.btn_trapecio = ctk.CTkButton(
            self.sidebar, text="Trapecio Compuesto",
            command=self.mostrar_trapecio, **btn_style
        )
        self.btn_trapecio.grid(row=2, column=0, padx=15, pady=7, sticky="ew")

        self.btn_simpson13 = ctk.CTkButton(
            self.sidebar, text="Simpson 1/3 Compuesto",
            command=self.mostrar_simpson13, **btn_style
        )
        self.btn_simpson13.grid(row=3, column=0, padx=15, pady=7, sticky="ew")

        self.btn_simpson38 = ctk.CTkButton(
            self.sidebar, text="Simpson 3/8 Compuesto",
            command=self.mostrar_simpson38, **btn_style
        )
        self.btn_simpson38.grid(row=4, column=0, padx=15, pady=7, sticky="ew")

        # Tema
        self.tema_label = ctk.CTkLabel(self.sidebar, text="Apariencia:", anchor="w")
        self.tema_label.grid(row=7, column=0, padx=20, pady=(10, 0))
        self.tema_menu = ctk.CTkOptionMenu(
            self.sidebar, values=["Dark", "Light", "System"],
            command=self.cambiar_tema
        )
        self.tema_menu.grid(row=8, column=0, padx=20, pady=(5, 20), sticky="ew")

        # ======= MAIN FRAME =======
        self.main_frame = ctk.CTkFrame(self, corner_radius=10)
        self.main_frame.grid(row=0, column=1, padx=15, pady=15, sticky="nsew")
        self.main_frame.grid_columnconfigure(0, weight=1)
        self.main_frame.grid_rowconfigure(2, weight=1)

        # Titulo del metodo actual
        self.titulo_metodo = ctk.CTkLabel(
            self.main_frame, text="Seleccione un metodo de integracion",
            font=ctk.CTkFont(size=20, weight="bold")
        )
        self.titulo_metodo.grid(row=0, column=0, padx=20, pady=(15, 5), sticky="w")

        # Frame de inputs
        self.input_frame = ctk.CTkFrame(self.main_frame)
        self.input_frame.grid(row=1, column=0, padx=15, pady=10, sticky="ew")
        self.input_frame.grid_columnconfigure(1, weight=1)

        # Frame inferior: resultados (izq) + grafico (der)
        self.bottom_frame = ctk.CTkFrame(self.main_frame, fg_color="transparent")
        self.bottom_frame.grid(row=2, column=0, padx=10, pady=(5, 15), sticky="nsew")
        self.bottom_frame.grid_columnconfigure(0, weight=1)
        self.bottom_frame.grid_columnconfigure(1, weight=1)
        self.bottom_frame.grid_rowconfigure(0, weight=1)

        # Area de resultados (texto)
        self.resultado_text = ctk.CTkTextbox(
            self.bottom_frame, font=ctk.CTkFont(family="Consolas", size=12),
            wrap="none"
        )
        self.resultado_text.grid(row=0, column=0, padx=(5, 5), pady=5, sticky="nsew")
        self.resultado_text.insert("1.0",
            "  Bienvenido al sistema de Integracion Numerica.\n\n"
            "  Seleccione un metodo del panel izquierdo.\n\n"
            "  Metodos disponibles:\n"
            "    - Trapecio Compuesto (n >= 1)\n"
            "    - Simpson 1/3 Compuesto (n par, >= 2)\n"
            "    - Simpson 3/8 Compuesto (n multiplo de 3, >= 3)\n\n"
            "  --- Funciones soportadas ---\n"
            "    sin(x), cos(x), tan(x), exp(x), log(x), ln(x),\n"
            "    sqrt(x), abs(x), pi, e\n"
            "    Potencias: x**2  o  x^2\n"
            "    Multiplicacion implicita: 2x, 3sin(x)\n"
            "    No necesita escribir 'math.' delante.\n"
        )
        self.resultado_text.configure(state="disabled")

        # Frame del grafico
        self.grafico_frame = ctk.CTkFrame(self.bottom_frame)
        self.grafico_frame.grid(row=0, column=1, padx=(5, 5), pady=5, sticky="nsew")

        # Crear figura de matplotlib
        self.fig = Figure(figsize=(5, 4), dpi=100)
        self.fig.patch.set_facecolor('#2b2b2b')
        self.ax = self.fig.add_subplot(111)
        self._estilo_grafico()

        self.canvas = FigureCanvasTkAgg(self.fig, master=self.grafico_frame)
        self.canvas.draw()
        self.canvas.get_tk_widget().pack(fill="both", expand=True, padx=5, pady=5)

        # Toolbar de navegacion
        self.toolbar_frame = ctk.CTkFrame(self.grafico_frame, height=35, fg_color="transparent")
        self.toolbar_frame.pack(fill="x", padx=5)
        self.toolbar = NavigationToolbar2Tk(self.canvas, self.toolbar_frame)
        self.toolbar.update()

        # Metodo activo (para saber cual ejecutar)
        self.metodo_activo = None

    # --- Utilidades ---
    def cambiar_tema(self, modo):
        ctk.set_appearance_mode(modo)

    def _estilo_grafico(self):
        self.ax.set_facecolor('#1e1e1e')
        self.ax.tick_params(colors='white')
        self.ax.xaxis.label.set_color('white')
        self.ax.yaxis.label.set_color('white')
        self.ax.title.set_color('white')
        for spine in self.ax.spines.values():
            spine.set_color('#555555')
        self.ax.grid(True, alpha=0.3, color='gray')

    def limpiar_inputs(self):
        for widget in self.input_frame.winfo_children():
            widget.destroy()

    def mostrar_resultado(self, texto):
        self.resultado_text.configure(state="normal")
        self.resultado_text.delete("1.0", "end")
        self.resultado_text.insert("1.0", texto)
        self.resultado_text.configure(state="disabled")

    def crear_entry(self, frame, row, label_text, placeholder, col_label=0, col_entry=1, width=300):
        label = ctk.CTkLabel(frame, text=label_text, font=ctk.CTkFont(size=13))
        label.grid(row=row, column=col_label, padx=(10, 5), pady=6, sticky="e")
        entry = ctk.CTkEntry(frame, placeholder_text=placeholder, width=width,
                             font=ctk.CTkFont(size=13))
        entry.grid(row=row, column=col_entry, padx=(5, 10), pady=6, sticky="w")
        return entry

    def crear_boton_calcular(self, frame, row, command, colspan=2):
        btn = ctk.CTkButton(
            frame, text="Calcular Integral", command=command,
            font=ctk.CTkFont(size=14, weight="bold"),
            height=40, corner_radius=8, fg_color="#2563EB", hover_color="#1D4ED8"
        )
        btn.grid(row=row, column=0, columnspan=colspan, pady=15, padx=10)

    # ===========================================================
    #  GRAFICO
    # ===========================================================
    def graficar_integracion(self, f_str_original, a, b, xs, ys, metodo_nombre):
        """Dibuja la funcion, los puntos y el area aproximada."""
        self.ax.clear()
        self._estilo_grafico()

        f_prep = preparar_funcion(f_str_original)

        # Curva suave
        x_plot = np.linspace(a - 0.5, b + 0.5, 500)
        try:
            y_plot = evaluar_funcion_array(f_prep, x_plot)
        except Exception:
            x_plot = np.linspace(a, b, 500)
            y_plot = evaluar_funcion_array(f_prep, x_plot)

        self.ax.plot(x_plot, y_plot, color='#00BFFF', linewidth=2, label='f(x)', zorder=5)

        # Area con trapecios / paneles rellenos
        xs_arr = np.array(xs)
        ys_arr = np.array(ys)
        self.ax.fill_between(xs_arr, 0, ys_arr, alpha=0.25, color='#00FF7F',
                             step=None, label='Area aproximada')
        # Dibujar los segmentos del metodo
        for i in range(len(xs) - 1):
            x_seg = np.array([xs[i], xs[i + 1]])
            y_seg = np.array([ys[i], ys[i + 1]])
            # Relleno trapecio
            self.ax.fill(
                [xs[i], xs[i], xs[i + 1], xs[i + 1]],
                [0, ys[i], ys[i + 1], 0],
                alpha=0.15, color='#FFD700', edgecolor='#FFD700', linewidth=0.8
            )
            # Linea superior del trapecio
            self.ax.plot(x_seg, y_seg, color='#FFD700', linewidth=1, alpha=0.7)

        # Puntos
        self.ax.scatter(xs_arr, ys_arr, color='#FF4500', s=30, zorder=6, label='Nodos')

        # Lineas verticales en los nodos
        for xi, yi in zip(xs, ys):
            self.ax.plot([xi, xi], [0, yi], color='#FFD700', linewidth=0.7, alpha=0.5, linestyle='--')

        # Eje x = 0
        self.ax.axhline(y=0, color='white', linewidth=0.5, alpha=0.5)

        self.ax.set_xlabel("x", fontsize=11)
        self.ax.set_ylabel("f(x)", fontsize=11)
        self.ax.set_title(metodo_nombre, fontsize=13, fontweight='bold')
        self.ax.legend(fontsize=9, loc='best', facecolor='#2b2b2b', edgecolor='#555555',
                       labelcolor='white')

        self.fig.tight_layout()
        self.canvas.draw()

    # ===========================================================
    #  CREAR FORMULARIO COMUN
    # ===========================================================
    def _crear_formulario(self, titulo, restriccion_n):
        self.limpiar_inputs()
        self.titulo_metodo.configure(text=titulo)

        self.entry_func = self.crear_entry(
            self.input_frame, 0, "f(x) =", "Ej: sin(x), x^3 - 2x + 1, exp(-x^2)")
        self.entry_a = self.crear_entry(
            self.input_frame, 1, "a (inf) =", "Limite inferior (ej: 0)")
        self.entry_b = self.crear_entry(
            self.input_frame, 2, "b (sup) =", "Limite superior (ej: 3.14159)")
        self.entry_n = self.crear_entry(
            self.input_frame, 3, "n (subint) =", restriccion_n)

        self.crear_boton_calcular(self.input_frame, 4, self.ejecutar_metodo)

    # ===========================================================
    #  MOSTRAR METODOS
    # ===========================================================
    def mostrar_trapecio(self):
        self.metodo_activo = "trapecio"
        self._crear_formulario("Regla del Trapecio Compuesto", "Entero >= 1 (ej: 10)")

    def mostrar_simpson13(self):
        self.metodo_activo = "simpson13"
        self._crear_formulario("Regla de Simpson 1/3 Compuesto", "Entero PAR >= 2 (ej: 10)")

    def mostrar_simpson38(self):
        self.metodo_activo = "simpson38"
        self._crear_formulario("Regla de Simpson 3/8 Compuesto", "Multiplo de 3 >= 3 (ej: 9)")

    # ===========================================================
    #  EJECUTAR METODO
    # ===========================================================
    def ejecutar_metodo(self):
        f_str = ""
        try:
            f_str = self.entry_func.get().strip()
            if not f_str:
                messagebox.showwarning("Advertencia", "Debe ingresar f(x)")
                return

            a_str = self.entry_a.get().strip()
            b_str = self.entry_b.get().strip()
            n_str = self.entry_n.get().strip()

            if not a_str or not b_str or not n_str:
                messagebox.showwarning("Advertencia", "Complete todos los campos (a, b, n)")
                return

            # Evaluar a y b como expresiones (permite escribir "pi", "2*pi", etc.)
            a = float(eval(a_str, {**_EVAL_NS}))
            b = float(eval(b_str, {**_EVAL_NS}))
            n = int(eval(n_str, {**_EVAL_NS}))

            if n < 1:
                messagebox.showwarning("Advertencia", "n debe ser >= 1")
                return
            if a >= b:
                messagebox.showwarning("Advertencia", "a debe ser menor que b")
                return

            if self.metodo_activo == "trapecio":
                resultado, h, xs, ys, texto = trapecio_compuesto(f_str, a, b, n)
                nombre = "Trapecio Compuesto"
            elif self.metodo_activo == "simpson13":
                resultado, h, xs, ys, texto = simpson_1_3_compuesto(f_str, a, b, n)
                nombre = "Simpson 1/3 Compuesto"
            elif self.metodo_activo == "simpson38":
                resultado, h, xs, ys, texto = simpson_3_8_compuesto(f_str, a, b, n)
                nombre = "Simpson 3/8 Compuesto"
            else:
                messagebox.showerror("Error", "Seleccione un metodo primero")
                return

            self.mostrar_resultado(texto)

            if resultado is not None:
                self.graficar_integracion(f_str, a, b, xs, ys, nombre)

        except Exception as e:
            f_procesada = preparar_funcion(f_str) if f_str else ""
            messagebox.showerror("Error",
                "Error al ejecutar integracion:\n{}\n\n"
                "f(x) procesada: {}".format(e, f_procesada))


# ==================================================================
#  MAIN
# ==================================================================
if __name__ == "__main__":
    app = IntegracionApp()
    app.mainloop()
