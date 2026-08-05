using Xunit;
using Zonkey.Scaffold;

// Program.Main writes straight to Console.Out/Console.Error, so these run in their own
// collection rather than in parallel with anything else that touches the console.
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
