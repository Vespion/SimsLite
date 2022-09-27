// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using FrontDTOs;
using FrontDTOs.Headers;
using FrontDTOs.Messages;
using Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;
using ServerObjects;
using SimServer.ConfigMap;

public class SimHostBuilder : HostBuilder
{

	

// public new IHost Build()
	// {
	// 	base.Build();
	// }
}
public class Program
{
	public static async Task Main(string[] args)
	{
		var host = CreateHostBuilder(args).Build();
		var log = host.Services.GetService<ILogger<Program>>();
		log?.LogInformation("Bootstrapping complete! Launching host");
		await host.RunAsync();

		log?.LogInformation("Host shutdown complete! Running socket cleanup");
		NetMQConfig.Cleanup();
		log?.LogInformation("Socket cleanup complete");
		log?.LogInformation("Server shutdown complete, goodbye!");
	}

	public static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureLogging(lb =>
			{
				lb.ClearProviders();
				lb.AddSimpleConsole();
				lb.AddConfiguration();
			})
			.ConfigureServices((hostContext, services) =>
			{
				services.AddHostedService<ServerCore>();
				services.AddScoped<ServerWorker>();

				// services.Configure<Certificates>(hostContext.Configuration.GetRequiredSection("Certificates"));
				// services.Configure<DataProtection>(hostContext.Configuration.GetRequiredSection("DataProtection"));

				services.Scan(scanner =>
				{
					

					scanner.FromAssemblies(typeof(StatusHandler).Assembly)
						.AddClasses(filter => { filter.Where(x => IsAssignableToGenericType(x, typeof(IHandle<>))); })
						.AsImplementedInterfaces()
						.WithScopedLifetime();
				});
			});
}