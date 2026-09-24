using Bloxstrap.Resources;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class UpdateCompleteDialog
    {
        public UpdateCompleteDialog(string version)
        {
            InitializeComponent();

            TitleTextBlock.Text = Strings.Update_Complete_Title;
            MessageTextBlock.Text = string.Format(Strings.Update_Complete_Message, version);
            CloseButton.Click += (_, _) => Close();
        }
    }
}
