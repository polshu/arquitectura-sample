namespace proyect.DTOs.Input;

// Un DTO (Data Transfer Object) es una clase "chatarra" que SOLO sirve
// para transportar datos entre capas. No tiene metodos, no tiene reglas,
// no se guarda sola.
//
// Este DTO representa "lo que el cliente puede mandar cuando quiere
// crear un juego". Notese que NO tiene:
//   - Id: lo genera la base automaticamente con auto-increment.
//   - Date: lo pone el servidor con DateTime.UtcNow. Si viniera del cliente,
//           un usuario podria mentir y cargar un juego "publicado en 1999".
//   - State: arranca siempre en "Private" hasta que el publisher lo
//           cambie a traves de otra accion.
//   - DiscountPercentage: tambien lo pone el servidor (arranca en 0) porque
//           aplicar descuentos es otra accion distinta, no parte de crear.
//
// Al separar el DTO de la entidad, el Service recibe solo los campos que
// SI son responsabilidad del cliente, y el resto los completamos nosotros.
// Esto evita que alguien pueda mandar un JSON con Id=99 o State="Public"
// y trampearnos el alta.
public class GameCreateDto
{
    public int IdPublisher { get; set; }

    public string GameName { get; set; } = "";
    public string Description { get; set; } = "";

    public int NumberOfAchievements { get; set; }
    public float PriceUSD { get; set; }
}
