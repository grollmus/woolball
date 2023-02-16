using WoolBall.Graph;

namespace WoolBall.Tests.Graph;

public sealed class ContainerFilterTests
{
	[Fact]
	public void DoesNothing_WhenNoFiltersSpecified()
	{
		// Arrange
		var source = new TypeContainer();
		source.AddProjects(new[] { "Project1", "Project2" });
		source.AddProjectReference("Project1", "Project2");
		source.AddTypes("Project1", new[] { "Type1", "Type2" });
		source.AddTypeReference("Type1", "Type2");

		var sut = new ContainerFilter(Array.Empty<string>(), Array.Empty<string>(), true);

		// Act
		var filtered = sut.Apply(source);

		// Assert
		filtered.Projects.Should().BeEquivalentTo(source.Projects);
		filtered.Types.Should().BeEquivalentTo(source.Types);
		filtered.ProjectReferences.Should().BeEquivalentTo(source.ProjectReferences);
		filtered.TypeReferences.Should().BeEquivalentTo(source.TypeReferences);
	}
}