namespace AutoGRN_Conveyor
{
    partial class Password
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.lbl_ForgetPw = new System.Windows.Forms.Label();
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.btn_Login = new System.Windows.Forms.Button();
            this.txt_Password = new System.Windows.Forms.TextBox();
            this.lbl_Password = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.lbl_ConfirmPw = new System.Windows.Forms.Label();
            this.lbl_NewPw = new System.Windows.Forms.Label();
            this.btn_Back = new System.Windows.Forms.Button();
            this.bnt_Change = new System.Windows.Forms.Button();
            this.txt_NewPw = new System.Windows.Forms.TextBox();
            this.txt_ConfirmPw = new System.Windows.Forms.TextBox();
            this.txt_MainPw = new System.Windows.Forms.TextBox();
            this.lbl_MainKey = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.tabControl1.Location = new System.Drawing.Point(-10, -36);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(256, 194);
            this.tabControl1.TabIndex = 9999;
            this.tabControl1.TabStop = false;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.lbl_ForgetPw);
            this.tabPage1.Controls.Add(this.btn_Cancel);
            this.tabPage1.Controls.Add(this.btn_Login);
            this.tabPage1.Controls.Add(this.txt_Password);
            this.tabPage1.Controls.Add(this.lbl_Password);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage1.Size = new System.Drawing.Size(248, 168);
            this.tabPage1.TabIndex = 9997;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // lbl_ForgetPw
            // 
            this.lbl_ForgetPw.AutoSize = true;
            this.lbl_ForgetPw.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.2F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_ForgetPw.ForeColor = System.Drawing.SystemColors.Highlight;
            this.lbl_ForgetPw.Location = new System.Drawing.Point(62, 61);
            this.lbl_ForgetPw.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_ForgetPw.Name = "lbl_ForgetPw";
            this.lbl_ForgetPw.Size = new System.Drawing.Size(86, 13);
            this.lbl_ForgetPw.TabIndex = 1;
            this.lbl_ForgetPw.Text = "Forget Password";
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.Location = new System.Drawing.Point(130, 88);
            this.btn_Cancel.Margin = new System.Windows.Forms.Padding(2);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(74, 32);
            this.btn_Cancel.TabIndex = 3;
            this.btn_Cancel.Text = "Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = true;
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click_1);
            // 
            // btn_Login
            // 
            this.btn_Login.Location = new System.Drawing.Point(39, 88);
            this.btn_Login.Margin = new System.Windows.Forms.Padding(2);
            this.btn_Login.Name = "btn_Login";
            this.btn_Login.Size = new System.Drawing.Size(74, 32);
            this.btn_Login.TabIndex = 2;
            this.btn_Login.Text = "Login";
            this.btn_Login.UseVisualStyleBackColor = true;
            this.btn_Login.Click += new System.EventHandler(this.btn_Login_Click_1);
            // 
            // txt_Password
            // 
            this.txt_Password.Location = new System.Drawing.Point(64, 41);
            this.txt_Password.Margin = new System.Windows.Forms.Padding(2);
            this.txt_Password.Name = "txt_Password";
            this.txt_Password.PasswordChar = '*';
            this.txt_Password.Size = new System.Drawing.Size(154, 20);
            this.txt_Password.TabIndex = 0;
            // 
            // lbl_Password
            // 
            this.lbl_Password.AutoSize = true;
            this.lbl_Password.Location = new System.Drawing.Point(10, 43);
            this.lbl_Password.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Password.Name = "lbl_Password";
            this.lbl_Password.Size = new System.Drawing.Size(53, 13);
            this.lbl_Password.TabIndex = 9999;
            this.lbl_Password.Text = "Password";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.lbl_ConfirmPw);
            this.tabPage2.Controls.Add(this.lbl_NewPw);
            this.tabPage2.Controls.Add(this.btn_Back);
            this.tabPage2.Controls.Add(this.bnt_Change);
            this.tabPage2.Controls.Add(this.txt_NewPw);
            this.tabPage2.Controls.Add(this.txt_ConfirmPw);
            this.tabPage2.Controls.Add(this.txt_MainPw);
            this.tabPage2.Controls.Add(this.lbl_MainKey);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage2.Size = new System.Drawing.Size(248, 168);
            this.tabPage2.TabIndex = 9996;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // lbl_ConfirmPw
            // 
            this.lbl_ConfirmPw.AutoSize = true;
            this.lbl_ConfirmPw.Location = new System.Drawing.Point(2, 81);
            this.lbl_ConfirmPw.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_ConfirmPw.Name = "lbl_ConfirmPw";
            this.lbl_ConfirmPw.Size = new System.Drawing.Size(94, 13);
            this.lbl_ConfirmPw.TabIndex = 2;
            this.lbl_ConfirmPw.Text = "Confirm Password:";
            // 
            // lbl_NewPw
            // 
            this.lbl_NewPw.AutoSize = true;
            this.lbl_NewPw.Location = new System.Drawing.Point(16, 58);
            this.lbl_NewPw.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_NewPw.Name = "lbl_NewPw";
            this.lbl_NewPw.Size = new System.Drawing.Size(81, 13);
            this.lbl_NewPw.TabIndex = 1;
            this.lbl_NewPw.Text = "New Password:";
            // 
            // btn_Back
            // 
            this.btn_Back.Location = new System.Drawing.Point(130, 113);
            this.btn_Back.Margin = new System.Windows.Forms.Padding(2);
            this.btn_Back.Name = "btn_Back";
            this.btn_Back.Size = new System.Drawing.Size(74, 32);
            this.btn_Back.TabIndex = 1004;
            this.btn_Back.Text = "Back";
            this.btn_Back.UseVisualStyleBackColor = true;
            this.btn_Back.Click += new System.EventHandler(this.btn_Back_Click);
            // 
            // bnt_Change
            // 
            this.bnt_Change.Location = new System.Drawing.Point(39, 113);
            this.bnt_Change.Margin = new System.Windows.Forms.Padding(2);
            this.bnt_Change.Name = "bnt_Change";
            this.bnt_Change.Size = new System.Drawing.Size(74, 32);
            this.bnt_Change.TabIndex = 1003;
            this.bnt_Change.Text = "Change";
            this.bnt_Change.UseVisualStyleBackColor = true;
            this.bnt_Change.Click += new System.EventHandler(this.bnt_Change_Click);
            // 
            // txt_NewPw
            // 
            this.txt_NewPw.Location = new System.Drawing.Point(98, 56);
            this.txt_NewPw.Margin = new System.Windows.Forms.Padding(2);
            this.txt_NewPw.Name = "txt_NewPw";
            this.txt_NewPw.PasswordChar = '*';
            this.txt_NewPw.Size = new System.Drawing.Size(126, 20);
            this.txt_NewPw.TabIndex = 1001;
            // 
            // txt_ConfirmPw
            // 
            this.txt_ConfirmPw.Location = new System.Drawing.Point(98, 79);
            this.txt_ConfirmPw.Margin = new System.Windows.Forms.Padding(2);
            this.txt_ConfirmPw.Name = "txt_ConfirmPw";
            this.txt_ConfirmPw.PasswordChar = '*';
            this.txt_ConfirmPw.Size = new System.Drawing.Size(126, 20);
            this.txt_ConfirmPw.TabIndex = 1002;
            // 
            // txt_MainPw
            // 
            this.txt_MainPw.Location = new System.Drawing.Point(98, 33);
            this.txt_MainPw.Margin = new System.Windows.Forms.Padding(2);
            this.txt_MainPw.Name = "txt_MainPw";
            this.txt_MainPw.PasswordChar = '*';
            this.txt_MainPw.Size = new System.Drawing.Size(126, 20);
            this.txt_MainPw.TabIndex = 1000;
            // 
            // lbl_MainKey
            // 
            this.lbl_MainKey.AutoSize = true;
            this.lbl_MainKey.Location = new System.Drawing.Point(40, 36);
            this.lbl_MainKey.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_MainKey.Name = "lbl_MainKey";
            this.lbl_MainKey.Size = new System.Drawing.Size(54, 13);
            this.lbl_MainKey.TabIndex = 0;
            this.lbl_MainKey.Text = "Main Key:";
            // 
            // Password
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(237, 123);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Password";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Password";
            this.Load += new System.EventHandler(this.Password_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Label lbl_ForgetPw;
        private System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.Button btn_Login;
        private System.Windows.Forms.TextBox txt_Password;
        private System.Windows.Forms.Label lbl_Password;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Label lbl_ConfirmPw;
        private System.Windows.Forms.Label lbl_NewPw;
        private System.Windows.Forms.Button btn_Back;
        private System.Windows.Forms.Button bnt_Change;
        private System.Windows.Forms.TextBox txt_NewPw;
        private System.Windows.Forms.TextBox txt_ConfirmPw;
        private System.Windows.Forms.TextBox txt_MainPw;
        private System.Windows.Forms.Label lbl_MainKey;
    }
}