using Xunit;

namespace ServiceBridge.Tests.Architecture;

public sealed class DeliveryArtifactBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ApiControllersDoNotExposeDeliveryArtifactOrProtectedValue()
    {
        var controllerRoot = Path.Combine(
            RepositoryRoot,
            "ServiceBridge.Api",
            "Controllers");
        var controllerSources = Directory
            .GetFiles(controllerRoot, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .ToArray();

        Assert.DoesNotContain(
            controllerSources,
            source => source.Contains(
                "DeliveryArtifact",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            controllerSources,
            source => source.Contains(
                "ProtectedValue",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionFlowsDoNotRevealOrCreateArtifacts()
    {
        var references = ProductionSources()
            .Where(item =>
                item.Source.Contains(
                    "IDeliveryArtifactProtector",
                    StringComparison.Ordinal) ||
                item.Source.Contains(
                    "IDeliveryArtifactWriter",
                    StringComparison.Ordinal) ||
                item.Source.Contains(
                    "DeliveryArtifact.Create(",
                    StringComparison.Ordinal) ||
                item.Source.Contains(
                    "DeliveryArtifacts.Add(",
                    StringComparison.Ordinal))
            .Select(item => Path.GetRelativePath(RepositoryRoot, item.Path))
            .OrderBy(path => path)
            .ToArray();

        Assert.Equal(
            new[]
            {
                Path.Combine(
                    "ServiceBridge.Api",
                    "Program.cs"),
                Path.Combine(
                    "ServiceBridge.Application",
                    "Interfaces",
                    "IDeliveryArtifactProtector.cs"),
                Path.Combine(
                    "ServiceBridge.Application",
                    "Interfaces",
                    "IDeliveryArtifactWriter.cs"),
                Path.Combine(
                    "ServiceBridge.Infrastructure",
                    "Services",
                    "DataProtectionDeliveryArtifactProtector.cs"),
                Path.Combine(
                    "ServiceBridge.Infrastructure",
                    "Services",
                    "DeliveryArtifactWriter.cs")
            }.OrderBy(path => path),
            references);

        var protectorSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "ServiceBridge.Infrastructure",
            "Services",
            "DataProtectionDeliveryArtifactProtector.cs"));
        var writerSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "ServiceBridge.Infrastructure",
            "Services",
            "DeliveryArtifactWriter.cs"));

        Assert.DoesNotContain("ILogger", protectorSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ILogger", writerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", writerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", writerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Notification", writerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Realtime", writerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Audit", writerSource, StringComparison.Ordinal);
    }

    private static IEnumerable<(string Path, string Source)> ProductionSources()
    {
        var projectNames = new[]
        {
            "ServiceBridge.Api",
            "ServiceBridge.Application",
            "ServiceBridge.Domain",
            "ServiceBridge.Infrastructure",
            "ServiceBridge.Web"
        };

        return projectNames.SelectMany(projectName => Directory
            .GetFiles(
                Path.Combine(RepositoryRoot, projectName),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Select(path => (path, File.ReadAllText(path))));
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
