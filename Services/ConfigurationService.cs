using System;
using System.IO;
using System.Text.Json;

namespace Coding_Club_Anticheat.Services
{
    public class ConfigurationService
    {
        private readonly AppSettings _appSettings;

        public ConfigurationService()
        {
            _appSettings = LoadConfiguration();
        }

        public AppSettings Settings => _appSettings;

        private AppSettings LoadConfiguration()
        {
            try
            {
                // Try multiple locations for appsettings.json
                var configPaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                    "appsettings.json",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Coding Club Anticheat", "appsettings.json")
                };

                string configPath = "";
                foreach (var path in configPaths)
                {
                    System.Diagnostics.Debug.WriteLine($"Checking config path: {path}");
                    if (File.Exists(path))
                    {
                        configPath = path;
                        System.Diagnostics.Debug.WriteLine($"✓ Found config at: {configPath}");
                        break;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"=== CONFIGURATION SERVICE DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                System.Diagnostics.Debug.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                System.Diagnostics.Debug.WriteLine($"Selected config path: {configPath}");
                System.Diagnostics.Debug.WriteLine($"Config file exists: {File.Exists(configPath)}");
                
                if (!File.Exists(configPath))
                {
                    System.Diagnostics.Debug.WriteLine("Configuration file not found in any location, using default settings");
                    return new AppSettings();
                }

                string jsonContent = File.ReadAllText(configPath);
                System.Diagnostics.Debug.WriteLine($"Config file content length: {jsonContent.Length} characters");
                System.Diagnostics.Debug.WriteLine($"Config file content preview: {jsonContent.Substring(0, Math.Min(200, jsonContent.Length))}...");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var appSettings = JsonSerializer.Deserialize<AppSettings>(jsonContent, options);
                
                if (appSettings != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuration loaded successfully:");
                    System.Diagnostics.Debug.WriteLine($"  Firebase.ProjectId: {appSettings.Firebase.ProjectId ?? "NOT SET"}");
                    System.Diagnostics.Debug.WriteLine($"  Firebase.ServiceAccountKeyPath: {appSettings.Firebase.ServiceAccountKeyPath ?? "NOT SET"}");
                    System.Diagnostics.Debug.WriteLine($"  Firebase.ApiKey: {(string.IsNullOrEmpty(appSettings.Firebase.ApiKey) ? "NOT SET" : "SET")}");
                    System.Diagnostics.Debug.WriteLine($"  TestCodeSettings.DefaultProjectId: {appSettings.TestCodeSettings.DefaultProjectId ?? "NOT SET"}");
                    
                    // If the service account key path is relative, try to make it absolute
                    if (!string.IsNullOrEmpty(appSettings.Firebase.ServiceAccountKeyPath) && 
                        !Path.IsPathRooted(appSettings.Firebase.ServiceAccountKeyPath))
                    {
                        var keyPaths = new[]
                        {
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appSettings.Firebase.ServiceAccountKeyPath),
                            Path.Combine(Directory.GetCurrentDirectory(), appSettings.Firebase.ServiceAccountKeyPath),
                            Path.Combine(Path.GetDirectoryName(configPath) ?? "", appSettings.Firebase.ServiceAccountKeyPath)
                        };

                        foreach (var keyPath in keyPaths)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Checking key file: {keyPath} - Exists: {File.Exists(keyPath)}");
                            if (File.Exists(keyPath))
                            {
                                appSettings.Firebase.ServiceAccountKeyPath = keyPath;
                                System.Diagnostics.Debug.WriteLine($"  ✓ Updated key path to: {keyPath}");
                                break;
                            }
                        }
                    }
                }
                
                return appSettings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new AppSettings();
            }
        }
    }

    public class AppSettings
    {
        public FirebaseSettings Firebase { get; set; } = new FirebaseSettings();
        public TestCodeSettings TestCodeSettings { get; set; } = new TestCodeSettings();
    }

    public class FirebaseSettings
    {
        public string ProjectId { get; set; } = "";
        public string ServiceAccountKeyPath { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string GoogleOAuthClientId { get; set; } = "";
        public string GoogleOAuthClientSecret { get; set; } = "";
    }

    public class TestCodeSettings
    {
        public string DefaultProjectId { get; set; } = "";
    }
}