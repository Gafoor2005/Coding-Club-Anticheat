using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Coding_Club_Anticheat.Services;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Timers;
using System.Collections.Generic;

namespace Coding_Club_Anticheat
{
    public sealed partial class HomePage : Page
    {
        private IWebDriver? _webDriver;
        private Timer? _urlMonitoringTimer;
        private string _allowedTestUrl = "";
        private string _lastValidUrl = "";
        private int _violationCount = 0;
        private readonly FirebaseService _firebaseService;
        private readonly ConfigurationService _configurationService;
        private bool _isDialogShowing = false; // Track if a dialog is currently open
        private List<string> _allowedDomains = new(); // Changed from readonly to mutable for dynamic updates

        public HomePage()
        {
            this.InitializeComponent();
            _firebaseService = new FirebaseService();
            _configurationService = new ConfigurationService();
            UpdateStartTestButtonState();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            System.Diagnostics.Debug.WriteLine($"[HomePage] OnNavigatedTo called");

            // Check if user confirmed test launch from TestLaunchPage
            if (Application.Current.Resources.ContainsKey("TestLaunchConfirmed"))
            {
                System.Diagnostics.Debug.WriteLine($"[HomePage] Found TestLaunchConfirmed key");
                
                if (Application.Current.Resources["TestLaunchConfirmed"] is bool confirmed && confirmed)
                {
                    System.Diagnostics.Debug.WriteLine($"[HomePage] TestLaunchConfirmed is TRUE - proceeding with launch");
                    
                    // Clear the flag immediately
                    Application.Current.Resources.Remove("TestLaunchConfirmed");

                    // Get the test info
                    if (Application.Current.Resources.ContainsKey("TestLaunchCode") &&
                        Application.Current.Resources.ContainsKey("TestLaunchInfo"))
                    {
                        string testCode = Application.Current.Resources["TestLaunchCode"] as string ?? "";
                        var testCodeInfo = Application.Current.Resources["TestLaunchInfo"] as TestCodeInfo;

                        System.Diagnostics.Debug.WriteLine($"[HomePage] Retrieved testCode: {testCode}");
                        System.Diagnostics.Debug.WriteLine($"[HomePage] Retrieved testCodeInfo: {testCodeInfo?.TestTitle ?? "NULL"}");

                        Application.Current.Resources.Remove("TestLaunchCode");
                        Application.Current.Resources.Remove("TestLaunchInfo");

                        if (testCodeInfo != null && !string.IsNullOrEmpty(testCode))
                        {
                            System.Diagnostics.Debug.WriteLine($"[HomePage] Launching browser for test: {testCodeInfo.TestTitle}");
                            
                            // Launch the browser
                            ShowStartTestLoading(true);
                            await LaunchControlledBrowser(testCode, testCodeInfo);
                            ShowStartTestLoading(false);
                            
                            System.Diagnostics.Debug.WriteLine($"[HomePage] Browser launch completed");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[HomePage] ERROR: testCodeInfo or testCode is null/empty");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[HomePage] ERROR: TestLaunchCode or TestLaunchInfo keys not found");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[HomePage] TestLaunchConfirmed is FALSE or not bool");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[HomePage] TestLaunchConfirmed key not found - normal navigation");
            }
        }

        private void UpdateStartTestButtonState()
        {
            bool isComplete = !string.IsNullOrEmpty(Digit1?.Text) &&
                             !string.IsNullOrEmpty(Digit2?.Text) &&
                             !string.IsNullOrEmpty(Digit3?.Text) &&
                             !string.IsNullOrEmpty(Digit4?.Text);
            
            if (StartTestButton != null)
                StartTestButton.IsEnabled = isComplete;
        }

        private void Digit_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = !IsDigitsOnly(args.NewText);
        }

        private bool IsDigitsOnly(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            
            return text.All(char.IsDigit);
        }

        private void Digit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                if (textBox == Digit1)
                    Digit2?.Focus(FocusState.Programmatic);
                else if (textBox == Digit2)
                    Digit3?.Focus(FocusState.Programmatic);
                else if (textBox == Digit3)
                    Digit4?.Focus(FocusState.Programmatic);
            }
            
            UpdateStartTestButtonState();
        }

        private void Digit_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is TextBox textBox && e.Key == VirtualKey.Back)
            {
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    if (textBox == Digit4)
                    {
                        Digit3?.Focus(FocusState.Programmatic);
                        if (Digit3 != null) Digit3.Text = "";
                    }
                    else if (textBox == Digit3)
                    {
                        Digit2?.Focus(FocusState.Programmatic);
                        if (Digit2 != null) Digit2.Text = "";
                    }
                    else if (textBox == Digit2)
                    {
                        Digit1?.Focus(FocusState.Programmatic);
                        if (Digit1 != null) Digit1.Text = "";
                    }
                    
                    UpdateStartTestButtonState();
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (Digit1 != null) Digit1.Text = "";
            if (Digit2 != null) Digit2.Text = "";
            if (Digit3 != null) Digit3.Text = "";
            if (Digit4 != null) Digit4.Text = "";
            
            Digit1?.Focus(FocusState.Programmatic);
            UpdateStartTestButtonState();
        }

        private async void StartTestButton_Click(object sender, RoutedEventArgs e)
        {
            string testCode = (Digit1?.Text ?? "") + (Digit2?.Text ?? "") + (Digit3?.Text ?? "") + (Digit4?.Text ?? "");
            
            if (testCode.Length == 4)
            {
                // Show loading state
                ShowStartTestLoading(true);
                
                try
                {
                    // First check if the test code exists in database
                    var testCodeInfo = await ValidateTestCodeAsync(testCode);
                    
                    // Hide loading state
                    ShowStartTestLoading(false);
                    
                    if (testCodeInfo != null)
                    {
                        // Navigate to the full-page test launch confirmation
                        Frame.Navigate(typeof(TestLaunchPage), new TestLaunchPageParameter
                        {
                            TestCodeInfo = testCodeInfo,
                            TestCode = testCode
                        });
                    }
                    else
                    {
                        // Test code not found or inactive
                        await ShowTestCodeNotFoundMessage(testCode);
                    }
                }
                catch (Exception ex)
                {
                    ShowStartTestLoading(false);
                    System.Diagnostics.Debug.WriteLine($"Error in StartTestButton_Click: {ex.Message}");
                    await ShowErrorMessage("Error", "An unexpected error occurred. Please try again.");
                }
            }
            else
            {
                await ShowIncompleteCodeMessage();
            }
        }

        private void ShowStartTestLoading(bool isLoading)
        {
            // Find the controls by name
            var startTestContent = StartTestButton?.FindName("StartTestContent") as StackPanel;
            var startTestLoadingContent = StartTestButton?.FindName("StartTestLoadingContent") as StackPanel;
            
            if (startTestContent != null && startTestLoadingContent != null && StartTestButton != null)
            {
                startTestContent.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
                startTestLoadingContent.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
                StartTestButton.IsEnabled = !isLoading;
                ClearButton.IsEnabled = !isLoading;
                
                // Disable digit inputs while loading
                Digit1.IsEnabled = !isLoading;
                Digit2.IsEnabled = !isLoading;
                Digit3.IsEnabled = !isLoading;
                Digit4.IsEnabled = !isLoading;
            }
        }

        private async Task<TestCodeInfo?> ValidateTestCodeAsync(string testCode)
        {
            try
            {
                // Get configuration settings
                var settings = _configurationService.Settings;
                string firebaseProjectId = settings.TestCodeSettings.DefaultProjectId;
                
                // Fallback to hardcoded project ID if not in config
                if (string.IsNullOrEmpty(firebaseProjectId))
                {
                    firebaseProjectId = settings.Firebase.ProjectId;
                }
                
                if (string.IsNullOrEmpty(firebaseProjectId))
                {
                    firebaseProjectId = "coding-club-anticheat"; // Final fallback
                }
                
                // Show diagnostic information
                System.Diagnostics.Debug.WriteLine("=== FIREBASE DIAGNOSTIC START ===");
                string serviceAccountKeyPath = settings.Firebase.ServiceAccountKeyPath;
                System.Diagnostics.Debug.WriteLine(_firebaseService.GetDiagnosticInfo(serviceAccountKeyPath));
                System.Diagnostics.Debug.WriteLine("=== FIREBASE DIAGNOSTIC END ===");
                
                bool initialized;
                if (!string.IsNullOrEmpty(serviceAccountKeyPath))
                {
                    // Use direct service account key path with embedded JSON support
                    System.Diagnostics.Debug.WriteLine($"Initializing Firebase with direct service account key: {serviceAccountKeyPath}");
                    initialized = await _firebaseService.InitializeAsync(firebaseProjectId, serviceAccountKeyPath, settings.Firebase.ServiceAccountKeyJson);
                }
                else if (!string.IsNullOrEmpty(settings.Firebase.ServiceAccountKeyJson))
                {
                    // Use embedded JSON credentials (for distributed apps)
                    System.Diagnostics.Debug.WriteLine("Initializing Firebase with embedded credentials");
                    initialized = await _firebaseService.InitializeAsync(firebaseProjectId, null, settings.Firebase.ServiceAccountKeyJson);
                }
                else
                {
                    // Fallback to environment variable method
                    System.Diagnostics.Debug.WriteLine("No service account key path in config, using environment variable method");
                    initialized = await _firebaseService.InitializeAsync(firebaseProjectId);
                }

                if (!initialized)
                {
                    string errorMessage = "Failed to connect to Firebase. Please check:\n\n" +
                        "1. Internet connection is working\n" +
                        "2. Firebase service account key is configured\n";
                    
                    if (!string.IsNullOrEmpty(serviceAccountKeyPath))
                    {
                        errorMessage += $"3. Service account key file exists at: {serviceAccountKeyPath}\n" +
                                      "4. Firebase project ID is correct\n\n";
                    }
                    else
                    {
                        errorMessage += "3. GOOGLE_APPLICATION_CREDENTIALS environment variable is set\n" +
                                      "4. Firebase project ID is correct\n\n" +
                                      "OR configure ServiceAccountKeyPath in appsettings.json\n\n";
                    }
                    
                    errorMessage += "Check the Output window for detailed diagnostic information.";
                    
                    await ShowErrorMessage("Firebase Connection Error", errorMessage);
                    return null;
                }

                var testCodeInfo = await _firebaseService.GetTestCodeInfoAsync(testCode);
                
                // Return the test code info only if it exists and is active
                if (testCodeInfo != null && testCodeInfo.IsActive)
                {
                    return testCodeInfo;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to validate test code: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception Details: {ex}");
                
                string errorMessage = "Failed to validate test code. ";
                
                // Provide specific error guidance based on exception type
                if (ex.Message.Contains("ApplicationDefaultCredentials") || ex.Message.Contains("Invalid key file"))
                {
                    errorMessage += "\n\nPROBLEM: Firebase credentials not found or invalid.\n" +
                                  "SOLUTION: Check your service account key file path in appsettings.json or set up environment variable.";
                }
                else if (ex.Message.Contains("network") || ex.Message.Contains("connection") || ex.Message.Contains("timeout"))
                {
                    errorMessage += "\n\nPROBLEM: Network connection issue.\n" +
                                  "SOLUTION: Check your internet connection and try again.";
                }
                else if (ex.Message.Contains("permission") || ex.Message.Contains("access") || ex.Message.Contains("forbidden"))
                {
                    errorMessage += "\n\nPROBLEM: Firebase permission issue.\n" +
                                  "SOLUTION: Check your Firebase project permissions.";
                }
                else
                {
                    errorMessage += $"\n\nError details: {ex.Message}";
                }
                
                await ShowErrorMessage("Validation Error", errorMessage);
                return null;
            }
        }

        private async Task ShowTestCodeMessage(TestCodeInfo testCodeInfo)
        {
            // Prevent multiple dialogs
            if (_isDialogShowing) return;
            
            try
            {
                _isDialogShowing = true;
                
                string testTitle = !string.IsNullOrEmpty(testCodeInfo.TestTitle) 
                    ? testCodeInfo.TestTitle 
                    : "Untitled Test";

                ContentDialog dialog = new ContentDialog()
                {
                    Title = $"🎯 Launching Test: {testTitle}",
                    Content = $"📋 Test Details:\n" +
                             $"• Title: {testTitle}\n" +
                             $"• Code: {testCodeInfo.TestCode}\n" +
                             $"• Usage Count: {testCodeInfo.UsageCount}\n\n" +
                             $"🚨 ZERO-TOLERANCE MODE ACTIVATED:\n" +
                             $"• ANY cheating attempt = IMMEDIATE TERMINATION\n" +
                             $"• No warnings, no second chances\n" +
                             $"• Tab switching = INSTANT EXIT\n" +
                             $"• Copy/paste = INSTANT EXIT\n" +
                             $"• Developer tools = INSTANT EXIT\n" +
                             $"• External navigation = INSTANT EXIT\n" +
                             $"• Right-click = INSTANT EXIT\n\n" +
                             $"⚠️ WARNING: This is your ONLY chance to complete the test!\n\n" +
                             $"Press OK to launch the zero-tolerance browser.",
                    CloseButtonText = "I Understand - Launch Test",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            finally
            {
                _isDialogShowing = false;
            }
        }

        private async Task ShowTestCodeNotFoundMessage(string testCode)
        {
            // Prevent multiple dialogs
            if (_isDialogShowing) return;
            
            try
            {
                _isDialogShowing = true;
                
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "❌ Test Code Not Found",
                    Content = $"The test code '{testCode}' was not found or is currently inactive.\n\n" +
                             $"Possible reasons:\n" +
                             $"• The code doesn't exist in the database\n" +
                             $"• The code has been disabled by the administrator\n" +
                             $"• The code has expired\n\n" +
                             $"Please check your code and try again, or contact your instructor for assistance.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            finally
            {
                _isDialogShowing = false;
            }
        }

        private async Task<string> GetTestUrlFromFirebase(string testCode)
        {
            try
            {
                // Get configuration settings
                var settings = _configurationService.Settings;
                string firebaseProjectId = settings.TestCodeSettings.DefaultProjectId;
                
                // Fallback to hardcoded project ID if not in config
                if (string.IsNullOrEmpty(firebaseProjectId))
                {
                    firebaseProjectId = settings.Firebase.ProjectId;
                }
                
                if (string.IsNullOrEmpty(firebaseProjectId))
                {
                    firebaseProjectId = "coding-club-anticheat"; // Final fallback
                }

                string serviceAccountKeyPath = settings.Firebase.ServiceAccountKeyPath;
                
                bool initialized;
                if (!string.IsNullOrEmpty(serviceAccountKeyPath))
                {
                    initialized = await _firebaseService.InitializeAsync(firebaseProjectId, serviceAccountKeyPath, settings.Firebase.ServiceAccountKeyJson);
                }
                else if (!string.IsNullOrEmpty(settings.Firebase.ServiceAccountKeyJson))
                {
                    initialized = await _firebaseService.InitializeAsync(firebaseProjectId, null, settings.Firebase.ServiceAccountKeyJson);
                }
                else
                {
                    initialized = await _firebaseService.InitializeAsync(firebaseProjectId);
                }

                if (!initialized)
                    return "";

                var testCodeInfo = await _firebaseService.GetTestCodeInfoAsync(testCode);
                if (testCodeInfo == null || !testCodeInfo.IsActive)
                    return "";

                await _firebaseService.IncrementUsageAsync(testCode);
                return testCodeInfo.TestUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get test URL from Firebase: {ex.Message}");
                return "";
            }
        }

        private async Task LaunchControlledBrowser(string testCode, TestCodeInfo? testCodeInfo = null)
        {
            try
            {
                _webDriver = await CreateControlledBrowser();
                
                string testUrl = "";
                
                if (testCodeInfo != null)
                {
                    // Use the URL from the validated test code info
                    testUrl = testCodeInfo.TestUrl;
                    // Increment usage count since we're launching the test
                    await _firebaseService.IncrementUsageAsync(testCode);
                }
                else
                {
                    // Fallback to Firebase lookup (shouldn't happen with new flow)
                    testUrl = await GetTestUrlFromFirebase(testCode);
                }

                if (!string.IsNullOrEmpty(testUrl))
                {
                    _allowedTestUrl = testUrl;
                    
                    // Dynamically set allowed domains based on test URL
                    try
                    {
                        var uri = new Uri(testUrl);
                        _allowedDomains.Clear();
                        _allowedDomains.Add(uri.Host.ToLowerInvariant());
                        
                        // Also allow www version
                        if (!uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                        {
                            _allowedDomains.Add($"www.{uri.Host}".ToLowerInvariant());
                        }
                        else
                        {
                            // If it starts with www, also allow non-www version
                            _allowedDomains.Add(uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant());
                        }
                        
                        Console.WriteLine($"ANTICHEAT: Allowed domains set to: {string.Join(", ", _allowedDomains)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ANTICHEAT: Failed to parse test URL domain: {ex.Message}");
                        // If URL parsing fails, clear domains (will allow any domain)
                        _allowedDomains.Clear();
                    }
                }
                else
                {
                    // Final fallback to HackerRank pattern
                    _allowedTestUrl = $"https://hackerrank.com";
                    _allowedDomains = new List<string> 
                    { 
                        "hackerrank.com", 
                        "www.hackerrank.com", 
                        "hr-challenge-images.s3.amazonaws.com",
                        "hrcdn.net"
                    };
                }
                
                _lastValidUrl = _allowedTestUrl;
                _webDriver.Navigate().GoToUrl(_allowedTestUrl);
                
                await InjectSecurityScriptOnly(_webDriver);
                StartUrlMonitoring();
                StartJavaScriptViolationMonitoring();
                
                Console.WriteLine($"ANTICHEAT: Browser launched with ZERO-TOLERANCE mode activated for test: {testCodeInfo?.TestTitle ?? "Unknown"}");
            }
            catch (Exception ex)
            {
                await ShowErrorMessage("Browser Launch Error", $"Failed to launch controlled browser: {ex.Message}");
            }
        }

        private static Task<IWebDriver> CreateControlledBrowser()
        {
            try
            {
                var options = new ChromeOptions();
                options.AddArgument("--no-default-browser-check");
                options.AddArgument("--disable-default-apps");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--disable-plugins");
                options.AddArgument("--disable-background-timer-throttling");
                options.AddArgument("--disable-backgrounding-occluded-windows");
                options.AddArgument("--disable-renderer-backgrounding");
                options.AddArgument("--disable-features=TranslateUI");
                options.AddArgument("--disable-ipc-flooding-protection");
                options.AddArgument("--disable-web-security");
                options.AddArgument("--disable-features=VizDisplayCompositor");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--kiosk");
                options.AddArgument("--start-maximized");
                options.AddArgument("--disable-popup-blocking");
                options.AddArgument("--disable-background-networking");
                options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
                options.AddUserProfilePreference("profile.default_content_settings.popups", 0);

                var driver = new ChromeDriver(options);
                return Task.FromResult<IWebDriver>(driver);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create controlled browser: {ex.Message}");
            }
        }

        private bool IsAllowedUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            
            try
            {
                var uri = new Uri(url);
                var domain = uri.Host.ToLowerInvariant();
                return _allowedDomains.Any(allowedDomain => 
                    domain == allowedDomain || domain.EndsWith("." + allowedDomain));
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void StartUrlMonitoring()
        {
            _urlMonitoringTimer = new Timer(1000);
            _urlMonitoringTimer.Elapsed += async (sender, e) => await MonitorUrlAndNavigation();
            _urlMonitoringTimer.AutoReset = true;
            _urlMonitoringTimer.Start();
            
            Console.WriteLine("ANTICHEAT: URL monitoring started");
        }

        private async Task MonitorUrlAndNavigation()
        {
            if (_webDriver == null) return;

            try
            {
                string currentUrl = _webDriver.Url;
                
                if (!IsAllowedUrl(currentUrl))
                {
                    await HandleCheatDetected("Unauthorized Website Access", currentUrl);
                    return;
                }
                else
                {
                    _lastValidUrl = currentUrl;
                    await EnsureSecurityScriptsActive();
                }
                
                var windowHandles = _webDriver.WindowHandles;
                if (windowHandles.Count > 1)
                {
                    await HandleCheatDetected("Multiple Tabs/Windows", $"Detected {windowHandles.Count} windows/tabs");
                    return;
                }
            }
            catch (OpenQA.Selenium.WebDriverException ex)
            {
                // Browser was closed or connection lost
                Console.WriteLine($"ANTICHEAT: Browser closed or connection lost: {ex.Message}");
                _urlMonitoringTimer?.Stop();
                
                // Cleanup and show message
                await CleanupBrowserResources();
                
                this.DispatcherQueue.TryEnqueue(async () =>
                {
                    await ShowBrowserClosedMessage();
                });
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // Network issue with WebDriver communication
                Console.WriteLine($"ANTICHEAT: WebDriver communication error: {ex.Message}");
                _urlMonitoringTimer?.Stop();
                await CleanupBrowserResources();
                
                this.DispatcherQueue.TryEnqueue(async () =>
                {
                    await ShowBrowserClosedMessage();
                });
            }
            catch (NullReferenceException ex)
            {
                // WebDriver was disposed
                Console.WriteLine($"ANTICHEAT: WebDriver was disposed: {ex.Message}");
                _urlMonitoringTimer?.Stop();
                await CleanupBrowserResources();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ANTICHEAT: URL monitoring error: {ex.Message}");
                _urlMonitoringTimer?.Stop();
                await CleanupBrowserResources();
            }
        }

        private async Task CleanupBrowserResources()
        {
            try
            {
                _urlMonitoringTimer?.Stop();
                _urlMonitoringTimer?.Dispose();
                _urlMonitoringTimer = null;

                if (_webDriver != null)
                {
                    try
                    {
                        _webDriver.Quit();
                    }
                    catch { }
                    
                    try
                    {
                        _webDriver.Dispose();
                    }
                    catch { }
                    
                    _webDriver = null;
                }
                
                Console.WriteLine("ANTICHEAT: Browser resources cleaned up");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ANTICHEAT: Error during cleanup: {ex.Message}");
            }
            
            await Task.CompletedTask;
        }

        private async Task ShowBrowserClosedMessage()
        {
            if (_isDialogShowing) return;
            
            try
            {
                _isDialogShowing = true;
                
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "⚠️ Browser Closed",
                    Content = "The test browser was closed.\n\n" +
                             "If this was unintentional, you'll need to restart the test with a new test code.\n\n" +
                             "If you encountered a violation, please contact your instructor.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ANTICHEAT: Error showing browser closed message: {ex.Message}");
            }
            finally
            {
                _isDialogShowing = false;
            }
        }

        private async Task HandleCheatDetected(string violationType, string details = "")
        {
            _violationCount++;
            Console.WriteLine($"ANTICHEAT: CHEATING DETECTED - Type: {violationType}, Details: {details}, Count: {_violationCount}");
            
            try
            {
                if (_webDriver != null)
                {
                    try
                    {
                        ((IJavaScriptExecutor)_webDriver).ExecuteScript($@"
                            alert('🚨 CHEATING DETECTED - TEST TERMINATED!\\n\\nViolation Type: {violationType}\\n{(string.IsNullOrEmpty(details) ? "" : $"Details: {details}\\n")}\\nThe test session will now end.');
                        ");
                    }
                    catch
                    {
                        // Browser might already be closed, ignore
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ANTICHEAT: Error showing cheat alert: {ex.Message}");
            }

            // Cleanup browser resources
            await CleanupBrowserResources();

            this.DispatcherQueue.TryEnqueue(async () =>
            {
                await ShowCheatDetectedDialog(violationType, details);
                Application.Current.Exit();
            });
        }

        private async Task ShowCheatDetectedDialog(string violationType, string details)
        {
            // Prevent multiple dialogs
            if (_isDialogShowing) return;
            
            try
            {
                _isDialogShowing = true;
                
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "🚨 CHEATING DETECTED - TEST TERMINATED",
                    Content = $"Violation: {violationType}\n{(string.IsNullOrEmpty(details) ? "" : $"Details: {details}\n")}\nViolation Count: {_violationCount}\n\nThe test session has been terminated due to cheating.\nThis incident has been logged.\n\nThe application will now close.",
                    CloseButtonText = "Exit",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            finally
            {
                _isDialogShowing = false;
            }
        }

        private async Task EnsureSecurityScriptsActive()
        {
            if (_webDriver == null) return;

            try
            {
                var indicatorExistsObj = ((IJavaScriptExecutor)_webDriver).ExecuteScript(
                    "return document.getElementById('anticheat-indicator') !== null;"
                );
                var functionExistsObj = ((IJavaScriptExecutor)_webDriver).ExecuteScript(
                    "return typeof window.anticheatViolationDetected === 'function';"
                );

                bool indicatorExists = indicatorExistsObj is bool b1 && b1;
                bool functionExists = functionExistsObj is bool b2 && b2;

                if (!indicatorExists || !functionExists)
                {
                    Console.WriteLine("ANTICHEAT: Page change detected - Re-injecting security scripts");
                    
                    var wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10));
                    wait.Until(d =>
                    {
                        if (d is IJavaScriptExecutor jsExecutor)
                        {
                            return jsExecutor.ExecuteScript("return document.readyState")?.Equals("complete") == true;
                        }
                        return false;
                    });

                    await InjectSecurityScriptOnly(_webDriver);
                }
            }
            catch (OpenQA.Selenium.WebDriverException ex)
            {
                Console.WriteLine($"ANTICHEAT: Browser disconnected during security check: {ex.Message}");
                // Don't try to inject, browser is likely closed
                throw; // Let the caller handle it
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                Console.WriteLine($"ANTICHEAT: Network error during security check: {ex.Message}");
                throw; // Let the caller handle it
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ANTICHEAT: Security check error: {ex.Message}");
                try
                {
                    if (_webDriver != null)
                    {
                        await InjectSecurityScriptOnly(_webDriver);
                    }
                }
                catch (Exception injectionEx)
                {
                    Console.WriteLine($"ANTICHEAT: Failed to re-inject scripts: {injectionEx.Message}");
                    await HandleCheatDetected("Security Script Failure", "Could not maintain security on page change");
                }
            }
        }

        private async Task InjectSecurityScriptOnly(IWebDriver driver)
        {
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript(@"
                    console.log('ANTICHEAT: Re-injecting security scripts...');
                    
                    const existingIndicator = document.getElementById('anticheat-indicator');
                    if (existingIndicator) {
                        existingIndicator.remove();
                    }
                    
                    window.anticheatViolationDetected = function(violationType, details) {
                        console.log('ANTICHEAT: VIOLATION DETECTED - TEST WILL TERMINATE:', violationType, details);
                        document.body.setAttribute('data-cheat-detected', violationType + '|' + (details || ''));
                    };
                    
                    window.isAllowedUrl = function(url) {
                        if (!url) return false;
                        
                        try {
                            const allowedDomains = [
                                'hackerrank.com',
                                'www.hackerrank.com', 
                                'hr-challenge-images.s3.amazonaws.com',
                                'hrcdn.net'
                            ];
                            
                            const urlObj = new URL(url);
                            const domain = urlObj.hostname.toLowerCase();
                            
                            return allowedDomains.some(allowedDomain => 
                                domain === allowedDomain || domain.endsWith('.' + allowedDomain)
                            );
                        } catch (e) {
                            console.log('ANTICHEAT: URL validation error:', e);
                            return false;
                        }
                    };
                    
                    (function() {
                        console.log('ANTICHEAT: Activating security measures...');
                        
                        // All the security JavaScript from the original implementation
                        const indicator = document.createElement('div');
                        indicator.id = 'anticheat-indicator';
                        indicator.innerHTML = '🛡️ SECURE MODE';
                        
                        indicator.style.cssText = `
                            position: fixed;
                            top: 20px;
                            right: 20px;
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: white;
                            padding: 8px 16px;
                            border-radius: 20px;
                            font-size: 12px;
                            z-index: 999999;
                            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                            font-weight: 600;
                            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
                            border: 1px solid rgba(255, 255, 255, 0.2);
                            backdrop-filter: blur(10px);
                            user-select: none;
                            pointer-events: auto;
                            cursor: default;
                            animation: slideIn 0.5s ease-out;
                            letter-spacing: 0.3px;
                            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                            opacity: 1;
                            visibility: visible;
                        `;
                        
                        let hideTimeout;
                        
                        indicator.addEventListener('mouseenter', function() {
                            this.style.opacity = '0';
                            this.style.visibility = 'hidden';
                            this.style.transform = 'scale(0.8) translateY(-10px)';
                        });
                        
                        indicator.addEventListener('mouseleave', function() {
                            clearTimeout(hideTimeout);
                            hideTimeout = setTimeout(() => {
                                this.style.opacity = '1';
                                this.style.visibility = 'visible';
                                this.style.transform = 'scale(1) translateY(0)';
                            }, 500);
                        });
                        
                        if (document.body) {
                            document.body.appendChild(indicator);
                        }
                        
                        // Add security monitoring...
                        document.addEventListener('keydown', function(e) {
                            if ((e.ctrlKey && (e.key === 'c' || e.key === 'v' || e.key === 'x' || e.key === 'a')) ||
                                (e.ctrlKey && e.shiftKey && (e.key === 'I' || e.key === 'J')) ||
                                e.key === 'F12' ||
                                (e.ctrlKey && e.key === 'u') ||
                                (e.ctrlKey && e.shiftKey && e.key === 'C') ||
                                e.key === 'F5' ||
                                (e.ctrlKey && e.key === 'r') ||
                                (e.ctrlKey && e.key === 't') ||
                                (e.ctrlKey && e.key === 'n') ||
                                (e.ctrlKey && e.key === 'w')) {
                                
                                e.preventDefault();
                                e.stopPropagation();
                                return false;
                            }
                        }, true);
                        
                        document.addEventListener('visibilitychange', function() {
                            if (document.hidden) {
                                window.anticheatViolationDetected('Tab Switch', 'Window lost focus');
                            }
                        });
                        
                        window.addEventListener('blur', function() {
                            window.anticheatViolationDetected('Focus Loss', 'Window lost focus');
                        });
                        
                        document.addEventListener('contextmenu', function(e) {
                            window.anticheatViolationDetected('Right Click', 'Context menu attempt');
                            e.preventDefault();
                            e.stopPropagation();
                            return false;
                        }, true);
                        
                        console.log('ANTICHEAT: Security interface completed');
                    })();
                ");

                Console.WriteLine("ANTICHEAT: Enhanced security scripts injected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ANTICHEAT: Enhanced script injection failed: {ex.Message}");
                throw;
            }
        }

        private void StartJavaScriptViolationMonitoring()
        {
            var jsViolationTimer = new Timer(500);
            jsViolationTimer.Elapsed += async (sender, e) =>
            {
                if (_webDriver == null)
                {
                    jsViolationTimer?.Stop();
                    return;
                }

                try
                {
                    var violationData = ((IJavaScriptExecutor)_webDriver).ExecuteScript("return document.body.getAttribute('data-cheat-detected');");
                    
                    if (violationData != null && !string.IsNullOrEmpty(violationData.ToString()))
                    {
                        var parts = violationData.ToString().Split('|');
                        var violationType = parts[0];
                        var details = parts.Length > 1 ? parts[1] : "";
                        
                        jsViolationTimer.Stop();
                        await HandleCheatDetected($"JavaScript: {violationType}", details);
                    }
                }
                catch (OpenQA.Selenium.WebDriverException)
                {
                    // Browser was closed or connection lost
                    jsViolationTimer.Stop();
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    // Network issue with WebDriver communication
                    jsViolationTimer.Stop();
                }
                catch (NullReferenceException)
                {
                    // WebDriver was disposed
                    jsViolationTimer.Stop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ANTICHEAT: JS violation monitoring error: {ex.Message}");
                    jsViolationTimer.Stop();
                }
            };
            jsViolationTimer.AutoReset = true;
            jsViolationTimer.Start();
            
            Console.WriteLine("ANTICHEAT: JavaScript violation monitoring started");
        }

        private async Task ShowTestCodeMessage()
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "⚠️ Launching Zero-Tolerance Test Environment",
                Content = "🚨 ZERO-TOLERANCE MODE ACTIVATED:\n• ANY cheating attempt = IMMEDIATE TERMINATION\n• No warnings, no second chances\n• Tab switching = INSTANT EXIT\n• Copy/paste = INSTANT EXIT\n• Developer tools = INSTANT EXIT\n• External navigation = INSTANT EXIT\n• Right-click = INSTANT EXIT\n\n⚠️ WARNING: This is your ONLY chance to complete the test!\n\nPress OK to launch the zero-tolerance browser.",
                CloseButtonText = "I Understand - Launch Test",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async Task ShowIncompleteCodeMessage()
        {
            // Prevent multiple dialogs
            if (_isDialogShowing) return;
            
            try
            {
                _isDialogShowing = true;
                
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Incomplete Code",
                    Content = "Please enter all 4 digits of the test code.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
                
                // Focus on the first empty digit
                if (string.IsNullOrEmpty(Digit1?.Text))
                    Digit1?.Focus(FocusState.Programmatic);
                else if (string.IsNullOrEmpty(Digit2?.Text))
                    Digit2?.Focus(FocusState.Programmatic);
                else if (string.IsNullOrEmpty(Digit3?.Text))
                    Digit3?.Focus(FocusState.Programmatic);
                else if (string.IsNullOrEmpty(Digit4?.Text))
                    Digit4?.Focus(FocusState.Programmatic);
            }
            finally
            {
                _isDialogShowing = false;
            }
        }

        private async Task ShowErrorMessage(string title, string message)
        {
            // Prevent multiple dialogs
            if (_isDialogShowing) return;
            
            try
            {
                _isDialogShowing = true;
                
                ContentDialog dialog = new ContentDialog()
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            finally
            {
                _isDialogShowing = false;
            }
        }
    }
}