using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SimsLite_Parents.ViewModels;

namespace SimsLite_Parents.Views
{
	public sealed partial class SplashPage
	{
		internal Rect SplashImageRect; // Rect to store splash screen image coordinates.
		private SplashScreen _splash; // Variable to hold the splash screen object.
		internal bool Dismissed; // Variable to track splash screen dismissal status.
		internal Frame RootFrame;
		public SplashViewModel ViewModel { get; } = new SplashViewModel();

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (e.Parameter is LaunchActivatedEventArgs launch)
			{
				_splash = launch.SplashScreen;

				// Listen for window resize events to reposition the extended splash screen image accordingly.
				// This ensures that the extended splash screen formats properly in response to window resizing.
				Window.Current.SizeChanged += ExtendedSplash_OnResize;

				if (_splash != null)
				{
					// Register an event handler to be executed when the splash screen has been dismissed.
					_splash.Dismissed += DismissedEventHandler;

					// Retrieve the window coordinates of the splash screen image.
					SplashImageRect = _splash.ImageLocation;
					PositionImage();

					// If applicable, include a method for positioning a progress control.
					PositionRing();
					PositionText();
				}
			}
		}

		void DismissedEventHandler(SplashScreen sender, object e)
		{
			Dismissed = true;
			PositionImage();
			PositionRing();
			PositionText();
			ViewModel.RunSetupOperationsCommand.Execute(null);
		}

		void PositionImage()
		{
			ExtendedSplashImage.SetValue(Canvas.LeftProperty, SplashImageRect.X);
			ExtendedSplashImage.SetValue(Canvas.TopProperty, SplashImageRect.Y);
			ExtendedSplashImage.Height = SplashImageRect.Height;
			ExtendedSplashImage.Width = SplashImageRect.Width;
		}

		void PositionRing()
		{
			SplashProgressRing.SetValue(Canvas.LeftProperty,
				SplashImageRect.X + (SplashImageRect.Width * 0.5) - (SplashProgressRing.Width * 0.5));
			SplashProgressRing.SetValue(Canvas.TopProperty,
				(SplashImageRect.Y + SplashImageRect.Height + SplashImageRect.Height * 0.1));
		}

		void PositionText()
		{
			SplashStatusText.SetValue(Canvas.TopProperty, ((double) SplashProgressRing.GetValue(Canvas.TopProperty)) + SplashProgressRing.Height + 3);
			SplashStatusText.SetValue(Canvas.LeftProperty,
				SplashImageRect.X + (SplashImageRect.Width * 0.5) - (SplashStatusText.ActualWidth * 0.5));
		}

		// Include code to be executed when the system has transitioned from the splash screen to the extended splash screen (application's first view).

		void ExtendedSplash_OnResize(object sender, WindowSizeChangedEventArgs e)
		{
			// Safely update the extended splash screen image coordinates. This function will be executed when a user resizes the window.
			if (_splash != null)
			{
				// Update the coordinates of the splash screen image.
				SplashImageRect = _splash.ImageLocation;
				PositionImage();

				// If applicable, include a method for positioning a progress control.
				PositionRing();
				PositionText();
			}
		}

		public SplashPage()
		{
			InitializeComponent();
			Loaded += (sender, args) => LoadingStoryBoard.Begin();
			ViewModel.PropertyChanged += (sender, args) =>
			{
				ExtendedSplash_OnResize(this, null);
			};
		}
	}
}