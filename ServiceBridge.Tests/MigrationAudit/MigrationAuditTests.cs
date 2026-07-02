using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace ServiceBridge.Tests.MigrationAudit;

public class MigrationAuditTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ProjectReferences_FollowLayeredMonolithBoundaries()
    {
        AssertProjectReferences("ServiceBridge.Domain", []);
        AssertProjectReferences("ServiceBridge.Application",
        [
            "ServiceBridge.Domain",
            "ServiceBridge.Shared"
        ]);
        AssertProjectReferences("ServiceBridge.Infrastructure",
        [
            "ServiceBridge.Application",
            "ServiceBridge.Domain",
            "ServiceBridge.Shared"
        ]);
        AssertProjectReferences("ServiceBridge.Api",
        [
            "ServiceBridge.Application",
            "ServiceBridge.Infrastructure",
            "ServiceBridge.Shared"
        ]);
        AssertProjectReferences("ServiceBridge.Web",
        [
            "ServiceBridge.Application",
            "ServiceBridge.Shared"
        ]);
    }

    [Fact]
    public void MigratedTextFiles_DoNotContainLegacyBrandToken()
    {
        var legacyToken = string.Concat("Pay", "Bridge");
        var matches = EnumerateAuditedFiles()
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, lineNumber = index + 1 }))
            .Where(item => item.line.Contains(legacyToken, StringComparison.OrdinalIgnoreCase))
            .Select(item => $"{Path.GetRelativePath(RepositoryRoot, item.path)}:{item.lineNumber}")
            .ToArray();

        Assert.Empty(matches);
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void ApiAppSettings_DoNotContainCommittedRuntimeSecrets(
        string fileName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(RepositoryRoot, "ServiceBridge.Api", fileName)));
        var root = document.RootElement;

        Assert.Empty(GetOptionalString(
            root,
            "ConnectionStrings",
            "DefaultConnection"));
        Assert.Empty(GetOptionalString(root, "Jwt", "SigningKey"));
        Assert.Empty(GetOptionalString(root, "AdminSeed", "Password"));
    }

    private static void AssertProjectReferences(string projectName, string[] expectedProjectNames)
    {
        var projectPath = Path.Combine(RepositoryRoot, projectName, $"{projectName}.csproj");
        var actualReferences = XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFileNameWithoutExtension(value!))
            .OrderBy(value => value)
            .ToArray();

        Assert.Equal(expectedProjectNames.OrderBy(value => value), actualReferences);
    }

    private static IEnumerable<string> EnumerateAuditedFiles()
    {
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".cshtml",
            ".csproj",
            ".sln",
            ".json",
            ".md",
            ".ps1"
        };

        return Directory.EnumerateFiles(RepositoryRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsUnderExcludedDirectory(path))
            .Where(path => allowedExtensions.Contains(Path.GetExtension(path)) ||
                           Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase))
            .Where(path =>
                Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
                path.Contains($"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(path) is ".cs" or ".cshtml" or ".csproj" or ".sln" or ".json" or ".ps1");
    }

    private static bool IsUnderExcludedDirectory(string path)
    {
        var relativePath = Path.GetRelativePath(RepositoryRoot, path);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Any(segment =>
            segment.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals(".dotnet_cli_home", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetOptionalString(
        JsonElement root,
        string sectionName,
        string propertyName)
    {
        if (!root.TryGetProperty(sectionName, out var section) ||
            !section.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return property.GetString() ?? string.Empty;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ServiceBridge.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate ServiceBridge.sln.");
    }
}
