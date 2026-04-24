using Microsoft.AspNetCore.Mvc;
using proyect.DTOs.Input;
using proyect.Services;

namespace proyect.Controllers;

// ==========================================================================
// GamesController: CRUD MVC clasico de Game.
// ==========================================================================
//
// Este controller no devuelve JSON: todas sus actions devuelven View()
// (pantalla HTML) o RedirectToAction (navega a otra pantalla). Es el CRUD
// "humano", el que usa el publisher desde el navegador.
//
// Rutas:
//   GET  /games                  -> Index          (lista todos los juegos)
//   GET  /games/details/{id}     -> Details        (ver un juego)
//   GET  /games/create           -> CreateForm     (formulario de alta)
//   POST /games/create           -> Create         (recibe el form y guarda)
//   GET  /games/edit/{id}        -> EditForm       (formulario de edicion)
//   POST /games/edit/{id}        -> Edit           (recibe el form y actualiza)
//   GET  /games/delete/{id}      -> DeleteConfirm  (pantalla de confirmacion)
//   POST /games/delete/{id}      -> Delete         (confirma y borra)
//
// Patron MVC clasico: despues de un POST exitoso, redirigimos con
// RedirectToAction para que el navegador quede apuntando al Index (o al
// Details). Asi si el usuario apreta F5, no re-postea el form.
// ==========================================================================
[Route("games")]
public class GamesController : Controller
{
    private readonly GameService _gameService;

    public GamesController()
    {
        _gameService = new GameService();
    }

    // ----------------------------------------------------------------------
    // GET /games
    // Lista todos los juegos (incluye Private). Renderiza Views/Games/Index.cshtml.
    // ----------------------------------------------------------------------
    [HttpGet("")]
    public IActionResult Index()
    {
        List<Game> allGames = _gameService.GetAll();
        return View(allGames);
    }

    // ----------------------------------------------------------------------
    // GET /games/details/5
    // Muestra un juego con los datos del publisher. Si no existe, 404.
    // ----------------------------------------------------------------------
    [HttpGet("details/{id:int}")]
    public IActionResult Details(int id)
    {
        Game? game = _gameService.GetById(id);
        if (game == null)
        {
            return NotFound("No existe un juego con ese Id.");
        }

        User? publisher = _gameService.GetPublisher(game);

        // Pasamos publisher por ViewBag para no inventar un ViewModel aparte.
        // ViewBag es una bolsa dinamica de datos extra que la vista puede leer.
        // Alcanza para un caso tan simple como "una entidad + una auxiliar".
        ViewBag.Publisher = publisher;
        return View(game);
    }

    // ----------------------------------------------------------------------
    // GET /games/create
    // Muestra el formulario vacio para crear un juego nuevo.
    // ----------------------------------------------------------------------
    [HttpGet("create")]
    public IActionResult CreateForm()
    {
        GameCreateDto emptyDto = new GameCreateDto();
        return View("Create", emptyDto);
    }

    // ----------------------------------------------------------------------
    // POST /games/create
    //
    // Recibe el formulario. [FromForm] le dice a ASP.NET Core que arme el
    // GameCreateDto leyendo los campos del form (Content-Type
    // application/x-www-form-urlencoded), NO un body JSON. Esta es la
    // diferencia clave con el GamesApiController.Create, que usa [FromBody].
    // ----------------------------------------------------------------------
    [HttpPost("create")]
    public IActionResult Create([FromForm] GameCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.GameName))
        {
            ViewBag.ErrorMessage = "El juego necesita un nombre.";
            return View("Create", dto);
        }

        if (dto.IdPublisher <= 0)
        {
            ViewBag.ErrorMessage = "Falta el Id del publisher.";
            return View("Create", dto);
        }

        Game? created = _gameService.Create(dto);
        if (created == null)
        {
            ViewBag.ErrorMessage = "El publisher indicado no existe.";
            return View("Create", dto);
        }

        // Patron "Post-Redirect-Get": despues de guardar, redirigimos al
        // Details del juego recien creado. Asi el URL del navegador queda
        // limpio y un F5 no reenvia el form.
        return RedirectToAction("Details", new { id = created.Id });
    }

    // ----------------------------------------------------------------------
    // GET /games/edit/5
    // Muestra el form de edicion con los datos actuales del juego.
    // ----------------------------------------------------------------------
    [HttpGet("edit/{id:int}")]
    public IActionResult EditForm(int id)
    {
        Game? game = _gameService.GetById(id);
        if (game == null)
        {
            return NotFound("No existe un juego con ese Id.");
        }

        // Armamos el DTO a partir del Game para pre-cargar el form.
        GameUpdateDto dto = new GameUpdateDto();
        dto.GameName = game.GameName;
        dto.Description = game.Description;
        dto.Date = game.Date;
        dto.NumberOfAchievements = game.NumberOfAchievements;
        dto.PriceUSD = game.PriceUSD;
        dto.DiscountPercentage = game.DiscountPercentage;
        dto.State = game.State;

        ViewBag.GameId = id;
        return View("Edit", dto);
    }

    // ----------------------------------------------------------------------
    // POST /games/edit/5
    // Recibe el form de edicion y actualiza el juego.
    // ----------------------------------------------------------------------
    [HttpPost("edit/{id:int}")]
    public IActionResult Edit(int id, [FromForm] GameUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.GameName))
        {
            ViewBag.GameId = id;
            ViewBag.ErrorMessage = "El juego necesita un nombre.";
            return View("Edit", dto);
        }

        Game? updated = _gameService.Update(id, dto);
        if (updated == null)
        {
            return NotFound("No existe un juego con ese Id.");
        }

        return RedirectToAction("Details", new { id = id });
    }

    // ----------------------------------------------------------------------
    // GET /games/delete/5
    // Pantalla de confirmacion antes de borrar. Es buena practica no borrar
    // directo con un GET: el usuario confirma y recien el POST borra.
    // ----------------------------------------------------------------------
    [HttpGet("delete/{id:int}")]
    public IActionResult DeleteConfirm(int id)
    {
        Game? game = _gameService.GetById(id);
        if (game == null)
        {
            return NotFound("No existe un juego con ese Id.");
        }

        return View("Delete", game);
    }

    // ----------------------------------------------------------------------
    // POST /games/delete/5
    // Borra el juego. Despues redirige al Index.
    // ----------------------------------------------------------------------
    [HttpPost("delete/{id:int}")]
    public IActionResult Delete(int id)
    {
        bool deleted = _gameService.Delete(id);
        if (!deleted)
        {
            return NotFound("No existe un juego con ese Id.");
        }

        return RedirectToAction("Index");
    }
}
