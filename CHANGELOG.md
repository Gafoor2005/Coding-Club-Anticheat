# Changelog
All notable changes to Coding Club Anticheat will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.1.0] - 2026-03-08

### 🎉 Major Features Added

#### TestLaunchPage Confirmation System
- **Full-page test launch confirmation** with detailed information before starting test
- Shows test title, code, usage count, and comprehensive zero-tolerance rules
- Clear "I Understand - Launch Test" confirmation button
- Improved user experience and reduced accidental test starts

#### Dynamic Domain Whitelisting
- **Automatic domain extraction** from Firebase test URLs
- No more hardcoded domain restrictions
- Supports any test platform (HackerRank, LeetCode, custom sites, etc.)
- Automatically allows both www and non-www versions of domains
- Fixes: Previously only HackerRank domains were allowed

### 🐛 Critical Bug Fixes

#### Browser Closure Exception Handling
- **Fixed:** NullReferenceException when browser closes
- **Fixed:** HttpRequestException spam during browser disconnection
- **Fixed:** WebDriverException crashes on browser closure
- **Added:** Centralized `CleanupBrowserResources()` method
- **Added:** User-friendly "Browser Closed" message
- **Added:** Graceful cleanup on all exception types
- **Added:** Proper timer disposal to prevent memory leaks

#### Release Mode Firebase Support
- **Fixed:** appsettings.json not included in Release builds
- **Fixed:** Firebase initialization failing in MSIX packages
- **Added:** Support for embedded JSON credentials
- **Added:** `CopyToPublishDirectory` for configuration files
- **Changed:** `PublishTrimmed` to `False` to prevent file removal
- Works reliably in both Debug and Release modes

#### Admin Panel Firebase Connection
- **Fixed:** Admin panel Firebase initialization in Release mode
- **Updated:** CreateTestPage to use 3-parameter Firebase initialization
- **Updated:** LoadTestCodesAsync with embedded JSON support
- Consistent Firebase initialization across all pages

### ✨ Enhancements

#### Dialog Prevention System
- **Added:** `_isDialogShowing` flag to prevent multiple dialogs
- Applied to all dialog methods:
  - ShowTestCodeMessage
  - ShowTestCodeNotFoundMessage
  - ShowCheatDetectedDialog
  - ShowBrowserClosedMessage
  - ShowIncompleteCodeMessage
  - ShowErrorMessage
- Prevents dialog stacking and improves UX

#### Enhanced Loading States
- **Improved:** `ShowStartTestLoading` now disables ALL digit inputs
- **Improved:** Disables Clear button during validation
- **Improved:** Better visual feedback during test code validation
- Prevents user interaction during async operations

#### Better Exception Handling
- **Enhanced:** `MonitorUrlAndNavigation` with specific exception types
- **Enhanced:** `EnsureSecurityScriptsActive` with browser disconnection handling
- **Enhanced:** `StartJavaScriptViolationMonitoring` with null checks
- **Enhanced:** `HandleCheatDetected` uses centralized cleanup
- Comprehensive error recovery throughout

#### Navigation Improvements
- **Added:** `OnNavigatedTo` method to handle TestLaunchPage returns
- **Added:** Application.Current.Resources for cross-page communication
- **Improved:** Test launch flow with proper state management
- Better separation of concerns between pages

### 🔧 Technical Improvements

#### Code Quality
- Added `using Microsoft.UI.Xaml.Navigation` directive
- Improved method organization and readability
- Added comprehensive error logging
- Better async/await patterns

#### Configuration
- Changed `_allowedDomains` from readonly to mutable
- Better path resolution for configuration files
- Improved diagnostic output for troubleshooting

### 📝 Documentation
- Added comprehensive Microsoft Store publishing guide
- Updated SETUP_GUIDE.md with publishing reference
- Added CHANGELOG.md for version tracking
- Improved inline code comments

### 🔄 Breaking Changes
None - fully backward compatible with 2.0.x

### 🏗️ Infrastructure
- Package version bumped to 2.1.0.0
- Build: Successful in Release mode
- All tests: Passing
- Memory leaks: Fixed

---

## [2.0.3] - [Previous Release Date]

### Previous Features
- Firebase test code management
- Zero-tolerance monitoring
- Admin panel
- Google OAuth authentication
- Kiosk mode browser
- URL monitoring
- Security script injection
- Violation detection

### Known Issues (Fixed in 2.1.0)
- ❌ Browser closure caused crashes
- ❌ Release mode Firebase connection failed
- ❌ Hardcoded domain restrictions
- ❌ Dialog stacking possible
- ❌ Admin panel Release mode issues

---

## [Unreleased]

### Planned Features
- Webhook integration for violation notifications
- Advanced analytics dashboard
- Multi-language support
- Customizable violation policies
- Screen recording during tests (optional)
- Facial recognition integration (optional)
- LMS integrations (Canvas, Blackboard, Moodle)

---

## How to Upgrade

### From 2.0.x to 2.1.0

1. **Backup your data:**
   - Export test codes from Admin Panel
   - Save appsettings.json configuration

2. **Update app:**
   - Download new version from Microsoft Store
   - Or rebuild from source (Release mode)

3. **Verify functionality:**
   - Test Firebase connection
   - Create a test code
   - Launch a test to verify browser monitoring
   - Check Admin Panel functionality

4. **New features available immediately:**
   - TestLaunchPage automatically appears
   - Dynamic domains work with existing test codes
   - Better error handling active

**No configuration changes required!**

---

## Support

**Issues or questions?**
- Email: thedeveloper.gg@gmail.com
- GitHub: [Repository URL]
- Documentation: See README.md and guides

**Report bugs:**
Include:
- Version number (check Package.appxmanifest)
- Steps to reproduce
- Expected vs actual behavior
- Screenshots if applicable

---

## Contributors

**Lead Developer:** Mohammad Abdul Gafoor (Developer.gg)

**Special Thanks:** 
- Microsoft for Windows App SDK
- Google for Firebase services
- OpenQA for Selenium WebDriver
- All beta testers and early adopters

---

**Note:** Version numbers follow Semantic Versioning:
- **MAJOR** (X.0.0): Breaking changes
- **MINOR** (0.X.0): New features, backward compatible
- **PATCH** (0.0.X): Bug fixes
- **BUILD** (0.0.0.X): Build metadata

Current: **2.1.0.0** (Minor feature update with critical fixes)
