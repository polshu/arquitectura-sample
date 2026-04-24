using Microsoft.AspNetCore.Mvc;

namespace proyect.Controllers;

// HomeController es el mas "tonto" de todos: no tiene Services y no toca
// la base. Solo sirve dos pantallas HTML:
//
//   GET /              -> Index       (landing del proyecto)
//   GET /api-tester    -> ApiTester   (pagina para llamar a la API por AJAX)
//
// Lo separamos del GamesController para que cada controller tenga una
// responsabilidad clara:
//   - HomeController: pantallas informativas.
//   - GamesController: CRUD MVC de Game (lista/alta/edicion/borrado).
//   - GamesApiController: API JSON de Game (invocable desde curl/Postman/AJAX).
public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
    }

    // La vista ApiTester es pura HTML + JavaScript. Desde ahi se llama a
    // los endpoints del GamesApiController con fetch() y se muestra la
    // respuesta JSON en pantalla. NO toca la base: la "prueba" se hace toda
    // contra la API, mismas URLs que usarian Postman o curl.
    [HttpGet("/api-tester")]
    public IActionResult ApiTester()
    {
        return View();
    }
}
