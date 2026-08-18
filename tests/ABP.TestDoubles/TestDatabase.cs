using Microsoft.Data.SqlClient;

namespace ABP.TestDoubles;

/// <summary>
/// Centraliza la resolución del servidor SQL de los tests de integración.
/// Prioridad: ABP_TEST_SQL_CONNECTION → ABP_TEST_SQL_SERVER → auto-detección
/// (LocalDB en Windows; si no está disponible, la instancia por defecto localhost).
/// </summary>
public static class TestDatabase
{
    private static readonly Lazy<string> DefaultServer = new(ResolveDefaultServer);

    public static string CreateConnectionString(string databaseName)
    {
        var configured = Environment.GetEnvironmentVariable("ABP_TEST_SQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return new SqlConnectionStringBuilder(configured)
            {
                InitialCatalog = databaseName
            }.ConnectionString;
        }

        var server = ResolveServer();
        return new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        }.ConnectionString;
    }

    public static string ResolveServer()
    {
        var configured = Environment.GetEnvironmentVariable("ABP_TEST_SQL_SERVER");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return DefaultServer.Value;
    }

    private static string ResolveDefaultServer()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "localhost";
        }

        return CanConnectToServer(@"(localdb)\MSSQLLocalDB")
            ? @"(localdb)\MSSQLLocalDB"
            : "localhost";
    }

    private static bool CanConnectToServer(string dataSource)
    {
        try
        {
            using var connection = new SqlConnection(
                new SqlConnectionStringBuilder
                {
                    DataSource = dataSource,
                    IntegratedSecurity = true,
                    TrustServerCertificate = true,
                    ConnectTimeout = 3
                }.ConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
