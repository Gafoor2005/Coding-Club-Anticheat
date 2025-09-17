using Firebase.Auth;
using System;
using System.Threading.Tasks;
using Coding_Club_Anticheat.Services;
using System.Text.Json;
using Windows.Storage;

namespace Coding_Club_Anticheat.Services
{
    public class AuthenticationService
    {
        private FirebaseAuthProvider? _authProvider;
        private FirebaseAuthLink? _currentAuthLink;
        private UserInfo? _currentUser;
        private readonly ConfigurationService _configurationService;
        
        private const string USER_STORAGE_KEY = "CodingClubAuth_CurrentUser";
        private const string TOKEN_STORAGE_KEY = "CodingClubAuth_AuthToken";

        public UserInfo? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public event EventHandler<UserInfo?>? AuthenticationStateChanged;

        public AuthenticationService()
        {
            _configurationService = new ConfigurationService();
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var settings = _configurationService.Settings;
                var apiKey = settings.Firebase.ApiKey;

                if (string.IsNullOrEmpty(apiKey))
                {
                    System.Diagnostics.Debug.WriteLine("Firebase API key not configured. Using demo mode.");
                    return true; // Allow demo mode
                }

                // Initialize Firebase Auth provider
                _authProvider = new FirebaseAuthProvider(new FirebaseConfig(apiKey));
                System.Diagnostics.Debug.WriteLine($"Firebase Authentication service initialized (Project: {settings.Firebase.ProjectId})");
                
                // Load user state from local storage
                _currentUser = await LoadUserStateAsync();
                if (_currentUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User state loaded: {_currentUser.Name}");
                    AuthenticationStateChanged?.Invoke(this, _currentUser);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No user state found, proceeding as guest.");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize Firebase Authentication: {ex.Message}");
                return true; // Still allow demo mode
            }
        }

        private async Task SaveUserStateAsync(UserInfo user, string? authToken = null)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                
                // Save user info
                var userJson = JsonSerializer.Serialize(user);
                localSettings.Values[USER_STORAGE_KEY] = userJson;
                
                // Save auth token if provided
                if (!string.IsNullOrEmpty(authToken))
                {
                    localSettings.Values[TOKEN_STORAGE_KEY] = authToken;
                }
                
                System.Diagnostics.Debug.WriteLine($"User state saved: {user.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save user state: {ex.Message}");
            }
        }

        private async Task<UserInfo?> LoadUserStateAsync()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                
                if (localSettings.Values.ContainsKey(USER_STORAGE_KEY))
                {
                    var userJson = localSettings.Values[USER_STORAGE_KEY] as string;
                    if (!string.IsNullOrEmpty(userJson))
                    {
                        var user = JsonSerializer.Deserialize<UserInfo>(userJson);
                        System.Diagnostics.Debug.WriteLine($"User state loaded: {user?.Name}");
                        return user;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load user state: {ex.Message}");
                return null;
            }
        }

        private async Task ClearUserStateAsync()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values.Remove(USER_STORAGE_KEY);
                localSettings.Values.Remove(TOKEN_STORAGE_KEY);
                System.Diagnostics.Debug.WriteLine("User state cleared");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear user state: {ex.Message}");
            }
        }

        public async Task<UserInfo?> SignInWithGoogleAsync()
        {
            try
            {
                var settings = _configurationService.Settings;
                var googleClientId = settings.Firebase.GoogleOAuthClientId;
                var googleClientSecret = settings.Firebase.GoogleOAuthClientSecret;

                if (string.IsNullOrEmpty(googleClientId) || googleClientId == "YOUR_GOOGLE_OAUTH_CLIENT_ID_HERE")
                {
                    System.Diagnostics.Debug.WriteLine("Google OAuth Client ID not configured. Using demo mode.");
                    return await CreateDemoUserAsync("google@demo.local", "Google User (Demo)");
                }

                // Use production Google OAuth with optional client secret
                var googleOAuth = new GoogleOAuthService(googleClientId, googleClientSecret);
                var googleUser = await googleOAuth.AuthenticateAsync();

                if (googleUser == null)
                {
                    System.Diagnostics.Debug.WriteLine("Google OAuth authentication failed");
                    return null;
                }

                // Create Firebase custom token or use the Google user info directly
                var user = new UserInfo
                {
                    Id = $"google_{googleUser.Id}",
                    Email = googleUser.Email,
                    Name = googleUser.Name,
                    ProfilePictureUrl = googleUser.Picture,
                    SignInTime = DateTime.UtcNow
                };

                _currentUser = user;
                
                // Save user state for persistence
                await SaveUserStateAsync(user);
                
                AuthenticationStateChanged?.Invoke(this, _currentUser);
                System.Diagnostics.Debug.WriteLine($"User signed in with Google: {_currentUser.Name} ({_currentUser.Email})");
                return _currentUser;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google sign-in failed: {ex.Message}");
                return null;
            }
        }

        public async Task<UserInfo?> SignInWithEmailAndPasswordAsync(string email, string password)
        {
            try
            {
                if (_authProvider == null)
                {
                    System.Diagnostics.Debug.WriteLine("Auth provider not initialized. Using demo mode.");
                    return await CreateDemoUserAsync(email);
                }

                // Real Firebase Auth
                _currentAuthLink = await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
                
                var user = new UserInfo
                {
                    Id = _currentAuthLink.User.LocalId,
                    Email = _currentAuthLink.User.Email,
                    Name = _currentAuthLink.User.DisplayName ?? ExtractNameFromEmail(_currentAuthLink.User.Email),
                    ProfilePictureUrl = _currentAuthLink.User.PhotoUrl ?? "",
                    SignInTime = DateTime.UtcNow
                };

                _currentUser = user;
                
                // Save user state for persistence
                await SaveUserStateAsync(user, _currentAuthLink.FirebaseToken);
                
                AuthenticationStateChanged?.Invoke(this, _currentUser);
                System.Diagnostics.Debug.WriteLine($"User signed in: {_currentUser.Name} ({_currentUser.Email})");
                return _currentUser;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign-in failed: {ex.Message}");
                // Fallback to demo mode for testing
                return await CreateDemoUserAsync(email);
            }
        }

        public async Task<UserInfo?> SignUpWithEmailAndPasswordAsync(string email, string password, string displayName = "")
        {
            try
            {
                if (_authProvider == null)
                {
                    System.Diagnostics.Debug.WriteLine("Auth provider not initialized. Using demo mode.");
                    return await CreateDemoUserAsync(email, displayName);
                }

                // Real Firebase Auth
                _currentAuthLink = await _authProvider.CreateUserWithEmailAndPasswordAsync(email, password, displayName);
                
                var user = new UserInfo
                {
                    Id = _currentAuthLink.User.LocalId,
                    Email = _currentAuthLink.User.Email,
                    Name = _currentAuthLink.User.DisplayName ?? displayName ?? ExtractNameFromEmail(_currentAuthLink.User.Email),
                    ProfilePictureUrl = _currentAuthLink.User.PhotoUrl ?? "",
                    SignInTime = DateTime.UtcNow
                };

                _currentUser = user;
                
                // Save user state for persistence
                await SaveUserStateAsync(user, _currentAuthLink.FirebaseToken);
                
                AuthenticationStateChanged?.Invoke(this, _currentUser);
                System.Diagnostics.Debug.WriteLine($"User signed up: {_currentUser.Name} ({_currentUser.Email})");
                return _currentUser;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign-up failed: {ex.Message}");
                // Fallback to demo mode for testing
                return await CreateDemoUserAsync(email, displayName);
            }
        }

        public async Task<UserInfo?> SignInAnonymouslyAsync()
        {
            try
            {
                if (_authProvider == null)
                {
                    System.Diagnostics.Debug.WriteLine("Auth provider not initialized. Using demo mode.");
                    return await CreateDemoUserAsync("anonymous@demo.local", "Anonymous User");
                }

                // Real Firebase Auth
                _currentAuthLink = await _authProvider.SignInAnonymouslyAsync();
                
                var user = new UserInfo
                {
                    Id = _currentAuthLink.User.LocalId,
                    Email = "anonymous@firebase.local",
                    Name = "Anonymous User",
                    ProfilePictureUrl = "",
                    SignInTime = DateTime.UtcNow
                };

                _currentUser = user;
                
                // Save user state for persistence
                await SaveUserStateAsync(user, _currentAuthLink.FirebaseToken);
                
                AuthenticationStateChanged?.Invoke(this, _currentUser);
                System.Diagnostics.Debug.WriteLine($"User signed in anonymously: {_currentUser.Id}");
                return _currentUser;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Anonymous sign-in failed: {ex.Message}");
                return await CreateDemoUserAsync("anonymous@demo.local", "Anonymous User");
            }
        }

        private async Task<UserInfo> CreateDemoUserAsync(string email, string name = "")
        {
            await Task.Delay(500); // Simulate network delay
            
            var demoUser = new UserInfo
            {
                Id = $"demo_{Guid.NewGuid().ToString("N")[..8]}",
                Email = email,
                Name = string.IsNullOrEmpty(name) ? ExtractNameFromEmail(email) : name,
                ProfilePictureUrl = "",
                SignInTime = DateTime.UtcNow
            };

            _currentUser = demoUser;
            
            // Save demo user state for persistence
            await SaveUserStateAsync(demoUser);
            
            AuthenticationStateChanged?.Invoke(this, _currentUser);
            
            System.Diagnostics.Debug.WriteLine($"Demo user created: {demoUser.Name} ({demoUser.Email})");
            return demoUser;
        }

        private string ExtractNameFromEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "User";
            
            var atIndex = email.IndexOf('@');
            if (atIndex > 0)
            {
                var localPart = email.Substring(0, atIndex);
                // Replace dots and underscores with spaces and capitalize
                return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                    localPart.Replace('.', ' ').Replace('_', ' '));
            }
            
            return "User";
        }

        public async Task SignOutAsync()
        {
            try
            {
                _currentUser = null;
                _currentAuthLink = null;
                
                // Clear saved user state
                await ClearUserStateAsync();
                
                AuthenticationStateChanged?.Invoke(this, null);
                
                System.Diagnostics.Debug.WriteLine("User signed out and state cleared");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign out failed: {ex.Message}");
            }
        }

        public async Task<UserInfo?> TryGetSilentTokenAsync()
        {
            try
            {
                // First, try to load saved user state
                if (_currentUser == null)
                {
                    var savedUser = await LoadUserStateAsync();
                    if (savedUser != null)
                    {
                        _currentUser = savedUser;
                        System.Diagnostics.Debug.WriteLine($"Restored user from storage: {savedUser.Name}");
                        return _currentUser;
                    }
                }
                
                // Check if current auth link is still valid
                if (_currentAuthLink != null && !_currentAuthLink.IsExpired())
                {
                    // Token is still valid
                    return _currentUser;
                }
                
                if (_currentAuthLink != null && _authProvider != null)
                {
                    // Try to refresh the token
                    _currentAuthLink = await _authProvider.RefreshAuthAsync(_currentAuthLink);
                    return _currentUser;
                }
                
                return _currentUser; // Return saved user even if Firebase token refresh fails
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Silent token refresh failed: {ex.Message}");
                // Try to return saved user as fallback
                return _currentUser;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            try
            {
                if (_authProvider == null)
                {
                    System.Diagnostics.Debug.WriteLine("Auth provider not initialized. Password reset not available in demo mode.");
                    return false;
                }

                await _authProvider.SendPasswordResetEmailAsync(email);
                System.Diagnostics.Debug.WriteLine($"Password reset email sent to: {email}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send password reset email: {ex.Message}");
                return false;
            }
        }

        public string GetAuthenticationStatus()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine($"Authentication Status:");
            status.AppendLine($"  Provider Initialized: {_authProvider != null}");
            status.AppendLine($"  User Authenticated: {IsAuthenticated}");
            status.AppendLine($"  Current User: {_currentUser?.Name ?? "None"}");
            status.AppendLine($"  User Email: {_currentUser?.Email ?? "None"}");
            status.AppendLine($"  User ID: {_currentUser?.Id ?? "None"}");
            status.AppendLine($"  Sign-in Time: {_currentUser?.SignInTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "None"}");
            
            if (_currentAuthLink != null)
            {
                status.AppendLine($"  Token Expired: {_currentAuthLink.IsExpired()}");
                status.AppendLine($"  Firebase Mode: Active");
            }
            else
            {
                status.AppendLine($"  Firebase Mode: Demo/Fallback");
            }
            
            return status.ToString();
        }
    }

    public class UserInfo
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string ProfilePictureUrl { get; set; } = "";
        public DateTime SignInTime { get; set; } = DateTime.UtcNow;
    }
}