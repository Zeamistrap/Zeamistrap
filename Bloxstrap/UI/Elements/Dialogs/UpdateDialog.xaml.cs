using System.Windows.Navigation;

using Bloxstrap.Models.APIs.GitHub;
using Bloxstrap.Resources;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class UpdateDialog
    {
        public UpdateDialog(string currentVersion, GithubRelease release)
        {
            InitializeComponent();

            CurrentVersionTextBlock.Text = FormatVersion(currentVersion);
            LatestVersionTextBlock.Text = FormatVersion(release.TagName);
            ReleaseNotesTextBlock.MarkdownText = string.IsNullOrWhiteSpace(release.Body)
                ? Strings.Update_Available_NoNotes
                : release.Body;
            ReleaseNotesLink.NavigateUri = GetReleaseNotesUri(release);

            UpdateButton.Click += (_, _) => DialogResult = true;
            LaterButton.Click += (_, _) => DialogResult = false;
        }

        private static string FormatVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return "-";

            string trimmed = version.Trim();
            return trimmed.StartsWith('v') ? trimmed : $"v{trimmed}";
        }

        private static Uri GetReleaseNotesUri(GithubRelease release)
        {
            if (Uri.TryCreate(release.HtmlUrl, UriKind.Absolute, out Uri? releaseUri)
                && releaseUri.Scheme == Uri.UriSchemeHttps
                && releaseUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return releaseUri;
            }

            string tag = string.IsNullOrWhiteSpace(release.TagName) ? "latest" : release.TagName;
            return new Uri($"{App.ProjectDownloadLink}/tag/{Uri.EscapeDataString(tag)}", UriKind.Absolute);
        }

        private void ReleaseNotesLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            const string LOG_IDENT = "UpdateDialog::ReleaseNotesLink_RequestNavigate";

            if (e.Uri is null
                || (e.Uri.Scheme != Uri.UriSchemeHttps && e.Uri.Scheme != Uri.UriSchemeHttp))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            e.Handled = true;
        }
    }
}
