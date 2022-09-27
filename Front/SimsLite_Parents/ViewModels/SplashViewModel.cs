using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using ApiSdk;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using SimsLite_Parents.Helpers;
using SimsLite_Parents.Services;
using SimsLite_Parents.Views;

namespace SimsLite_Parents.ViewModels
{
    public class SplashViewModel : ObservableObject
    {
        private string _statusText = "Starting up...";
        private IConfigurationRoot _configuration;
        [CanBeNull] private ILogger<SplashViewModel> _log;

        public SplashViewModel()
        {
            RunSetupOperationsCommand = new AsyncRelayCommand(RunSetupOperations);
        }

        private async Task RunSetupOperations()
        {
            if (((App)Application.Current).Services == null)
            {
                Debug.WriteLine("Bootstrapping services");
                Debug.WriteLine("Building configuration");
                var builder = new ConfigurationBuilder();
                builder.Add(new AppDataConfigurationSource
                {
                    ReloadOnChange = true,
                    User = ((App)Application.Current).LaunchedBy
                });
                _configuration = builder.Build();
                
                var serviceCollection = new ServiceCollection();
                Debug.WriteLine("Registering configuration");
                serviceCollection.AddSingleton<IConfiguration>(_ => _configuration);
                Debug.WriteLine("Registering api");
                serviceCollection.RegisterApi(_configuration);
                Debug.WriteLine("Registering logs");
                serviceCollection.AddLogging(lb =>
                {
                    lb.SetMinimumLevel(LogLevel.Trace);
                    lb.AddDebug();
                });
                Debug.WriteLine("Scanning assemblies");
                serviceCollection.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<ShellViewModel>()
                        .AddClasses(selector =>
                        {
                            selector.InNamespaceOf<ShellViewModel>();
                        })
                        .AsSelf()
                        .WithScopedLifetime();
                    Debug.WriteLine("ViewModels collected");
                });
                
                var provider = serviceCollection.BuildServiceProvider();
                _log = provider.GetService<ILogger<SplashViewModel>>();
                _log?.LogDebug("Service bootstrap complete");
                //Force the provider to construct the instance to ensure it's registered for disposal
                provider.GetRequiredService<IConfiguration>();
                ((App)Application.Current).Services = provider;
            }

            var redirect = !await CheckServerConfigured();
            if (redirect)
            {
                _log?.LogDebug("Server configuration missing, navigating to setup page");
                await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => NavigationService.Navigate(typeof(ServerSetupPage)));
            }
            else
            {
                _log?.LogDebug("Server configuration OK, navigating to home page");
                await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, SwitchNavigationMode);
            }
        }

        private async Task<bool> CheckServerConfigured()
        {
            _log?.LogDebug("Checking server configuration");
            await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusText = "Checking server configuration");

            var user = ((App)Application.Current).LaunchedBy;
            var settings = (await ApplicationData.GetForUserAsync(user)).LocalSettings;

            var serverBlock = settings.CreateContainer("ApiClient", ApplicationDataCreateDisposition.Always);
            return serverBlock.Values.ContainsKey("AuthPort") &&
            serverBlock.Values.ContainsKey("MainPort");
        }
        public ICommand RunSetupOperationsCommand {get; private set;}

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private void SwitchNavigationMode()
        {
            //Navigating to a ShellPage, this will replaces NavigationService frame for an inner frame to change navigation handling.
            NavigationService.Navigate<ShellPage>();

            //Navigating now to a HomePage, this will be the first navigation on a NavigationPane menu
            NavigationService.Navigate<HomePage>();
        }
    }
}
