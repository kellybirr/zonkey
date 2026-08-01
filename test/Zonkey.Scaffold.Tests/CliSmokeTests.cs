using Xunit;
using Zonkey.Scaffold;

// Program.Main's --help and parse-error paths write straight to Console.Out/Console.Error
// (System.CommandLine's default InvocationConfiguration.Output/Error), so this class joins
// CommandTests in the "ScaffoldConsole" collection — see the doc comment on
// ScaffoldConsoleCollection in Cli/CommandTests.cs for why.
[Collection("ScaffoldConsole")]
public class CliSmokeTests
{
    [Fact]
    public async Task Help_exits_zero()
    {
        int exit = await Program.Main(["--help"]);
        Assert.Equal(0, exit);
    }

    [Fact]
    public async Task Unknown_command_exits_nonzero()
    {
        int exit = await Program.Main(["frobnicate"]);
        Assert.NotEqual(0, exit);
    }
}
