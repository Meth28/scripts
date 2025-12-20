using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Ionic.Zip;
using LauncherConfig;
using System.Windows.Documents;
using System.Windows.Threading;
using System.IO.Compression;
using System.Threading;

namespace CanaryLauncherUpdate
{
    public partial class MainWindow : Window
    {
        private static string launcherConfigUrl = "http://login.luneraot.com/launcher_config.json";
        private static ClientConfig clientConfig = ClientConfig.loadFromFile(launcherConfigUrl);
        private static string clientExecutableName = clientConfig.clientExecutable;
        private static string urlClient = clientConfig.newClientUrl;
        private static string programVersion = clientConfig.launcherVersion;
        private static readonly HttpClient httpClient = new HttpClient();

        private string newVersion = "";
#pragma warning disable CS0414 // Variable is assigned but its value is never used
        private bool clientDownloaded = false;
#pragma warning restore CS0414
        private bool needUpdate = false;
        private List<NewsItem> currentNewsItems = new List<NewsItem>();
        private int currentNewsIndex = 0;
        private BoostedCreature currentBoostedCreature;
        private BoostedCreature currentBoostedBoss;
        private List<CountdownEvent> currentCountdowns = new List<CountdownEvent>();
        private DispatcherTimer countdownTimer;

        // Preloaded data from splash screen
        private bool dataPreloaded = false;
        private bool launcherUpdateAvailable = false;
        private string launcherUpdateVersion = "";

        private WebClient webClient = new WebClient();

        private string GetLauncherPath(bool onlyBaseDirectory = false)
        {
            string launcherPath = "";
            if (string.IsNullOrEmpty(clientConfig.clientFolder) || onlyBaseDirectory)
            {
                launcherPath = AppDomain.CurrentDomain.BaseDirectory.ToString();
            }
            else
            {
                launcherPath = AppDomain.CurrentDomain.BaseDirectory.ToString() + "/" + clientConfig.clientFolder;
            }

            return launcherPath;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the logo immediately after preloaded data is received
        /// </summary>
        private void SetLogoImmediately()
        {
            if (preloadedLogoImage != null)
            {
                ImageLogoServer.Source = preloadedLogoImage;
            }

            if (preloadedCompanyLogoImage != null)
            {
                ImageLogoCompany.Source = preloadedCompanyLogoImage;
            }
        }

        // Preloaded images from splash screen
        private BitmapImage preloadedLogoImage;
        private BitmapImage preloadedCompanyLogoImage;
        private BitmapImage preloadedBoostedCreatureImage;
        private BitmapImage preloadedBoostedBossImage;

        /// <summary>
        /// Sets preloaded data from the splash screen to avoid reloading
        /// </summary>
        public void SetPreloadedData(
            List<NewsItem> news,
            BoostedCreature boostedCreature,
            BoostedCreature boostedBoss,
            List<CountdownEvent> countdowns,
            bool launcherUpdateAvailable,
            string launcherUpdateVersion,
            bool clientUpdateNeeded,
            string clientVersion,
            BitmapImage logoImage,
            BitmapImage companyLogoImage,
            BitmapImage boostedCreatureImage,
            BitmapImage boostedBossImage)
        {
            dataPreloaded = true;

            // Set news data
            if (news != null)
            {
                currentNewsItems = news;
                currentNewsIndex = 0;
            }

            // Set boosted creatures data
            currentBoostedCreature = boostedCreature;
            currentBoostedBoss = boostedBoss;

            // Set countdowns data
            if (countdowns != null)
            {
                currentCountdowns = countdowns;
            }

            // Set update information
            this.launcherUpdateAvailable = launcherUpdateAvailable;
            this.launcherUpdateVersion = launcherUpdateVersion;
            this.needUpdate = clientUpdateNeeded;
            this.newVersion = clientVersion ?? clientConfig.clientVersion;

            // Set preloaded images
            this.preloadedLogoImage = logoImage;
            this.preloadedCompanyLogoImage = companyLogoImage;
            this.preloadedBoostedCreatureImage = boostedCreatureImage;
            this.preloadedBoostedBossImage = boostedBossImage;

            // Set logo immediately so it appears instantly when window opens
            SetLogoImmediately();
        }

        private void UpdateButtonToPlayState()
        {
            buttonPlay.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(76, 175, 80), 0),
                    new GradientStop(Color.FromRgb(46, 125, 50), 1)
                },
                new Point(0, 0),
                new Point(0, 1)
            );
            buttonPlayIcon.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "pack://application:,,,/Assets/icon_play.png"));

            // Update the text in the button
            var stackPanel = buttonPlay.Content as StackPanel;
            if (stackPanel != null)
            {
                var textBlock = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();
                if (textBlock != null)
                {
                    textBlock.Text = "PLAY";
                }
            }
        }

        private void UpdateButtonToUpdateState()
        {
            buttonPlay.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(255, 152, 0), 0),
                    new GradientStop(Color.FromRgb(255, 87, 34), 1)
                },
                new Point(0, 0),
                new Point(0, 1)
            );
            buttonPlayIcon.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "pack://application:,,,/Assets/icon_update.png"));

            // Update the text in the button
            var stackPanel = buttonPlay.Content as StackPanel;
            if (stackPanel != null)
            {
                var textBlock = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();
                if (textBlock != null)
                {
                    textBlock.Text = "UPDATE";
                }
            }
        }

        private static void CreateShortcut()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            // Asignar nombre por defecto si está vacío
            string shortcutName = string.IsNullOrWhiteSpace(clientConfig.namelauncher)
                ? "LuneraOT"
                : clientConfig.namelauncher;

            string shortcutPath = Path.Combine(desktopPath, shortcutName + ".lnk");

            Type t = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(t);
            var lnk = shell.CreateShortcut(shortcutPath);

            try
            {
                lnk.TargetPath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                lnk.Description = clientConfig.clientFolder;
                lnk.Save();
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(lnk);
            }
        }

        private async void TibiaLauncher_Load(object sender, RoutedEventArgs e)
        {
            // Only set logos if they haven't been set by preloaded data
            if (ImageLogoServer.Source == null)
            {
                if (preloadedLogoImage != null)
                {
                    ImageLogoServer.Source = preloadedLogoImage;
                }
                else
                {
                    ImageLogoServer.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "pack://application:,,,/Assets/logo.png"));
                }
            }

            if (ImageLogoCompany.Source == null)
            {
                if (preloadedCompanyLogoImage != null)
                {
                    ImageLogoCompany.Source = preloadedCompanyLogoImage;
                }
                else
                {
                    ImageLogoCompany.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "pack://application:,,,/Assets/logo_company.png"));
                }
            }

            // Hide download progress elements initially
            progressbarDownload.Visibility = Visibility.Collapsed;
            labelClientVersion.Visibility = Visibility.Collapsed;
            labelDownloadPercent.Visibility = Visibility.Collapsed;

            // Set version label
            labelVersion.Text = "v" + programVersion;

            if (dataPreloaded)
            {
                // Use preloaded data from splash screen
                await SetupWithPreloadedData();
            }
            else
            {
                // Fallback: Load data if not preloaded
                await LoadDataFallback();
            }

            // Load online players
            await LoadOnlinePlayersAsync();

            // Start the countdown timer to update every second
            StartCountdownTimer();
        }

        private async Task LoadOnlinePlayersAsync(bool forceRefresh = false)
        {
            try
            {
                // Show loading state
                Dispatcher.Invoke(() =>
                {
                    OnlinePlayersTextBlock.Text = "Players Online: Loading...";
                });

                string onlinePlayersUrl = "http://login.luneraot.com/login.php";
                string cachePath = GetLauncherPath() + "/cache/onlinenumbers.json";

                // Check if cached data exists and isn't forced to refresh
                if (!forceRefresh && File.Exists(cachePath))
                {
                    string cachedJson = File.ReadAllText(cachePath);
                    try
                    {
                        dynamic data = JsonConvert.DeserializeObject(cachedJson);
                        int playerCount = data?.playersonline ?? 0;
                        Dispatcher.Invoke(() =>
                        {
                            OnlinePlayersTextBlock.Text = $"Players Online: {playerCount}";
                        });
                        return; // Use cached data if available
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("Invalid cached onlinenumbers.json, fetching fresh data.");
                    }
                }

                // Prepare JSON payload for POST request
                var requestData = new { type = "cacheinfo" };
                var content = new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json");

                // Send POST request to login.php
                var response = await httpClient.PostAsync(onlinePlayersUrl, content);
                response.EnsureSuccessStatusCode();
                string jsonString = await response.Content.ReadAsStringAsync();

                // Parse the response
                dynamic onlineData = JsonConvert.DeserializeObject(jsonString);
                int onlinePlayerCount = onlineData?.playersonline ?? 0;

                // Update UI
                Dispatcher.Invoke(() =>
                {
                    OnlinePlayersTextBlock.Text = $"Players Online: {onlinePlayerCount}";
                });

                // Cache the data to onlinenumbers.json
                try
                {
                    Directory.CreateDirectory(GetLauncherPath() + "/cache");
                    File.WriteAllText(cachePath, jsonString);
                    File.SetAttributes(cachePath, FileAttributes.ReadOnly); // Match AddReadOnly logic
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error caching online players: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching online players: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    OnlinePlayersTextBlock.Text = "Players Online: Unavailable";
                });
            }
        }

        private async void RefreshOnlinePlayersButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadOnlinePlayersAsync(forceRefresh: true);
        }

        private async Task SetupWithPreloadedData()
        {
            // Check for launcher updates first (if available)
            if (launcherUpdateAvailable)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"A new launcher version is available!\n\nCurrent version: {programVersion}\nNew version: {launcherUpdateVersion}\n\nWould you like to update now?",
                    "Launcher Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    await UpdateLauncher();
                    return; // Exit early as launcher will restart
                }
            }

            // Display preloaded news
            if (currentNewsItems != null && currentNewsItems.Count > 0)
            {
                string formattedNews = NewsService.FormatNewsForDisplayWithHighlight(currentNewsItems, currentNewsIndex);
                hintsBox.Text = formattedNews;
            }
            else
            {
                hintsBox.Text = GetDefaultNewsContent();
            }

            // Display preloaded boosted creatures
            if (currentBoostedCreature != null || currentBoostedBoss != null)
            {
                UpdateBoostedCreaturesDisplay();
            }
            else
            {
                LoadFallbackBoostedCreatures();
            }

            // Display preloaded countdowns
            if (currentCountdowns != null && currentCountdowns.Count > 0)
            {
                UpdateCountdownsDisplay();
            }
            else
            {
                LoadFallbackCountdowns();
            }

            // Set up client update status
            SetupClientUpdateStatus();
        }

        private async Task LoadDataFallback()
        {
            // This is a fallback in case data wasn't preloaded
            // Check for launcher updates first
            await CheckForLauncherUpdate();

            // Load news, boosted creatures, and countdowns asynchronously
            await LoadNewsAsync();
            await LoadBoostedCreaturesAsync();
            await LoadCountdownsAsync();

            // Set up client update status
            SetupClientUpdateStatus();
        }

        private void SetupClientUpdateStatus()
        {
            // Use only the preloaded state from the splash screen to set the UI
            if (needUpdate)
            {
                UpdateButtonToUpdateState();
                labelClientVersion.Text = newVersion;
                labelClientVersion.Visibility = Visibility.Visible;
                buttonPlay.Visibility = Visibility.Visible;
                buttonPlay_tooltip.Text = "Update";
            }
            else
            {
                UpdateButtonToPlayState();
                labelClientVersion.Text = newVersion;
                labelClientVersion.Visibility = Visibility.Visible;
                buttonPlay.Visibility = Visibility.Visible;
                buttonPlay_tooltip.Text = "Play";
                // Set clientDownloaded to true if the client executable exists
                if (File.Exists(GetLauncherPath() + "/bin/" + clientExecutableName))
                {
                    clientDownloaded = true;
                }
            }
        }

        private static string GetClientVersion(string path)
        {
            string jsonPath = Path.Combine(path, "launcher_config.json");
            if (!File.Exists(jsonPath))
                return "";
            try
            {
                string jsonString = File.ReadAllText(jsonPath);
                dynamic config = JsonConvert.DeserializeObject(jsonString);
                if (config?.clientVersion != null)
                {
                    return config.clientVersion.ToString();
                }
            }
            catch { }
            return "";
        }

        private void AddReadOnly()
        {
            // If the files "eventschedule/boostedcreature/onlinenumbers" exist, set them as read-only
            string eventSchedulePath = GetLauncherPath() + "/cache/eventschedule.json";
            if (File.Exists(eventSchedulePath))
            {
                File.SetAttributes(eventSchedulePath, FileAttributes.ReadOnly);
            }
            string boostedCreaturePath = GetLauncherPath() + "/cache/boostedcreature.json";
            if (File.Exists(boostedCreaturePath))
            {
                File.SetAttributes(boostedCreaturePath, FileAttributes.ReadOnly);
            }
            string onlineNumbersPath = GetLauncherPath() + "/cache/onlinenumbers.json";
            if (File.Exists(onlineNumbersPath))
            {
                File.SetAttributes(onlineNumbersPath, FileAttributes.ReadOnly);
            }
        }

        private void UpdateClient()
        {
            if (!Directory.Exists(GetLauncherPath(true)))
            {
                Directory.CreateDirectory(GetLauncherPath());
            }
            labelDownloadPercent.Visibility = Visibility.Visible;
            progressbarDownload.Visibility = Visibility.Visible;
            labelClientVersion.Visibility = Visibility.Collapsed;
            buttonPlay.Visibility = Visibility.Collapsed;
            webClient.DownloadProgressChanged += Client_DownloadProgressChanged;
            webClient.DownloadFileCompleted += Client_DownloadFileCompleted;
            webClient.DownloadFileAsync(new Uri(urlClient), GetLauncherPath() + "/tibia.zip");
        }

        private async Task CheckForLauncherUpdate()
        {
            try
            {
                // Get the current launcher version from local config
                string currentLauncherVersion = GetCurrentLauncherVersion();

                // Only check for updates if we have a valid current version
                if (currentLauncherVersion == null)
                {
                    System.Diagnostics.Debug.WriteLine("Launcher Update: No local config found, skipping update check");
                    return;
                }

                // Fetch fresh remote config for launcher update check
                ClientConfig remoteConfig = await GetRemoteConfig();
                if (remoteConfig == null)
                {
                    System.Diagnostics.Debug.WriteLine("Launcher Update: Failed to fetch remote config");
                    return;
                }

                // Get the remote launcher version from fresh config
                string remoteLauncherVersion = remoteConfig.launcherVersion;

                // Debug output
                System.Diagnostics.Debug.WriteLine($"Launcher Update Check: Local={currentLauncherVersion}, Remote={remoteLauncherVersion}");

                // Compare versions
                if (currentLauncherVersion != remoteLauncherVersion)
                {
                    System.Diagnostics.Debug.WriteLine("Launcher Update: Versions differ, showing update dialog");

                    // Show update dialog
                    MessageBoxResult result = MessageBox.Show(
                        $"A new launcher version is available!\n\nCurrent version: {currentLauncherVersion}\nNew version: {remoteLauncherVersion}\n\nWould you like to update now?",
                        "Launcher Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        await UpdateLauncher();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Launcher Update: Versions match, no update needed");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt launcher startup
                System.Diagnostics.Debug.WriteLine($"Error checking for launcher update: {ex.Message}");
            }
        }

        private async Task<ClientConfig> GetRemoteConfig()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Add headers to avoid caching issues
                    client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                    client.DefaultRequestHeaders.Add("Pragma", "no-cache");

                    string jsonString = await client.GetStringAsync(launcherConfigUrl);
                    return JsonConvert.DeserializeObject<ClientConfig>(jsonString);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching remote config: {ex.Message}");
                return null;
            }
        }

        private string GetCurrentLauncherVersion()
        {
            try
            {
                // Read version from local launcher_config.json
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

                // If local config doesn't exist or doesn't have launcherVersion, return null
                return null;
            }
            catch
            {
                // Return null if there's any error reading the config
                return null;
            }
        }

        private async Task UpdateLauncher()
        {
            try
            {
                // Show update progress
                labelDownloadPercent.Text = "Downloading launcher update...";
                labelDownloadPercent.Visibility = Visibility.Visible;
                progressbarDownload.Visibility = Visibility.Visible;
                progressbarDownload.Value = 0;

                // Hide other UI elements during update
                buttonPlay.Visibility = Visibility.Collapsed;
                labelClientVersion.Visibility = Visibility.Collapsed;

                // Get current executable path
                string currentExePath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                string tempExePath = currentExePath + ".new";
                string backupExePath = currentExePath + ".old";

                // Download new launcher
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(clientConfig.newLauncherUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var downloadedBytes = 0L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempExePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            // Update progress
                            if (totalBytes > 0)
                            {
                                var progressPercentage = (int)((downloadedBytes * 100) / totalBytes);
                                Dispatcher.Invoke(() =>
                                {
                                    progressbarDownload.Value = progressPercentage;
                                    labelDownloadPercent.Text = $"Downloading launcher update... {progressPercentage}%";
                                });
                            }
                        }
                    }
                }

                // Update progress
                Dispatcher.Invoke(() =>
                {
                    progressbarDownload.Value = 100;
                    labelDownloadPercent.Text = "Preparing to restart...";
                });

                // Download latest launcher_config.json to update launcherVersion
                try
                {
                    WebClient webClient = new WebClient();
                    string localConfigPath = System.IO.Path.Combine(GetLauncherPath(true), "launcher_config.json");
                    await Task.Run(() => webClient.DownloadFile(launcherConfigUrl, localConfigPath));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to update launcher_config.json: {ex.Message}");
                }

                // Create update script
                string updateScript = CreateLauncherUpdateScript(currentExePath, tempExePath, backupExePath);

                // Show final message
                Dispatcher.Invoke(() =>
                {
                    labelDownloadPercent.Text = "Restarting launcher...";
                });

                // Execute update script
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{updateScript}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                });

                // Wait a moment then force close current launcher
                await Task.Delay(1000);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Show error and restore UI
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Failed to update launcher: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Restore UI
                    progressbarDownload.Visibility = Visibility.Collapsed;
                    labelDownloadPercent.Visibility = Visibility.Collapsed;
                    buttonPlay.Visibility = Visibility.Visible;
                    labelClientVersion.Visibility = Visibility.Visible;
                });
            }
        }

        private string CreateLauncherUpdateScript(string currentExePath, string tempExePath, string backupExePath)
        {
            string scriptPath = Path.Combine(Path.GetTempPath(), "launcher_update.bat");

            string scriptContent = $@"@echo off
echo Updating GlobalLauncher...

REM Wait for the current launcher to close completely
timeout /t 3 /nobreak >nul

REM Kill any remaining launcher processes to prevent conflicts
taskkill /f /im ""GlobalLauncher.exe"" >nul 2>&1

REM Wait a bit more to ensure process is fully terminated
timeout /t 2 /nobreak >nul

REM Clean up any existing backup files first
if exist ""{backupExePath}"" del ""{backupExePath}"" >nul 2>&1

REM Backup current launcher if it exists
if exist ""{currentExePath}"" (
    echo Backing up current launcher...
    move ""{currentExePath}"" ""{backupExePath}"" >nul 2>&1
    if errorlevel 1 (
        echo Failed to backup current launcher
        goto :restore
    )
)

REM Replace with new launcher
if exist ""{tempExePath}"" (
    echo Installing new launcher...
    move ""{tempExePath}"" ""{currentExePath}"" >nul 2>&1
    if errorlevel 1 (
        echo Failed to install new launcher
        goto :restore
    )
) else (
    echo New launcher file not found
    goto :restore
)

REM Verify new launcher was installed correctly
if exist ""{currentExePath}"" (
    echo Update successful, starting new launcher...
    timeout /t 1 /nobreak >nul
    start """" ""{currentExePath}""
    goto :cleanup
) else (
    echo New launcher installation failed
    goto :restore
)

:restore
echo Restoring backup launcher...
if exist ""{backupExePath}"" (
    move ""{backupExePath}"" ""{currentExePath}"" >nul 2>&1
    if exist ""{currentExePath}"" (
        echo Backup restored, starting original launcher...
        start """" ""{currentExePath}""
    )
)
goto :cleanup

:cleanup
REM Clean up temporary files
timeout /t 2 /nobreak >nul
if exist ""{backupExePath}"" del ""{backupExePath}"" >nul 2>&1
if exist ""{tempExePath}"" del ""{tempExePath}"" >nul 2>&1
del ""%~f0"" >nul 2>&1
";

            File.WriteAllText(scriptPath, scriptContent);
            return scriptPath;
        }

        private void buttonPlay_Click(object sender, RoutedEventArgs e)
        {
            // Check if an update is needed or if the client directory doesn't exist
            if (needUpdate == true || !Directory.Exists(GetLauncherPath()))
            {
                try
                {
                    // Download and install the update
                    UpdateClient();
                }
                catch (Exception ex)
                {
                    labelVersion.Text = ex.ToString();
                }
            }
            else
            {
                // No update needed, just start the client
                if (File.Exists(GetLauncherPath() + "/bin/" + clientExecutableName))
                {
                    // Start the client and close the launcher
                    Process.Start(GetLauncherPath() + "/bin/" + clientExecutableName);
                    // Close the launcher completely
                    this.Close();
                }
                else
                {
                    // Client executable not found, try to download it
                    try
                    {
                        UpdateClient();
                    }
                    catch (Exception ex)
                    {
                        labelVersion.Text = ex.ToString();
                    }
                }
            }
        }

        private async Task ExtractZipFast(string zipPath, string extractPath, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use System.IO.Compression.ZipFile for better performance
                using (var archive = System.IO.Compression.ZipFile.OpenRead(zipPath))
                {
                    int totalEntries = archive.Entries.Count;
                    int processedEntries = 0;

                    // Create extraction directory if it doesn't exist
                    Directory.CreateDirectory(extractPath);

                    // Process entries sequentially to ensure reliability
                    foreach (var entry in archive.Entries)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            await ExtractEntryAsync(entry, extractPath, cancellationToken);

                            // Update progress
                            var completed = Interlocked.Increment(ref processedEntries);
                            if (progress != null)
                            {
                                var progressPercent = (int)((completed * 100.0) / totalEntries);
                                progress.Report(progressPercent);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the error but continue with other files
                            Debug.WriteLine($"Error extracting {entry.FullName}: {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fast extraction failed: {ex.Message}");
                // Fallback to the original method if the fast method fails
                await Task.Run(() => ExtractZipFallback(zipPath, extractPath));
            }
        }

        private async Task ExtractEntryAsync(ZipArchiveEntry entry, string extractPath, CancellationToken cancellationToken)
        {
            // Skip directories (but don't return, as we need to create the directory)
            bool isDirectory = string.IsNullOrEmpty(entry.Name) || entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\");

            string destinationPath = Path.Combine(extractPath, entry.FullName.Replace('/', '\\'));

            // Ensure the directory exists
            string directoryPath = isDirectory ? destinationPath : Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // If it's a directory, we're done after creating it
            if (isDirectory)
                return;

            // Use larger buffer for better performance (1MB for large files, 80KB for small files)
            int bufferSize = entry.Length > 1024 * 1024 ? 1024 * 1024 : 81920;

            // Extract the file with optimized buffer size and file options
            using (var entryStream = entry.Open())
            using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write,
                FileShare.None, bufferSize, FileOptions.SequentialScan))
            {
                await entryStream.CopyToAsync(fileStream, bufferSize, cancellationToken);
            }

            // Preserve file timestamps
            File.SetLastWriteTime(destinationPath, entry.LastWriteTime.DateTime);
        }

        private void ExtractZipFallback(string path, string extractPath)
        {
            // Fallback to original method if fast extraction fails
            using (Ionic.Zip.ZipFile modZip = Ionic.Zip.ZipFile.Read(path))
            {
                // Set extraction behavior
                modZip.ExtractExistingFile = Ionic.Zip.ExtractExistingFileAction.OverwriteSilently;
                modZip.ZipErrorAction = Ionic.Zip.ZipErrorAction.Skip;

                // Extract all entries
                modZip.ExtractAll(extractPath, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
            }
        }

        private async void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                // Check if download was cancelled or failed
                if (e.Cancelled || e.Error != null)
                {
                    labelDownloadPercent.Text = "Download failed. Please try again.";
                    buttonPlay.Visibility = Visibility.Visible;
                    return;
                }

                labelDownloadPercent.Text = "Extracting files...";
                progressbarDownload.Value = 0; // Reset progress bar for extraction

                // Backup user settings before extraction
                string tempBackupPath = Path.Combine(Path.GetTempPath(), "GlobalLauncher_Backup_" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempBackupPath);

                // Backup conf and characterdata folders if they exist
                string confPath = Path.Combine(GetLauncherPath(), "conf");
                string characterDataPath = Path.Combine(GetLauncherPath(), "characterdata");
                string backupConfPath = Path.Combine(tempBackupPath, "conf");
                string backupCharacterDataPath = Path.Combine(tempBackupPath, "characterdata");

                bool confBackedUp = false;
                bool characterDataBackedUp = false;

                if (Directory.Exists(confPath))
                {
                    CopyDirectory(confPath, backupConfPath);
                    confBackedUp = true;
                }

                if (Directory.Exists(characterDataPath))
                {
                    CopyDirectory(characterDataPath, backupCharacterDataPath);
                    characterDataBackedUp = true;
                }

                if (clientConfig.replaceFolders)
                {
                    foreach (ReplaceFolderName folderName in clientConfig.replaceFolderName)
                    {
                        string folderPath = Path.Combine(GetLauncherPath(), folderName.name);
                        if (Directory.Exists(folderPath))
                        {
                            Directory.Delete(folderPath, true);
                        }
                    }
                }

                // Create progress reporter for extraction
                var extractionProgress = new Progress<int>(percent =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        progressbarDownload.Value = percent;
                        labelDownloadPercent.Text = $"Extracting files... {percent}%";
                    });
                });

                // Use cancellation token for better control
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    // Extract files with fast method and progress reporting
                    string zipPath = GetLauncherPath() + "/tibia.zip";
                    string extractPath = GetLauncherPath();

                    Directory.CreateDirectory(extractPath);

                    // Perform fast extraction with progress updates
                    await ExtractZipFast(zipPath, extractPath, extractionProgress, cancellationTokenSource.Token);

                    // Clean up the zip file
                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath);
                    }
                }

                // Update UI to show completion
                Dispatcher.Invoke(() =>
                {
                    progressbarDownload.Value = 100;
                    labelDownloadPercent.Text = "Finalizing...";
                });

                // Download launcher_config.json from url to the launcher path
                WebClient webClient = new WebClient();
                string localPath = Path.Combine(GetLauncherPath(true), "launcher_config.json");
                await Task.Run(() => webClient.DownloadFile(launcherConfigUrl, localPath));

                // Restore user settings after extraction
                if (confBackedUp && Directory.Exists(backupConfPath))
                {
                    // Remove any conf folder that might have been extracted
                    if (Directory.Exists(confPath))
                    {
                        Directory.Delete(confPath, true);
                    }
                    CopyDirectory(backupConfPath, confPath);
                }

                if (characterDataBackedUp && Directory.Exists(backupCharacterDataPath))
                {
                    // Remove any characterdata folder that might have been extracted
                    if (Directory.Exists(characterDataPath))
                    {
                        Directory.Delete(characterDataPath, true);
                    }
                    CopyDirectory(backupCharacterDataPath, characterDataPath);
                }

                // Clean up temporary backup
                try
                {
                    if (Directory.Exists(tempBackupPath))
                    {
                        Directory.Delete(tempBackupPath, true);
                    }
                }
                catch (Exception)
                {
                    // Ignore cleanup errors
                }

                AddReadOnly();
                CreateShortcut();

                // Update button to show play state
                Dispatcher.Invoke(() =>
                {
                    UpdateButtonToPlayState();
                    needUpdate = false;
                    clientDownloaded = true;
                    labelClientVersion.Text = GetClientVersion(GetLauncherPath(true));
                    buttonPlay_tooltip.Text = GetClientVersion(GetLauncherPath(true));
                    labelClientVersion.Visibility = Visibility.Visible;
                    buttonPlay.Visibility = Visibility.Visible;
                    progressbarDownload.Visibility = Visibility.Collapsed;
                    labelDownloadPercent.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception)
            {
                // Handle any errors during extraction
                Dispatcher.Invoke(() =>
                {
                    labelDownloadPercent.Text = "Extraction failed. Please try again.";
                    buttonPlay.Visibility = Visibility.Visible;
                    progressbarDownload.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressbarDownload.Value = e.ProgressPercentage;
            if (progressbarDownload.Value == 100)
            {
                labelDownloadPercent.Text = "Finishing, wait...";
            }
            else
            {
                // Only show package size for first-time downloads or when folder is missing
                if (!File.Exists(GetLauncherPath(true) + "/launcher_config.json") || !Directory.Exists(GetLauncherPath()))
                {
                    labelDownloadPercent.Text = SizeSuffix(e.BytesReceived) + " / " + SizeSuffix(e.TotalBytesToReceive);
                }
                else
                {
                    // For updates, just show percentage
                    labelDownloadPercent.Text = $"Downloading... {e.ProgressPercentage}%";
                }
            }
        }

        private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        private static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }
            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        /// <summary>
        /// Recursively copies a directory and all its contents to a new location
        /// </summary>
        /// <param name="sourceDir">Source directory path</param>
        /// <param name="destDir">Destination directory path</param>
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true); // Overwrite if exists
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(subDir, destSubDir);
            }
        }

        private void buttonPlay_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        private void buttonPlay_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop the countdown timer when closing
            if (countdownTimer != null)
            {
                countdownTimer.Stop();
            }

            Close();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResizeMode != ResizeMode.NoResize)
            {
                if (WindowState == WindowState.Normal)
                    WindowState = WindowState.Maximized;
                else
                    WindowState = WindowState.Normal;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private async void HintsBox_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right-click to refresh news
            await LoadNewsAsync();
        }

        private async void HintsBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check if we have news items
            if (currentNewsItems != null && currentNewsItems.Count > 0)
            {
                try
                {
                    // Open the current news item
                    await OpenNewsUrl(currentNewsItems[currentNewsIndex].Url);

                    // Move to next news item for next click
                    currentNewsIndex = (currentNewsIndex + 1) % currentNewsItems.Count;

                    // Update display to show which news will be opened next
                    UpdateNewsDisplay();
                }
                catch (Exception)
                {
                    // If opening URL fails, refresh news instead
                    await LoadNewsAsync();
                }
            }
            else
            {
                // If no news items, refresh news
                await LoadNewsAsync();
            }
        }

        private void UpdateNewsDisplay()
        {
            if (currentNewsItems != null && currentNewsItems.Count > 0)
            {
                // Update the display to highlight which news will be opened next
                string formattedNews = NewsService.FormatNewsForDisplayWithHighlight(currentNewsItems, currentNewsIndex);
                Dispatcher.Invoke(() =>
                {
                    hintsBox.Text = formattedNews;
                });
            }
        }

        private async Task OpenNewsUrl(string url)
        {
            try
            {
                if (!url.StartsWith("http"))
                {
                    url = "http://luneraot.com/" + url.TrimStart('?');
                }

                // Open the URL in the default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                // If opening URL fails, refresh news instead
                await LoadNewsAsync();
            }
        }

        private async Task LoadNewsAsync()
        {
            try
            {
                // Show loading message only if not preloaded
                if (!dataPreloaded)
                {
                    hintsBox.Text = "Loading news...";
                }

                // Fetch news from the website
                var newsItems = await NewsService.FetchNewsAsync();

                // Store the news items for click handling
                currentNewsItems = newsItems;
                currentNewsIndex = 0; // Reset to first news item

                // Format and display the news with highlight
                string formattedNews = NewsService.FormatNewsForDisplayWithHighlight(newsItems, currentNewsIndex);

                // Update the UI on the main thread
                Dispatcher.Invoke(() =>
                {
                    hintsBox.Text = formattedNews;
                });
            }
            catch (Exception)
            {
                // Fallback to default content if news loading fails
                currentNewsItems = new List<NewsItem>();
                currentNewsIndex = 0;
                Dispatcher.Invoke(() =>
                {
                    hintsBox.Text = GetDefaultNewsContent();
                });
            }
        }

        private string GetDefaultNewsContent()
        {
            return "🏆 BIENVENIDOS A LuneraOT!\n\n" +
                   "🎮 Nuevas Características:\n" +
                   "• Sistema de Battle Royale mejorado\n" +
                   "• Duelos 1 vs 1 con ranking\n" +
                   "• Nuevas zonas de PvP y eventos\n" +
                   "• Sistema de guilds renovado\n\n" +
                   "⚡ Actualizaciones Recientes:\n" +
                   "• Balance de clases mejorado\n" +
                   "• Nuevos items y equipamiento épico\n" +
                   "• Optimización de rendimiento\n" +
                   "• Corrección de bugs críticos\n\n" +
                   "⚠️ Importante:\n" +
                   "LuneraOT puede ser peligroso. ¡Mantente alerta!\n\n" +
                   "📅 Próximos Eventos:\n" +
                   "• Torneo de guilds este fin de semana\n" +
                   "• Evento de experiencia doble\n" +
                   "• Nueva quest épica disponible\n\n" +
                   "Servidor en constante desarrollo y mejora.";
        }

        private async Task LoadBoostedCreaturesAsync(bool forceRefresh = false)
        {
            try
            {
                // Only fetch if not preloaded or force refresh is requested
                if (!dataPreloaded || forceRefresh)
                {
                    // Fetch boosted creatures from the website
                    var (creature, boss) = await BoostedCreatureService.FetchBoostedCreaturesAsync(forceRefresh);

                    // Store the boosted creatures
                    currentBoostedCreature = creature;
                    currentBoostedBoss = boss;
                }

                // Update the UI on the main thread
                Dispatcher.Invoke(() =>
                {
                    UpdateBoostedCreaturesDisplay();
                });
            }
            catch (Exception)
            {
                // Use fallback data if loading fails
                Dispatcher.Invoke(() =>
                {
                    LoadFallbackBoostedCreatures();
                });
            }
        }

        private void UpdateBoostedCreaturesDisplay()
        {
            try
            {
                if (currentBoostedCreature != null)
                {
                    BoostedCreatureName.Text = currentBoostedCreature.Name;

                    // Use preloaded image if available, otherwise load from URL
                    if (preloadedBoostedCreatureImage != null)
                    {
                        BoostedCreatureImage.Source = preloadedBoostedCreatureImage;
                    }
                    else
                    {
                        LoadImageAsync(BoostedCreatureImage, currentBoostedCreature.ImageUrl);
                    }
                }

                if (currentBoostedBoss != null)
                {
                    BoostedBossName.Text = currentBoostedBoss.Name;

                    // Use preloaded image if available, otherwise load from URL
                    if (preloadedBoostedBossImage != null)
                    {
                        BoostedBossImage.Source = preloadedBoostedBossImage;
                    }
                    else
                    {
                        LoadImageAsync(BoostedBossImage, currentBoostedBoss.ImageUrl);
                    }
                }
            }
            catch (Exception)
            {
                LoadFallbackBoostedCreatures();
            }
        }

        private void LoadFallbackBoostedCreatures()
        {
            BoostedCreatureName.Text = "Loading...";
            BoostedBossName.Text = "Loading...";

            // Clear images when loading
            BoostedCreatureImage.Source = null;
            BoostedBossImage.Source = null;
        }

        private async void LoadImageAsync(Image imageControl, string imageUrl)
        {
            // If no URL provided, clear the image
            if (string.IsNullOrEmpty(imageUrl))
            {
                Dispatcher.Invoke(() =>
                {
                    imageControl.Source = null;
                });
                return;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                    var imageBytes = await client.GetByteArrayAsync(imageUrl);

                    Dispatcher.Invoke(() =>
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(imageBytes);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        imageControl.Source = bitmap;
                    });
                }
            }
            catch (Exception)
            {
                // If image loading fails, we'll just leave it empty or use a placeholder
                Dispatcher.Invoke(() =>
                {
                    imageControl.Source = null;
                });
            }
        }

        private async void BoostedCreaturesPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Force refresh boosted creatures when clicked
            await LoadBoostedCreaturesAsync(forceRefresh: true);
        }

        private async void CountdownsPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Force refresh countdowns when clicked
            await LoadCountdownsAsync(forceRefresh: true);
        }

        private async Task LoadCountdownsAsync(bool forceRefresh = false)
        {
            try
            {
                // Show loading state only if not preloaded
                if (!dataPreloaded)
                {
                    Dispatcher.Invoke(() =>
                    {
                        FirstCountdownName.Text = "Loading...";
                        FirstCountdownTime.Text = "--:--:--";
                        SecondCountdownName.Text = "Loading...";
                        SecondCountdownTime.Text = "--:--:--";
                    });
                }

                // Only fetch if not preloaded or force refresh is requested
                if (!dataPreloaded || forceRefresh)
                {
                    // Fetch countdowns from the website
                    var countdowns = await CountdownService.FetchCountdownsAsync(forceRefresh);

                    // Store the countdowns
                    currentCountdowns = countdowns;
                }

                // Update the UI on the main thread
                Dispatcher.Invoke(() =>
                {
                    UpdateCountdownsDisplay();
                });
            }
            catch (Exception)
            {
                // Use fallback data if loading fails
                Dispatcher.Invoke(() =>
                {
                    LoadFallbackCountdowns();
                });
            }
        }

        private void UpdateCountdownsDisplay()
        {
            try
            {
                // Hide containers if no countdowns
                if (currentCountdowns == null || currentCountdowns.Count == 0)
                {
                    FirstCountdownContainer.Visibility = Visibility.Collapsed;
                    SecondCountdownContainer.Visibility = Visibility.Collapsed;
                    return;
                }

                // First countdown
                if (currentCountdowns.Count > 0)
                {
                    FirstCountdownContainer.Visibility = Visibility.Visible;
                    FirstCountdownName.Text = currentCountdowns[0].Name;
                    FirstCountdownTime.Text = currentCountdowns[0].GetFormattedRemainingTime();
                }
                else
                {
                    FirstCountdownContainer.Visibility = Visibility.Collapsed;
                }

                // Second countdown
                if (currentCountdowns.Count > 1)
                {
                    SecondCountdownContainer.Visibility = Visibility.Visible;
                    SecondCountdownName.Text = currentCountdowns[1].Name;
                    SecondCountdownTime.Text = currentCountdowns[1].GetFormattedRemainingTime();
                }
                else
                {
                    SecondCountdownContainer.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                LoadFallbackCountdowns();
            }
        }

        private void LoadFallbackCountdowns()
        {
            FirstCountdownName.Text = "Battle Royale";
            FirstCountdownTime.Text = "Loading...";
            SecondCountdownName.Text = "Double XP";
            SecondCountdownTime.Text = "Loading...";
        }

        private void StartCountdownTimer()
        {
            // Create and start a timer that ticks every second
            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            // Update countdowns
            if (currentCountdowns != null && currentCountdowns.Count > 0)
            {
                UpdateCountdownsDisplay();
            }

            // Refresh online players every 60 seconds
            if (DateTime.Now.Second % 60 == 0)
            {
                _ = LoadOnlinePlayersAsync(forceRefresh: true);
            }
        }

        /// <summary>
        /// Repositions the countdown section to a new location in the launcher
        /// </summary>
        /// <param name="row">The grid row to place the countdown section</param>
        /// <param name="rowSpan">How many rows the countdown section should span</param>
        /// <param name="verticalAlignment">Vertical alignment (Top, Center, Bottom)</param>
        /// <param name="horizontalAlignment">Horizontal alignment (Left, Center, Right)</param>
        /// <param name="margin">Margin around the countdown section (left,top,right,bottom)</param>
        public void RepositionCountdowns(int row, int rowSpan, VerticalAlignment verticalAlignment,
            HorizontalAlignment horizontalAlignment, Thickness margin)
        {
            // Set the grid row and row span
            Grid.SetRow(CountdownsGrid, row);
            Grid.SetRowSpan(CountdownsGrid, rowSpan);

            // Set the alignment for the entire grid
            CountdownsGrid.VerticalAlignment = verticalAlignment;
            CountdownsGrid.HorizontalAlignment = horizontalAlignment;

            // Set the margin for the entire grid
            CountdownsGrid.Margin = margin;

            // Reset the container alignments to stretch to fill the grid
            CountdownsBackgroundContainer.VerticalAlignment = VerticalAlignment.Stretch;
            CountdownsBackgroundContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
            CountdownsBackgroundContainer.Margin = new Thickness(0);

            // Adjust the title and content positions based on vertical alignment
            if (verticalAlignment == VerticalAlignment.Top)
            {
                // Position title at the top
                CountdownsTitle.VerticalAlignment = VerticalAlignment.Top;
                CountdownsTitle.Margin = new Thickness(0, -15, 0, 0);

                // Position content below the title
                CountdownsStackPanel.VerticalAlignment = VerticalAlignment.Top;
                CountdownsStackPanel.Margin = new Thickness(0, 35, 0, 0);
            }
            else if (verticalAlignment == VerticalAlignment.Bottom)
            {
                // Position title at the top of the container
                CountdownsTitle.VerticalAlignment = VerticalAlignment.Top;
                CountdownsTitle.Margin = new Thickness(0, -15, 0, 0);

                // Position content below the title
                CountdownsStackPanel.VerticalAlignment = VerticalAlignment.Top;
                CountdownsStackPanel.Margin = new Thickness(0, 35, 0, 0);
            }
            else // Center
            {
                // Position title at the top of the container
                CountdownsTitle.VerticalAlignment = VerticalAlignment.Top;
                CountdownsTitle.Margin = new Thickness(0, -15, 0, 0);

                // Position content below the title
                CountdownsStackPanel.VerticalAlignment = VerticalAlignment.Top;
                CountdownsStackPanel.Margin = new Thickness(0, 35, 0, 0);
            }
        }

        private void buttonBuyCoins_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the Global Coins purchase page in the default browser
                string buyCoinsUrl = "https://luneraot.com/?donations";

                Process.Start(new ProcessStartInfo
                {
                    FileName = buyCoinsUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                // If opening URL fails, show error in version label temporarily
                string originalText = labelVersion.Text;
                labelVersion.Text = "Error opening shop";

                // Reset the text after 3 seconds
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, args) =>
                {
                    labelVersion.Text = originalText;
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void DiscordLogo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Open the Discord invite link in the default browser
                string discordInviteUrl = "https://discord.com/invite/X8kkhFCe98";

                Process.Start(new ProcessStartInfo
                {
                    FileName = discordInviteUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                // If opening URL fails, show error in version label temporarily
                string originalText = labelVersion.Text;
                labelVersion.Text = "Error opening Discord";

                // Reset the text after 3 seconds
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, args) =>
                {
                    labelVersion.Text = originalText;
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void ImageLogoServer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check if the click is on a non-transparent pixel
            if (IsClickOnVisiblePixel(sender as Image, e))
            {
                try
                {
                    // Open the 127.0.0.1 website in the default browser
                    string globalotUrl = "http://luneraot.com";

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = globalotUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception)
                {
                    // If opening URL fails, show error in version label temporarily
                    string originalText = labelVersion.Text;
                    labelVersion.Text = "Error opening website";

                    // Reset the text after 3 seconds
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, args) =>
                    {
                        labelVersion.Text = originalText;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
        }

        private void ImageLogoServer_MouseMove(object sender, MouseEventArgs e)
        {
            var image = sender as Image;
            if (image == null) return;

            // Check if the mouse is over a visible pixel and update cursor and tooltip accordingly
            if (IsMouseOverVisiblePixel(image, e))
            {
                image.Cursor = Cursors.Hand;

                // Show tooltip only over visible pixels
                if (image.ToolTip == null)
                {
                    var tooltip = new ToolTip
                    {
                        Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                        Foreground = new SolidColorBrush(Colors.White),
                        Content = new TextBlock
                        {
                            Text = "Click to visit LuneraOT.com",
                            FontWeight = FontWeights.Bold
                        }
                    };
                    image.ToolTip = tooltip;
                }
            }
            else
            {
                image.Cursor = Cursors.Arrow;

                // Hide tooltip when over transparent pixels
                image.ToolTip = null;
            }
        }

        private void ImageLogoServer_MouseLeave(object sender, MouseEventArgs e)
        {
            var image = sender as Image;
            if (image == null) return;

            // Reset cursor and hide tooltip when leaving the image
            image.Cursor = Cursors.Arrow;
            image.ToolTip = null;
        }

        private bool IsClickOnVisiblePixel(Image image, MouseButtonEventArgs e)
        {
            if (image?.Source == null) return false;

            try
            {
                // Get the position of the click relative to the image
                Point clickPosition = e.GetPosition(image);

                // Get the image source as BitmapSource
                BitmapSource bitmapSource = image.Source as BitmapSource;
                if (bitmapSource == null) return true; // If we can't check, allow the click

                // Calculate the actual pixel coordinates considering the image scaling
                double scaleX = bitmapSource.PixelWidth / image.ActualWidth;
                double scaleY = bitmapSource.PixelHeight / image.ActualHeight;

                int pixelX = (int)(clickPosition.X * scaleX);
                int pixelY = (int)(clickPosition.Y * scaleY);

                // Check bounds
                if (pixelX < 0 || pixelX >= bitmapSource.PixelWidth ||
                    pixelY < 0 || pixelY >= bitmapSource.PixelHeight)
                    return false;

                // Create a cropped bitmap of just the clicked pixel
                var croppedBitmap = new CroppedBitmap(bitmapSource, new Int32Rect(pixelX, pixelY, 1, 1));

                // Get the pixel data
                byte[] pixelData = new byte[4]; // RGBA
                croppedBitmap.CopyPixels(pixelData, 4, 0);

                // Check if the alpha channel indicates the pixel is visible (not fully transparent)
                byte alpha = pixelData[3];

                // Return true if the pixel is not fully transparent (alpha > 0)
                return alpha > 0;
            }
            catch (Exception)
            {
                // If there's any error in pixel checking, allow the click
                return true;
            }
        }

        private bool IsMouseOverVisiblePixel(Image image, MouseEventArgs e)
        {
            if (image?.Source == null) return false;

            try
            {
                // Get the position of the mouse relative to the image
                Point mousePosition = e.GetPosition(image);

                // Get the image source as BitmapSource
                BitmapSource bitmapSource = image.Source as BitmapSource;
                if (bitmapSource == null) return true; // If we can't check, assume visible

                // Calculate the actual pixel coordinates considering the image scaling
                double scaleX = bitmapSource.PixelWidth / image.ActualWidth;
                double scaleY = bitmapSource.PixelHeight / image.ActualHeight;

                int pixelX = (int)(mousePosition.X * scaleX);
                int pixelY = (int)(mousePosition.Y * scaleY);

                // Check bounds
                if (pixelX < 0 || pixelX >= bitmapSource.PixelWidth ||
                    pixelY < 0 || pixelY >= bitmapSource.PixelHeight)
                    return false;

                // Create a cropped bitmap of just the pixel under the mouse
                var croppedBitmap = new CroppedBitmap(bitmapSource, new Int32Rect(pixelX, pixelY, 1, 1));

                // Get the pixel data
                byte[] pixelData = new byte[4]; // RGBA
                croppedBitmap.CopyPixels(pixelData, 4, 0);

                // Check if the alpha channel indicates the pixel is visible (not fully transparent)
                byte alpha = pixelData[3];

                // Return true if the pixel is not fully transparent (alpha > 0)
                return alpha > 0;
            }
            catch (Exception)
            {
                // If there's any error in pixel checking, assume visible
                return true;
            }
        }
    }
}