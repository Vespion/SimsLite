using Windows.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SimsLite_Parents.Helpers;
using SimsLite_Parents.ViewModels;

namespace SimsLite_Parents.Views
{
    // TODO: Change the icons and titles for all NavigationViewItems in ShellPage.xaml.
    public sealed partial class ShellPage
    {
        public ShellViewModel ViewModel { get; } = Application.Current.GetProvider().GetRequiredService<ShellViewModel>();

        public ShellPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.Initialize(ShellFrame, NavigationView, KeyboardAccelerators);
        }
    }
}
