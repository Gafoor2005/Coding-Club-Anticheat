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