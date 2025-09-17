using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Coding_Club_Anticheat.Services
{
    public class FirebaseService
    {
        private FirestoreDb? _firestoreDb;
        private const string CollectionName = "test_codes";
        
        // Helper method to get IST TimeZone
        private static TimeZoneInfo GetISTTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback for systems where "India Standard Time" is not available
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
                }
                catch (TimeZoneNotFoundException)
                {
                    // Final fallback - create IST manually
                    return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "India Standard Time", "IST");
                }
            }
        }
        
        // Helper method to convert DateTime to IST
        private static DateTime ConvertToIST(DateTime dateTime)
        {
            TimeZoneInfo istTimeZone = GetISTTimeZone();
            
            // If the DateTime is already in local time and we're in IST, just return it
            if (dateTime.Kind == DateTimeKind.Unspecified || dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime;
            }
            
            // Convert from UTC to IST
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, istTimeZone);
        }
        
        // Helper method to get current IST time (for display purposes)
        private static DateTime GetCurrentISTTime()
        {
            TimeZoneInfo istTimeZone = GetISTTimeZone();
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);
        }
        
        // Helper method to convert UTC DateTime from Firestore to IST for display
        private static DateTime ConvertUTCToIST(DateTime utcDateTime)
        {
            TimeZoneInfo istTimeZone = GetISTTimeZone();
            // Ensure the DateTime is treated as UTC
            DateTime utcTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, istTimeZone);
        }

        /// <summary>
        /// Initialize Firebase using service account key file path directly
        /// </summary>
        /// <param name="projectId">Firebase project ID</param>
        /// <param name="serviceAccountKeyPath">Path to the service account JSON key file</param>
        /// <returns>True if initialization successful, false otherwise</returns>
        public async Task<bool> InitializeAsync(string projectId, string serviceAccountKeyPath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== FIREBASE INITIALIZATION START (Direct Path Method) ===");
                System.Diagnostics.Debug.WriteLine($"Project ID: {projectId}");
                System.Diagnostics.Debug.WriteLine($"Service Account Key Path: {serviceAccountKeyPath}");
                System.Diagnostics.Debug.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                
                // Resolve absolute path if relative path is provided
                string absolutePath = Path.IsPathRooted(serviceAccountKeyPath) 
                    ? serviceAccountKeyPath 
                    : Path.GetFullPath(serviceAccountKeyPath);
                
                System.Diagnostics.Debug.WriteLine($"Resolved Absolute Path: {absolutePath}");

                // Check if the service account key file exists
                if (!File.Exists(absolutePath))
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Service account key file not found at: {absolutePath}");
                    
                    // Try to find the file in common locations
                    var searchPaths = new[]
                    {
                        serviceAccountKeyPath,
                        Path.Combine(Directory.GetCurrentDirectory(), serviceAccountKeyPath),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, serviceAccountKeyPath),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Keys", "firebase-service-account.json")
                    };

                    System.Diagnostics.Debug.WriteLine("Searching for file in alternative paths:");
                    foreach (var searchPath in searchPaths)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Checking: {searchPath} - Exists: {File.Exists(searchPath)}");
                        if (File.Exists(searchPath))
                        {
                            absolutePath = searchPath;
                            System.Diagnostics.Debug.WriteLine($"  Found file at: {absolutePath}");
                            break;
                        }
                    }

                    if (!File.Exists(absolutePath))
                    {
                        return false;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Service account key file found at: {absolutePath}");

                // Test internet connectivity first
                bool hasInternetConnection = await TestInternetConnectivityAsync();
                if (!hasInternetConnection)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: No internet connection detected");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("Loading credentials from service account key file...");

                // Load credentials from the service account key file
                GoogleCredential credential;
                using (var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream);
                    System.Diagnostics.Debug.WriteLine("Credentials loaded successfully from stream");
                }

                // Ensure the credential has the required scopes
                if (credential.IsCreateScopedRequired)
                {
                    System.Diagnostics.Debug.WriteLine("Adding required scopes to credential...");
                    credential = credential.CreateScoped(new[] { "https://www.googleapis.com/auth/datastore" });
                }

                System.Diagnostics.Debug.WriteLine("Creating Firestore database connection...");

                // Create Firestore database connection with the specific credentials
                var firestoreDbBuilder = new FirestoreDbBuilder
                {
                    ProjectId = projectId,
                    Credential = credential
                };

                _firestoreDb = await firestoreDbBuilder.BuildAsync();
                
                System.Diagnostics.Debug.WriteLine("Firestore database connection created, testing connection...");
                
                // Test the connection by trying to access the database
                await TestFirebaseConnectionAsync();
                
                System.Diagnostics.Debug.WriteLine("=== FIREBASE INITIALIZATION SUCCESS (Direct Path Method) ===");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== FIREBASE INITIALIZATION FAILED (Direct Path Method) ===");
                System.Diagnostics.Debug.WriteLine($"Firebase initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Check for specific error types and provide helpful messages
                if (ex.Message.Contains("Invalid key file") || ex.Message.Contains("Invalid JSON"))
                {
                    System.Diagnostics.Debug.WriteLine("SOLUTION: Check that your service account key file is valid JSON and has the correct format");
                }
                else if (ex.Message.Contains("network") || ex.Message.Contains("connection"))
                {
                    System.Diagnostics.Debug.WriteLine("SOLUTION: Check your internet connection and firewall settings");
                }
                else if (ex.Message.Contains("permission") || ex.Message.Contains("access"))
                {
                    System.Diagnostics.Debug.WriteLine("SOLUTION: Check your Firebase project permissions and service account roles");
                }
                else if (ex.Message.Contains("ApplicationDefaultCredentials") || ex.Message.Contains("default credentials"))
                {
                    System.Diagnostics.Debug.WriteLine("SOLUTION: This suggests the direct credential loading failed and it's falling back to default credentials");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Initialize Firebase using environment variable (fallback method)
        /// </summary>
        /// <param name="projectId">Firebase project ID</param>
        /// <returns>True if initialization successful, false otherwise</returns>
        public async Task<bool> InitializeAsync(string projectId)
        {
            try
            {
                // Enhanced error checking and diagnostics
                System.Diagnostics.Debug.WriteLine($"Attempting to initialize Firebase with Project ID: {projectId}");
                
                // Check if credentials are set
                string? credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                System.Diagnostics.Debug.WriteLine($"GOOGLE_APPLICATION_CREDENTIALS: {credentialsPath ?? "NOT SET"}");
                
                if (!string.IsNullOrEmpty(credentialsPath))
                {
                    if (File.Exists(credentialsPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Credentials file found at: {credentialsPath}");
                        // Use the direct path method instead
                        return await InitializeAsync(projectId, credentialsPath);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR: Credentials file NOT found at: {credentialsPath}");
                        return false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("WARNING: GOOGLE_APPLICATION_CREDENTIALS environment variable not set");
                    System.Diagnostics.Debug.WriteLine("Attempting to use default credentials...");
                }

                // Test internet connectivity first
                bool hasInternetConnection = await TestInternetConnectivityAsync();
                if (!hasInternetConnection)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: No internet connection detected");
                    return false;
                }

                // Create Firestore database connection using default credentials
                _firestoreDb = FirestoreDb.Create(projectId);
                
                // Test the connection by trying to access the database
                await TestFirebaseConnectionAsync();
                
                System.Diagnostics.Debug.WriteLine("Firebase initialization successful using default credentials!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Check for specific error types and provide helpful messages
                if (ex.Message.Contains("ApplicationDefaultCredentials"))
                {
                    System.Diagnostics.Debug.WriteLine("SOLUTION: Set up your Firebase service account key file and environment variable, or use the InitializeAsync(projectId, keyPath) method");
                }
                else if (ex.Message.Contains("network") || ex.Message.Contains("connection"))
                {
                    System.Diagnostics.Debug.WriteLine("SOLUTION: Check your internet connection and firewall settings");
                }
                else if (ex.Message.Contains("permission") || ex.Message.Contains("access"))
                {
                    System.Diagnostics.Debug.WriteLine("SOLUTION: Check your Firebase project permissions and service account roles");
                }
                
                return false;
            }
        }

        private async Task<bool> TestInternetConnectivityAsync()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                // Test connection to Google's public DNS
                var response = await client.GetAsync("https://www.google.com");
                System.Diagnostics.Debug.WriteLine($"Internet connectivity test: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Internet connectivity test failed: {ex.Message}");
                return false;
            }
        }

        private async Task TestFirebaseConnectionAsync()
        {
            if (_firestoreDb == null)
                throw new InvalidOperationException("Firestore database not initialized");

            try
            {
                // Try to read from the collection (this will create it if it doesn't exist)
                var collectionRef = _firestoreDb.Collection(CollectionName);
                var query = collectionRef.Limit(1);
                var snapshot = await query.GetSnapshotAsync();
                
                System.Diagnostics.Debug.WriteLine($"Firebase connection test successful. Collection '{CollectionName}' accessible.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase connection test failed: {ex.Message}");
                throw;
            }
        }

        public async Task<string> CreateTestCodeAsync(string testUrl, string testTitle = "", string? ownerId = null)
        {
            if (_firestoreDb == null)
                throw new InvalidOperationException("Firebase not initialized");

            try
            {
                // Generate unique 4-digit code
                string testCode = await GenerateUniqueCodeAsync();

                // Get current time in IST for logging, but store as UTC in Firestore
                DateTime istNow = GetCurrentISTTime();
                DateTime utcNow = DateTime.UtcNow; // Firestore requires UTC

                // Create test code document
                var testCodeData = new Dictionary<string, object>
                {
                    {"test_url", testUrl},
                    {"test_title", testTitle},
                    {"created_at", utcNow}, // Store in UTC for Firestore compatibility
                    {"is_active", true},
                    {"usage_count", 0}
                };

                // Add owner information if provided
                if (!string.IsNullOrEmpty(ownerId))
                {
                    testCodeData.Add("owner_id", ownerId);
                    testCodeData.Add("created_by", ownerId);
                }

                // Store in Firestore
                await _firestoreDb.Collection(CollectionName).Document(testCode).SetAsync(testCodeData);
                System.Diagnostics.Debug.WriteLine($"Test code '{testCode}' created successfully at {istNow:yyyy-MM-dd HH:mm:ss} IST by user {ownerId ?? "anonymous"}");

                return testCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create test code: {ex.Message}");
                throw;
            }
        }

        public async Task<TestCodeInfo?> GetTestCodeInfoAsync(string testCode)
        {
            if (_firestoreDb == null)
                throw new InvalidOperationException("Firebase not initialized");

            try
            {
                var docRef = _firestoreDb.Collection(CollectionName).Document(testCode);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    System.Diagnostics.Debug.WriteLine($"Test code '{testCode}' not found in database");
                    return null;
                }

                var data = snapshot.ToDictionary();
                
                // Handle created_at field with robust type checking and IST conversion
                DateTime createdAt = DateTime.MinValue;
                if (data.ContainsKey("created_at"))
                {
                    var createdAtValue = data["created_at"];
                    if (createdAtValue is Timestamp timestamp)
                    {
                        // Firestore Timestamp is in UTC, convert to IST for display
                        createdAt = ConvertUTCToIST(timestamp.ToDateTime());
                    }
                    else if (createdAtValue is DateTime dateTime)
                    {
                        // If it's already a DateTime, check if it's UTC and convert to IST
                        createdAt = dateTime.Kind == DateTimeKind.Utc ? ConvertUTCToIST(dateTime) : dateTime;
                    }
                    else if (createdAtValue is string dateString)
                    {
                        if (DateTime.TryParse(dateString, out DateTime parsedDate))
                        {
                            createdAt = parsedDate.Kind == DateTimeKind.Utc ? ConvertUTCToIST(parsedDate) : parsedDate;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not parse created_at string value: {dateString}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Unknown created_at field type: {createdAtValue?.GetType().Name ?? "null"}");
                    }
                }
                
                var testCodeInfo = new TestCodeInfo
                {
                    TestCode = testCode,
                    TestUrl = data["test_url"].ToString() ?? "",
                    TestTitle = data.ContainsKey("test_title") ? data["test_title"].ToString() ?? "" : "",
                    CreatedAt = createdAt,
                    IsActive = data.ContainsKey("is_active") && (bool)data["is_active"],
                    UsageCount = data.ContainsKey("usage_count") ? Convert.ToInt32(data["usage_count"]) : 0,
                    OwnerId = data.ContainsKey("owner_id") ? data["owner_id"].ToString() ?? "" : "",
                    CreatedBy = data.ContainsKey("created_by") ? data["created_by"].ToString() ?? "" : ""
                };

                System.Diagnostics.Debug.WriteLine($"Test code '{testCode}' retrieved: {testCodeInfo.TestTitle} (Active: {testCodeInfo.IsActive}, Created: {createdAt:yyyy-MM-dd HH:mm:ss} IST, Owner: {testCodeInfo.OwnerId})");
                return testCodeInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get test code info: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> IncrementUsageAsync(string testCode)
        {
            if (_firestoreDb == null)
                return false;

            try
            {
                var docRef = _firestoreDb.Collection(CollectionName).Document(testCode);
                await docRef.UpdateAsync("usage_count", FieldValue.Increment(1));
                System.Diagnostics.Debug.WriteLine($"Usage count incremented for test code '{testCode}'");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to increment usage: {ex.Message}");
                return false;
            }
        }

        public async Task<List<TestCodeInfo>> GetAllTestCodesAsync(string? userId = null)
        {
            if (_firestoreDb == null)
                return new List<TestCodeInfo>();

            try
            {
                Query query = _firestoreDb.Collection(CollectionName);
                
                // If userId is provided, filter by owner
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.WhereEqualTo("owner_id", userId);
                }
                
                query = query.OrderByDescending("created_at");
                var querySnapshot = await query.GetSnapshotAsync();

                var testCodes = new List<TestCodeInfo>();

                foreach (var document in querySnapshot.Documents)
                {
                    var data = document.ToDictionary();
                    
                    // Handle created_at field with robust type checking and IST conversion
                    DateTime createdAt = DateTime.MinValue;
                    if (data.ContainsKey("created_at"))
                    {
                        var createdAtValue = data["created_at"];
                        if (createdAtValue is Timestamp timestamp)
                        {
                            // Firestore Timestamp is in UTC, convert to IST for display
                            createdAt = ConvertUTCToIST(timestamp.ToDateTime());
                        }
                        else if (createdAtValue is DateTime dateTime)
                        {
                            // If it's already a DateTime, check if it's UTC and convert to IST
                            createdAt = dateTime.Kind == DateTimeKind.Utc ? ConvertUTCToIST(dateTime) : dateTime;
                        }
                        else if (createdAtValue is string dateString)
                        {
                            if (DateTime.TryParse(dateString, out DateTime parsedDate))
                            {
                                createdAt = parsedDate.Kind == DateTimeKind.Utc ? ConvertUTCToIST(parsedDate) : parsedDate;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not parse created_at string value: {dateString}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Unknown created_at field type: {createdAtValue?.GetType().Name ?? "null"}");
                        }
                    }
                    
                    testCodes.Add(new TestCodeInfo
                    {
                        TestCode = document.Id,
                        TestUrl = data["test_url"].ToString() ?? "",
                        TestTitle = data.ContainsKey("test_title") ? data["test_title"].ToString() ?? "" : "",
                        CreatedAt = createdAt,
                        IsActive = data.ContainsKey("is_active") && (bool)data["is_active"],
                        UsageCount = data.ContainsKey("usage_count") ? Convert.ToInt32(data["usage_count"]) : 0,
                        OwnerId = data.ContainsKey("owner_id") ? data["owner_id"].ToString() ?? "" : "",
                        CreatedBy = data.ContainsKey("created_by") ? data["created_by"].ToString() ?? "" : ""
                    });
                }

                System.Diagnostics.Debug.WriteLine($"Retrieved {testCodes.Count} test codes from database{(string.IsNullOrEmpty(userId) ? "" : $" for user {userId}")}");
                return testCodes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get test codes: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<TestCodeInfo>();
            }
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            if (_firestoreDb == null)
                throw new InvalidOperationException("Firebase not initialized");

            var random = new Random();
            string testCode;
            bool isUnique = false;

            do
            {
                // Generate 4-digit code
                testCode = random.Next(1000, 9999).ToString();

                // Check if code already exists
                var docRef = _firestoreDb.Collection(CollectionName).Document(testCode);
                var snapshot = await docRef.GetSnapshotAsync();
                isUnique = !snapshot.Exists;

            } while (!isUnique);

            System.Diagnostics.Debug.WriteLine($"Generated unique test code: {testCode} at {GetCurrentISTTime():yyyy-MM-dd HH:mm:ss} IST");
            return testCode;
        }

        public async Task<bool> DeleteTestCodeAsync(string testCode)
        {
            if (_firestoreDb == null)
                return false;

            try
            {
                await _firestoreDb.Collection(CollectionName).Document(testCode).DeleteAsync();
                System.Diagnostics.Debug.WriteLine($"Test code '{testCode}' deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete test code: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool success, int deletedCount)> DeleteAllTestCodesAsync(string? userId = null)
        {
            if (_firestoreDb == null)
                return (false, 0);

            try
            {
                System.Diagnostics.Debug.WriteLine($"Starting bulk delete of test codes{(string.IsNullOrEmpty(userId) ? "" : $" for user {userId}")}...");
                
                Query query = _firestoreDb.Collection(CollectionName);
                
                // If userId is provided, filter by owner
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.WhereEqualTo("owner_id", userId);
                }
                
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Documents.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No test codes found to delete");
                    return (true, 0);
                }

                int totalCodes = snapshot.Documents.Count;
                int deletedCount = 0;
                
                // Create a batch for efficient deletion
                WriteBatch batch = _firestoreDb.StartBatch();
                
                foreach (var document in snapshot.Documents)
                {
                    batch.Delete(document.Reference);
                    deletedCount++;
                }
                
                // Commit the batch delete
                await batch.CommitAsync();
                
                System.Diagnostics.Debug.WriteLine($"Successfully deleted {deletedCount} test codes{(string.IsNullOrEmpty(userId) ? "" : $" for user {userId}")} at {GetCurrentISTTime():yyyy-MM-dd HH:mm:ss} IST");
                return (true, deletedCount);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete test codes: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return (false, 0);
            }
        }

        public async Task<bool> ToggleTestCodeStatusAsync(string testCode)
        {
            if (_firestoreDb == null)
                return false;

            try
            {
                var docRef = _firestoreDb.Collection(CollectionName).Document(testCode);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    return false;

                var data = snapshot.ToDictionary();
                bool currentStatus = data.ContainsKey("is_active") && (bool)data["is_active"];
                bool newStatus = !currentStatus;

                await docRef.UpdateAsync("is_active", newStatus);
                System.Diagnostics.Debug.WriteLine($"Test code '{testCode}' status changed to: {(newStatus ? "ACTIVE" : "INACTIVE")} at {GetCurrentISTTime():yyyy-MM-dd HH:mm:ss} IST");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to toggle test code status: {ex.Message}");
                return false;
            }
        }

        // Diagnostic method to help troubleshoot configuration issues
        public string GetDiagnosticInfo()
        {
            var diagnostics = new List<string>();
            
            // Check environment variable
            string? credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            diagnostics.Add($"GOOGLE_APPLICATION_CREDENTIALS: {credentialsPath ?? "NOT SET"}");
            
            // Check if credentials file exists
            if (!string.IsNullOrEmpty(credentialsPath))
            {
                diagnostics.Add($"Credentials file exists: {File.Exists(credentialsPath)}");
                if (File.Exists(credentialsPath))
                {
                    var fileInfo = new FileInfo(credentialsPath);
                    diagnostics.Add($"Credentials file size: {fileInfo.Length} bytes");
                    diagnostics.Add($"Credentials file modified: {fileInfo.LastWriteTime}");
                }
            }
            
            // Check Firebase initialization status
            diagnostics.Add($"Firebase initialized: {_firestoreDb != null}");
            
            return string.Join("\n", diagnostics);
        }

        /// <summary>
        /// Get diagnostic information including service account key file path
        /// </summary>
        /// <param name="serviceAccountKeyPath">Optional path to service account key file to check</param>
        /// <returns>Diagnostic information string</returns>
        public string GetDiagnosticInfo(string? serviceAccountKeyPath = null)
        {
            var diagnostics = new List<string>();
            
            // Check environment variable
            string? envCredentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            diagnostics.Add($"GOOGLE_APPLICATION_CREDENTIALS: {envCredentialsPath ?? "NOT SET"}");
            
            // Check environment variable credentials file if set
            if (!string.IsNullOrEmpty(envCredentialsPath))
            {
                diagnostics.Add($"Environment credentials file exists: {File.Exists(envCredentialsPath)}");
                if (File.Exists(envCredentialsPath))
                {
                    var fileInfo = new FileInfo(envCredentialsPath);
                    diagnostics.Add($"Environment credentials file size: {fileInfo.Length} bytes");
                    diagnostics.Add($"Environment credentials file modified: {fileInfo.LastWriteTime}");
                }
            }

            // Check provided service account key file path
            if (!string.IsNullOrEmpty(serviceAccountKeyPath))
            {
                diagnostics.Add($"Provided service account key path: {serviceAccountKeyPath}");
                diagnostics.Add($"Provided credentials file exists: {File.Exists(serviceAccountKeyPath)}");
                if (File.Exists(serviceAccountKeyPath))
                {
                    var fileInfo = new FileInfo(serviceAccountKeyPath);
                    diagnostics.Add($"Provided credentials file size: {fileInfo.Length} bytes");
                    diagnostics.Add($"Provided credentials file modified: {fileInfo.LastWriteTime}");
                }
            }
            
            // Check Firebase initialization status
            diagnostics.Add($"Firebase initialized: {_firestoreDb != null}");
            
            return string.Join("\n", diagnostics);
        }
    }

    public class TestCodeInfo
    {
        public string TestCode { get; set; } = "";
        public string TestUrl { get; set; } = "";
        public string TestTitle { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
        public string OwnerId { get; set; } = "";
        public string CreatedBy { get; set; } = "";
    }
}