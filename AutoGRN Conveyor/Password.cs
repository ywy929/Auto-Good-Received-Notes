using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace AutoGRN_Conveyor
{
    public partial class Password : Form
    {
        string main_key = "AutoGRN1";
        string password;
        public Password()
        {
            InitializeComponent();
        }
        private void Password_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("Enter");
            get_password();
            txt_Password.Select();

            txt_Password.KeyPress += new KeyPressEventHandler(txt_Password_KeyPress);
        }

        private void get_password()
        {
            string pathRoot = Path.Combine(@"C:\AutoGRN_config");
            DirectoryInfo directoryinfo = new DirectoryInfo(pathRoot);
            string json_config = File.ReadAllText(pathRoot + @"\Password.txt");

            var dictionary = JsonConvert.DeserializeObject<IDictionary>(json_config);
            foreach (DictionaryEntry entry in dictionary)
            {
                password = Convert.ToString(entry.Value);
            }
        }
        private void btn_Login_Click(object sender, EventArgs e)
        {
            if (txt_Password.Text.ToString() == password)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Incorrect Password!");
            }
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void lbl_ForgetPw_Click(object sender, EventArgs e)
        {
            this.tabControl1.SelectedTab = tabPage2;
            txt_MainPw.Select();
            txt_Password.Text = "";
            txt_MainPw.Text = "";
            txt_NewPw.Text = "";
            txt_ConfirmPw.Text = "";
        }

        private void bnt_Change_Click(object sender, EventArgs e)
        {
            if (txt_MainPw.Text.ToString() == main_key)
            {
                if (txt_NewPw.Text.ToString() == txt_ConfirmPw.Text.ToString())
                {
                    string temp_pw = txt_ConfirmPw.Text.ToString();

                    string[] initial_txt = new string[1] {"{\r\n\tPassword:\"" + temp_pw +
                    "\"\r\n}" };

                    string pathRoot = Path.Combine(Environment.CurrentDirectory);

                    File.WriteAllLines(pathRoot + @"\Password.txt", initial_txt);
                    txt_MainPw.Text = "";
                    txt_NewPw.Text = "";
                    txt_ConfirmPw.Text = "";
                    MessageBox.Show("New Password Saved!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Incorrect Key Password!");
            }
        }

        private void btn_Back_Click(object sender, EventArgs e)
        {
            this.tabControl1.SelectedTab = tabPage1;
            get_password();
            txt_Password.Text = "";
        }
        private void txt_Password_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals(Convert.ToChar(13)))
            {
                btn_Login_Click(sender, e);
            }
            if (e.KeyChar.Equals(Convert.ToChar(27)))
            {
                Close();
            }

        }

        private void btn_Login_Click_1(object sender, EventArgs e)
        {
            if (txt_Password.Text.ToString() == password)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Incorrect Password!");
            }
        }

        private void btn_Cancel_Click_1(object sender, EventArgs e)
        {
            Close();
        }
    }
}
