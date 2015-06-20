using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnitTestProject1
{
    public partial class VerificationCodeInputBox : Form
    {
        public VerificationCodeInputBox()
        {
            InitializeComponent();
        }

        private void VerificationCodeInputBox_Load(object sender, EventArgs e)
        {
            this.BringToFront();
        }

        public string ShowDialog(string imageUrl)
        {
            VerificationImageBox.LoadAsync(imageUrl);
            if (base.ShowDialog() == DialogResult.OK)
                return VerificationTextBox.Text;
            return null;
        }
    }
}
