using Windows.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SimsLite_Parents.Helpers;
using SimsLite_Parents.ViewModels;

namespace SimsLite_Parents.Views
{
    public sealed partial class HomePage
    {
        public HomeViewModel ViewModel { get; } = Application.Current.GetProvider().GetRequiredService<HomeViewModel>();

        public HomePage()
        {
            InitializeComponent();
        }
    }
}
