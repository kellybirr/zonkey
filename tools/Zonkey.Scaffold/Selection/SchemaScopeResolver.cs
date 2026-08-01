using Zonkey.Scaffold.Diagnostics;

namespace Zonkey.Scaffold.Selection;

/// <summary>
/// Decides which schemas to generate from. There is deliberately no implicit all-schemas
/// default: implicit scope makes `check` fire when an unrelated schema is added later, and
/// makes cross-schema class-name collisions surface hardest on the databases where generating
/// everything was least wanted.
/// </summary>
public static class SchemaScopeResolver
{
    public static IReadOnlyList<string> Resolve(
        IReadOnlyList<string> requested,
        IReadOnlyList<string> availableNonSystem)
    {
        if (availableNonSystem.Count == 0)
            throw new ScaffoldException(
                "No non-system schemas found in this database. Check the connection string and " +
                "that the account can read catalog metadata.");

        if (requested.Count == 1 && requested[0] == "*")
            return availableNonSystem;

        if (requested.Count > 0)
        {
            var resolved = new List<string>();
            foreach (string want in requested)
            {
                string? match = availableNonSystem
                    .FirstOrDefault(a => string.Equals(a, want, StringComparison.OrdinalIgnoreCase));

                if (match is null)
                    throw new ScaffoldException(
                        $"Schema '{want}' was not found. Available schemas: " +
                        $"{string.Join(", ", availableNonSystem)}.");

                resolved.Add(match);
            }
            return resolved;
        }

        if (availableNonSystem.Count == 1)
            return availableNonSystem;

        throw new ScaffoldException(
            $"This database has {availableNonSystem.Count} non-system schemas: " +
            $"{string.Join(", ", availableNonSystem)}. " +
            "Specify which to generate from with --schema <name> (repeatable), " +
            "or --schema \"*\" for all of them.");
    }
}
