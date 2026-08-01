using System.CommandLine;
using Zonkey.Scaffold.Cli;
using Zonkey.Scaffold.Config;
using Zonkey.Scaffold.Diagnostics;

namespace Zonkey.Scaffold;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // The default InvocationConfiguration has EnableDefaultExceptionHandler = true, which
            // catches any exception thrown by a command action *before* the catch blocks below
            // ever see it — it prints its own message (unredacted: it has no idea SecretRedactor
            // exists) and returns exit code 1 for everything, unexpected exceptions included. That
            // defeats both the exit-code contract (1 = refused, 2 = crashed) and the entire reason
            // SecretRedactor exists: an unhandled exception from a database driver routinely echoes
            // the connection string verbatim. Disabling it here is what makes the catch blocks
            // below the only exception handler in the process.
            return await CommandFactory.Create().Parse(args)
                .InvokeAsync(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
        }
        catch (ScaffoldException ex)
        {
            // Already user-facing and already carries its remedy — print it as-is.
            Console.Error.WriteLine(SecretRedactor.Redact(ex.Message, connectionString: null));
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                SecretRedactor.Redact($"Unexpected error: {ex.Message}", connectionString: null));
            return 2;
        }
    }
}
