using System.Text.Json;

namespace ConviTEC
{
    // Punto de entrada de una sede de ConviTEC.
    // Ejecución: dotnet run --project ConviTEC -- --sede liberia
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuración de la sede

            string sede = builder.Configuration["sede"] ?? "liberia";
            string raiz = BuscarRaiz();
            string rutaConfig = Path.Combine(raiz, "config", $"sede.{sede}.json");

            if (!File.Exists(rutaConfig))
            {
                Console.Error.WriteLine($"No se encontró la configuración de la sede: {rutaConfig}");
                return;
            }

            using var config = JsonDocument.Parse(File.ReadAllText(rutaConfig));
            int puerto = config.RootElement.GetProperty("puerto").GetInt32();
            builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");

            // cargar el resto de la configuración (código, rabbitHost, carpetaDatos)
            // crear la carpeta de datos de la sede si no existe
            // recuperar el estado desde los archivos (Repositorio)
            // crear los servicios e iniciar la conexión con RabbitMQ (BusSedes)

            var app = builder.Build();

            // Frontend estático (wwwroot)
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Endpoints REST
            app.MapGet("/api/estado", () => Results.Ok(new { sede, puerto }));

            // catálogo e inventario  -> /api/catalogo, /api/inventario
            // pedidos                -> /api/pedidos, /api/pedidos/pendientes
            // entregas               -> /api/entregas/siguiente
            // ventas y estadísticas  -> /api/ventas, /api/estadisticas

            await app.RunAsync();
        }


        // Busca hacia arriba la carpeta raíz del repositorio (la que contiene "config")
        private static string BuscarRaiz()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "config")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
