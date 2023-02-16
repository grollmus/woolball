using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Hosting;
using Serilog;
using WoolBall.Bootstrapping;
using WoolBall.CommandLine;

await new CommandLineBuilder(new RunCommand())
	.UseHost(_ => Host.CreateDefaultBuilder()
			.UseSerilog((_, config) =>
			{
				config
					.MinimumLevel.Verbose()
					.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
					.WriteTo.Console();
			}),
		host =>
		{
			host.ConfigureServices(services => { services.AddServices(); });

			host.ConfigureCommandHandlers();
		})
	.UseDefaults()
	.Build()
	.InvokeAsync(args);