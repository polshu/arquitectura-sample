using proyect.Infrastructure.Persistence;

namespace proyect.Infrastructure.Persistence.Repositories;

// El Repository es el unico lugar del proyecto que sabe:
//   - como se llama la tabla
//   - como se llaman las columnas
//   - que SQL ejecutar para cada operacion
//   - como armar los parametros
//   - como transformar una fila de la base en una entidad Game
//
// No sabe de HTTP, ni de sesion, ni de reglas de negocio. Recibe y devuelve
// entidades. Si manana quisiera usar stored procedures, cambiarian las SQL
// aca adentro y nada mas.
public class GameRepository
{
    // Se podria inicializar el campo directamente en la declaracion:
    //     private readonly DBPostgres _db = new DBPostgres();
    // Lo hacemos en el constructor para que se vea claro cuando se crea.
    private readonly DBPostgres _db;

    public GameRepository()
    {
        _db = new DBPostgres();
    }

    public void Insert(Game game)
    {
        string sql = @"
            INSERT INTO Game (GameName, Description, IdPublisher, Date,
                              NumberOfAchievements, PriceUSD,
                              DiscountPercentage, State)
            VALUES (@GameName, @Description, @IdPublisher, @Date,
                    @NumberOfAchievements, @PriceUSD,
                    @DiscountPercentage, @State);";

        // ------------------------------------------------------------------
        // COMO SE PASAN LOS PARAMETROS AL SQL
        //
        // Nosotros armamos un Dictionary<string, object> a mano. Cada clave
        // es el nombre del parametro SQL (sin el arroba) y cada valor es lo
        // que va a reemplazar a ese @Nombre cuando el comando se ejecute
        // en la base.
        //
        // En otras librerias (por ejemplo Dapper, que se ve en BD.cs) se
        // pueden escribir formas mas compactas como estas:
        //
        //     _db.Execute(sql, game);
        //     _db.ExecuteScalar<int>(sql, new { PublisherId = publisherId });
        //
        // Parecen magia, pero no lo son. Lo que pasa ahi es:
        //   1) La libreria recibe el objeto que le pasaste (el Game entero,
        //      o el objeto anonimo "new { PublisherId = publisherId }").
        //   2) Usa REFLECTION: lee los NOMBRES de las propiedades
        //      ("GameName", "Description", "PublisherId", etc.) y sus
        //      VALORES actuales.
        //   3) Arma internamente un diccionario parecido al que nosotros
        //      armamos aca a mano, y lo usa como parametros SQL.
        //
        // Es decir: BuildParametersFromGame hace exactamente el mismo
        // trabajo que la reflexion de Dapper, pero paso a paso y a la vista,
        // asi se entiende que parametro se arma con que valor. Cuando
        // entiendan bien lo que pasa, pueden elegir la forma compacta.
        // ------------------------------------------------------------------
        Dictionary<string, object> parameters = BuildParametersFromGame(game);
        _db.Execute(sql, parameters);
    }

    public void Update(Game game)
    {
        string sql = @"
            UPDATE Game
            SET GameName = @GameName,
                Description = @Description,
                Date = @Date,
                NumberOfAchievements = @NumberOfAchievements,
                PriceUSD = @PriceUSD,
                DiscountPercentage = @DiscountPercentage,
                State = @State
            WHERE Id = @Id;";

        Dictionary<string, object> parameters = BuildParametersFromGame(game);
        // El UPDATE necesita saber que fila actualizar, asi que agregamos
        // el Id como parametro extra. Insert no lo necesita porque el Id
        // lo genera la base con el auto-increment.
        parameters.Add("Id", game.Id);

        _db.Execute(sql, parameters);
    }

    public Game? GetById(int id)
    {
        string sql = "SELECT * FROM Game WHERE Id = @Id;";

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("Id", id);

        Dictionary<string, object?>? row = _db.QueryFirst(sql, parameters);
        if (row == null)
        {
            return null;
        }

        Game game = BuildGameFromRow(row);
        return game;
    }

    public List<Game> GetByName(string name)
    {
        string sql = "SELECT * FROM Game WHERE GameName ILIKE @Name;";

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("Name", "%" + name + "%");

        List<Dictionary<string, object?>> rows = _db.Query(sql, parameters);

        List<Game> games = new List<Game>();
        foreach (Dictionary<string, object?> row in rows)
        {
            Game game = BuildGameFromRow(row);
            games.Add(game);
        }
        return games;
    }

    public List<Game> GetByPublisher(int publisherId)
    {
        string sql = "SELECT * FROM Game WHERE IdPublisher = @PublisherId;";

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("PublisherId", publisherId);

        List<Dictionary<string, object?>> rows = _db.Query(sql, parameters);

        List<Game> games = new List<Game>();
        foreach (Dictionary<string, object?> row in rows)
        {
            Game game = BuildGameFromRow(row);
            games.Add(game);
        }
        return games;
    }

    // Esta es la version compacta del foreach que hacen GetByName y
    // GetByPublisher.
    //
    //     rows.Select(BuildGameFromRow).ToList()
    //
    // Es equivalente a escribir:
    //
    //     List<Game> games = new List<Game>();
    //     foreach (Dictionary<string, object?> row in rows)
    //     {
    //         games.Add(BuildGameFromRow(row));
    //     }
    //     return games;
    //
    // Usa LINQ (.Select y .ToList), que es una forma mas corta de recorrer
    // una lista y transformar cada elemento. Lo dejamos como ejemplo para
    // que lo vean, pero las demas versiones usan el foreach explicito.
    public List<Game> GetPublic()
    {
        string sql = "SELECT * FROM Game WHERE State <> 'Private';";

        List<Dictionary<string, object?>> rows = _db.Query(sql);
        return rows.Select(BuildGameFromRow).ToList();
    }

    // Devuelve TODOS los juegos, incluidos los Private. Lo usa el CRUD
    // "administrativo" (las vistas MVC), donde el publisher quiere ver y
    // editar tambien los suyos que todavia no son publicos. El catalogo
    // publico (GetPublic) sigue existiendo aparte.
    public List<Game> GetAll()
    {
        string sql = "SELECT * FROM Game ORDER BY Id;";

        List<Dictionary<string, object?>> rows = _db.Query(sql);

        List<Game> games = new List<Game>();
        foreach (Dictionary<string, object?> row in rows)
        {
            Game game = BuildGameFromRow(row);
            games.Add(game);
        }
        return games;
    }

    // Borra el juego con ese Id. Devuelve cuantas filas tocaron, para que el
    // Service pueda distinguir "se borro" (1) de "no existia" (0).
    public int Delete(int id)
    {
        string sql = "DELETE FROM Game WHERE Id = @Id;";

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("Id", id);

        int affectedRows = _db.Execute(sql, parameters);
        return affectedRows;
    }

    public int CountByPublisher(int publisherId)
    {
        string sql = "SELECT COUNT(*) FROM Game WHERE IdPublisher = @PublisherId;";

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("PublisherId", publisherId);

        object? scalarResult = _db.ExecuteScalar(sql, parameters);

        if (scalarResult == null)
        {
            return 0;
        }

        // COUNT(*) en Postgres vuelve como long (Int64). Convert.ToInt32 lo
        // pasa a int sin tener que escribir un cast explicito (int)(long).
        int count = Convert.ToInt32(scalarResult);
        return count;
    }

    // Arma un diccionario con los datos del Game listos para usar como
    // parametros SQL. Cada clave coincide con el nombre que usa el SQL
    // despues del arroba (por ej. la clave "GameName" corresponde a @GameName).
    //
    // No incluimos "Id" porque el Id lo genera la base sola (auto-increment)
    // en el INSERT. Para el UPDATE, el Id se agrega por fuera de este metodo.
    private Dictionary<string, object> BuildParametersFromGame(Game game)
    {
        Dictionary<string, object> parameters = new Dictionary<string, object>();
        parameters.Add("IdPublisher", game.IdPublisher);
        parameters.Add("Date", game.Date);
        parameters.Add("GameName", game.GameName);
        parameters.Add("Description", game.Description);
        parameters.Add("State", game.State);
        parameters.Add("NumberOfAchievements", game.NumberOfAchievements);
        parameters.Add("PriceUSD", game.PriceUSD);
        parameters.Add("DiscountPercentage", game.DiscountPercentage);
        return parameters;
    }

    // Toma una fila de la base (diccionario columna -> valor) y arma el
    // Game correspondiente. Aca es donde decidimos "la columna X va en la
    // propiedad Y" de la entidad. Si la base cambia un nombre de columna,
    // este metodo es el unico que hay que tocar.
    //
    // Convert.ToXxx sirve para convertir el valor que trajo la base (que
    // siempre llega como object) al tipo concreto que necesita la propiedad.
    private Game BuildGameFromRow(Dictionary<string, object?> row)
    {
        Game game = new Game();
        game.Id = Convert.ToInt32(row["Id"]);
        game.IdPublisher = Convert.ToInt32(row["IdPublisher"]);
        game.Date = Convert.ToDateTime(row["Date"]);
        game.GameName = Convert.ToString(row["GameName"]) ?? "";
        game.Description = Convert.ToString(row["Description"]) ?? "";
        game.State = Convert.ToString(row["State"]) ?? "";
        game.NumberOfAchievements = Convert.ToInt32(row["NumberOfAchievements"]);
        game.PriceUSD = Convert.ToSingle(row["PriceUSD"]);
        game.DiscountPercentage = Convert.ToSingle(row["DiscountPercentage"]);
        return game;
    }
}
