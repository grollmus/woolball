using System.Xml.Linq;

namespace WoolBall.Graph;

internal interface IGraphWriter
{
	XDocument Write(TypeContainer container);
	string CommonNamePrefix { get; set; }
}