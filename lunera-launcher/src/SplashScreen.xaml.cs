using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Newtonsoft.Json;
using LauncherConfig;

namespace CanaryLauncherUpdate
{
	public partial class SplashScreen : Window
	{
		static string launcerConfigUrl = "http://login.luneraot.com/launcher_config.json";
		static ClientConfig clientConfig = ClientConfig.loadFromFile(launcerConfigUrl);
		static readonly HttpClient httpClient = new HttpClient();

		// Data to pass to MainWindow
		public List<NewsItem> LoadedNews { get; private set; }
		public BoostedCreature LoadedBoostedCreature { get; private set; }
		public BoostedCreature LoadedBoostedBoss { get; private set; }
		public List<CountdownEvent> LoadedCountdowns { get; private set; }
		public bool LauncherUpdateAvailable { get; private set; }
		public string LauncherUpdateVersion { get; private set; }
		public bool ClientUpdateNeeded { get; private set; }
		public string ClientVersion { get; private set; }
		
		// Preloaded images
		public BitmapImage LoadedLogoImage { get; private set; }
		public BitmapImage LoadedCompanyLogoImage { get; private set; }
		public BitmapImage LoadedBoostedCreatureImage { get; private set; }
		public BitmapImage LoadedBoostedBossImage { get; private set; }

		private int currentStep = 0;
		private readonly string[] stepNames = {
			"Checking launcher updates...",
			"Loading news...",
			"Loading boosted creatures...",
			"Loading countdowns...",
			"Loading images...",
			"Finalizing..."
		};

		private string GetLauncherPath(bool onlyBaseDirectory = false)
		{
			string launcherPath = "";
			if (string.IsNullOrEmpty(clientConfig.clientFolder) || onlyBaseDirectory) {
				launcherPath = AppDomain.CurrentDomain.BaseDirectory.ToString();
			} else {
				launcherPath = AppDomain.CurrentDomain.BaseDirectory.ToString() + "/" + clientConfig.clientFolder;
			}

			return launcherPath;
		}

		static string GetClientVersion(string path)
		{
			try
			{
				string jsonPath = Path.Combine(path, "launcher_config.json");
				if (!File.Exists(jsonPath)) return "";
				string jsonContent = File.ReadAllText(jsonPath);
				dynamic config = JsonConvert.DeserializeObject(jsonContent);
				if (config?.clientVersion != null)
				{
					return config.clientVersion.ToString();
				}
			}
			catch (Exception)
			{
				// Ignore errors and return empty string
			}
			return "";
		}

		public SplashScreen()
		{
			InitializeComponent();
			
			// Set version text
			VersionText.Text = $"LuneraOT Launcher v{clientConfig.launcherVersion}";
			
			// Load and store the logo for reuse in main window
			try
			{
				LoadedLogoImage = new BitmapImage();
				LoadedLogoImage.BeginInit();
				LoadedLogoImage.UriSource = new Uri("pack://application:,,,/Assets/logo.png");
				LoadedLogoImage.CacheOption = BitmapCacheOption.OnLoad;
				LoadedLogoImage.EndInit();
				LoadedLogoImage.Freeze(); // Make it thread-safe
				
				// Set it to the splash screen logo
				LogoImage.Source = LoadedLogoImage;
			}
			catch (Exception)
			{
				// If logo fails to load, continue anyway
			}
			
			// Start loading process
			_ = StartLoadingProcess();
		}

		private async Task StartLoadingProcess()
		{
			try
			{
				await LoadAllData();
				
				// Show completion
				UpdateProgress(100, "Loading complete!");
				await Task.Delay(500); // Brief pause to show completion
				
				// Open main window with loaded data
				OpenMainWindow();
			}
			catch (Exception ex)
			{
				// Handle any critical errors
				UpdateProgress(100, "Error occurred, opening launcher...");
				System.Diagnostics.Debug.WriteLine($"Splash screen error: {ex.Message}");
				await Task.Delay(1000);
				OpenMainWindow();
			}
		}

		private async Task LoadAllData()
		{
			// Step 1: Check launcher updates
			await ExecuteStep(0, CheckLauncherUpdates);
			
			// Step 2: Load news
			await ExecuteStep(1, LoadNews);
			
			// Step 3: Load boosted creatures
			await ExecuteStep(2, LoadBoostedCreatures);
			
			// Step 4: Load countdowns
			await ExecuteStep(3, LoadCountdowns);
			
			// Step 5: Load images
			await ExecuteStep(4, LoadImages);
			
			// Step 6: Check client updates
			await ExecuteStep(5, CheckClientUpdates);
		}

		private async Task ExecuteStep(int stepIndex, Func<Task> stepAction)
		{
			currentStep = stepIndex;
			
			// Update UI
			Dispatcher.Invoke(() =>
			{
				StatusText.Text = stepNames[stepIndex];
				LoadingProgressBar.Value = (stepIndex * 100.0) / stepNames.Length;
				
				// Update step indicators
				for (int i = 0; i <= stepIndex && i < 6; i++)
				{
					var stepBorder = FindName($"Step{i + 1}") as Border;
					if (stepBorder != null)
					{
						stepBorder.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
					}
				}
			});
			
			try
			{
				await stepAction();
				await Task.Delay(300); // Small delay for visual feedback
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Step {stepIndex} failed: {ex.Message}");
				// Continue with other steps even if one fails
			}
		}

		private async Task CheckLauncherUpdates()
		{
			try
			{
				string currentLauncherVersion = GetCurrentLauncherVersion();
				if (currentLauncherVersion != null)
				{
					ClientConfig remoteConfig = await GetRemoteConfig();
					if (remoteConfig != null && currentLauncherVersion != remoteConfig.launcherVersion)
					{
						LauncherUpdateAvailable = true;
						LauncherUpdateVersion = remoteConfig.launcherVersion;
					}
				}
			}
			catch (Exception)
			{
				// Ignore launcher update check errors
			}
		}

		private async Task<ClientConfig> GetRemoteConfig()
		{
			try
			{
				using (var client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
					client.DefaultRequestHeaders.Add("Pragma", "no-cache");
					
					string jsonString = await client.GetStringAsync(launcerConfigUrl);
					return JsonConvert.DeserializeObject<ClientConfig>(jsonString);
				}
			}
			catch (Exception)
			{
				return null;
			}
		}

		private string GetCurrentLauncherVersion()
		{
			try
			{
				string localConfigPath = Path.Combine(GetLauncherPath(true), "launcher_config.json");
				if (File.Exists(localConfigPath))
				{
					string json = File.ReadAllText(localConfigPath);
					dynamic config = JsonConvert.DeserializeObject(json);
					if (config?.launcherVersion != null)
					{
						return config.launcherVersion.ToString();
					}
				}
				return null;
			}
			catch
			{
				return null;
			}
		}

		private async Task LoadNews()
		{
			try
			{
				LoadedNews = await NewsService.FetchNewsAsync();
			}
			catch (Exception)
			{
				LoadedNews = new List<NewsItem>(); // Empty list as fallback
			}
		}

		private async Task LoadBoostedCreatures()
		{
			try
			{
				var (creature, boss) = await BoostedCreatureService.FetchBoostedCreaturesAsync();
				LoadedBoostedCreature = creature;
				LoadedBoostedBoss = boss;
			}
			catch (Exception)
			{
				LoadedBoostedCreature = null;
				LoadedBoostedBoss = null;
			}
		}

		private async Task LoadCountdowns()
		{
			try
			{
				LoadedCountdowns = await CountdownService.FetchCountdownsAsync();
			}
			catch (Exception)
			{
				LoadedCountdowns = new List<CountdownEvent>(); // Empty list as fallback
			}
		}

		private async Task LoadImages()
		{
			try
			{
				// Logo is already loaded in constructor, just load company logo
				Dispatcher.Invoke(() =>
				{
					// Load company logo
					LoadedCompanyLogoImage = new BitmapImage();
					LoadedCompanyLogoImage.BeginInit();
					LoadedCompanyLogoImage.UriSource = new Uri("pack://application:,,,/Assets/logo_company.png");
					LoadedCompanyLogoImage.CacheOption = BitmapCacheOption.OnLoad;
					LoadedCompanyLogoImage.EndInit();
					LoadedCompanyLogoImage.Freeze();
				});

				// Load boosted creature images if available
				if (LoadedBoostedCreature != null && !string.IsNullOrEmpty(LoadedBoostedCreature.ImageUrl))
				{
					LoadedBoostedCreatureImage = await LoadImageFromUrlAsync(LoadedBoostedCreature.ImageUrl);
				}

				if (LoadedBoostedBoss != null && !string.IsNullOrEmpty(LoadedBoostedBoss.ImageUrl))
				{
					LoadedBoostedBossImage = await LoadImageFromUrlAsync(LoadedBoostedBoss.ImageUrl);
				}
			}
			catch (Exception)
			{
				// Ignore image loading errors - main window will handle fallbacks
			}
		}

		private async Task<BitmapImage> LoadImageFromUrlAsync(string imageUrl)
		{
			try
			{
				using (var client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("User-Agent", 
						"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
					
					var imageBytes = await client.GetByteArrayAsync(imageUrl);
					
					return await Task.Run(() =>
					{
						return Dispatcher.Invoke(() =>
						{
							var bitmap = new BitmapImage();
							bitmap.BeginInit();
							bitmap.StreamSource = new MemoryStream(imageBytes);
							bitmap.CacheOption = BitmapCacheOption.OnLoad;
							bitmap.EndInit();
							bitmap.Freeze();
							return bitmap;
						});
					});
				}
			}
			catch (Exception)
			{
				return null;
			}
		}

		private Task CheckClientUpdates()
		{
			try
			{
				string newVersion = clientConfig.clientVersion;
				ClientVersion = newVersion;
				
				if (File.Exists(GetLauncherPath(true) + "/launcher_config.json"))
				{
					string actualVersion = GetClientVersion(GetLauncherPath(true));
					if (newVersion != actualVersion)
					{
						ClientUpdateNeeded = true;
					}
				}
				else if (!Directory.Exists(GetLauncherPath()) || 
						(Directory.Exists(GetLauncherPath()) && 
						 Directory.GetFiles(GetLauncherPath()).Length == 0 && 
						 Directory.GetDirectories(GetLauncherPath()).Length == 0))
				{
					ClientUpdateNeeded = true;
				}
				
				// Create client directory if it doesn't exist
				if (!Directory.Exists(GetLauncherPath()))
				{
					Directory.CreateDirectory(GetLauncherPath());
				}
			}
			catch (Exception)
			{
				// Ignore client update check errors
			}
			
			return Task.CompletedTask;
		}

		private void UpdateProgress(double value, string status)
		{
			Dispatcher.Invoke(() =>
			{
				LoadingProgressBar.Value = value;
				StatusText.Text = status;
			});
		}

		private void OpenMainWindow()
		{
			Dispatcher.Invoke(() =>
			{
				try
				{
					MainWindow mainWindow = new MainWindow();
					
					// Pass loaded data to main window
					mainWindow.SetPreloadedData(
						LoadedNews,
						LoadedBoostedCreature,
						LoadedBoostedBoss,
						LoadedCountdowns,
						LauncherUpdateAvailable,
						LauncherUpdateVersion,
						ClientUpdateNeeded,
						ClientVersion,
						LoadedLogoImage,
						LoadedCompanyLogoImage,
						LoadedBoostedCreatureImage,
						LoadedBoostedBossImage
					);
					
					mainWindow.Show();
					this.Close();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error opening main window: {ex.Message}");
					// Force close splash screen even if main window fails
					this.Close();
				}
			});
		}
	}
}