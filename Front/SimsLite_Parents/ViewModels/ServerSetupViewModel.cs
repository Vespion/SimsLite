using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using ApiSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using SimsLite_Parents.Helpers;
using SimsLite_Parents.Services;
using SimsLite_Parents.Views;

namespace SimsLite_Parents.ViewModels
{
    public class ServerSetupViewModel : ObservableObject, IDisposable
    {
        private string _serverAddress;
        private readonly IDisposable _settingsChange;

        public ServerSetupViewModel()
        {
            VerifyServerCommand = new AsyncRelayCommand(VerifyServer);
            var monitor = Application.Current.GetProvider().GetRequiredService<IOptionsMonitor<ApiConfiguration>>();
            _settingsChange = monitor.OnChange(Navigate);
        }

        private void Navigate(ApiConfiguration _)
        {
            CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    NavigationService.Navigate<ShellPage>();
                    NavigationService.Navigate<HomePage>();
                });
            _settingsChange.Dispose();
        }
        
        public string ServerAddress
        {
            get => _serverAddress;
            set => SetProperty(ref _serverAddress, value);
        }

        public ICommand VerifyServerCommand { get; set; }

        public async Task VerifyServer()
        {
            var settings = (await ApplicationData.GetForUserAsync(Application.Current.GetLauncher())).LocalSettings;

            var container = settings.CreateContainer("ApiClient", ApplicationDataCreateDisposition.Always);
            container.SaveString("AuthPort", "5576");
            container.SaveString("SimsPort", "5575");
            container.SaveString("CertPort", "5574");
            container.SaveString("Host", ServerAddress);
            
            settings.Notify();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _settingsChange?.Dispose();
        }
    }
}
