using System.CommandLine.Hosting;
using Microsoft.Extensions.Hosting;
using WoolBall.CommandLine;

namespace WoolBall.Bootstrapping;

internal static class HostBuilderExtensions
{
	public static void ConfigureCommandHandlers(this IHostBuilder host)
	{
		host.UseCommandHandler<RunCommand, RunCommand.Handler>();
	}
}