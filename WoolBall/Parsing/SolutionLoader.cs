using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Serilog;

namespace WoolBall.Parsing;

internal sealed class SolutionLoader : IProgress<ProjectLoadProgress>, ISolutionLoader
{
	public SolutionLoader()
	{
		MSBuildLocator.RegisterDefaults();
	}

	public async Task<List<Project>> ParseSolution(string solutionFile, bool includeTests)
	{
		Log.Information("Loading {SolutionFile}", solutionFile);

		using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string> { { "CheckForSystemRuntimeDependency", "true" } });
		workspace.LoadMetadataForReferencedProjects = true;
		workspace.AssociateFileExtensionWithLanguage("depproj", LanguageNames.CSharp);
		var msBuildSolution = await workspace.OpenSolutionAsync(solutionFile, this);

		var projects = msBuildSolution.Projects;
		if (!includeTests)
			projects = projects.Where(IsNotTestProject);

		return projects.ToList();
	}

	public void Report(ProjectLoadProgress value)
	{
		Log.Debug("{FilePath} {Operation} [{Time}]", value.FilePath, value.Operation,
			value.ElapsedTime);
	}

	private static bool IsNotTestProject(Project project) => !IsTestProject(project);

	private static bool IsTestProject(Project project)
	{
		var testProjectAssemblies = new[]
		{
			"xunit.core.dll",
			"nunit.framework.dll",
			"Microsoft.VisualStudio.TestPlatform.TestFramework.dll"
		};

		var result = project.MetadataReferences
			.Select(reference => Path.GetFileName(reference.Display))
			.Any(assemblyName => testProjectAssemblies.Contains(assemblyName, StringComparer.OrdinalIgnoreCase));

		return result;
	}
}