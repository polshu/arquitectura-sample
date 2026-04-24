namespace proyect.DTOs.External;

// Este es OTRO tipo de DTO: no representa algo que nos manda nuestro
// cliente, sino algo que nos devuelve un SISTEMA EXTERNO. En este caso,
// la forma de la respuesta de la Steam Store API
// (https://store.steampowered.com/api/appdetails?appids={id}).
//
// NO es nuestra entidad Game. La entidad Game vive en la base de Indeura,
// con nuestros campos, nuestras reglas y nuestro Id. Esto, en cambio, es
// "lo que Steam tiene para decirnos de un juego" y lo tratamos como
// datos de paso: llegan, se leen, y o bien se devuelven al cliente asi
// como estan, o se copian selectivamente a campos de nuestra entidad.
//
// Si manana Steam cambia el nombre de un campo, este DTO es el unico
// lugar del proyecto que se entera. Todo lo demas (Service, Controller)
// sigue hablando en terminos de SteamGameInfoDto.
public class SteamGameInfoDto
{
    public int AppId { get; set; }

    public string Name { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string HeaderImageUrl { get; set; } = "";

    public string Publisher { get; set; } = "";
    public string Developer { get; set; } = "";

    public string PriceFormatted { get; set; } = "";
    public string ReleaseDate { get; set; } = "";
}
