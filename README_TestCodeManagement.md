# Coding Club Anticheat - Test Code Management

## ?? Overview

This enhanced version of the Coding Club Anticheat system now includes a comprehensive test code management feature that allows administrators to create and manage test codes using Firebase Firestore. Instead of hardcoded URLs, the system can dynamically generate unique 4-digit codes that map to specific test URLs.

## ?? New Features

### ? **Admin Mode**
- **Create Test Codes**: Generate unique 4-digit codes for any test URL
- **Manage Existing Codes**: View, enable/disable, and delete test codes
- **Firebase Integration**: Store test codes and URLs in Firestore
- **Usage Tracking**: Monitor how many times each code has been used

### ? **Student Mode (Enhanced)**
- **Firebase Lookup**: Enter 4-digit code to automatically load the correct test URL
- **Backward Compatibility**: Still supports hardcoded HackerRank URLs as fallback
- **Enhanced Security**: Same zero-tolerance anticheat monitoring

## ?? Setup Instructions

### 1. Firebase Configuration

#### Create Firebase Project
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Create a new project or use existing one
3. Enable Firestore Database
4. Create a collection named `test_codes`

#### Set up Authentication
1. In Firebase Console, go to **Project Settings** ? **Service Accounts**
2. Generate a new private key (JSON file)
3. Save the JSON file securely (don't commit to version control)

#### Configure Application
1. Update `appsettings.json` with your Firebase project details:
```json
{
  "Firebase": {
    "ProjectId": "your-firebase-project-id",
    "ServiceAccountKeyPath": "path/to/your/service-account-key.json"
  }
}
```

2. Set environment variable for Firebase credentials:
```bash
set GOOGLE_APPLICATION_CREDENTIALS=path\to\your\service-account-key.json
```

### 2. Firestore Database Structure

The system automatically creates documents in the `test_codes` collection with this structure:
```json
{
  "test_url": "https://hackerrank.com/challenges/example",
  "test_title": "Data Structures Quiz",
  "created_at": "2024-01-15T10:30:00Z",
  "is_active": true,
  "usage_count": 0
}
```

## ?? How to Use

### **Admin Workflow**

1. **Launch Application**
2. **Click "Create Test Code"** in Admin Mode section
3. **Fill in Details**:
   - Test Title (optional)
   - Test URL (required)
   - Firebase Project ID (required)
4. **Click "Generate Test Code"**
5. **Copy the generated 4-digit code**
6. **Share the code with students**

### **Student Workflow**

1. **Launch Application**
2. **Enter 4-digit code** in Student Mode section
3. **Click "Start Test"**
4. **Confirm in dialog**
5. **Secure browser launches** with anticheat monitoring

## ??? Security Features

### **Enhanced Monitoring**
- Real-time URL validation against Firebase data
- Smart indicator that hides on hover to avoid obstruction
- Silent keyboard shortcut blocking (no test termination)
- Zero-tolerance for tab switching, external navigation, dev tools

### **Firebase Security**
- Secure authentication using service account keys
- Firestore security rules (configure as needed)
- Encrypted data transmission
- Usage tracking for audit purposes

## ?? Test Code Management

### **View All Codes**
- See all created test codes with their details
- View creation date, usage count, and status
- Filter and sort capabilities

### **Manage Codes**
- **Enable/Disable**: Toggle code availability
- **Delete**: Permanently remove codes
- **Copy**: Easily copy codes to clipboard

### **Usage Analytics**
- Track how many times each code has been used
- Monitor active vs inactive codes
- View creation timestamps

## ?? Error Handling

### **Common Issues & Solutions**

#### Firebase Connection Failed
- **Problem**: "Firebase initialization failed"
- **Solution**: Check project ID and credentials path

#### Invalid Test Code
- **Problem**: Code not found in database
- **Solution**: Verify code exists and is active

#### URL Validation Error
- **Problem**: Invalid URL format
- **Solution**: Ensure URL starts with http:// or https://

## ?? Code Architecture

### **Key Components**

1. **FirebaseService.cs**: Handles all Firebase operations
2. **CreateTestPage.xaml**: Admin interface for managing codes
3. **MainWindow.xaml**: Updated with navigation support
4. **TestCodeInfo.cs**: Data model for test codes

### **Data Flow**

```
Admin Creates Code ? Firebase Storage ? Student Enters Code ? 
Firebase Lookup ? URL Retrieved ? Secure Browser Launch
```

## ?? API Reference

### **FirebaseService Methods**

```csharp
// Initialize Firebase connection
Task<bool> InitializeAsync(string projectId)

// Create new test code
Task<string> CreateTestCodeAsync(string testUrl, string testTitle = "")

// Get test code information
Task<TestCodeInfo?> GetTestCodeInfoAsync(string testCode)

// Get all test codes
Task<List<TestCodeInfo>> GetAllTestCodesAsync()

// Toggle code active status
Task<bool> ToggleTestCodeStatusAsync(string testCode)

// Delete test code
Task<bool> DeleteTestCodeAsync(string testCode)

// Increment usage counter
Task<bool> IncrementUsageAsync(string testCode)
```

## ?? Security Best Practices

1. **Never commit service account keys** to version control
2. **Use environment variables** for sensitive configuration
3. **Configure Firestore security rules** appropriately
4. **Regularly audit test code usage**
5. **Monitor for suspicious activity**

## ?? Deployment

### **Development Environment**
1. Set up Firebase credentials locally
2. Configure `appsettings.json`
3. Test with development Firebase project

### **Production Environment**
1. Use production Firebase project
2. Secure credential storage
3. Configure proper Firestore security rules
4. Enable logging and monitoring

## ?? Support

For issues or questions:
1. Check Firebase console for any service issues
2. Verify Firestore security rules
3. Check application logs for error details
4. Ensure proper network connectivity

---

## ?? **Ready to Use!**

Your Coding Club Anticheat system now supports dynamic test code management with Firebase integration. Students can use simple 4-digit codes while administrators have full control over test URLs and access management.