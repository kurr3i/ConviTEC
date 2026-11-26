using System.Text.Json;
using ConviTEC.Estructuras;
using ConviTEC.Mensajeria;
using ConviTEC.Modelo;
using ConviTEC.Persistencia;
using ConviTEC.Servicios;

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

            // Cargar la configuración de la sede desde el archivo JSON
            using var config = JsonDocument.Parse(File.ReadAllText(rutaConfig));
            int puerto = config.RootElement.GetProperty("puerto").GetInt32();
            builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");

            // Cargar la configuración de la sede
            string codigoSede = config.RootElement.GetProperty("codigo").GetString()!;
            string rabbitHost = config.RootElement.GetProperty("rabbitHost").GetString()!;

            // Lista de sedes desde el config: "sedes": ["LIB", "ALA", "SJO", "LIM"]
            var elementosSedes = config.RootElement.GetProperty("sedes");
            string[] sedes = new string[elementosSedes.GetArrayLength()];

            int indice = 0;
            foreach (var elemento in elementosSedes.EnumerateArray())
                sedes[indice++] = elemento.GetString()!;

            // Carpeta de datos de la sede
            string nombreSede = config.RootElement.GetProperty("nombre").GetString()!;
            string carpetaDatos = Path.Combine(raiz, config.RootElement.GetProperty("carpetaDatos").GetString()!);
            Directory.CreateDirectory(carpetaDatos);

            // Red de distribución y estado del recorrido
            DistribucionService distribucion;
            string rutaRecorrido = Path.Combine(carpetaDatos, "recorrido.json");
            try
            {
                GrafoMatriz grafo = Repositorio.CargarGrafo(Path.Combine(raiz, "data", "grafo.json"));
                distribucion = new DistribucionService(grafo, nombreSede);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is ArgumentException)
            {
                Console.Error.WriteLine($"No se pudo cargar la red de distribución: {ex.Message}");
                return;
            }

            try
            {
                EstadoRecorrido? estado = Repositorio.CargarRecorrido(rutaRecorrido);
                if (estado != null)
                    distribucion.RestaurarEstado(estado);
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is ArgumentException)
            {
                Console.Error.WriteLine($"[Distribución] {ex.Message} El recorrido empieza desde la sede.");
            }

            // Usuario y clave desde variables de entorno
            string rabbitUsuario = builder.Configuration["RABBIT_USUARIO"] ?? "convitec";
            string rabbitClave = builder.Configuration["RABBIT_CLAVE"] ?? "";

            // Inicializar el bus de mensajes (RabbitMQ)
            await using var bus = new BusSedes(codigoSede, sedes, rabbitHost, rabbitUsuario, rabbitClave);
            
            // Suscribir un manejador para el tipo de mensaje Ping (solo para pruebas)
            bus.Suscribir(TipoMensaje.Ping, mensaje =>
            {
                Console.WriteLine($"[{codigoSede}] Ping de {mensaje.Origen}: {mensaje.ObtenerContenido<string>()}");
                return Task.CompletedTask;
            });
            
            await bus.IniciarAsync();


            // recuperar el estado desde los archivos (Repositorio)

            var app = builder.Build();

            // Frontend estático (wwwroot)
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Endpoints REST
            app.MapGet("/api/estado", () => Results.Ok(new { sede, puerto }));

            app.MapPost("/api/ping/{destino}", async (string destino) =>
            {
                var mensaje = Mensaje.Crear(TipoMensaje.Ping, codigoSede, destino, $"hola desde {codigoSede}");
                await bus.EnviarAsync(mensaje);
                return Results.Ok(new { enviado = true, destino });
            });

            // catálogo e inventario  -> /api/catalogo, /api/inventario
            // pedidos                -> /api/pedidos, /api/pedidos/pendientes
            // ventas y estadísticas  -> /api/ventas, /api/estadisticas

            // Distribución
            app.MapGet("/api/destinos", () => Results.Ok(distribucion.ObtenerDestinos()));

            // TODO: cambiar por pedidoService.ObtenerPendientesParaEntrega y pedidoService.MarcarEntregados
            MapearEntregas(app, distribucion, rutaRecorrido,
                obtenerPendientes: () => [],
                marcarEntregados: (ids, tiempoTraslado) => { });

            await app.RunAsync();
        }

        // DistribucionService no conoce los pedidos: los recibe de afuera y devuelve
        // cuáles se entregaron, así no queda una dependencia circular con PedidoService
        private static void MapearEntregas(WebApplication app, DistribucionService distribucion, string rutaRecorrido,
            Func<EntregaPendiente[]> obtenerPendientes, Action<string[], int> marcarEntregados)
        {
            // Evita que dos solicitudes muevan el vehículo al mismo tiempo
            object bloqueo = new();

            app.MapGet("/api/entregas/siguiente", () =>
            {
                PropuestaEntrega? propuesta;
                lock (bloqueo)
                    propuesta = distribucion.CalcularSiguiente(obtenerPendientes());

                if (propuesta == null)
                    return Results.Ok(new { destino = (string?)null, mensaje = "No quedan destinos alcanzables desde la ubicación actual." });
                return Results.Ok(propuesta);
            });

            app.MapPost("/api/entregas/siguiente", () =>
            {
                ResultadoEntrega? resultado;
                lock (bloqueo)
                {
                    resultado = distribucion.EntregarSiguiente(obtenerPendientes());
                    if (resultado != null)
                    {
                        marcarEntregados(resultado.PedidosEntregados, resultado.TiempoTotal);
                        Repositorio.GuardarRecorrido(rutaRecorrido, distribucion.ObtenerEstado());
                    }
                }

                if (resultado == null)
                    return Results.Conflict(new { mensaje = "No hay entregas pendientes alcanzables." });
                return Results.Ok(resultado);
            });
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
