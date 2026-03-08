# 🚀 Microsoft Store Publishing Guide
## Coding Club Anticheat v2.1.0

Complete step-by-step guide to publish your app on the Microsoft Store.

---

## 📋 **Prerequisites**

### **Required:**
- ✅ Windows 10/11 with Visual Studio 2022
- ✅ Valid Microsoft account
- ✅ Partner Center developer account ($19 USD one-time registration fee)
- ✅ Completed app with all features working
- ✅ App icons and assets prepared
- ✅ Privacy policy published online

### **Your Current Status:**
- ✅ Version: **2.1.0.0**
- ✅ Publisher: **Developer.gg**
- ✅ Package Name: **Developer.gg.CodingClubAnticheat**
- ✅ Privacy Policy: Ready (PRIVACY_POLICY_STORE.md)

---

## 🎯 **STEP 1: Create Partner Center Account**

### **1.1 Register as Developer**

1. Go to [Microsoft Partner Center](https://partner.microsoft.com/dashboard)
2. Sign in with your Microsoft account
3. Click **"Enroll"** → **"Individual"** or **"Company"**
4. Fill out registration form:
   - **Account type:** Individual (recommended for solo dev)
   - **Country:** Your country
   - **Publisher display name:** `Developer.gg` (as in your manifest)
5. Pay $19 USD registration fee
6. Wait for approval (usually instant, sometimes 24-48 hours)

### **1.2 Verify Email**
- Check your email for verification link
- Complete email verification

---

## 🎨 **STEP 2: Prepare Assets & Store Listing**

### **2.1 Required Assets (Already in your project)**

Your app already has these assets in the `Assets` folder:
- ✅ Square150x150Logo (Medium tile)
- ✅ Square44x44Logo (App list icon)
- ✅ Wide310x150Logo (Wide tile)
- ✅ SplashScreen
- ✅ LockScreenLogo

**Additional assets you may need to create:**

**Store Listing Screenshots:**
- **At least 1 screenshot** (1920x1080 recommended)
- Show your app in action
- Can use Windows Snipping Tool or screenshot feature

**Store Icon (Optional but recommended):**
- 1240 x 600 px promotional image
- Showcases your app in the Store

### **2.2 Create Screenshots**

1. Run your app in Release mode
2. Navigate to key features:
   - Home page with test code entry
   - Admin panel (if signed in)
   - Test launch confirmation page
3. Press `Windows + Shift + S` to take screenshots
4. Save as PNG files

**Recommended screenshots:**
- HomePage with 4-digit code entry
- TestLaunchPage confirmation
- Admin panel with test code management
- Browser with secure mode indicator

---

## 📦 **STEP 3: Create MSIX Package**

### **3.1 Configure Signing (If Not Done)**

**Option A: Create Self-Signed Certificate**
1. Right-click project → **Publish** → **Create App Packages**
2. Choose **Microsoft Store as a new app**
3. Visual Studio will prompt to create certificate
4. Follow wizard to create self-signed cert

**Option B: Use Partner Center Certificate (Recommended)**
1. In Partner Center, go to your app
2. Navigate to **Product management** → **Package settings**
3. Download the certificate
4. Double-click to install in **Local Machine → Trusted Root Certification Authorities**

### **3.2 Build Release Package**

**Using Visual Studio:**

1. **Set Configuration to Release:**
   - Configuration dropdown → **Release**
   - Platform → **x64** (or your target)

2. **Clean Solution:**
   ```
   Build → Clean Solution
   ```

3. **Rebuild:**
   ```
   Build → Rebuild Solution
   ```

4. **Create App Package:**
   - Right-click project → **Publish** → **Create App Packages**
   - Select **Microsoft Store as a new app**
   - Click **Next**

5. **Configure Package:**
   - **Output location:** Choose a folder
   - **Generate app bundle:** Select **Never**
   - **Include public symbol files:** Check this box
   - **Architecture:**
     - ✅ x64
     - ✅ x86 (optional, for older PCs)
     - ✅ ARM64 (optional, for ARM devices)
   - Click **Create**

6. **Wait for Build:**
   - Visual Studio will create the `.msixupload` file
   - Location: Usually in `AppPackages` folder
   - File: `CodingClubAnticheat_2.1.0.0_x64_bundle.msixupload`

### **3.3 Verify Package**

Before uploading, test your package:

```powershell
# Install locally to test
Add-AppxPackage -Path "path\to\your\package.msix"

# Run and verify all features work
# Uninstall after testing
Get-AppxPackage -Name "*CodingClubAnticheat*" | Remove-AppxPackage
```

### **3.4 Run Windows App Certification Kit (WACK) - RECOMMENDED**

**WACK is Microsoft's official tool to pre-check your app before submission.**

**How to run WACK:**

```powershell
# Method 1: From Visual Studio
# After creating package, check "Launch Windows App Certification Kit" box

# Method 2: Manual run
& "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe" test -appxpackagepath "path\to\your.msix"
```

**Expected Results:**

✅ **PASS:**
- App launch test
- Crashes and hangs test  
- App manifest compliance
- Windows security features test
- Platform version launch test
- App manifest resources test
- Package sanity test

⚠️ **WARNING (Safe to ignore):**
- Blocked executables test - Framework DLLs (see Troubleshooting)

❌ **FAIL (Fixed in this version):**
- ~~Debug configuration test~~ - Fixed by upgrading Microsoft.Identity.Client

**WACK Report Location:**
```
C:\Users\[YourName]\AppData\Local\Microsoft\AppCertKit\results\
```

**If WACK shows errors:**
1. Read the detailed report
2. Check Troubleshooting section below
3. Fix issues
4. Re-run WACK
5. Only submit when WACK passes (warnings OK)

**Pro Tip:** Run WACK on **every submission** to avoid Microsoft Store rejections!

---

## 🌐 **STEP 4: Create Store Listing**

### **4.1 Reserve App Name**

1. Go to [Partner Center Dashboard](https://partner.microsoft.com/dashboard/apps)
2. Click **"+ New product"** → **"MSIX or PWA app"**
3. Enter app name: **"Coding Club Anticheat"**
4. Click **"Reserve product name"**
5. If name is taken, try:
   - "Coding Club Anti-Cheat"
   - "Test Monitor Pro"
   - "Academic Integrity Tool"

### **4.2 Fill Store Listing**

Navigate to your app → **Store listings** → **English (United States)**

**App Name:**
```
Coding Club Anticheat
```

**Description:**
```
🛡️ ZERO-TOLERANCE TEST MONITORING SYSTEM

Coding Club Anticheat is a powerful anti-cheating solution designed for coding competitions, online assessments, and academic tests. Ensure academic integrity with advanced monitoring features.

✨ KEY FEATURES:
• Zero-tolerance monitoring with instant violation detection
• Dynamic test code management via Firebase
• Admin panel for creating and managing test codes
• Secure browser environment with kiosk mode
• Real-time URL monitoring and domain whitelisting
• Comprehensive security features:
  - Tab switching detection
  - Copy/paste prevention
  - Developer tools blocking
  - Right-click menu disabling
  - Multiple window/tab detection
  - Focus loss detection

👨‍💼 FOR ADMINISTRATORS:
• Create unique 4-digit test codes
• Link codes to any test URL (HackerRank, LeetCode, custom platforms)
• Track usage statistics
• Enable/disable codes on demand
• Manage multiple tests simultaneously

👨‍🎓 FOR STUDENTS:
• Simple 4-digit code entry
• Clear test launch confirmation
• Minimal setup required
• No personal data collection
• Local-only monitoring during tests

🔒 PRIVACY & SECURITY:
• No student personal information collected
• Browser monitoring is local-only and temporary
• FERPA, GDPR, and CCPA compliant
• Encrypted data transmission
• Secure OAuth 2.0 authentication for admins

⚡ PERFECT FOR:
• Coding bootcamps and clubs
• Online coding competitions
• Academic institutions
• Remote assessments
• Certification exams
• Programming contests

🎯 ZERO-TOLERANCE MODE:
When activated, ANY violation results in immediate test termination with no warnings or second chances. This ensures the highest level of integrity for your assessments.

📊 USAGE TRACKING:
Track how many times each test code is used, monitor active vs inactive codes, and maintain complete control over your testing environment.

🌐 FIREBASE INTEGRATION:
Powered by Google Firebase for reliable, scalable test code management and real-time synchronization across all instances.

📱 REQUIREMENTS:
• Windows 10 version 17763.0 or higher
• Internet connection for admin features
• Google Chrome for test browser

💡 NOTE: Admin features require Google account authentication. Student mode requires no login or personal information.

Contact: thedeveloper.gg@gmail.com
Developer: Mohammad Abdul Gafoor (Developer.gg)
```

**Short Description (Store tile):**
```
Zero-tolerance anti-cheating system for coding tests and competitions. Secure browser monitoring with admin controls.
```

**Screenshots:**
- Upload 1-10 screenshots (at least 1 required)
- 1920x1080 recommended
- Add captions to explain features

**App icon (Store logo):**
- Upload square icon (1240x600 px)

**Category:**
- Primary: **Education** → **Tools**
- Secondary: **Developer tools** (if available)

**Privacy Policy URL:**
```
https://github.com/YourUsername/coding-club-anticheat/blob/main/PRIVACY_POLICY_STORE.md
```
*(Replace with your actual GitHub/website URL)*

**Support contact info:**
```
Email: thedeveloper.gg@gmail.com
Website: [Your website or GitHub repo]
```

### **4.3 Age Ratings & Content**

Navigate to **Age ratings**:
- Click **"Get IARC rating"**
- Answer questionnaire honestly:
  - Violence: None
  - Sexual content: None
  - Language: None
  - Controlled substances: None
  - Gambling: None
- App monitors browser activity: **Yes**
- Collects personal info: **Only for admins (email, name)**
- Get rating (usually E for Everyone or E10+)

### **4.4 Properties**

Navigate to **Properties**:

**Category:** Education → Tools
**Sub-category:** Computer Science

**Display mode:** Full screen (kiosk for tests)

**Accessibility:** Check boxes for:
- ✅ Keyboard accessible
- ✅ High contrast mode

**Hardware requirements:**
- Minimum RAM: 4 GB
- Recommended RAM: 8 GB
- Minimum DirectX: Not applicable
- Required processor: x86, x64, ARM64

---

## 📤 **STEP 5: Upload Package & Submit**

### **5.1 Upload MSIX Package**

1. Go to your app → **Submissions**
2. Click **"Start update"** (or "Start your first submission")
3. Navigate to **Packages**
4. Click **"Browse"** and select your `.msixupload` file
5. Wait for upload (may take 5-10 minutes)
6. Review package details:
   - ✅ Version: 2.1.0.0
   - ✅ Architecture: x64 (or multiple)
   - ✅ Target device families: Windows Desktop
   - ✅ Capabilities: internetClient, runFullTrust

### **5.2 Pricing & Availability**

Navigate to **Pricing and availability**:

**Markets:** Select markets where app will be available
- Recommended: Start with your country, expand later
- Or select **"All possible markets"**

**Pricing:**
- **Free** (recommended for educational tool)
- Or set price (e.g., $4.99, $9.99)

**Schedule:**
- Make available as soon as possible after certification

**Visibility:**
- ✅ Public audience (recommended)
- Or private audience (for testing)

### **5.3 Review & Submit**

1. Review all sections:
   - ✅ Properties
   - ✅ Pricing and availability
   - ✅ Age ratings
   - ✅ Packages
   - ✅ Store listings
2. Click **"Submit for certification"**
3. Wait for Microsoft review

---

## ⏳ **STEP 6: Certification Process**

### **6.1 What Happens Next**

**Timeline:**
- **Automated checks:** 1-2 hours
- **Manual review:** 1-3 business days
- **Total time:** Usually 24-72 hours

**Review Stages:**
1. ✅ Pre-processing (file validation)
2. 🔄 In review (manual testing)
3. ✅ Publishing (making live)
4. 🎉 In Store (live and searchable)

**What Microsoft Checks:**
- App launches correctly
- Features work as described
- No malware or security issues
- Privacy policy is accessible
- Age rating is appropriate
- Store listing is accurate

### **6.2 Common Rejection Reasons**

**If rejected, common fixes:**

1. **Crash on launch:**
   - Test .msixupload locally first
   - Check all dependencies included
   - Verify Firebase credentials not required for basic launch

2. **Privacy policy issues:**
   - Ensure URL is accessible
   - Policy must be in English
   - Must explain all data collection

3. **Description inaccuracies:**
   - Screenshots must match actual app
   - Features described must work
   - No misleading claims

4. **Technical issues:**
   - App must work offline (at least launch)
   - Must handle network errors gracefully
   - No hardcoded credentials

### **6.3 If Rejected**

1. Read rejection email carefully
2. Fix issues mentioned
3. Increment version number (e.g., 2.1.0.0 → 2.1.1.0)
4. Create new package
5. Submit update

---

## ✅ **STEP 7: Post-Publication**

### **7.1 Monitor Reviews**

- Check Partner Center dashboard regularly
- Respond to user reviews
- Address reported issues

### **7.2 Release Updates**

**For each update:**

1. **Bump version** in `Package.appxmanifest`:
   ```xml
   Version="2.2.0.0"  <!-- or next version -->
   ```

2. **Create changelog:**
   - Document new features
   - List bug fixes
   - Note breaking changes

3. **Build new package:**
   - Follow Step 3 again
   - Upload new `.msixupload`

4. **Submit update:**
   - Go to app → **Submissions** → **Start update**
   - Upload new package
   - Update "What's new in this version"
   - Submit

### **7.3 Marketing**

**Promote your app:**
- Share Store link on social media
- Post in coding communities
- Add to your GitHub README
- Create demo video for YouTube

**Your Store Link** (after approval):
```
https://www.microsoft.com/store/apps/[your-app-id]
```

---

## 🛠️ **Troubleshooting**

### **Package Creation Fails**

```powershell
# Clean build folders
Remove-Item -Path "bin" -Recurse -Force
Remove-Item -Path "obj" -Recurse -Force

# Rebuild
msbuild "Coding Club Anticheat.csproj" /t:Rebuild /p:Configuration=Release /p:Platform=x64
```

### **Signing Issues**

If you get signing errors:
1. Open Visual Studio as Administrator
2. Right-click project → **Properties** → **Package** → **Choose Certificate**
3. Create new test certificate or select existing
4. Rebuild

### **Upload Fails**

- Check file size (<2 GB limit)
- Ensure `.msixupload` not `.msix`
- Try different browser
- Clear browser cache

### **App Crashes on Testers' PCs**

Common cause: Firebase credentials
- Use **embedded JSON credentials** (see EMBEDDED_CREDENTIALS_GUIDE.md)
- Don't require external files for basic launch
- Handle Firebase init failure gracefully

### **❌ WACK Error: Debug Configuration**

**Error Message:**
```
The binary Microsoft.Identity.Client.NativeInterop.dll is built in debug mode.
```

**Solution (Already Applied):**
✅ Upgraded Microsoft.Identity.Client to v4.66.2
✅ Added UseDebugLibraries=False for Release builds
✅ Disabled debug symbols in Release configuration

**To verify fix:**
1. Clean solution (`Build → Clean Solution`)
2. Rebuild in Release mode
3. Run WACK test again:
   ```powershell
   # In PowerShell
   & "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe" reset
   & "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe" test -appxpackagepath "path\to\your.msix"
   ```

**Alternative if still failing:**
If upgrading doesn't work, remove Microsoft.Identity.Client temporarily:
1. Comment out Google OAuth features
2. Publish without admin authentication
3. Add it back in future update

### **⚠️ WACK Warning: Blocked Executables**

**Warning Message:**
```
File X.dll contains a reference to a "Launch Process" related API
File Y.dll contains a blocked executable reference to "cmd/bash/reg"
```

**Action Required:** ✅ **NONE - You can safely ignore these**

**Reasons:**
- These are **framework DLLs** (System.*, Microsoft.*, Selenium)
- References are for **compatibility checks**, not actual execution
- Your app doesn't launch these executables
- Microsoft Store **allows** apps with these warnings
- Not a certification blocker

**From Microsoft's own guidance:**
> "If the flagged files are part of your application, you may ignore the warning."

**Affected DLLs (all safe to ignore):**
- `System.Diagnostics.Process.dll` - .NET framework
- `WebDriver.dll` - Selenium WebDriver (for Chrome automation)
- `Microsoft.Identity.Client.dll` - Microsoft's own library
- `System.Windows.Forms.dll` - WinForms framework
- And 50+ other framework DLLs

**What Microsoft actually checks:**
- Your main .exe doesn't launch unauthorized processes ✅
- App works within sandboxed environment ✅
- No actual malware or security violations ✅

**Your app is safe because:**
- You only launch Chrome via Selenium (allowed)
- No cmd.exe, bash.exe, or reg.exe actually executed
- All "blocked" references are in third-party frameworks

### **Certification Rejection - Common Issues**

**1. App Crashes on Launch**
- Cause: Firebase credentials required
- Fix: Make app launch without Firebase (show error gracefully)
- Test: Install .msix without appsettings.json

**2. Privacy Policy Not Accessible**
- Cause: URL returns 404
- Fix: Upload PRIVACY_POLICY_STORE.md to GitHub
- Verify: Open URL in incognito mode

**3. Missing Capabilities**
- Cause: App needs permissions not declared
- Fix: Check Package.appxmanifest has:
  ```xml
  <Capability Name="internetClient" />
  <rescap:Capability Name="runFullTrust" />
  ```

**4. Package Validation Errors**
- Clean solution
- Delete bin/ and obj/ folders
- Rebuild package
- Re-run WACK test before submitting

---

## 📊 **Version History**

| Version | Date | Changes |
|---------|------|---------|
| 2.1.0.0 | 2026-03-08 | Major update: TestLaunchPage, browser closure handling, dynamic domains |
| 2.0.3.0 | [Previous] | Previous stable version |

---

## 📞 **Support**

**Need Help?**
- Microsoft Store support: [https://developer.microsoft.com/en-us/microsoft-store/support](https://developer.microsoft.com/en-us/microsoft-store/support)
- Partner Center docs: [https://docs.microsoft.com/en-us/windows/uwp/publish/](https://docs.microsoft.com/en-us/windows/uwp/publish/)

**Questions about this app?**
- Email: thedeveloper.gg@gmail.com
- GitHub: [Your repo URL]

---

## ✨ **Quick Checklist**

Before submitting:
- ✅ Version bumped to 2.1.0.0
- ✅ All features tested in Release mode
- ✅ Package builds successfully
- ✅ App tested from .msix package
- ✅ Screenshots prepared (1-10 images)
- ✅ Privacy policy URL accessible
- ✅ Store description written
- ✅ Age rating questionnaire completed
- ✅ Pricing set
- ✅ Markets selected
- ✅ Support email provided

**Ready to publish! 🚀**

---

**Good luck with your Microsoft Store submission!**
