using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Mapping;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Providers;

public static class ProviderFactory
{
    /// <summary>Accepted aliases per provider key. Plan 2 adds pgsql, mysql, and mssql.</summary>
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sqlite"] = "sqlite",
        ["sqlite3"] = "sqlite",
        ["postgresql"] = "postgresql",
        ["postgres"] = "postgresql",
        ["pgsql"] = "postgresql",
        ["pg"] = "postgresql",
        ["mysql"] = "mysql",
        ["mariadb"] = "mysql",
        ["sqlserver"] = "sqlserver",
        ["mssql"] = "sqlserver",
        ["sql"] = "sqlserver",
    };

    public static string Normalize(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ScaffoldException(
                "No provider specified. Use --provider with one of: " + Supported() + ".");

        if (Aliases.TryGetValue(provider, out string? key)) return key;

        throw new ScaffoldException(
            $"Unknown provider '{provider}'. Supported providers: {Supported()}.");
    }

    public static ISchemaReader CreateReader(string provider, string connectionString)
        => Normalize(provider) switch
        {
            "sqlite" => new SqliteSchemaReader(connectionString),
            "postgresql" => new PostgreSqlSchemaReader(connectionString),
            "mysql" => new MySqlSchemaReader(connectionString),
            "sqlserver" => new SqlServerSchemaReader(connectionString),
            _ => throw new ScaffoldException($"Unknown provider '{provider}'.")
        };

    public static ITypeMapper CreateTypeMapper(string provider)
        => Normalize(provider) switch
        {
            "sqlite" => new SqliteTypeMapper(),
            "postgresql" => new PostgreSqlTypeMapper(),
            "mysql" => new MySqlTypeMapper(),
            "sqlserver" => new SqlServerTypeMapper(),
            _ => throw new ScaffoldException($"Unknown provider '{provider}'.")
        };

    private static string Supported()
        => string.Join(", ", Aliases.Values.Distinct().OrderBy(v => v, StringComparer.Ordinal));
}
