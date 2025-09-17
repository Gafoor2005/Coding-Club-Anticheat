using Coding_Club_Anticheat.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.ApplicationModel.DataTransfer;

namespace Coding_Club_Anticheat
{
    public sealed partial class CreateTestPage : Page
    {
        private readonly FirebaseService _firebaseService;
        private readonly ConfigurationService _configurationService;
        private readonly ObservableCollection<TestCodeViewModel> _testCodes;
        private string _fetchedTitle = "";
        private string _projectId = "";
        private CancellationTokenSource? _fetchCancellationTokenSource;
        private System.Timers.Timer? _debounceTimer;
        private UserInfo? _currentUser;

        public CreateTestPage()
        {
            this.InitializeComponent();
            _firebaseService = new FirebaseService();
            _configurationService = new ConfigurationService();
            _testCodes = new ObservableCollection<TestCodeViewModel>();
            TestCodesListView.ItemsSource = _testCodes;

            // Load configuration settings
            LoadConfigurationSettings();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Get user information passed from navigation
            if (e.Parameter is UserInfo userInfo)
            {
                _currentUser = userInfo;
                UpdateUserInterface();
            }
            
            await LoadTestCodesAsync();
        }

        private void UpdateUserInterface()
        {
            if (_currentUser != null)
            {
                // Update page title to show it's user-specific
                PageTitle.Text = $"My Test Codes - {_currentUser.Name}";
                WelcomeMessage.Text = $"Welcome, {_currentUser.Name}! Create and manage your test codes below.";
                WelcomeMessage.Visibility = Visibility.Visible;
            }
        }

        private void LoadConfigurationSettings()
        {
            var settings = _configurationService.Settings;
            
            // Set project ID from configuration
            if (!string.IsNullOrEmpty(settings.TestCodeSettings.DefaultProjectId))
            {
                _projectId = settings.TestCodeSettings.DefaultProjectId;
            }
            else if (!string.IsNullOrEmpty(settings.Firebase.ProjectId))
            {
                _projectId = settings.Firebase.ProjectId;
            }
            else
            {
                _projectId = "coding-club-anticheat"; // Fallback
            }
            
            System.Diagnostics.Debug.WriteLine($"Using Project ID from configuration: {_projectId}");
        }

        private void TestUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
            
            string url = TestUrlTextBox.Text.Trim();
            
            // Cancel any previous debounce timer
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            
            if (IsValidUrl(url))
            {
                // Show title fetch panel immediately
                TitleFetchPanel.Visibility = Visibility.Visible;
                
                // Use debounce timer to delay the actual fetch
                _debounceTimer = new System.Timers.Timer(800); // 800ms delay
                _debounceTimer.Elapsed += (s, args) =>
                {
                    _debounceTimer?.Stop();
                    
                    // Dispatch back to UI thread for the actual fetch
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        // Re-check URL validity in case it changed during debounce
                        if (IsValidUrl(TestUrlTextBox.Text.Trim()) && TestUrlTextBox.Text.Trim() == url)
                        {
                            // Cancel any previous fetch operation
                            _fetchCancellationTokenSource?.Cancel();
                            _fetchCancellationTokenSource = new CancellationTokenSource();
                            
                            _ = FetchPageTitleAsync(url, _fetchCancellationTokenSource.Token);
                        }
                    });
                };
                _debounceTimer.Start();
            }
            else
            {
                // Cancel any ongoing operations and hide title fetch panel for invalid URLs
                _fetchCancellationTokenSource?.Cancel();
                TitleFetchPanel.Visibility = Visibility.Collapsed;
                _fetchedTitle = "";
            }
        }

        private void TestTitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void ValidateInputs()
        {
            string url = TestUrlTextBox.Text.Trim();
            string manualTitle = TestTitleTextBox.Text.Trim();

            bool isValidUrl = IsValidUrl(url);
            bool hasTitle = !string.IsNullOrEmpty(manualTitle) || !string.IsNullOrEmpty(_fetchedTitle);

            // Show URL validation message
            if (!string.IsNullOrEmpty(url) && !isValidUrl)
            {
                UrlValidationText.Text = "Please enter a valid URL (must start with http:// or https://)";
                UrlValidationText.Visibility = Visibility.Visible;
            }
            else
            {
                UrlValidationText.Visibility = Visibility.Collapsed;
            }

            // Enable create button if URL is valid and we have either manual title or fetched title
            CreateCodeButton.IsEnabled = isValidUrl && hasTitle;
        }

        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private async void CreateCodeButton_Click(object sender, RoutedEventArgs e)
        {
            await CreateTestCodeAsync();
        }

        private async Task CreateTestCodeAsync()
        {
            try
            {
                // Cancel any ongoing title fetch and cleanup timers
                _fetchCancellationTokenSource?.Cancel();
                _debounceTimer?.Stop();
                _debounceTimer?.Dispose();
                
                // Show loading
                ShowLoading(true);
                HideSuccess();

                string testUrl = TestUrlTextBox.Text.Trim();
                string manualTitle = TestTitleTextBox.Text.Trim();
                
                // Use manual title if provided, otherwise use fetched title
                string testTitle = !string.IsNullOrEmpty(manualTitle) ? manualTitle : _fetchedTitle;
                
                // Debug output to show which title source is being used
                if (!string.IsNullOrEmpty(manualTitle))
                {
                    System.Diagnostics.Debug.WriteLine($"Using manual title: '{testTitle}'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Using fetched title: '{testTitle}'");
                }

                // Get service account key path from configuration
                var settings = _configurationService.Settings;
                string serviceAccountKeyPath = settings.Firebase.ServiceAccountKeyPath;

                bool initialized;
                if (!string.IsNullOrEmpty(serviceAccountKeyPath))
                {
                    // Use direct service account key path
                    System.Diagnostics.Debug.WriteLine($"Initializing Firebase with direct service account key: {serviceAccountKeyPath}");
                    initialized = await _firebaseService.InitializeAsync(_projectId, serviceAccountKeyPath);
                }
                else
                {
                    // Fallback to environment variable method
                    System.Diagnostics.Debug.WriteLine("No service account key path in config, using environment variable method");
                    initialized = await _firebaseService.InitializeAsync(_projectId);
                }

                if (!initialized)
                {
                    string errorMessage = "Failed to initialize Firebase. ";
                    if (string.IsNullOrEmpty(serviceAccountKeyPath))
                    {
                        errorMessage += "Please check the Firebase configuration in appsettings.json. " +
                                      "Consider setting the ServiceAccountKeyPath or " +
                                      "the GOOGLE_APPLICATION_CREDENTIALS environment variable.";
                    }
                    else
                    {
                        errorMessage += $"Please verify the service account key file exists at: {serviceAccountKeyPath}";
                    }
                    
                    await ShowErrorDialogAsync("Firebase Error", errorMessage);
                    return;
                }

                // Create test code with the chosen title and current user as owner
                string generatedCode = await _firebaseService.CreateTestCodeAsync(testUrl, testTitle, _currentUser?.Id);

                // Show success
                GeneratedCodeText.Text = generatedCode;
                ShowSuccess(true);

                // Clear inputs and cancel any ongoing operations
                TestUrlTextBox.Text = "";
                TestTitleTextBox.Text = "";
                _fetchedTitle = "";
                _fetchCancellationTokenSource?.Cancel();
                _debounceTimer?.Stop();
                _debounceTimer?.Dispose();
                TitleFetchPanel.Visibility = Visibility.Collapsed;

                // Refresh the list
                await LoadTestCodesAsync(); // This will handle the loading state properly

            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Error", $"Failed to create test code: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
                ValidateInputs(); // Re-validate to update button state
            }
        }

        private async void CopyCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(GeneratedCodeText.Text);
                Clipboard.SetContent(dataPackage);

                // Show brief feedback with proper content
                var originalContent = CopyCodeButton.Content;
                CopyCodeButton.Content = new StackPanel 
                { 
                    Orientation = Orientation.Horizontal, 
                    Spacing = 6,
                    Children = 
                    {
                        new FontIcon { Glyph = "\uE73E", FontSize = 12 }, // Checkmark icon
                        new TextBlock { Text = "Copied!", FontSize = 12, VerticalAlignment = VerticalAlignment.Center }
                    }
                };
                
                await Task.Delay(2000);
                CopyCodeButton.Content = originalContent;
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Copy Error", $"Failed to copy to clipboard: {ex.Message}");
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadTestCodesAsync();
        }

        private async Task LoadTestCodesAsync()
        {
            try
            {
                // Show loading state for test codes
                ShowTestCodesLoading(true);

                if (string.IsNullOrEmpty(_projectId))
                {
                    _testCodes.Clear();
                    UpdateEmptyState();
                    return;
                }

                // Get service account key path from configuration
                var settings = _configurationService.Settings;
                string serviceAccountKeyPath = settings.Firebase.ServiceAccountKeyPath;

                // Initialize Firebase if needed
                if (!string.IsNullOrEmpty(serviceAccountKeyPath))
                {
                    await _firebaseService.InitializeAsync(_projectId, serviceAccountKeyPath);
                }
                else
                {
                    await _firebaseService.InitializeAsync(_projectId);
                }

                // Get test codes from Firebase - only for current user
                var testCodes = await _firebaseService.GetAllTestCodesAsync(_currentUser?.Id);

                // Update UI
                _testCodes.Clear();
                foreach (var testCode in testCodes)
                {
                    _testCodes.Add(new TestCodeViewModel(testCode));
                }

                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load test codes: {ex.Message}");
                // Optionally show an error message to the user
                await ShowErrorDialogAsync("Load Error", $"Failed to load test codes: {ex.Message}");
            }
            finally
            {
                // Hide loading state for test codes
                ShowTestCodesLoading(false);
            }
        }

        private void ShowTestCodesLoading(bool show)
        {
            TestCodesLoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            TestCodesScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            
            // Disable refresh and delete all buttons while loading
            RefreshButton.IsEnabled = !show;
            DeleteAllButton.IsEnabled = !show;
        }

        private void UpdateEmptyState()
        {
            EmptyStatePanel.Visibility = _testCodes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            TestCodesScrollViewer.Visibility = _testCodes.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void ToggleStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string testCode)
            {
                try
                {
                    // Show loading briefly
                    ShowTestCodesLoading(true);
                    
                    await _firebaseService.ToggleTestCodeStatusAsync(testCode);
                    await LoadTestCodesAsync(); // This will handle the loading state
                }
                catch (Exception ex)
                {
                    ShowTestCodesLoading(false);
                    await ShowErrorDialogAsync("Error", $"Failed to toggle status: {ex.Message}");
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string testCode)
            {
                try
                {
                    // Show confirmation dialog
                    ContentDialog deleteDialog = new ContentDialog()
                    {
                        Title = "Delete Test Code",
                        Content = $"Are you sure you want to delete test code '{testCode}'? This action cannot be undone.",
                        PrimaryButtonText = "Delete",
                        CloseButtonText = "Cancel",
                        XamlRoot = this.XamlRoot
                    };

                    var result = await deleteDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        // Show loading briefly
                        ShowTestCodesLoading(true);
                        
                        await _firebaseService.DeleteTestCodeAsync(testCode);
                        await LoadTestCodesAsync(); // This will handle the loading state
                    }
                }
                catch (Exception ex)
                {
                    ShowTestCodesLoading(false);
                    await ShowErrorDialogAsync("Error", $"Failed to delete test code: {ex.Message}");
                }
            }
        }

        private async void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show confirmation dialog with stronger warning
                ContentDialog deleteAllDialog = new ContentDialog()
                {
                    Title = "⚠️ Delete All My Test Codes",
                    Content = $"Are you absolutely sure you want to delete ALL {_testCodes.Count} of your test codes?\n\n" +
                             "🚨 WARNING: This action will permanently delete:\n" +
                             $"• All {_testCodes.Count} of your test codes\n" +
                             "• All associated data and usage statistics\n" +
                             "• This action CANNOT be undone!\n\n" +
                             "Students will no longer be able to use any of your existing test codes.\n\n" +
                             "Type 'DELETE ALL' in the text box below to confirm:",
                    PrimaryButtonText = "Delete All My Codes",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                // Add a text input for confirmation
                var confirmationTextBox = new TextBox()
                {
                    PlaceholderText = "Type 'DELETE ALL' to confirm",
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var contentPanel = new StackPanel();
                contentPanel.Children.Add(new TextBlock() 
                { 
                    Text = $"Are you absolutely sure you want to delete ALL {_testCodes.Count} of your test codes?\n\n" +
                           "🚨 WARNING: This action will permanently delete:\n" +
                           $"• All {_testCodes.Count} of your test codes\n" +
                           "• All associated data and usage statistics\n" +
                           "• This action CANNOT be undone!\n\n" +
                           "Students will no longer be able to use any of your existing test codes.\n\n" +
                           "Type 'DELETE ALL' in the text box below to confirm:",
                    TextWrapping = TextWrapping.Wrap 
                });
                contentPanel.Children.Add(confirmationTextBox);
                
                deleteAllDialog.Content = contentPanel;

                var result = await deleteAllDialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    // Check confirmation text
                    if (confirmationTextBox.Text.Trim().ToUpper() != "DELETE ALL")
                    {
                        await ShowErrorDialogAsync("Confirmation Required", 
                            "You must type 'DELETE ALL' exactly to confirm this destructive action.");
                        return;
                    }

                    // Show loading state
                    ShowLoading(true);
                    
                    try
                    {
                        // Perform the bulk delete - only for current user
                        var (success, deletedCount) = await _firebaseService.DeleteAllTestCodesAsync(_currentUser?.Id);
                        
                        if (success)
                        {
                            // Show success message
                            await ShowSuccessDialogAsync("Bulk Delete Completed", 
                                $"Successfully deleted {deletedCount} of your test codes.\n\n" +
                                "All your test codes have been permanently removed from the database.");
                            
                            // Refresh the list to show empty state
                            await LoadTestCodesAsync(); // This will handle the loading state properly
                        }
                        else
                        {
                            await ShowErrorDialogAsync("Delete Failed", 
                                "Failed to delete all test codes. Please try again or check your connection.");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        await ShowErrorDialogAsync("Delete Error", $"Failed to delete all test codes: {innerEx.Message}");
                    }
                    finally
                    {
                        ShowLoading(false); // Hide the main loading panel
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Error", $"Failed to delete all test codes: {ex.Message}");
                ShowLoading(false);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back using NavigationView - no longer needed
            // The NavigationView handles navigation automatically
        }

        private void ShowLoading(bool show)
        {
            LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            
            if (!show)
            {
                // Re-enable create button based on current validation state
                ValidateInputs();
            }
            else
            {
                // Disable create button while loading
                CreateCodeButton.IsEnabled = false;
            }
        }

        private void ShowSuccess(bool show)
        {
            SuccessPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HideSuccess()
        {
            ShowSuccess(false);
        }

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            ContentDialog errorDialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await errorDialog.ShowAsync();
        }

        private async Task ShowSuccessDialogAsync(string title, string message)
        {
            ContentDialog successDialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await successDialog.ShowAsync();
        }

        private async Task FetchPageTitleAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if operation was cancelled before starting
                cancellationToken.ThrowIfCancellationRequested();
                
                // Show loading state - but only if not cancelled
                if (!cancellationToken.IsCancellationRequested)
                {
                    TitleLoadingPanel.Visibility = Visibility.Visible;
                    FetchedTitleText.Visibility = Visibility.Collapsed;
                    TitleErrorText.Visibility = Visibility.Collapsed;
                }

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                // Set a user agent to avoid blocking
                httpClient.DefaultRequestHeaders.Add("User-Agent", 
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                // Check cancellation before making HTTP request
                cancellationToken.ThrowIfCancellationRequested();
                
                string html = await httpClient.GetStringAsync(url, cancellationToken);
                
                // Check cancellation before processing HTML
                cancellationToken.ThrowIfCancellationRequested();
                
                // Extract title using regex
                string title = ExtractTitleFromHtml(html);
                
                // Check cancellation before updating UI
                cancellationToken.ThrowIfCancellationRequested();
                
                if (!string.IsNullOrEmpty(title))
                {
                    _fetchedTitle = title;
                    FetchedTitleText.Text = title;
                    FetchedTitleText.Visibility = Visibility.Visible;
                    TitleErrorText.Visibility = Visibility.Collapsed; // Ensure error is hidden
                }
                else
                {
                    _fetchedTitle = "Untitled Page";
                    FetchedTitleText.Text = "Untitled Page";
                    FetchedTitleText.Visibility = Visibility.Visible;
                    TitleErrorText.Visibility = Visibility.Collapsed; // Ensure error is hidden
                }

                TitleLoadingPanel.Visibility = Visibility.Collapsed;
                
                // Re-validate inputs after title is fetched (only if not cancelled)
                if (!cancellationToken.IsCancellationRequested)
                {
                    ValidateInputs();
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - this is expected behavior, don't show error
                System.Diagnostics.Debug.WriteLine($"Title fetch cancelled for URL: {url}");
                return;
            }
            catch (Exception ex)
            {
                // Only show error if operation wasn't cancelled
                if (!cancellationToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to fetch page title: {ex.Message}");
                    
                    // Show error state
                    TitleLoadingPanel.Visibility = Visibility.Collapsed;
                    FetchedTitleText.Visibility = Visibility.Collapsed;
                    TitleErrorText.Text = "Could not fetch page title. A generic title will be used.";
                    TitleErrorText.Visibility = Visibility.Visible;
                    
                    // Use URL as fallback title
                    try
                    {
                        var uri = new Uri(url);
                        _fetchedTitle = $"Test from {uri.Host}";
                    }
                    catch
                    {
                        _fetchedTitle = "Coding Test";
                    }
                    
                    // Re-validate inputs after fallback title is set
                    ValidateInputs();
                }
            }
        }

        private string ExtractTitleFromHtml(string html)
        {
            try
            {
                // Use regex to extract title from HTML
                var titleMatch = Regex.Match(html, @"<title[^>]*>([^<]*)</title>", RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                {
                    string title = titleMatch.Groups[1].Value.Trim();
                    
                    // Decode HTML entities
                    title = System.Net.WebUtility.HtmlDecode(title);
                    
                    // Clean up the title
                    title = title.Replace("\n", " ").Replace("\r", "").Replace("\t", " ");
                    title = Regex.Replace(title, @"\s+", " ").Trim();
                    
                    return title;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting title: {ex.Message}");
            }
            
            return "";
        }
    }

    public class TestCodeViewModel
    {
        public TestCodeViewModel(TestCodeInfo testCodeInfo)
        {
            TestCode = testCodeInfo.TestCode;
            TestUrl = testCodeInfo.TestUrl;
            TestTitle = string.IsNullOrEmpty(testCodeInfo.TestTitle) ? "Untitled Test" : testCodeInfo.TestTitle;
            CreatedAt = testCodeInfo.CreatedAt;
            IsActive = testCodeInfo.IsActive;
            UsageCount = testCodeInfo.UsageCount;
            OwnerId = testCodeInfo.OwnerId;
            CreatedBy = testCodeInfo.CreatedBy;
        }

        public string TestCode { get; set; }
        public string TestUrl { get; set; }
        public string TestTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
        public string OwnerId { get; set; }
        public string CreatedBy { get; set; }

        public string CreatedAtString => CreatedAt.ToString("MMM dd, yyyy HH:mm") + " IST";
        public string UsageString => $"Used {UsageCount} times";
        public string StatusText => IsActive ? "ACTIVE" : "DISABLED";
        public string StatusColor => IsActive ? "#4CAF50" : "#F44336";
        public string ToggleText => IsActive ? "Disable" : "Enable";
        public Visibility HasTitle => string.IsNullOrEmpty(TestTitle) || TestTitle == "Untitled Test" ? Visibility.Collapsed : Visibility.Visible;
    }
}