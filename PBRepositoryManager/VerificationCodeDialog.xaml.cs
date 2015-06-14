using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PrettyBots.Visitors;

namespace PBRepositoryManager
{
    /// <summary>
    /// VerificationCodeDialog.xaml 的交互逻辑
    /// </summary>
    public partial class VerificationCodeDialog : Window
    {
        public VerificationCodeDialog()
        {
            InitializeComponent();
        }

        public string ShowDialog(string imageUrl)
        {
            VImageBox.Source = new BitmapImage(new Uri(imageUrl, UriKind.Absolute));
            VCodeBox.Text = "";
            VCodeBox.Focus();
            if (base.ShowDialog() == true)
                return VCodeBox.Text;
            return null;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }

    public class InteractiveVCodeRecognizer : VerificationCodeRecognizer
    {
        protected override string RecognizeFromUrl(string imageUrl, WebSession session)
        {
            var dlg = new VerificationCodeDialog();
            return dlg.ShowDialog(imageUrl);
        }
    }
}
