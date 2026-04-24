using Microsoft.Data.SqlClient;

namespace proyect.Infrastructure.Persistence;

// Espejo de DBPostgres, pero para SQL Server Express (desarrollo local).
// La API publica es IDENTICA a la de DBPostgres. Un Repository puede
// cambiar de motor reemplazando `new DBPostgres()` por `new DBMssql()` y
// adaptando la sintaxis SQL (ILIKE -> LIKE, LIMIT -> TOP, etc.).
public class DBMssql
{
    private readonly string _connectionString;

    public DBMssql()
    {
        _connectionString = DBConfig.GetSqlServerConnectionString();
    }

    public int Execute(string sql, Dictionary<string, object>? parameters = null)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParametersToCommand(command, parameters);

                int affectedRows = command.ExecuteNonQuery();
                return affectedRows;
            }
        }
    }

    public List<Dictionary<string, object?>> Query(string sql, Dictionary<string, object>? parameters = null)
    {
        List<Dictionary<string, object?>> allRows = new List<Dictionary<string, object?>>();

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParametersToCommand(command, parameters);

                using (SqlDataReader reader = command.ExecuteReader())
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

    public object? ExecuteScalar(string sql, Dictionary<string, object>? parameters = null)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParametersToCommand(command, parameters);

                object? result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    return null;
                }

                return result;
            }
        }
    }

    private static void AddParametersToCommand(SqlCommand command, Dictionary<string, object>? parameters)
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

    private static Dictionary<string, object?> ReadCurrentRow(SqlDataReader reader)
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
