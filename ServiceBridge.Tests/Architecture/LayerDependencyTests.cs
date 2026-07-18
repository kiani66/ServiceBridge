using System.Xml.Linq;
using Xunit;

namespace ServiceBridge.Tests.Architecture;

public class LayerDependencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Domain_HasNoHigherLayerOrFrameworkDependencies()
    {
        var dependencies = ReadDependencies("ServiceBridge.Domain");

        Assert.Empty(dependencies.ProjectReferences);
        AssertNoDependencyContains(
            dependencies,
            "ServiceBridge.Application",
            "ServiceBridge.Infrastructure",
            "ServiceBridge.Api",
            "ServiceBridge.Web",
            "Microsoft.EntityFrameworkCore",
            "SqlServer",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Application_DependsOnlyOnDomainAndShared()
    {
        var dependencies = ReadDependencies("ServiceBridge.Application");

        AssertProjectReferences(
            dependencies,
            "ServiceBridge.Domain",
            "ServiceBridge.Shared");
        AssertNoDependencyContains(
            dependencies,
            "ServiceBridge.Infrastructure",
            "ServiceBridge.Api",
            "ServiceBridge.Web");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnPresentationProjects()
    {
        var dependencies = ReadDependencies("ServiceBridge.Infrastructure");

        AssertProjectReferences(
            dependencies,
            "ServiceBridge.Application",
            "ServiceBridge.Domain",
            "ServiceBridge.Shared");
        AssertNoDependencyContains(
            dependencies,
            "ServiceBridge.Api",
            "ServiceBridge.Web");
    }

    [Fact]
    public void PresentationProjects_RetainApprovedCompositionRootReferences()
    {
        AssertProjectReferences(
            ReadDependencies("ServiceBridge.Api"),
            "ServiceBridge.Application",
            "ServiceBridge.Infrastructure",
            "ServiceBridge.Shared");
        AssertProjectReferences(
            ReadDependencies("ServiceBridge.Web"),
            "ServiceBridge.Application",
            "ServiceBridge.Shared");
    }

    [Fact]
    public void Shared_RemainsAFrameworkIndependentLeafProject()
    {
        var dependencies = ReadDependencies("ServiceBridge.Shared");

        Assert.Empty(dependencies.ProjectReferences);
        Assert.Empty(dependencies.PackageReferences);
        Assert.Empty(dependencies.FrameworkReferences);
    }

    private static void AssertProjectReferences(
        ProjectDependencies dependencies,
        params string[] expectedProjectNames)
    {
        Assert.Equal(
            expectedProjectNames.OrderBy(value => value),
            dependencies.ProjectReferences);
    }

    private static void AssertNoDependencyContains(
        ProjectDependencies dependencies,
        params string[] forbiddenFragments)
    {
        var violations = dependencies.All
            .Where(reference => forbiddenFragments.Any(fragment =>
                reference.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.Empty(violations);
    }

    private static ProjectDependencies ReadDependencies(string projectName)
    {
        var projectPath = Path.Combine(
            RepositoryRoot,
            projectName,
            $"{projectName}.csproj");
        var document = XDocument.Load(projectPath);

        return new ProjectDependencies(
            ReadReferences(document, "ProjectReference", normalizeProjectName: true),
            ReadReferences(document, "PackageReference"),
            ReadReferences(document, "FrameworkReference"));
    }

    private static string[] ReadReferences(
        XDocument document,
        string elementName,
        bool normalizeProjectName = false)
    {
        return document
            .Descendants(elementName)
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => normalizeProjectName
                ? Path.GetFileNameWithoutExtension(value!)
                : value!)
            .OrderBy(value => value)
            .ToArray();
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

    private sealed record ProjectDependencies(
        string[] ProjectReferences,
        string[] PackageReferences,
        string[] FrameworkReferences)
    {
        public IEnumerable<string> All => ProjectReferences
            .Concat(PackageReferences)
            .Concat(FrameworkReferences);
    }
}
