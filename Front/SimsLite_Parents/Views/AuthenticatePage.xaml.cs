using Windows.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SimsLite_Parents.Helpers;
using SimsLite_Parents.ViewModels;

namespace SimsLite_Parents.Views
{
    public sealed partial class AuthenticatePage
    {
        public AuthenticateViewModel ViewModel { get; } = Application.Current.GetProvider().GetRequiredService<AuthenticateViewModel>();

        public AuthenticatePage()
        {
            InitializeComponent();
        }
    }
}
