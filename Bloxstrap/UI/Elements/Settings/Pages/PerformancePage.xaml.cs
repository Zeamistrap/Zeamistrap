using System.Windows.Input;

using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for PerformancePage.xaml
    /// </summary>
    public partial class PerformancePage
    {
        public PerformancePage()
        {
            DataContext = new PerformanceViewModel();
            InitializeComponent();
        }

        private void ValidateUInt32(object sender, TextCompositionEventArgs e) => e.Handled = !UInt32.TryParse(e.Text, out uint _);
    }
}