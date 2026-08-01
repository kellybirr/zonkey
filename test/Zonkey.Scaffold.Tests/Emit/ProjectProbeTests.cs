using Xunit;
using Zonkey.Scaffold.Emit;

public class ProjectProbeTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("zprobe").FullName;
    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string WriteProject(string fileName, string body)
    {
        string path = Path.Combine(_root, fileName);
        File.WriteAllText(path, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
            {body}
              </PropertyGroup>
            </Project>
            """);
        return path;
    }

    [Fact]
    public void Finds_project_in_ancestor_directory()
    {
        WriteProject("App.csproj", "    <TargetFramework>net10.0</TargetFramework>");
        string nested = Directory.CreateDirectory(Path.Combine(_root, "Data", "Entities")).FullName;

        var caps = ProjectProbe.Probe(nested);

        Assert.NotNull(caps.ProjectPath);
        Assert.EndsWith("App.csproj", caps.ProjectPath);
    }

    [Fact]
    public void Net10_without_explicit_langversion_supports_field_keyword()
    {
        WriteProject("App.csproj", "    <TargetFramework>net10.0</TargetFramework>");
        Assert.True(ProjectProbe.Probe(_root).SupportsFieldKeyword);
    }

    [Fact]
    public void Net8_does_not_support_field_keyword()
    {
        WriteProject("App.csproj", "    <TargetFramework>net8.0</TargetFramework>");
        Assert.False(ProjectProbe.Probe(_root).SupportsFieldKeyword);
    }

    [Fact]
    public void Explicit_langversion_latest_supports_field_keyword()
    {
        WriteProject("App.csproj", """
                <TargetFramework>net8.0</TargetFramework>
                <LangVersion>latest</LangVersion>
            """);
        Assert.True(ProjectProbe.Probe(_root).SupportsFieldKeyword);
    }

    [Fact]
    public void Explicit_langversion_12_does_not()
    {
        WriteProject("App.csproj", """
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>12.0</LangVersion>
            """);
        Assert.False(ProjectProbe.Probe(_root).SupportsFieldKeyword);
    }

    [Fact]
    public void Vbproj_never_supports_field_keyword()
    {
        WriteProject("App.vbproj", "    <TargetFramework>net10.0</TargetFramework>");
        Assert.False(ProjectProbe.Probe(_root).SupportsFieldKeyword);
    }

    [Fact]
    public void Nullable_enable_is_detected()
    {
        WriteProject("App.csproj", """
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
            """);
        Assert.True(ProjectProbe.Probe(_root).NullableEnabled);
    }

    [Fact]
    public void Missing_nullable_property_reads_as_disabled()
    {
        WriteProject("App.csproj", "    <TargetFramework>net10.0</TargetFramework>");
        Assert.False(ProjectProbe.Probe(_root).NullableEnabled);
    }

    [Fact]
    public void No_project_anywhere_yields_conservative_defaults()
    {
        var caps = ProjectProbe.Probe(_root);
        Assert.Null(caps.ProjectPath);
        Assert.False(caps.SupportsFieldKeyword);
        Assert.False(caps.NullableEnabled);
    }

    [Theory]
    [InlineData("net10.0", true)]
    [InlineData("net9.0", false)]
    [InlineData("net8.0", false)]
    [InlineData("net11.0", true)]
    [InlineData("net10.0-windows", true)]
    [InlineData("net48", false)]
    [InlineData("net472", false)]
    [InlineData("netstandard2.0", false)]
    [InlineData("", false)]
    [InlineData("nonsense", false)]
    public void TargetFramework_matrix_without_explicit_langversion(string tfm, bool expected)
    {
        WriteProject("App.csproj", $"    <TargetFramework>{tfm}</TargetFramework>");
        Assert.Equal(expected, ProjectProbe.Probe(_root).SupportsFieldKeyword);
    }

    [Theory]
    [InlineData("net10.0;net8.0", false)]
    [InlineData("net8.0;net10.0", false)]
    [InlineData("net10.0;net11.0", true)]
    public void TargetFrameworks_multi_target_requires_all_entries_to_support(string tfms, bool expected)
    {
        WriteProject("App.csproj", $"    <TargetFrameworks>{tfms}</TargetFrameworks>");
        Assert.Equal(expected, ProjectProbe.Probe(_root).SupportsFieldKeyword);
    }

    [Theory]
    [InlineData("latest", true)]
    [InlineData("preview", true)]
    [InlineData("latestMajor", true)]
    [InlineData("14.0", true)]
    [InlineData("14", true)]
    [InlineData("13.0", false)]
    [InlineData("12", false)]
    [InlineData("abc", false)]
    public void LangVersion_matrix_overrides_target_framework(string langVersion, bool expected)
    {
        // net8.0 alone would not support the field keyword, so these cases isolate
        // the LangVersion branch: an explicit value always wins over the TFM fallback.
        WriteProject("App.csproj", $"""
                <TargetFramework>net8.0</TargetFramework>
                <LangVersion>{langVersion}</LangVersion>
            """);
        Assert.Equal(expected, ProjectProbe.Probe(_root).SupportsFieldKeyword);
    }
}
