public class User
{
    public int Id { get; set; }

    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";

    public string ProfilePicture { get; set; } = "";
    public string Description { get; set; } = "";

    public int Followers { get; set; }
    public int Followed { get; set; }
    public int GamesOwned { get; set; }

    public bool Verified { get; set; }
    public string VerifyHash { get; set; } = "";
}
