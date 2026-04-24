namespace proyect.DTOs.Input;

// DTO para editar un juego existente. Se separa del GameCreateDto porque
// las reglas son distintas:
//
//   - Al CREAR, el cliente NO puede setear State (arranca siempre en
//     "Private") ni DiscountPercentage (arranca en 0). Por eso no estan
//     en GameCreateDto.
//
//   - Al EDITAR, el publisher SI puede cambiar el State (de Private a
//     Public para publicarlo, por ejemplo) y aplicar descuentos. Por eso
//     aparecen aca.
//
// No incluimos Id ni IdPublisher:
//   - El Id viene por URL (/games/edit/5), no por body.
//   - El IdPublisher de un juego no cambia con un Edit: si cambiara, seria
//     una accion distinta tipo "transferir publisher", que vivira en otro
//     metodo del Service el dia que haga falta.
public class GameUpdateDto
{
    public string GameName { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; set; }

    public int NumberOfAchievements { get; set; }
    public float PriceUSD { get; set; }
    public float DiscountPercentage { get; set; }

    public string State { get; set; } = "Private";
}
