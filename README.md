# Modelado y Simulación — Webapp

Aplicación web en .NET para resolver métodos numéricos: raíces de ecuaciones (Bisección, Punto Fijo, Newton-Raphson, Aitken/Steffensen), interpolación de Lagrange, integración por Newton-Cotes (Trapecio, Simpson 1/3, Simpson 3/8) y Monte Carlo.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Cómo correr

1. **Clonar el repositorio**

   ```bash
   git clone <url-del-repo>
   cd modelado-y-simulacion
   ```

2. **Compilar**

   ```bash
   dotnet build dotnet-webapp
   ```

3. **Ejecutar**

   ```bash
   dotnet run --project dotnet-webapp
   ```

4. **Abrir en el navegador**

   Ir a [http://localhost:5050](http://localhost:5050)

## Docker

```bash
docker compose up --build
```

Abrir [http://localhost:8080](http://localhost:8080).

## Estructura

```
dotnet-webapp/
├── Pages/           # Razor Pages (Index, Lagrange, NewtonCotes, MonteCarlo)
├── Services/        # Lógica de cálculo (FunctionParser, LagrangeService, etc.)
├── Models/          # Modelos de datos
├── wwwroot/         # Archivos estáticos (CSS, JS, librerías)
└── Program.cs       # Punto de entrada
```
