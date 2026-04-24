public class Game
{
    public int Id { get; set; }
    public int IdPublisher { get; set; }
    public DateTime Date { get; set; }

    public string GameName { get; set; } = "";
    public string Description { get; set; } = "";
    public string State { get; set; } = "Private";

    public int NumberOfAchievements { get; set; }
    public float PriceUSD { get; set; }
    public float DiscountPercentage { get; set; }
}
