using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Timers;
using System.Threading.Tasks;
using Coding_Club_Anticheat.Services;

namespace Coding_Club_Anticheat
{
    public sealed partial class MainWindow : Window
    {
        private Timer? _cleanupTimer;
        private readonly AuthenticationService _authenticationService;

        public MainWindow()
        {
            this.InitializeComponent();
            
            _authenticationService = new AuthenticationService();
            _authenticationService.AuthenticationStateChanged += OnAuthenticationStateChanged;
            
            // Initialize authentication
            _ = InitializeAuthenticationAsync();
            
            // Navigate to the home page by default
            ContentFrame.Navigate(typeof(HomePage));
            
            // Set up the title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }

        private async System.Threading.Tasks.Task InitializeAuthenticationAsync()
        {
            await _authenticationService.InitializeAsync();
            
            // Check if user was loaded during initialization
            if (_authenticationService.CurrentUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"User found during initialization: {_authenticationService.CurrentUser.Name}");
                UpdateAuthenticationUI(_authenticationService.CurrentUser);
            }
            else
            {
                // Try to get cached user as fallback
                var cachedUser = await _authenticationService.TryGetSilentTokenAsync();
                if (cachedUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User found via silent token: {cachedUser.Name}");
                    UpdateAuthenticationUI(cachedUser);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No authenticated user found - showing sign-in state");
                    UpdateAuthenticationUI(null);
                }
            }
        }

        private void OnAuthenticationStateChanged(object? sender, UserInfo? user)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateAuthenticationUI(user);
            });
        }

        private void UpdateAuthenticationUI(UserInfo? user)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateAuthenticationUI called with user: {user?.Name ?? "NULL"}");
            
            if (user != null)
            {
                System.Diagnostics.Debug.WriteLine($"User is signed in: {user.Name} - Showing Admin Panel");
                
                // User is signed in
                SignInNavItem.Visibility = Visibility.Collapsed;
                UserNavItem.Visibility = Visibility.Visible;
                SignOutNavItem.Visibility = Visibility.Visible;
                AuthSeparator.Visibility = Visibility.Visible;
                
                // Show and enable admin panel
                AdminPanelItem.Visibility = Visibility.Visible;
                AdminPanelItem.IsEnabled = true;
                
                
                UserNameText.Text = user.Name;
                UserInitials.Text = GetUserInitials(user.Name);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("User is NOT signed in - Hiding Admin Panel");
                
                // User is not signed in
                SignInNavItem.Visibility = Visibility.Visible;
                UserNavItem.Visibility = Visibility.Collapsed;
                SignOutNavItem.Visibility = Visibility.Collapsed;
                AuthSeparator.Visibility = Visibility.Collapsed;
                
                // Hide admin panel completely when not signed in
                AdminPanelItem.Visibility = Visibility.Collapsed;
                
                // If currently on admin panel, navigate to home
                if (ContentFrame.Content is CreateTestPage)
                {
                    System.Diagnostics.Debug.WriteLine("Currently on admin panel - navigating to home");
                    MainNavigationView.SelectedItem = MainNavigationView.MenuItems[0];
                    ContentFrame.Navigate(typeof(HomePage));
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Admin Panel Visibility set to: {AdminPanelItem.Visibility}");
        }

        private string GetUserInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "U";
            
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1)
            {
                return parts[0][0].ToString().ToUpper();
            }
            
            return "U";
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = args.SelectedItem as NavigationViewItem;
            if (selectedItem?.Tag is string tag)
            {
                Type? pageType = tag switch
                {
                    "HomePage" => typeof(HomePage),
                    "CreateTestPage" => typeof(CreateTestPage),
                    "AboutPage" => null, // We'll implement this later
                    _ => null
                };

                if (pageType != null)
                {
                    // No need to check authentication since Admin Panel is hidden when not authenticated
                    ContentFrame.Navigate(pageType, _authenticationService.CurrentUser);
                }
                else if (tag == "AboutPage")
                {
                    _ = ShowAuthenticationResultAsync("About", 
                        "Coding Club Anticheat v2.0\n\n" +
                        "Features:\n" +
                        "• Student Mode - Take tests with zero-tolerance monitoring\n" +
                        "• Admin Panel - Create and manage test codes (requires sign-in)\n" +
                        "• Firebase Authentication - Secure user-specific test management\n" +
                        "• Firebase Integration - Cloud-based test code storage\n\n" +
                        "Built with WinUI 3 and .NET 8");
                }
            }
        }

        private void SignInNavItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            _ = HandleDirectGoogleSignInAsync();
        }

        private void SignOutNavItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            _ = SignOutAsync();
        }

        private async Task HandleDirectGoogleSignInAsync()
        {
            try
            {
                // Show loading message
                var loadingDialog = new ContentDialog()
                {
                    Title = "🔐 Signing in with Google...",
                    Content = "Please complete the sign-in process in your browser.\n\nThe browser will open automatically.",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.Content.XamlRoot
                };

                // Start the sign-in process asynchronously
                var signInTask = _authenticationService.SignInWithGoogleAsync();
                
                // Show the loading dialog without waiting for it
                var dialogTask = loadingDialog.ShowAsync().AsTask();

                // Wait for either the sign-in to complete or the dialog to be cancelled
                var completedTask = await Task.WhenAny(signInTask, dialogTask);

                if (completedTask == signInTask)
                {
                    // Sign-in completed, close the loading dialog
                    loadingDialog.Hide();
                    
                    var user = await signInTask;
                    if (user != null)
                    {
                        await ShowAuthenticationResultAsync("Google Sign-In Successful!", 
                            $"Welcome, {user.Name}!\n\n" +
                            "You've successfully signed in with Google.\n" +
                            "You can now access the Admin Panel to create and manage your test codes.");
                    }
                    else
                    {
                        await ShowAuthenticationResultAsync("Google Sign-In Failed", 
                            "Unable to sign in with Google. Please try again.\n\n" +
                            "Make sure you complete the authorization in your browser.");
                    }
                }
                else
                {
                    // Dialog was cancelled, but sign-in might still be in progress
                    // We'll let it complete in the background
                    System.Diagnostics.Debug.WriteLine("Sign-in dialog cancelled by user");
                }
            }
            catch (Exception ex)
            {
                await ShowAuthenticationResultAsync("Google Sign-In Error", 
                    $"An error occurred during Google sign-in:\n\n{ex.Message}");
            }
        }

        private async Task SignOutAsync()
        {
            try
            {
                await _authenticationService.SignOutAsync();
                
                await ShowAuthenticationResultAsync("Signed Out", 
                    "You have been successfully signed out.\n\nYou can still use Student Mode to take tests.");
            }
            catch (Exception ex)
            {
                await ShowAuthenticationResultAsync("Sign Out Error", 
                    $"An error occurred during sign out: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task ShowAuthenticationResultAsync(string title, string message)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
