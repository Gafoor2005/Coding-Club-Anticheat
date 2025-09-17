# Setup Guide for Coding Club Anticheat

## 🔧 Initial Setup

### 1. **Clone the Repository**
```bash
git clone [your-repo-url]
cd "Coding Club Anticheat"
```

### 2. **Configure appsettings.json**
```bash
# Copy the template
cp appsettings.template.json appsettings.json
```

Then edit `appsettings.json` with your actual credentials:

```json
{
  "Firebase": {
    "ProjectId": "your-actual-firebase-project-id",
    "ServiceAccountKeyPath": "Keys/firebase-service-account.json",
    "ApiKey": "YOUR_ACTUAL_FIREBASE_API_KEY",
    "GoogleOAuthClientId": "YOUR_ACTUAL_GOOGLE_CLIENT_ID",
    "GoogleOAuthClientSecret": "YOUR_ACTUAL_GOOGLE_CLIENT_SECRET"
  },
  "TestCodeSettings": {
    "DefaultProjectId": "your-actual-firebase-project-id"
  }
}
```

### 3. **Firebase Service Account Key**
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project
3. Go to **Project Settings** → **Service Accounts**
4. Click **Generate New Private Key**
5. Save the JSON file as `Keys/firebase-service-account.json`

### 4. **Google OAuth Credentials**
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Enable Google+ API
3. Create OAuth 2.0 credentials
4. Set redirect URI to `http://localhost:8080/auth/callback`
5. Copy Client ID and Client Secret to appsettings.json

## 🚨 **SECURITY WARNING**
- **NEVER commit `appsettings.json`** with real credentials
- **NEVER commit the `Keys/` folder**
- **NEVER share your Firebase service account key**
- The `.gitignore` file is configured to prevent accidental commits

## 🔍 **Verification**
After setup, your file structure should look like:
```
Coding Club Anticheat/
├── appsettings.json (your actual config - NOT in git)
├── appsettings.template.json (template - safe to commit)
├── Keys/
│   └── firebase-service-account.json (NOT in git)
└── ... (other files)
```

## 🏃‍♂️ **Running the Application**
1. Open the solution in Visual Studio 2022
2. Ensure .NET 8 SDK is installed
3. Build and run the project
4. The app will automatically find and use your configuration

## 🌐 **Firebase Setup**
1. Create a Firebase project
2. Enable Firestore Database
3. Set up authentication (Google provider)
4. Configure Firestore security rules as needed

## 📱 **Publishing to Microsoft Store**
See the main README for complete publishing instructions.