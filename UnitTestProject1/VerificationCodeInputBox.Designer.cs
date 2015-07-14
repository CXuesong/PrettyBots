namespace UnitTestProject1
{
    partial class VerificationCodeInputBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.VerificationImageBox = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.VerificationTextBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.VerificationImageBox)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // VerificationImageBox
            // 
            this.VerificationImageBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.VerificationImageBox.Image = global::UnitTestProject1.Properties.Resources.f001;
            this.VerificationImageBox.Location = new System.Drawing.Point(0, 0);
            this.VerificationImageBox.Name = "VerificationImageBox";
            this.VerificationImageBox.Size = new System.Drawing.Size(284, 194);
            this.VerificationImageBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.VerificationImageBox.TabIndex = 0;
            this.VerificationImageBox.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.VerificationTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 194);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(284, 67);
            this.panel1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Dock = System.Windows.Forms.DockStyle.Top;
            this.button2.Location = new System.Drawing.Point(0, 44);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(284, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "取消";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Dock = System.Windows.Forms.DockStyle.Top;
            this.button1.Location = new System.Drawing.Point(0, 21);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(284, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "确定";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // VerificationTextBox
            // 
            this.VerificationTextBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.VerificationTextBox.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.VerificationTextBox.Location = new System.Drawing.Point(0, 0);
            this.VerificationTextBox.Name = "VerificationTextBox";
            this.VerificationTextBox.Size = new System.Drawing.Size(284, 21);
            this.VerificationTextBox.TabIndex = 0;
            // 
            // VerificationCodeInputBox
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button2;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.VerificationImageBox);
            this.Controls.Add(this.panel1);
            this.Name = "VerificationCodeInputBox";
            this.Text = "VerificationCodeInputBox";
            this.Load += new System.EventHandler(this.VerificationCodeInputBox_Load);
            ((System.ComponentModel.ISupportInitialize)(this.VerificationImageBox)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox VerificationImageBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox VerificationTextBox;

    }
}