using proyect.DTOs.External;
using proyect.DTOs.Input;
using proyect.Infrastructure.Email;
using proyect.Infrastructure.Persistence.Repositories;
using proyect.Infrastructure.Steam;

namespace proyect.Services;

// El Service es la puerta de entrada para todo lo que se puede hacer con
// un Game. Los Controllers y las Views no deberian tocar el Repository ni
// la base directamente: siempre hablan con el Service.
//
// Aca viven las reglas de negocio simples (validaciones, orquestacion entre
// entidades, llamadas a clientes externos). Si hay que tocar varias tablas
// o servicios para una sola accion, se arma aca y se llama a los
// Repositories / clientes correspondientes.
public class GameService
{
    // Se podrian inicializar los campos directamente en la declaracion:
    //     private readonly GameRepository _gameRepository = new GameRepository();
    // Lo dejamos explicito en el constructor para que se vea bien donde y
    // cuando se crea cada dependencia.
    private readonly GameRepository _gameRepository;
    private readonly UserService _userService;
    private readonly EmailSenderFake _emailSender;
    private readonly SteamInfoClient _steamInfoClient;

    public GameService()
    {
        _gameRepository = new GameRepository();
        _userService = new UserService();
        _emailSender = new EmailSenderFake();
        _steamInfoClient = new SteamInfoClient();
    }

    // --------------------------------------------------------------------
    // Create: ejemplo completo de ORQUESTACION.
    //
    // Recibe un DTO (solo los campos que el cliente tiene permitido mandar)
    // y hace tres cosas en conjunto:
    //   1) Chequea con UserService que el publisher exista.
    //   2) Arma el Game con los defaults del servidor (State="Private",
    //      Date=UtcNow, DiscountPercentage=0) y lo guarda via Repository.
    //   3) Le avisa al publisher por mail a traves del EmailSender.
    //
    // Todo eso vive en una sola funcion del Service porque todos esos pasos
    // forman parte de la accion "crear un juego" en el dominio. El
    // Controller NO hace nada de esto: solo recibe el pedido HTTP y llama
    // a este metodo.
    // --------------------------------------------------------------------
    public Game? Create(GameCreateDto dto)
    {
        // Regla: el publisher tiene que existir. Usamos UserService,
        // nunca UserRepository, porque no nos saltamos capas.
        User? publisher = _userService.GetById(dto.IdPublisher);
        if (publisher == null)
        {
            return null;
        }

        // Mapeo DTO -> entidad "a mano", property por property. Es el
        // mismo trabajo que haria AutoMapper, escrito paso a paso.
        // Aca decidimos CUALES campos vienen del cliente y CUALES los
        // completa el servidor: la diferencia entre confiar y no confiar.
        Game game = new Game();
        game.IdPublisher = dto.IdPublisher;
        game.GameName = dto.GameName;
        game.Description = dto.Description;
        game.NumberOfAchievements = dto.NumberOfAchievements;
        game.PriceUSD = dto.PriceUSD;
        game.DiscountPercentage = 0;
        game.State = "Private";
        game.Date = DateTime.UtcNow;

        // Persistimos via Repository. Nada de SQL aca adentro.
        _gameRepository.Insert(game);

        // Disparamos el efecto colateral: el mail. El EmailSender es un
        // cliente externo (vive en Infrastructure/), y desde el Service
        // no nos importa SI el mail se manda por SMTP o se escribe en
        // una consola: eso es problema del EmailSender.
        _emailSender.SendWelcomeEmail(publisher.Email, game.GameName);

        return game;
    }

    public Game? GetById(int id)
    {
        return _gameRepository.GetById(id);
    }

    public List<Game> GetByName(string name)
    {
        return _gameRepository.GetByName(name);
    }

    public List<Game> GetByPublisher(int publisherId)
    {
        return _gameRepository.GetByPublisher(publisherId);
    }

    public List<Game> GetPublic()
    {
        return _gameRepository.GetPublic();
    }

    // Lista completa (incluye Private). La usan las pantallas MVC del CRUD.
    // El catalogo publico (GetPublic) sigue siendo otro metodo aparte.
    public List<Game> GetAll()
    {
        return _gameRepository.GetAll();
    }

    // --------------------------------------------------------------------
    // Update: edita un juego existente.
    //
    // Recibe el Id por parametro (viene de la URL) y un DTO con los campos
    // que el publisher tiene permitido cambiar. Notar que el DTO NO incluye
    // Id ni IdPublisher: el Id lo pasamos por fuera, y el publisher de un
    // juego no cambia con un Edit.
    //
    // Devuelve el Game actualizado, o null si el Id no existia.
    // --------------------------------------------------------------------
    public Game? Update(int id, GameUpdateDto dto)
    {
        Game? existing = _gameRepository.GetById(id);
        if (existing == null)
        {
            return null;
        }

        // Copiamos los campos del DTO sobre el Game que trajimos de la base.
        // Asi mantenemos el IdPublisher y la Date original si no vinieran
        // en el DTO (en este caso Date si viene, por decision del form).
        existing.GameName = dto.GameName;
        existing.Description = dto.Description;
        existing.Date = dto.Date;
        existing.NumberOfAchievements = dto.NumberOfAchievements;
        existing.PriceUSD = dto.PriceUSD;
        existing.DiscountPercentage = dto.DiscountPercentage;
        existing.State = dto.State;

        _gameRepository.Update(existing);
        return existing;
    }

    // --------------------------------------------------------------------
    // Delete: borra un juego por Id.
    //
    // Devuelve true si lo borro, false si no existia. El Repository devuelve
    // cuantas filas toco; traducimos ese numero a bool aca para que el
    // Controller no tenga que saber detalles de SQL.
    // --------------------------------------------------------------------
    public bool Delete(int id)
    {
        int affectedRows = _gameRepository.Delete(id);
        return affectedRows > 0;
    }

    public int CountByPublisher(int publisherId)
    {
        return _gameRepository.CountByPublisher(publisherId);
    }

    // Ejemplo de orquestacion entre entidades: Game.IdPublisher apunta a un
    // User, y devolver ese User es parte de "cosas que se pueden hacer con
    // un Game". Por eso vive en el GameService y no en el Controller.
    //
    // Cuando un metodo tiene una sola linea, se puede escribir compacto con
    // la flecha =>, asi:
    //     public User? GetPublisher(Game game) => _userService.GetById(game.IdPublisher);
    // Es exactamente lo mismo que el bloque de abajo, solo que mas corto.
    public User? GetPublisher(Game game)
    {
        return _userService.GetById(game.IdPublisher);
    }

    // --------------------------------------------------------------------
    // GetSteamInfo: fachada al cliente externo.
    //
    // Este metodo no va a la base: va a "Steam" (en realidad al
    // SteamInfoClient, que hoy es un fake). Lo importante es que el
    // Controller no llama directo al cliente externo, pasa por el Service.
    //
    // Por que: si manana Steam devuelve mas campos, o queremos combinar
    // info de Steam con info de nuestra base, ese merge vive aca adentro
    // y el Controller no se entera.
    // --------------------------------------------------------------------
    public SteamGameInfoDto? GetSteamInfo(int steamAppId)
    {
        return _steamInfoClient.GetGameInfo(steamAppId);
    }
}
