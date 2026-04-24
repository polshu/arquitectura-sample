using Microsoft.AspNetCore.Mvc;
using proyect.DTOs.External;
using proyect.DTOs.Input;
using proyect.Services;

namespace proyect.Controllers;

// ==========================================================================
// GamesApiController: API JSON "pura".
// ==========================================================================
//
// Este controller es el par API del GamesController. La diferencia es que
// este NUNCA devuelve una vista: todos sus endpoints devuelven JSON. Sirve
// para ser consumido desde:
//   - curl / Postman / Insomnia
//   - la barra del navegador (para los GET)
//   - la pagina /api-tester que hace fetch() desde el browser
//   - cualquier otro cliente (otra app, un script, una SPA, etc.)
//
// Lo separamos del CRUD MVC porque mezclar "devolver HTML" con "devolver
// JSON" en el mismo controller hace que las responsabilidades se pisen.
// Cada uno en lo suyo:
//   - GamesController  -> /games/*       (MVC, devuelve View)
//   - GamesApiController -> /api/games/* (API, devuelve Json)
//
// Los dos comparten el MISMO GameService. Ninguno duplica logica: los dos
// son solo una "forma" distinta de exponer lo mismo.
// ==========================================================================
[Route("api/games")]
public class GamesApiController : Controller
{
    private readonly GameService _gameService;

    public GamesApiController()
    {
        _gameService = new GameService();
    }

    // GET /api/games                    -> lista de juegos publicos
    [HttpGet("")]
    public IActionResult Catalog()
    {
        List<Game> publicGames = _gameService.GetPublic();
        return Json(publicGames);
    }

    // GET /api/games/all                -> lista completa (incluye Private)
    [HttpGet("all")]
    public IActionResult All()
    {
        List<Game> allGames = _gameService.GetAll();
        return Json(allGames);
    }

    // GET /api/games/details/5          -> un juego + su publisher
    [HttpGet("details/{id:int}")]
    public IActionResult Details(int id)
    {
        Game? game = _gameService.GetById(id);
        if (game == null)
        {
            return NotFound("No existe un juego con ese Id.");
        }

        User? publisher = _gameService.GetPublisher(game);

        var response = new
        {
            Game = game,
            Publisher = publisher
        };
        return Json(response);
    }

    // GET /api/games/search?name=halo   -> busqueda por nombre parcial
    [HttpGet("search")]
    public IActionResult Search(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            List<Game> empty = new List<Game>();
            return Json(empty);
        }

        List<Game> matchingGames = _gameService.GetByName(name);
        return Json(matchingGames);
    }

    // POST /api/games/create            -> alta con JSON body
    //
    // Como es una API, esperamos el GameCreateDto en el body como JSON.
    // Por eso usamos [FromBody]. La version MVC (GamesController) usa
    // [FromForm] porque recibe el mismo DTO pero desde un <form> HTML.
    [HttpPost("create")]
    public IActionResult Create([FromBody] GameCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.GameName))
        {
            return BadRequest("El juego necesita un nombre.");
        }

        if (dto.IdPublisher <= 0)
        {
            return BadRequest("Falta el Id del publisher.");
        }

        Game? created = _gameService.Create(dto);
        if (created == null)
        {
            return BadRequest("El publisher indicado no existe.");
        }

        return Ok(created);
    }

    // GET /api/games/steam/730          -> info del fake de Steam
    [HttpGet("steam/{appId:int}")]
    public IActionResult SteamPreview(int appId)
    {
        SteamGameInfoDto? info = _gameService.GetSteamInfo(appId);
        if (info == null)
        {
            return NotFound("Steam no tiene registro de ese appId en el fake.");
        }

        return Json(info);
    }
}
