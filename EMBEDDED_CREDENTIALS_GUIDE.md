# 🔐 Embedded Firebase Credentials Guide

## 🎯 Problem Statement

When you package your app for distribution (e.g., Microsoft Store, standalone installer), the Firebase service account key JSON file won't be available on users' PCs. This causes the **"Firebase Connection Error"** because the app can't find the credentials file.

## ✅ Solution: Embed Credentials in Configuration

Instead of relying on an external file, embed your Firebase credentials directly in `appsettings.json` as a **Base64-encoded string**. This way, the credentials are bundled with your app and work on any PC.

---

## 📋 Quick Start

### Step 1: Convert Firebase Key to Base64

Open PowerShell and run:

```powershell
# Navigate to your project directory
cd "C:\Users\Gafoor\source\repos\Coding Club Anticheat"

# Convert the JSON file to Base64
$jsonContent = Get-Content -Path "Keys\firebase-service-account.json" -Raw
$bytes = [System.Text.Encoding]::UTF8.GetBytes($jsonContent)
$base64 = [Convert]::ToBase64String($bytes)

# Copy to clipboard
$base64 | Set-Clipboard

# Also save to file
$base64 | Out-File -FilePath "firebase-credentials-base64.txt" -Encoding utf8

Write-Host "✓ Base64 string copied to clipboard and saved to firebase-credentials-base64.txt"
Step 2: Update appsettings.json
Paste the Base64 string into appsettings.json:
{
  "Firebase": {
    "ProjectId": "coding-club-anticheat",
    "ServiceAccountKeyPath": "",
    "ServiceAccountKeyJson": "hgjgkj",
    "ApiKey": "YOUR_API_KEY",
    "GoogleOAuthClientId": "YOUR_CLIENT_ID",
    "GoogleOAuthClientSecret": "YOUR_CLIENT_SECRET"
  },
  "TestCodeSettings": {
    "DefaultProjectId": "coding-club-anticheat"
  }
}
Important:
•	The Base64 string will be very long (1000+ characters) - that's normal
•	Leave ServiceAccountKeyPath empty ("") when using embedded JSON
•	Remove ALL line breaks from the Base64 string (must be one continuous line)
Step 3: Test It
1.	Delete or rename the Keys folder temporarily
2.	Run your app
3.	Try entering a test code
4.	Check the Debug Output window
You should see:
Attempting to load credentials from embedded JSON...
✓ Credentials loaded successfully from embedded JSON
✓ Firebase connection test successful
 
🔧 Detailed Configuration Options
Option 1: Embedded JSON Only (✅ Recommended for Store Apps)
Best for: Microsoft Store, standalone installers, distributed apps
{
  "Firebase": {
    "ProjectId": "your-project-id",
    "ServiceAccountKeyPath": "",
    "ServiceAccountKeyJson": "YOUR_BASE64_STRING_HERE"
  }
}
Advantages:
•	✅ Works on any PC without external files
•	✅ Perfect for Microsoft Store deployment
•	✅ No file path resolution issues
•	✅ Simpler deployment
Disadvantages:
•	⚠️ appsettings.json contains sensitive data (must be in .gitignore)
•	⚠️ Need to regenerate Base64 if credentials change
 
Option 2: Hybrid Approach (Development + Distribution)
Best for: Development environments where you frequently update credentials
{
  "Firebase": {
    "ProjectId": "your-project-id",
    "ServiceAccountKeyPath": "Keys/firebase-service-account.json",
    "ServiceAccountKeyJson": "YOUR_BASE64_STRING_HERE"
  }
}
How it works:
1.	During development: Uses the file path (easier to update)
2.	After deployment: Falls back to embedded JSON if file not found
Initialization priority:
1.	Try embedded JSON first (if provided)
2.	Try file path second (if embedded JSON fails)
3.	Try environment variable last (fallback)
 
Option 3: File Path Only (Development Only)
Best for: Local development only
{
  "Firebase": {
    "ProjectId": "your-project-id",
    "ServiceAccountKeyPath": "Keys/firebase-service-account.json",
    "ServiceAccountKeyJson": ""
  }
}
⚠️ Warning: This won't work on other PCs unless the file is manually copied!
 
🛠️ PowerShell Helper Script
Save this as Convert-FirebaseCredentials.ps1:
# Firebase Credentials Converter
# Converts Firebase service account JSON to Base64 for embedding

param(
    [string]$JsonPath = "Keys\firebase-service-account.json",
    [string]$OutputFile = "firebase-credentials-base64.txt"
)

Write-Host ""
Write-Host "🔐 Firebase Credentials Converter" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

# Check if JSON file exists
if (-not (Test-Path $JsonPath)) {
    Write-Host "❌ Error: Firebase JSON file not found at: $JsonPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure your service account key file is at:" -ForegroundColor Yellow
    Write-Host "  $JsonPath" -ForegroundColor Yellow
    exit 1
}

try {
    # Read and convert to Base64
    Write-Host "📖 Reading Firebase credentials..." -ForegroundColor Yellow
    $jsonContent = Get-Content -Path $JsonPath -Raw
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($jsonContent)
    $base64 = [Convert]::ToBase64String($bytes)

    Write-Host "✓ Credentials converted to Base64" -ForegroundColor Green
    Write-Host "  Length: $($base64.Length) characters" -ForegroundColor Gray
    Write-Host ""

    # Save to file
    $base64 | Out-File -FilePath $OutputFile -Encoding utf8
    Write-Host "✓ Base64 saved to: $OutputFile" -ForegroundColor Green

    # Copy to clipboard
    $base64 | Set-Clipboard
    Write-Host "✓ Base64 copied to clipboard" -ForegroundColor Green
    Write-Host ""

    Write-Host "📝 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Open appsettings.json" -ForegroundColor White
    Write-Host "2. Find 'ServiceAccountKeyJson' field" -ForegroundColor White
    Write-Host "3. Paste the Base64 string (Ctrl+V)" -ForegroundColor White
    Write-Host "4. Set 'ServiceAccountKeyPath' to empty string ('')" -ForegroundColor White
    Write-Host "5. Save and test your app" -ForegroundColor White
    Write-Host ""

    Write-Host "⚠️  Security Reminder:" -ForegroundColor Yellow
    Write-Host "- Add appsettings.json to .gitignore" -ForegroundColor Gray
    Write-Host "- Do NOT commit sensitive credentials to git" -ForegroundColor Gray
    Write-Host "- Delete $OutputFile after copying" -ForegroundColor Gray
}
catch {
    Write-Host ""
    Write-Host "❌ Error converting credentials: $_" -ForegroundColor Red
    exit 1
}
Usage:
# Basic usage
.\Convert-FirebaseCredentials.ps1

# Custom paths
.\Convert-FirebaseCredentials.ps1 -JsonPath "path\to\your\key.json" -OutputFile "output.txt"
 
🔒 Security Best Practices
✅ DO:
1.	Add appsettings.json to .gitignore
appsettings.json
firebase-credentials-base64.txt
Keys/
2.	Use appsettings.template.json for version control
{
  "Firebase": {
    "ProjectId": "your-project-id-here",
    "ServiceAccountKeyPath": "",
    "ServiceAccountKeyJson": "PASTE_YOUR_BASE64_HERE",
    "ApiKey": "YOUR_API_KEY",
    "GoogleOAuthClientId": "YOUR_CLIENT_ID",
    "GoogleOAuthClientSecret": "YOUR_CLIENT_SECRET"
  }
}
3.	Regenerate credentials if exposed
•	Go to Firebase Console → Project Settings → Service Accounts
•	Delete compromised key
•	Generate new key
•	Re-convert to Base64
4.	Use Firebase Security Rules
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /test_codes/{code} {
      allow read: if request.auth != null;
      allow write: if request.auth != null && request.auth.token.admin == true;
    }
  }
}
5.	Limit service account permissions
•	Only grant "Cloud Datastore User" role
•	Avoid "Owner" or "Editor" roles
❌ DON'T:
1.	❌ Commit appsettings.json with real credentials
2.	❌ Share Base64 strings publicly or in Discord/Slack
3.	❌ Use online Base64 converters for production credentials
4.	❌ Store credentials in source control
5.	❌ Give service account more permissions than needed
 
🐛 Troubleshooting
Issue 1: "No credentials available"
Symptoms:
ERROR: No credentials available (neither embedded JSON nor file path worked)
Firebase Connection Error
Solution:
1.	Verify ServiceAccountKeyJson is not empty in appsettings.json
2.	Check Base64 string has no line breaks
3.	Ensure Base64 string is valid (try re-generating)
Debug:
# Test if Base64 is valid
$base64 = Get-Content "firebase-credentials-base64.txt" -Raw
$bytes = [Convert]::FromBase64String($base64)
$json = [System.Text.Encoding]::UTF8.GetString($bytes)
$json | ConvertFrom-Json
 
Issue 2: "Invalid JSON" or "Invalid key file"
Symptoms:
Firebase initialization failed: Invalid key file
Exception Type: InvalidOperationException
Solution:
1.	Re-generate Base64 string from original JSON file
2.	Ensure no extra characters or whitespace in Base64
3.	Verify original JSON file is valid
Debug:
# Validate your JSON file
Get-Content "Keys\firebase-service-account.json" -Raw | ConvertFrom-Json
 
Issue 3: "Permission denied" or "PERMISSION_DENIED"
Symptoms:
✗ Firebase connection test failed: Permission denied
ISSUE: Permission denied - check Firebase security rules
Solution:
1.	Check Firebase Console → Firestore → Rules
2.	Verify service account has proper roles:
•	Go to IAM & Admin → Service Accounts
•	Ensure "Cloud Datastore User" role is assigned
3.	Test with temporary permissive rules:
allow read, write: if true; // TEMPORARY - for testing only
 
Issue 4: App works locally but fails on other PCs
Symptoms:
•	Works on development PC
•	Fails with "Service account key file not found" on other PCs
Solution:
•	You're using ServiceAccountKeyPath instead of embedded JSON
•	Convert to embedded JSON using the PowerShell script above
•	Or include the Keys folder in your deployment package
 
Issue 5: Base64 string is too long
Symptoms:
•	JSON editor complains about line length
•	Difficulty copying/pasting the string
Solution: This is normal! Firebase service account keys are 1000-2000 characters when Base64-encoded.
Workarounds:
1.	Use a text editor with no line length limits (VS Code, Notepad++)
2.	Save Base64 to file first, then copy from file
3.	Use JSON minifier if needed (but Base64 is already compact)
 
📊 Comparison Table
Feature	File Path	Embedded JSON	Environment Variable
Works without external files	❌	✅	❌
Easy to update	✅	⚠️	⚠️
Microsoft Store compatible	❌	✅	❌
Version control safe	✅	⚠️	✅
Multi-PC deployment	❌	✅	⚠️
Development friendly	✅	⚠️	✅
Recommended for:	Development	Production/Store	Server Deployments
 
📝 Complete Example
Here's a complete appsettings.json example for production deployment:
{
 bkhkhjnlknlk'll
 llkpl

}
 
🎓 Additional Resources
•	Firebase Service Accounts Documentation
•	Firestore Security Rules
•	Base64 Encoding (Wikipedia)
 
💡 Pro Tips
1.	Keep a backup of your original JSON file in a secure location
2.	Use different credentials for development and production
3.	Rotate credentials regularly (every 90 days recommended)
4.	Monitor usage in Firebase Console to detect unauthorized access
5.	Test on a clean VM before distributing to ensure embedded credentials work
 
📞 Support
If you encounter issues:
1.	Check the Debug Output window for detailed error messages
2.	Review the Troubleshooting section above
3.	Verify your Firebase project settings
4.	Test with a fresh Firebase project to isolate the issue
 
Last Updated: March 8, 2026 Version: 1.0.0