using System.CommandLine;
using System.CommandLine.Invocation;
using JetBrains.Annotations;
using Serilog;
using WoolBall.Graph;
using WoolBall.Parsing;

namespace WoolBall.CommandLine;

internal sealed class RunCommand : RootCommand
{
	public RunCommand()
	{
		AddArgument(new Argument<FileInfo>("solution", "The solution file to parse."));
		AddArgument(new Argument<FileInfo>("output", "The output file to write the graph to."));

		AddOption(new Option<string[]>(new[] { "--exclude", "-e" }, Array.Empty<string>, "Exclude types."));
		AddOption(new Option<string[]>(new[] { "--filter", "-f" }, Array.Empty<string>, "Only include projects specified here."));
		AddOption(new Option<bool>(new[] { "--include-tests" }, () => false, "Include test projects in the graph."));
		AddOption(new Option<bool>(new[]{"--orphaned"}, () => false, "Include orphaned types in the graph."));
		AddOption(new Option<string[]>(new[]{"--display", "-d"}, () => new []{"all"}, "Only display the specified edges. (all, references, inheritance))"));
	}

	public new sealed class Handler : ICommandHandler
	{
		public Handler(ISolutionLoader solutionLoader, IGraphWriter graphWriter, ITypeExtractor typeExtractor)
		{
			_solutionLoader = solutionLoader;
			_graphWriter = graphWriter;
			_typeExtractor = typeExtractor;
		}
		[UsedImplicitly] public bool Orphaned { get; set; }
		[UsedImplicitly] public string[] Display { get; set; } = default!;
		
		[UsedImplicitly]
		public string[] Exclude { get; set; } = default!;

		[UsedImplicitly]
		public bool IncludeTests { get; set; }

		[UsedImplicitly]
		public string[] Filter { get; set; } = default!;

		[UsedImplicitly]
		public FileInfo Output { get; set; } = default!;

		[UsedImplicitly]
		public FileInfo Solution { get; set; } = default!;

		public int Invoke(InvocationContext context) => throw new NotImplementedException();

		public async Task<int> InvokeAsync(InvocationContext context)
		{
			_graphWriter.CommonNamePrefix = Path.GetFileNameWithoutExtension(Solution.FullName) + ".";

			var projects = await _solutionLoader.ParseSolution(Solution.FullName, IncludeTests);

			var referenceTypes = ParseReferenceTypes(Display);

			var container = new TypeContainer();
			container.AddProjects(projects.Select(p => p.Name));
			foreach (var project in projects)
			{
				Log.Information("Project: {ProjectName}", project.Name);

				foreach (var reference in project.ProjectReferences)
				{
					var referencedProject = projects.Single(p => p.Id == reference.ProjectId);
					container.AddProjectReference(project.Name, referencedProject.Name);
				}

				var types = await _typeExtractor.GetTypeNamesIn(project).ToListAsync();
				container.AddTypes(project.Name, types);

				var references = await _typeExtractor.GetTypeReferencesIn(project, referenceTypes).ToListAsync();
				foreach (var reference in references)
				{
					container.AddTypeReference(reference.Source, reference.Target);
				}
			}

			var filter = new ContainerFilter(Filter, Exclude, Orphaned);
			container = filter.Apply(container);

			var xml = _graphWriter.Write(container);
			xml.Save(Output.FullName);

			Log.Information("Done");
			return 0;
		}

		private static ReferenceType ParseReferenceTypes(IEnumerable<string> displays)
		{
			return displays.Aggregate(ReferenceType.None, (current, display) => current | ParseDisplayValue(display));
		}
		
		private static ReferenceType ParseDisplayValue(string display)
		{
			return display switch
			{
				"all" => ReferenceType.All,
				"references" => ReferenceType.Reference,
				"inheritance" => ReferenceType.Inheritance,
				_ => throw new ArgumentException($"Unknown display type: {display}")
			};
		}

		private readonly ISolutionLoader _solutionLoader;
		private readonly IGraphWriter _graphWriter;
		private readonly ITypeExtractor _typeExtractor;
	}
}

[Flags]
enum ReferenceType
{
	None = 0,
	Reference = 1,
	Inheritance = 2,
	All = Reference | Inheritance
}