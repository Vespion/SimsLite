using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using SimsLite_Parents.Core.Helpers;

namespace SimsLite_Parents.Helpers
{
	public class AppDataChangeObserver
	{
		public event EventHandler Changed;

		public void OnChanged()
		{
			ThreadPool.QueueUserWorkItem(_ => Changed?.Invoke(this, null));
		}

		#region singleton

		private static readonly Lazy<AppDataChangeObserver> Lazy = new Lazy<AppDataChangeObserver>(() => new AppDataChangeObserver());

		private AppDataChangeObserver() { }

		public static AppDataChangeObserver Instance => Lazy.Value;

		#endregion singleton
	}
	
	public class AppDataConfigurationSource : IConfigurationSource
	{
		[CanBeNull] public User User { get; set; }
		public bool ReloadOnChange { get; set; }
		public bool IgnoreAppSettings { get; set; }
		public TimeSpan ReloadDelay { get; set; } = TimeSpan.FromSeconds(1);

		/// <inheritdoc />
		public IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			return new AppDataConfigurationProvider(this);
		}
	}
	
	public class AppDataConfigurationProvider: ConfigurationProvider, IDisposable
	{
		private readonly AppDataConfigurationSource _source;
		private readonly IDisposable _changeTokenRegistration;
		private CancellationTokenSource _cancellationTokenSource;

		public AppDataConfigurationProvider(AppDataConfigurationSource source)
		{
			_source = source;
			if (_source.ReloadOnChange)
			{
				_changeTokenRegistration = ChangeToken.OnChange(
					() =>
					{
						_cancellationTokenSource?.Dispose();
						_cancellationTokenSource = new CancellationTokenSource();
						var token = new CancellationChangeToken(_cancellationTokenSource.Token);
						AppDataChangeObserver.Instance.Changed += (sender, args) =>
						{
							_cancellationTokenSource.Cancel();
						};
						return token;
					},
					() =>
					{
						Thread.Sleep(_source.ReloadDelay);
						LoadInternal().FireAndForget();
					});
			}
		}

		private async Task<IEnumerable<ApplicationDataContainer>> FetchContainers()
		{
			var containers = new List<ApplicationDataContainer>(4);

			if (!_source.IgnoreAppSettings)
			{
				containers.Add(ApplicationData.Current.RoamingSettings);
				containers.Add(ApplicationData.Current.LocalSettings);
			}

			if (_source.User != null)
			{
				containers.Add((await ApplicationData.GetForUserAsync(_source.User)).RoamingSettings);
				containers.Add((await ApplicationData.GetForUserAsync(_source.User)).LocalSettings);
			}

			return containers;
		}

		private async Task LoadInternal()
		{
			var containers = await FetchContainers();
			var dataSettings = new Dictionary<string, string>();

			void HandleContainer(ApplicationDataContainer container, string prefix = null)
			{
				foreach (var (key, applicationDataContainer) in container.Containers)
				{
					HandleContainer(applicationDataContainer, prefix + key + ":");
				}

				foreach (var (key, value) in container.Values)
				{
					dataSettings[prefix + key] = value.ToString();
				}
			}

			foreach (var applicationDataContainer in containers)
			{
				HandleContainer(applicationDataContainer);
			}

			Data = dataSettings;

			OnReload();
		}

        
        public override void Load()
        {
	        LoadInternal().FireAndForget();
        }

        /// <inheritdoc />
        public void Dispose()
        {
	        _changeTokenRegistration?.Dispose();
	        _cancellationTokenSource?.Dispose();
        }
	}
}