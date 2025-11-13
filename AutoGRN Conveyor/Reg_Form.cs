using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoGRN_Conveyor
{
    public partial class Reg_Form : Form
    {
        private string original_vendor_name = "";
        public Reg_Form()
        {
            InitializeComponent();

        }
        public string click_source { get; set; }

        private void WriteToLogFile(string content)
        {
            using (StreamWriter sw = File.AppendText($".\\Log\\{System.DateTime.Today.ToString("yyyyMdd")}.txt"))
            {
                sw.WriteLine($"[{System.DateTime.Now}]:RegisterForm:{content}");
            }
        }
        private void Save_Vendor_Click(object sender, EventArgs e)
        {
            
            if (txt_VendorName.Text == "")
            {
                MessageBox.Show("Vendor Name cannot be empty", "Error!");
                WriteToLogFile("Vendor Name cannot be empty");
                return;
            }
            if (txt_ManufacturerName.Text == "")
            {
                MessageBox.Show("Manufacturer Name cannot be empty", "Error!");
                WriteToLogFile("Manufacturer Name cannot be empty");
                return;
            }
            if (txt_Spliter.Text == "")
            {
                MessageBox.Show("Spliter cannot be empty", "Error!");
                WriteToLogFile("Spliter cannot be empty");
                return;
            }
            if (txt_capturetime.Text == "")
            {
                MessageBox.Show("Capture Time cannot be empty", "Error!");
                WriteToLogFile("Capture Time cannot be empty");
                return;
            }
            if (txt_printtime.Text == "")
            {
                MessageBox.Show("Print Time cannot be empty", "Error!");
                WriteToLogFile("Print Time cannot be empty");
                return;
            }
            if (P_Index.Text == "")
            {
                MessageBox.Show("Please Select Part Number Index", "Error!");
                WriteToLogFile("Please Select Part Number Index");
                return;
            }
            if (Q_Index.Text == "")
            {
                MessageBox.Show("Please Select Quantity Index", "Error!");
                WriteToLogFile("Please Select Quantity Index");
                return;
            }
            if (M_Index.Text == "")
            {
                MessageBox.Show("Please Select Manufacturer Index", "Error!");
                WriteToLogFile("Please Select Manufacturer Index");
                return;
            }
            if (vendorCodeOrderCombobox.Text == "")
            {
                MessageBox.Show("Please Select Vendor Code Index", "Error!");
                WriteToLogFile("Please Select Vendor Code Index");
                return;
            }
            if (dateCode_index.Text == "")
            {
                MessageBox.Show("Please Select Date Code Index", "Error!");
                WriteToLogFile("Please Select Date Code Index");
                return;
            }
            if (lotCode.Text == "")
            {
                MessageBox.Show("Please Select Lot Code Index", "Error!");
                WriteToLogFile("Please Select Lot Code Index");
                return;
            }
            if (PONo_index.Text == "")
            {
                MessageBox.Show("Please Select PO No Index", "Error!");
                WriteToLogFile("Please Select PO No Index");
                return;
            }
            if (packingNo_index.Text == "")
            {
                MessageBox.Show("Please Select Packing No Index", "Error!");
                WriteToLogFile("Please Select Packing No Index");
                return;
            }
            if (expireDate_index.Text == "")
            {
                MessageBox.Show("Please Select Expire Date Index", "Error!");
                WriteToLogFile("Please Select Expire Date Index");
                return;
            }
            if (txt_Spliter.Text != "CR" && txt_Spliter.Text != "LF")
            {
                if (txt_Spliter.Text.Length != 1)
                {
                    MessageBox.Show("Spliter is only a character", "Error!");
                    WriteToLogFile("Spliter is only a character");
                    return;
                }
            }
            string ori_vd_name = originalVendorNameLabel.Text;
            string vd_name = txt_VendorName.Text;
            string manu_name = txt_ManufacturerName.Text;
            string vd_code = MEVendorCodeEntry.Text.ToUpper();
            string v_codetype = codeTypeCombobox.Text;
            string v_spliter = txt_Spliter.Text;
            string v_capturetime = txt_capturetime.Text;
            string v_printtime = txt_printtime.Text;
            string part_idx = P_Index.Text;
            string qty_idx = Q_Index.Text;
            string manu_idx = M_Index.Text;
            string v_vd_code_order = vendorCodeOrderCombobox.Text;
            string dateCode_idx = dateCode_index.Text;
            string lotCode_idx = lotCode_index.Text;
            string PONo_idx = PONo_index.Text;
            string packingNo_idx = packingNo_index.Text;
            string expireDate_idx = expireDate_index.Text;
            string p_start = txt_P_Start.Text;
            string q_start = txt_Q_Start.Text;
            string m_start = txt_M_Start.Text;
            string vendorCode_start = txt_vendorcode_start.Text;
            string dateCode_start = txt_datecodestart.Text;
            string lotcode_start = txt_lotcode_start.Text;
            string pono_start = txt_pono_start.Text;
            string packingNo_start = txt_packingno_start.Text;
            string expireDate_start = txt_expdate_start.Text;
            string p_end = txt_P_End.Text;
            string q_end = txt_Q_End.Text;
            string m_end = txt_M_End.Text;
            string vendorCode_end = txt_vendorcode_end.Text;
            string dateCode_end = txt_datecode_end.Text;
            string lotcode_end = txt_lotcode_end.Text;
            string pono_end = txt_pono_end.Text;
            string packingNo_end = txt_packingno_end.Text;
            string expireDate_end = txt_expdate_end.Text;
            string manualDateCode = MEDateCodeEntry.Text;
            string manualLotCode = MELotCodeEntry.Text;
            string manualPONo = MEPONoEntry.Text;
            string manualPackingNo = MEPackingNoEntry.Text;
            string manualExpireDate = MEExpireDateEntry.Text;
            string manualPartNo = MEPartNoEntry.Text;
            string manualQuantity = MEQuantityEntry.Text;
            string manualManufacturer = MEManufacturerEntry.Text;

            string vendor_combine = vd_name + "|" + vd_code + "|" + part_idx + "|" + p_start + "|" + qty_idx + "|" + q_start + "|" + manu_idx + "|" + m_start + "|" + v_spliter + "|" + v_capturetime + "|" + v_codetype + "|" + v_vd_code_order + "|" + vendorCode_start + "|" + v_printtime + "|" + dateCode_idx + "|" + dateCode_start + "|" + lotCode_idx + "|" + lotcode_start + "|" + PONo_idx + "|" + pono_start + "|" + packingNo_idx + "|" + packingNo_start + "|" + expireDate_idx + "|" + expireDate_start + "|" + p_end + "|" + q_end + "|" + m_end + "|" + vendorCode_end + "|" + dateCode_end + "|" + lotcode_end + "|" + pono_end + "|" + packingNo_end + "|" + expireDate_end + "|" + manualDateCode + "|" + manualLotCode + "|" + manualPONo + "|" + manualPackingNo + "|" + manualExpireDate + "|" + manualPartNo + "|" + manualQuantity + "|" + manualManufacturer + "|" + manu_name;

            string pathRoot = Path.Combine(@"C:\AutoGRN_config");
            FileStream fs = null;
            StreamReader fr = null;
            int vendor_ext = 0;
            fs = new FileStream(pathRoot + @"\vendor.txt", FileMode.Open, FileAccess.Read);
            fr = new StreamReader(fs);
            String s = null;
            s = fr.ReadLine();
            
            while (s != null)
            {
                string[] strsplit = s.Split('|');//Note:Make sure there is a space between name and password, if it is other characters, you can do the appropriate substitute for
                if (vd_name == strsplit[0])
                {
                    if (click_source == "register")
                    {
                        MessageBox.Show("Vendor Name already exist in current list.\nVendor Name: " + strsplit[0], "VENDOR EXIST");
                        WriteToLogFile("Vendor Name already exist in current list. Vendor Name: " + strsplit[0]);
                        fr.Close();
                        return;
                    }
                }
                
                if (click_source == "edit")
                {
                    //validation part
                    if (vd_name == strsplit[0] && strsplit[0] != ori_vd_name)
                    {
                        MessageBox.Show("Vendor Name already exist in current list.\nVendor Name: " + strsplit[0], "VENDOR EXIST");
                        WriteToLogFile("Vendor Name already exist in current list. Vendor Name: " + strsplit[0]);
                        fr.Close();
                        return;
                    }

                    // saving part
                    if (strsplit[0] == ori_vd_name)
                    {
                        fr.Close();
                        File.WriteAllText(pathRoot + @"\vendor.txt", File.ReadAllText(pathRoot + @"\vendor.txt").Replace(s, vendor_combine));
                        DialogResult = DialogResult.OK;
                        Close();
                        File.AppendAllText(pathRoot + @"\log.txt", $"{DateTime.Now.ToString()} Edit" + Environment.NewLine);
                        WriteToLogFile($"Edited Vendor: {vendor_combine}");
                        return;       
                    }

                }
                vendor_ext++;
                s = fr.ReadLine();
            }
            fr.Close();

            if (click_source == "register")
            {
                File.AppendAllText(pathRoot + @"\vendor.txt", vendor_combine + Environment.NewLine);
                File.AppendAllText(pathRoot + @"\log.txt", $"{DateTime.Now.ToString()} Register" + Environment.NewLine);
                WriteToLogFile($"Registered Vendor: {vendor_combine}");
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Cancel_Vendor_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Code_Type_Changed(object sender, EventArgs e)
        {
            if (codeTypeCombobox.Text == "BorgWarner")
            {
                txt_Spliter.Text = "";
                P_Index.Text = "1";
                txt_P_Start.Text = "1";
                txt_P_End.Text = "8";
                Q_Index.Text = "1";
                txt_Q_Start.Text = "25";
                txt_Q_End.Text = "29";
                M_Index.Text = "N/A";
                vendorCodeOrderCombobox.Text = "N/A";
                MEVendorCodeEntry.Text = "I1531";
                dateCode_index.Text = "N/A";
                lotCode_index.Text = "1";
                txt_lotcode_start.Text = "15";
                txt_lotcode_end.Text = "24";
                PONo_index.Text = "N/A";
                packingNo_index.Text = "N/A";
                expireDate_index.Text = "N/A";
                txt_Spliter.Text = "~";
            }
        }

        private void Only_Allow_Digit(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }

        }

        private void vendorCodeOrderCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vendorCodeOrderCombobox.Text == "N/A")
            {
                MEVendorCodeEntry.Enabled = true;
                txt_vendorcode_start.Text = "";
                txt_vendorcode_end.Text = "";
            }
            else
            {
                MEVendorCodeEntry.Enabled = false;
                MEVendorCodeEntry.Text = txt_VendorName.Text;
            }
        }

        private void dateCode_index_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dateCode_index.Text == "N/A")
            {
                MEDateCodeEntry.Enabled = true;
                txt_datecodestart.Text = "";
                txt_datecode_end.Text = "";
            }
            else
            {
                MEDateCodeEntry.Enabled = false;
                MEDateCodeEntry.Text = "";
            }
        }

        private void lotCode_index_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lotCode_index.Text == "N/A")
            {
                MELotCodeEntry.Enabled = true;
                txt_lotcode_start.Text = "";
                txt_lotcode_end.Text = "";
            }
            else
            {
                MELotCodeEntry.Enabled = false;
                MELotCodeEntry.Text = "";
            }
        }

        private void PONo_index_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PONo_index.Text == "N/A")
            {
                MEPONoEntry.Enabled = true;
                txt_pono_start.Text = "";
                txt_pono_end.Text = "";
            }
            else
            {
                MEPONoEntry.Enabled = false;
                MEPONoEntry.Text = "";
            }
        }

        private void packingNo_index_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (packingNo_index.Text == "N/A")
            {
                MEPackingNoEntry.Enabled = true;
                txt_packingno_start.Text = "";
                txt_packingno_end.Text = "";
            }
            else
            {
                MEPackingNoEntry.Enabled = false;
                MEPackingNoEntry.Text = "";
            }
        }

        private void expireDate_index_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (expireDate_index.Text == "N/A")
            {
                MEExpireDateEntry.Enabled = true;
                txt_expdate_start.Text = "";
                txt_expdate_end.Text = "";
            }
            else
            {
                MEExpireDateEntry.Enabled = false;
                MEExpireDateEntry.Text = "";
            }
        }

        private void P_Index_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (P_Index.Text == "N/A")
            {
                MEPartNoEntry.Enabled = true;
                txt_P_Start.Text = "";
                txt_P_End.Text = "";
            }
            else
            {
                MEPartNoEntry.Enabled = false;
                MEPartNoEntry.Text = "";
            }
        }

        private void Q_Index_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Q_Index.Text == "N/A")
            {
                MEQuantityEntry.Enabled = true;
                txt_Q_Start.Text = "";
                txt_Q_End.Text = "";
            }
            else
            {
                MEQuantityEntry.Enabled = false;
                MEQuantityEntry.Text = "";
            }
        }

        private void M_Index_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (M_Index.Text == "N/A")
            {
                MEManufacturerEntry.Enabled = true;
                txt_M_Start.Text = "";
                txt_M_End.Text = "";
            }
            else
            {
                MEManufacturerEntry.Enabled = false;
                MEManufacturerEntry.Text = "";
            }
        }
    }


}
