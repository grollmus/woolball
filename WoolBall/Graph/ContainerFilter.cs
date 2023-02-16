using System.Text;
using System.Text.RegularExpressions;

namespace WoolBall.Graph;

internal sealed class ContainerFilter
{
	public ContainerFilter(string[] filters, string[] typeExcludes, bool includeOrphaned)
	{
		_filters = filters;
		_includeOrphaned = includeOrphaned;
		_excludeRegex = BuildExcludeRegex(typeExcludes);
	}

	public TypeContainer Apply(TypeContainer source)
	{
		var container = new TypeContainer();

		AddProjects(source, container);
		AddTypes(source, container);

		if( !_includeOrphaned)
			RemoveOrphans(container);

		return container;
	}

	private void AddProjects(TypeContainer from, TypeContainer to)
	{
		var projects = FindProjects(from);
		to.AddProjects(projects);

		foreach (var reference in from.ProjectReferences)
		{
			if (IsIncluded(reference.Source))
				to.AddProjectReference(reference.Source, reference.Target);
		}
	}

	private void AddTypes(TypeContainer from, TypeContainer to)
	{
		foreach (var project in to.Projects)
		{
			to.AddTypes(project, from.Types[project].Where(IsTypeIncluded));
		}

		foreach (var reference in from.TypeReferences)
		{
			if (IsTypeIncludedIn(reference.Source, to))
				to.AddTypeReference(reference.Source, reference.Target);
		}
	}

	private static Regex BuildExcludeRegex(IEnumerable<string> typeExcludes)
	{
		var pattern = new StringBuilder();
		foreach (var exclude in typeExcludes)
		{
			if (pattern.Length > 0)
				pattern.Append('|');

			pattern.Append(exclude.Replace(".", @"\.").Replace("*", ".*"));
		}

		return new Regex(pattern.ToString(), RegexOptions.Compiled, TimeSpan.FromSeconds(1));
	}

	private IEnumerable<string> FindProjects(TypeContainer source)
	{
		var list = new List<string>();
		list.AddRange(source.Projects.Where(IsIncluded));
		foreach (var reference in source.ProjectReferences.Where(r => IsIncluded(r.Source)))
		{
			list.Add(reference.Source);
			list.Add(reference.Target);
		}

		return list.Distinct();
	}

	private bool IsIncluded(string projectName)
	{
		if (!_filters.Any())
			return true;

		return _filters.Contains(projectName, StringComparer.OrdinalIgnoreCase);
	}

	private bool IsTypeIncluded(string type) => !_excludeRegex.IsMatch(type);

	private bool IsTypeIncludedIn(string type, TypeContainer container)
	{
		foreach (var projectTypes in container.Types.Values)
		{
			foreach (var projectType in projectTypes)
			{
				if (projectType.Equals(type, StringComparison.OrdinalIgnoreCase))
					return true;
			}
		}

		return false;
	}

	private void RemoveOrphans(TypeContainer container)
	{
		foreach (var project in container.Projects)
		{
			var projectTypes = container.Types[project];
			var typesToRemove = new List<string>();

			foreach (var type in projectTypes)
			{
				if (!container.TypeReferences.Any(r => r.Source == type || r.Target == type))
					typesToRemove.Add(type);
			}

			foreach (var type in typesToRemove)
			{
				projectTypes.Remove(type);
			}
		}
	}

	private readonly Regex _excludeRegex;

	private readonly string[] _filters;
	private readonly bool _includeOrphaned;
}