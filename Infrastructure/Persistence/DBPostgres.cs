using Npgsql;

namespace proyect.Infrastructure.Persistence;

// Esta clase es la unica que sabe hablar con Postgres.
// Se encarga de:
//   1) abrir y cerrar la conexion a la base
//   2) preparar el comando SQL
//   3) pasarle los parametros
//   4) ejecutar y devolver los resultados como diccionarios
//      (clave = nombre de columna, valor = lo que trajo la base)
//
// El Repository es el que despues transforma esos diccionarios en entidades
// (Game, User, etc.). Esa separacion hace que esta clase no tenga que saber
// nada de que entidades existen en el proyecto: solo sabe de SQL y datos.
public class DBPostgres
{
    private readonly string _connectionString;

    public DBPostgres()
    {
        _connectionString = DBConfig.GetPostgresConnectionString();
    }

    // Ejecuta un INSERT, UPDATE o DELETE y devuelve cuantas filas se tocaron.
    public int Execute(string sql, Dictionary<string, object>? parameters = null)
    {
        // El bloque "using (...)" se asegura de que la conexion se cierre
        // cuando terminamos, incluso si hay un error.
        //
        // Hay una forma mas corta con "using var connection = new ...;"
        // (sin llaves), pero aca usamos la forma larga para que se vea
        // donde empieza y donde termina el scope.
        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                // Copiamos cada entrada del diccionario como parametro SQL.
                // Si el dict tiene {"Id": 5}, el comando se entera de que
                // @Id vale 5.
                AddParametersToCommand(command, parameters);

                // ExecuteNonQuery se usa cuando la query no devuelve filas
                // (INSERT, UPDATE, DELETE). Devuelve el numero de filas
                // que quedaron afectadas.
                int affectedRows = command.ExecuteNonQuery();
                return affectedRows;
            }
        }
    }

    // Ejecuta un SELECT que trae varias filas. Cada fila se devuelve como
    // un diccionario: las claves son los nombres de columna, los valores
    // son lo que devolvio la base (puede ser null).
    public List<Dictionary<string, object?>> Query(string sql, Dictionary<string, object>? parameters = null)
    {
        List<Dictionary<string, object?>> allRows = new List<Dictionary<string, object?>>();

        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                AddParametersToCommand(command, parameters);

                // El reader recorre los resultados fila por fila.
                // En cada vuelta del while tenemos una fila nueva.
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Dictionary<string, object?> currentRow = ReadCurrentRow(reader);
                        allRows.Add(currentRow);
                    }
                }
            }
        }

        return allRows;
    }

    // Ejecuta un SELECT y devuelve solo la primera fila, o null si no hay.
    public Dictionary<string, object?>? QueryFirst(string sql, Dictionary<string, object>? parameters = null)
    {
        List<Dictionary<string, object?>> allRows = Query(sql, parameters);

        if (allRows.Count == 0)
        {
            return null;
        }

        Dictionary<string, object?> firstRow = allRows[0];
        return firstRow;
    }

    // Ejecuta una query que devuelve un solo valor. Sirve para cosas como
    // SELECT COUNT(*) o un SELECT que devuelve una sola columna de una
    // sola fila. El Repository despues convierte este object al tipo
    // concreto que necesita (int, string, etc.).
    public object? ExecuteScalar(string sql, Dictionary<string, object>? parameters = null)
    {
        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                AddParametersToCommand(command, parameters);

                object? result = command.ExecuteScalar();

                // La base puede devolver DBNull (su forma propia de decir
                // "null"). Lo convertimos a null de C# para que el Repository
                // pueda chequearlo de manera uniforme.
                if (result == null || result == DBNull.Value)
                {
                    return null;
                }

                return result;
            }
        }
    }

    // Agrega cada par (clave -> valor) del diccionario al comando como
    // parametro SQL. La clave "Id" se convierte en el parametro "@Id".
    private static void AddParametersToCommand(NpgsqlCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters == null)
        {
            return;
        }

        foreach (KeyValuePair<string, object> parameter in parameters)
        {
            string parameterName = "@" + parameter.Key;
            object parameterValue = parameter.Value;
            command.Parameters.AddWithValue(parameterName, parameterValue);
        }
    }

    // Lee la fila actual del reader y la arma como diccionario.
    // Usamos un diccionario case-insensitive (StringComparer.OrdinalIgnoreCase)
    // para que no importe si la base devolvio "GameName" o "gamename".
    private static Dictionary<string, object?> ReadCurrentRow(NpgsqlDataReader reader)
    {
        Dictionary<string, object?> row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (int columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
        {
            string columnName = reader.GetName(columnIndex);
            object? columnValue;

            if (reader.IsDBNull(columnIndex))
            {
                columnValue = null;
            }
            else
            {
                columnValue = reader.GetValue(columnIndex);
            }

            row[columnName] = columnValue;
        }

        return row;
    }
}
