using WoolBall.Services;

namespace WoolBall.Graph;

internal sealed class TypeContainer
{
	public void AddProjectReference(string projectName, string referencedProjectName)
	{
		_projectReferences.Add(new Reference(projectName, referencedProjectName));
	}

	public void AddProjects(IEnumerable<string> projects)
	{
		foreach (var project in projects)
		{
			_projects.Add(project);
			
			if (!_types.ContainsKey(project))
				_types.Add(project, new List<string>());
		}
		
	}

	public void AddTypeReference(string typeName, string referencedTypeName)
	{
		_typeReferences.Add(new Reference(typeName, referencedTypeName));
	}

	public void AddTypes(string project, IEnumerable<string> types)
	{
		if (!_types.TryGetValue(project, out var typeList))
		{
			typeList = new List<string>();
			_types.Add(project, typeList);
		}
		
		typeList.AddRange(types);
	}

	public IReadOnlyCollection<string> Projects => _projects;
	public IReadOnlyCollection<Reference> ProjectReferences => _projectReferences;
	public IReadOnlyDictionary<string, List<string>> Types => _types;
	public IReadOnlyCollection<Reference> TypeReferences => _typeReferences;

	private readonly List<string> _projects = new();
	private readonly List<Reference> _projectReferences = new();
	private readonly Dictionary<string, List<string>> _types = new();
	private readonly List<Reference> _typeReferences = new();
}