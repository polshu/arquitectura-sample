using proyect.DTOs.External;

namespace proyect.Infrastructure.Steam;

// Este es un "cliente externo" ENTRANTE: nuestro proyecto se comunica
// hacia afuera para TRAER datos. En este caso, informacion publica de
// juegos que vive en la Steam Store API.
//
// En produccion, GetGameInfo haria algo parecido a:
//
//     using (HttpClient http = new HttpClient())
//     {
//         string url = "https://store.steampowered.com/api/appdetails?appids=" + steamAppId;
//         string json = http.GetStringAsync(url).Result;
//         // Parsear el JSON y completar el SteamGameInfoDto.
//         return BuildDtoFromJson(json);
//     }
//
// Como en clase lo que nos interesa es la ARQUITECTURA (donde viven las
// cosas, quien llama a quien), y no el parseo de JSON ni async/await,
// este cliente es un "fake": en lugar de pegarle a Steam, tiene tres
// juegos cargados a mano en memoria. El contrato publico es el mismo:
// pasame un appId, te devuelvo un SteamGameInfoDto o null si no lo conozco.
//
// Quien use esta clase (por ejemplo GameService) no se entera de que es
// fake. El dia que se quiera conectar a Steam de verdad, se reemplaza
// el cuerpo de GetGameInfo y nadie mas en el proyecto se toca.
//
// Igual que EmailSenderFake, esta clase vive en Infrastructure/ porque
// no es parte del dominio: es una herramienta de entrada que usa el
// dominio para enriquecer sus respuestas.
public class SteamInfoClient
{
    // "Base falsa" en memoria. Se inicializa una sola vez cuando la
    // clase se carga, llamando al metodo BuildFakeStore que arma el
    // diccionario DTO por DTO.
    private static readonly Dictionary<int, SteamGameInfoDto> _fakeStore = BuildFakeStore();

    public SteamGameInfoDto? GetGameInfo(int steamAppId)
    {
        // ContainsKey chequea si ese appId existe en el diccionario.
        // Si existe, lo devolvemos. Si no, null.
        if (_fakeStore.ContainsKey(steamAppId))
        {
            SteamGameInfoDto info = _fakeStore[steamAppId];
            return info;
        }
        return null;
    }

    // Arma el diccionario fake a mano. Escribimos cada DTO propiedad por
    // propiedad a proposito: asi se ve que los datos que entran a nuestro
    // sistema NO son entidades Game, son "objetos chatarra" que reflejan
    // lo que hay del otro lado (Steam). La conversion a entidad Game, si
    // hiciera falta, seria otro paso en otra capa.
    private static Dictionary<int, SteamGameInfoDto> BuildFakeStore()
    {
        Dictionary<int, SteamGameInfoDto> store = new Dictionary<int, SteamGameInfoDto>();

        SteamGameInfoDto counterStrike = new SteamGameInfoDto();
        counterStrike.AppId = 730;
        counterStrike.Name = "Counter-Strike 2";
        counterStrike.ShortDescription = "Shooter tactico por equipos en primera persona.";
        counterStrike.HeaderImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/730/header.jpg";
        counterStrike.Publisher = "Valve";
        counterStrike.Developer = "Valve";
        counterStrike.PriceFormatted = "Free To Play";
        counterStrike.ReleaseDate = "Sep 27, 2023";
        store.Add(counterStrike.AppId, counterStrike);

        SteamGameInfoDto dota = new SteamGameInfoDto();
        dota.AppId = 570;
        dota.Name = "Dota 2";
        dota.ShortDescription = "MOBA competitivo 5 vs 5.";
        dota.HeaderImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/570/header.jpg";
        dota.Publisher = "Valve";
        dota.Developer = "Valve";
        dota.PriceFormatted = "Free To Play";
        dota.ReleaseDate = "Jul 9, 2013";
        store.Add(dota.AppId, dota);

        SteamGameInfoDto teamFortress = new SteamGameInfoDto();
        teamFortress.AppId = 440;
        teamFortress.Name = "Team Fortress 2";
        teamFortress.ShortDescription = "Shooter por clases con estilo cartoon.";
        teamFortress.HeaderImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/440/header.jpg";
        teamFortress.Publisher = "Valve";
        teamFortress.Developer = "Valve";
        teamFortress.PriceFormatted = "Free To Play";
        teamFortress.ReleaseDate = "Oct 10, 2007";
        store.Add(teamFortress.AppId, teamFortress);

        return store;
    }
}
