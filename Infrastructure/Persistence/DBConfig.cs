using Newtonsoft.Json.Linq;

namespace proyect.Infrastructure.Persistence;

// Esta clase lee las connection strings desde appsettings.json.
// La primera vez que se usa, carga el archivo JSON a memoria.
// Las siguientes veces reutiliza el JSON ya cargado, asi no se lee el
// archivo una y otra vez.
public static class DBConfig
{
    // Aca guardamos el JSON cargado. Empieza en null porque todavia no lo
    // leimos. La primera llamada a LoadConfigIfNeeded() lo completa.
    private static JObject? _cachedConfig = null;

    public static string GetPostgresConnectionString()
    {
        JObject config = LoadConfigIfNeeded();

        // Navegamos por el JSON paso a paso:
        //   appsettings.json  ->  "Database"  ->  "Postgres"  ->  "ConnectionString"
        JToken? databaseNode = config["Database"];
        if (databaseNode == null)
        {
            return "";
        }

        JToken? postgresNode = databaseNode["Postgres"];
        if (postgresNode == null)
        {
            return "";
        }

        JToken? connectionStringNode = postgresNode["ConnectionString"];
        if (connectionStringNode == null)
        {
            return "";
        }

        string connectionString = connectionStringNode.ToString();
        return connectionString;
    }

    public static string GetSqlServerConnectionString()
    {
        JObject config = LoadConfigIfNeeded();

        //   appsettings.json  ->  "Database"  ->  "SqlServer"  ->  "ConnectionString"
        JToken? databaseNode = config["Database"];
        if (databaseNode == null)
        {
            return "";
        }

        JToken? sqlServerNode = databaseNode["SqlServer"];
        if (sqlServerNode == null)
        {
            return "";
        }

        JToken? connectionStringNode = sqlServerNode["ConnectionString"];
        if (connectionStringNode == null)
        {
            return "";
        }

        string connectionString = connectionStringNode.ToString();
        return connectionString;
    }

    // Carga appsettings.json una sola vez y lo guarda en _cachedConfig.
    // Si alguien llama de nuevo, se devuelve lo que ya esta cacheado.
    private static JObject LoadConfigIfNeeded()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        string pathToFile = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        string jsonContent = File.ReadAllText(pathToFile);
        _cachedConfig = JObject.Parse(jsonContent);

        return _cachedConfig;
    }
}
