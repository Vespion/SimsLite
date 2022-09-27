using AuthServer;
using AuthServer.Data;
using AuthServer.Data.Protection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerBase;

var host = ServerHost.CreateSimHostBuilder(args)
	.ConfigureServices((context, services) =>
	{
		services.Configure<CertificateProviderOptions>(context.Configuration.GetSection("CertificateProvider"));
		services.AddHostedService<CertificateProviderService>();

		services.AddDbContext<UserContext>(c =>
		{
			c.EnableDetailedErrors();
			c.EnableThreadSafetyChecks();
			c.UseLazyLoadingProxies();
			c.UseInMemoryDatabase("Identity");
			// c.UseSqlite(new SqliteConnection
			// {
			// 	ConnectionString = new SqliteConnectionStringBuilder
			// 		{
			// 			DataSource = "./id.db",
			// 			Mode = SqliteOpenMode.ReadWriteCreate,
			// 			ForeignKeys = true,
			// 			Cache = SqliteCacheMode.Shared,
			// 			RecursiveTriggers = true,
			// 			Pooling = true
			// 		}
			// 		.ToString()
			// });
		});

		services.AddAuthentication();
		services.AddIdentityCore<User>(options =>
			{
				options.Stores.ProtectPersonalData = true;
				options.Stores.MaxLengthForKeys = 128;
			})
			.AddSignInManager()
			.AddEntityFrameworkStores<UserContext>()
			.AddDefaultTokenProviders();
		
		services.AddScoped<ILookupProtectorKeyRing, LookupKeyRing>();
		services.AddScoped<ILookupProtector, LookupProtector>();
		services.AddScoped<IPersonalDataProtector, PersonalDataProtector>();
	})
	.Build();

await host.RunServerAsync();