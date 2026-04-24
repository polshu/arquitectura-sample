var builder = WebApplication.CreateBuilder(args);

// MVC con soporte de Razor Views. Alcanza para que el proyecto
// tenga Controllers y pueda renderizar paginas .cshtml.
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseRouting();

// Ruta "por convencion": /{controller}/{action}/{id?}. Como GamesController
// tiene sus propias rutas por atributo ([Route("games")], [HttpGet("/")]),
// esta ruta default queda casi como documentacion de respaldo.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Games}/{action=Index}/{id?}");

app.Run();
