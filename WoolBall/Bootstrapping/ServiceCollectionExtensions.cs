using Microsoft.Extensions.DependencyInjection;
using WoolBall.Graph;
using WoolBall.Parsing;

namespace WoolBall.Bootstrapping;

internal static class ServiceCollectionExtensions
{
	public static void AddServices(this IServiceCollection services)
	{
		services.AddSingleton<IGraphWriter, YedGraphMlWriter>();
		services.AddSingleton<ISolutionLoader, SolutionLoader>();
		services.AddSingleton<ITypeExtractor, TypeExtractor>();
	}
}