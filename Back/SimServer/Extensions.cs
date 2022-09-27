﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimServer;

public static class Extensions
{
	public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
	{
		var interfaceTypes = givenType.GetInterfaces();

		if (interfaceTypes.Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericType))
		{
			return true;
		}

		if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
			return true;

		var baseType = givenType.BaseType;
		return baseType != null && IsAssignableToGenericType(baseType, genericType);
	}

	public static IHostBuilder CreateSimHostBuilder(params string[] args)
	{
		return Host.CreateDefaultBuilder(args)
			.ConfigureServices((hostContext, services) =>
			{
				services.AddHostedService<ServerCore>();
				services.AddScoped<ServerWorker>();
			});
	}
}