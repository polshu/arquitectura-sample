using proyect.Infrastructure.Persistence;

namespace proyect.Infrastructure.Persistence.Repositories;

// Nota sobre la tabla "User":
//   User es palabra reservada en Postgres (y en SQL Server tambien), por eso
//   el nombre de la tabla va entre comillas dobles en toda la SQL. Los
//   nombres de columna que no son reservados quedan sin comillas.
public class UserRepository
{
    // Se podria inicializar el campo directamente en la declaracion:
    //     private readonly DBPostgres _db = new DBPostgres();
    // Lo hacemos en el constructor para que se vea claro cuando se crea.
    private readonly DBPostgres _db;

    public UserRepository()
    {
        _db = new DBPostgres();
    }

    public void Insert(User user)
    {
        string sql = @"
            INSERT INTO ""User"" (UserName, PasswordHash, Email, ProfilePicture, Description,
                                  Followers, Followed, GamesOwned, Verified, VerifyHash)
            VALUES (@UserName, @PasswordHash, @Email, @ProfilePicture, @Description,
                    @Followers, @Followed, @GamesOwned, @Verified, @VerifyHash);";

        Dictionary<string, object> parameters = BuildParametersFromUser(user);
        _db.Execute(sql, parameters);
    }

    public void Update(User user)
    {
        string sql = @"
            UPDATE ""User""
            SET UserName = @UserName,
                Email = @Email,
                ProfilePicture = @ProfilePicture,
                Description = @Description,
                Verified = @Verified
            WHERE Id = @Id;";

        Dictionary<string, object> parameters = BuildParametersFromUser(user);
        parameters.Add("Id", user.Id);

        _db.Execute(sql, parameters);
    }

    public User? GetById(int id)
    {
        string sql = @"SELECT * FROM ""User"" WHERE Id = @Id;";

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("Id", id);

        Dictionary<string, object?>? row = _db.QueryFirst(sql, parameters);
        if (row == null)
        {
            return null;
        }

        User user = BuildUserFromRow(row);
        return user;
    }

    public User? GetByUserName(string userName)
    {
        string sql = @"SELECT * FROM ""User"" WHERE UserName = @UserName;";

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("UserName", userName);

        Dictionary<string, object?>? row = _db.QueryFirst(sql, parameters);
        if (row == null)
        {
            return null;
        }

        User user = BuildUserFromRow(row);
        return user;
    }

    public User? GetByEmail(string email)
    {
        string sql = @"SELECT * FROM ""User"" WHERE Email = @Email;";

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("Email", email);

        Dictionary<string, object?>? row = _db.QueryFirst(sql, parameters);
        if (row == null)
        {
            return null;
        }

        User user = BuildUserFromRow(row);
        return user;
    }

    public int CountByEmail(string email)
    {
        string sql = @"SELECT COUNT(*) FROM ""User"" WHERE Email = @Email;";

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("Email", email);

        object? scalarResult = _db.ExecuteScalar(sql, parameters);

        if (scalarResult == null)
        {
            return 0;
        }

        int count = Convert.ToInt32(scalarResult);
        return count;
    }

    // Arma los parametros a partir del User. No incluye "Id" porque lo
    // genera la base (auto-increment) en el INSERT. Para el UPDATE, el Id
    // se agrega por fuera.
    private Dictionary<string, object> BuildParametersFromUser(User user)
    {
        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("UserName", user.UserName);
        parameters.Add("Email", user.Email);
        parameters.Add("PasswordHash", user.PasswordHash);
        parameters.Add("ProfilePicture", user.ProfilePicture);
        parameters.Add("Description", user.Description);
        parameters.Add("Followers", user.Followers);
        parameters.Add("Followed", user.Followed);
        parameters.Add("GamesOwned", user.GamesOwned);
        // La columna Verified se guarda como entero en la base (0 = false,
        // 1 = true). Por eso convertimos el bool a int antes de mandarlo.
        parameters.Add("Verified", user.Verified ? 1 : 0);
        parameters.Add("VerifyHash", user.VerifyHash);
        return parameters;
    }

    private User BuildUserFromRow(Dictionary<string, object?> row)
    {
        User user = new User();
        user.Id = Convert.ToInt32(row["Id"]);
        user.UserName = Convert.ToString(row["UserName"]) ?? "";
        user.Email = Convert.ToString(row["Email"]) ?? "";
        user.PasswordHash = Convert.ToString(row["PasswordHash"]) ?? "";
        user.ProfilePicture = Convert.ToString(row["ProfilePicture"]) ?? "";
        user.Description = Convert.ToString(row["Description"]) ?? "";
        user.Followers = Convert.ToInt32(row["Followers"]);
        user.Followed = Convert.ToInt32(row["Followed"]);
        user.GamesOwned = Convert.ToInt32(row["GamesOwned"]);
        // Verified llega como entero (0/1) y lo convertimos a bool.
        user.Verified = Convert.ToBoolean(row["Verified"]);
        user.VerifyHash = Convert.ToString(row["VerifyHash"]) ?? "";
        return user;
    }
}
