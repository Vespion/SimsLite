using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetMQ;
using ServerObjects;

namespace ServerBase
{
    public static class ServerHost
    {
        public static async Task RunServerAsync(this IHost host, CancellationToken token = default)
        {
            var log = host.Services.GetService<ILogger<IHost>>();
            log?.LogInformation("Bootstrapping complete! Launching host");
            await host.RunAsync(token);

            log?.LogInformation("Host shutdown complete! Running socket cleanup");
            NetMQConfig.Cleanup();
            log?.LogInformation("Socket cleanup complete");
            log?.LogInformation("Server shutdown complete, goodbye!");
        }

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

                    services.Configure<ServerSettings>(hostContext.Configuration.GetSection("Server"));
                    services.Configure<SecuritySettings>(hostContext.Configuration.GetSection("Security"));

                    var securitySettings = new SecuritySettings();
                    hostContext.Configuration.Bind("Security", securitySettings);

                    if (!string.IsNullOrWhiteSpace(securitySettings.KeyringDirectory))
                    {
                        var dpBuilder = services.AddDataProtection(options =>
                        {
                            options.ApplicationDiscriminator = securitySettings.ApplicationDiscriminator ?? "SimLite";
                        }).AddKeyManagementOptions(options =>
                        {
                            options.NewKeyLifetime = TimeSpan.FromDays(7);
                        });

                        dpBuilder.PersistKeysToFileSystem(new DirectoryInfo(securitySettings.KeyringDirectory));

                        if (!string.IsNullOrWhiteSpace(securitySettings.KeyringCertificateDirectory))
                        {
                            var certificates = new List<X509Certificate2>();
                            var files = Directory.EnumerateFiles(securitySettings.KeyringCertificateDirectory, "*",
                                SearchOption.AllDirectories);

                            foreach (var file in files)
                            {
                                try
                                {
                                    certificates.Add(X509Certificate2.CreateFromPemFile(file));
                                }
                                catch(CryptographicException)
                                {
                                    //We ignore any loading errors and just exclude the file from the certificate list
                                }
                            }

                            dpBuilder.UnprotectKeysWithAnyCertificate(certificates.ToArray());
                            
                            X509Certificate2 primary;
                            if (string.IsNullOrWhiteSpace(securitySettings.PrimaryKeyringCertificate))
                            {
                                primary = certificates.OrderBy(x => x.NotAfter).First();
                            }
                            else
                            {
                                primary = certificates.Find(x =>
                                    x.Thumbprint == securitySettings.PrimaryKeyringCertificate) ?? throw new FileNotFoundException("Could not find certificate with supplied thumbprint");
                            }

                            dpBuilder.ProtectKeysWithCertificate(primary);
                        }
                    }

                    services.Scan(scanner =>
                    {
                        scanner.FromCallingAssembly()
                            .AddClasses(filter => { filter.Where(x => x.IsAssignableToGenericType(typeof(IHandle<>))); })
                            .AsImplementedInterfaces()
                            .WithScopedLifetime();
                    });
                });
        }
    }
}