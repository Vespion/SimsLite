using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using SimsLite_Parents.Helpers;
using SimsLite_Parents.ViewModels;

namespace SimsLite_Parents.Views
{
    // TODO: Change the URL for your privacy policy in the Resource File, currently set to https://YourPrivacyUrlGoesHere
    public sealed partial class SettingsPage
    {
        public SettingsViewModel ViewModel { get; } = Application.Current.GetProvider().GetRequiredService<SettingsViewModel>();

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.InitializeAsync();
        }
    }
}
