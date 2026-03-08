using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Coding_Club_Anticheat.Services;

namespace Coding_Club_Anticheat
{
    public sealed partial class TestLaunchPage : Page
    {
        private TestCodeInfo? _testCodeInfo;
        private string _testCode = "";

        public TestLaunchPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Receive test code info from navigation parameter
            if (e.Parameter is TestLaunchPageParameter parameter)
            {
                _testCodeInfo = parameter.TestCodeInfo;
                _testCode = parameter.TestCode;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (_testCodeInfo != null)
            {
                string testTitle = !string.IsNullOrEmpty(_testCodeInfo.TestTitle)
                    ? _testCodeInfo.TestTitle
                    : "Untitled Test";

                // Update header
                TestTitleText.Text = $"🎯 {testTitle}";
                TestCodeDisplay.Text = $"Code: {_testCodeInfo.TestCode}";

                // Update details card
                TestTitleDetail.Text = testTitle;
                TestCodeDetail.Text = _testCodeInfo.TestCode;
                UsageCountDetail.Text = _testCodeInfo.UsageCount == 1
                    ? "1 student"
                    : $"{_testCodeInfo.UsageCount} students";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to home page
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to home page (same as back button)
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            // Set launch confirmation flag BEFORE navigating back
            Application.Current.Resources["TestLaunchConfirmed"] = true;
            Application.Current.Resources["TestLaunchCode"] = _testCode;
            Application.Current.Resources["TestLaunchInfo"] = _testCodeInfo;

            System.Diagnostics.Debug.WriteLine($"[TestLaunchPage] User confirmed launch for test code: {_testCode}");
            System.Diagnostics.Debug.WriteLine($"[TestLaunchPage] Set TestLaunchConfirmed flag to true");

            // Navigate back to HomePage which will detect the flag and launch browser
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }

    // Parameter class to pass data to TestLaunchPage
    public class TestLaunchPageParameter
    {
        public TestCodeInfo TestCodeInfo { get; set; } = null!;
        public string TestCode { get; set; } = "";
    }
}
