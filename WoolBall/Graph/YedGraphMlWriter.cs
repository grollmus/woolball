using System.Xml.Linq;

namespace WoolBall.Graph;

// http://graphml.graphdrawing.org/primer/graphml-primer.html
internal sealed class YedGraphMlWriter : IGraphWriter
{
	public string CommonNamePrefix { get; set; } = "";
	private int Id => ++_id;

	public XDocument Write(TypeContainer container)
	{
		XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
		var xml = new XDocument(new XDeclaration("1.0", "UTF-8", null));

		var root = new XElement("graphml",
			new XAttribute(XNamespace.Xmlns + "y", "http://www.yworks.com/xml/graphml"),
			new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
			new XAttribute(xsi + "schemaLocator", "http://graphml.graphdrawing.org/xmlns http://www.yworks.com/xml/schema/graphml/1.0/ygraphml.xsd"),
			new XElement("key",
				new XAttribute("attr.name", "description"),
				new XAttribute("attr.type", "string"),
				new XAttribute("for", "node"), new XAttribute("id", "d0")),
			new XElement("key",
				new XAttribute("yfiles.type", "nodegraphics"),
				new XAttribute("for", "node"), new XAttribute("id", "d1")));

		var graph = new XElement("graph",
			new XAttribute("edgedefault", "directed"),
			new XAttribute("id", "G")
		);

		AddProjectNodesTo(graph, container);
		AddProjectReferencesTo(graph, container);
		AddTypeReferencesTo(graph, container);

		root.Add(graph);
		xml.Add(root);
		return xml;
	}

	private void AddProjectNodesTo(XElement xml, TypeContainer container)
	{
		foreach (var project in container.Projects)
		{
			var id = Id;
			_idMap.Add(project, id);

			var subGraph = new XElement("graph", new XAttribute("id", $"g{id}"), new XAttribute("edgedefault", "directed"));
			AddTypesTo(subGraph, project, container);

			var displayName = DisplayName(project, null);

			xml.Add(new XElement("node", NodeId(id), NodeName(displayName), subGraph));
		}
	}

	private void AddProjectReferencesTo(XElement xml, TypeContainer container)
	{
		foreach (var reference in container.ProjectReferences)
		{
			var sourceId = _idMap[reference.Source];
			var targetId = _idMap[reference.Target];
			xml.Add(new XElement("edge",
				new XAttribute("source", $"n{sourceId}"),
				new XAttribute("target", $"n{targetId}")
			));
		}
	}

	private void AddTypeReferencesTo(XElement xml, TypeContainer container)
	{
		foreach (var reference in container.TypeReferences)
		{
			if (
				!_idMap.TryGetValue(reference.Source, out var sourceId) ||
				!_idMap.TryGetValue(reference.Target, out var targetId))
				continue;

			xml.Add(new XElement("edge",
				new XAttribute("source", $"n{sourceId}"),
				new XAttribute("target", $"n{targetId}")
			));
		}
	}

	private void AddTypesTo(XElement xml, string project, TypeContainer container)
	{
		var typeList = container.Types[project];
		foreach (var type in typeList)
		{
			if (_idMap.ContainsKey(type))
				continue;

			var id = Id;
			_idMap.Add(type, id);

			var displayName = DisplayName(type, project);
			xml.Add(new XElement("node",
				NodeId(id),
				NodeName(displayName),
				NodeLabel(displayName)
			));
		}
	}

	private string DisplayName(string name, string? parentName)
	{
		name = name
			.Replace("global::", "")
			.Replace(CommonNamePrefix, "");

		if (!string.IsNullOrEmpty(parentName))
		{
			var parentDisplayName = DisplayName(parentName, null);

			if (name.StartsWith(parentDisplayName))
				name = name[(parentDisplayName.Length + 1)..];
		}

		return name;
	}

	private static XAttribute NodeId(int id) => new("id", $"n{id}");

	private static XElement NodeLabel(string displayName) => new("data", new XAttribute("key", "d1"),
		new XElement(Y + "ShapeNode", new XElement(Y + "NodeLabel", displayName)));

	private static XElement NodeName(string displayName) => new("data", new XAttribute("key", "d0"), displayName);

	private readonly Dictionary<string, int> _idMap = new();
	private int _id;

	private static readonly XNamespace Y = "http://www.yworks.com/xml/graphml";
}