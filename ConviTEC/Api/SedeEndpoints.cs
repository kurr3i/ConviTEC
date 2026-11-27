using ConviTEC.Servicios;

namespace ConviTEC.Api
{
    public static class SedeEndpoints
    {
        public static void MapSede(this WebApplication app, string codigoSede, string nombreSede,
                                   string[] sedes, InventarioService inventario, InventarioGlobal global)
        {
            // Datos de la sede para el encabezado de la interfaz
            app.MapGet("/api/sede", () => Results.Ok(new { codigo = codigoSede, nombre = nombreSede, sedes }));

            // Catálogo con las existencias de cada sede
            app.MapGet("/api/catalogo-global",
                () => Results.Ok(CatalogoGlobal.Construir(inventario, global, codigoSede, sedes)));
        }
    }
}