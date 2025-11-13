using Automation.BDaq;
using Basler.Pylon;
using Cognex.VisionPro;
using Cognex.VisionPro.ID;
using Cognex.VisionPro.ImageFile;
using LabelManager2;
using luvo.mes.ngmes4_dll;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace AutoGRN_Conveyor
{
    public partial class Form1 : Form
    {
        //GRN API
        CMES4DLL cl = null;
        bool isconnect = false;

        //result
        string[] result1;
        string[] result2;
        string[] result3;
        string[] result4;
        string[] result5;
        string[] si_return;
        string strTemp;

        //Error Status
        private bool gotError = false;

        //Register_form_opened
        private bool register_form_opened = false;

        //Scanner Serial Port
        private SerialPort com4Port;
        private Thread serialThread;
        private delegate void SetTextDeleg(string text);

        //Brady Printer and Codesoft
        private LppxLT.ILppxLT codeSoftApp;
        private string LppxLT_id = "LppxLT.ILppxLT";
        private Object codeSoftObject;
        private Object codeSoftObjectPersistent;
        private LabelManager2.Application labelApp;
        private Thread sendPrintJobThread;
        private Thread startPrintJobThread;
        private bool sendingJob = false;
        private bool startingJob = false;

        //Advantech PCIE 1730 I/O
        InstantDoCtrl doCtrl = new InstantDoCtrl(); //Initiate IDO controller to control relay output      
        InstantDiCtrl diCtrl = new InstantDiCtrl();//Initiate IDI controller to read sensor/printer input
        private Thread listenToInputThread;
        private string[] portZeroStates = new string[] { "0", "0", "0", "0", "0", "0", "0", "0" }; //Save PCIE Port 0 input states that connect to relay board
        private string previousSensorOneState = "0";

        //Cognex
        //First Thread Left Scanner
        private CogID firstLeftOneDScanner;
        private CogID firstLeftQRScanner;
        private CogID firstLeftDataMatrixScanner;
        private CogIDResults firstLeftOneDResults;
        private CogIDResults firstLeftDataMatrixResults;
        private CogIDResults firstLeftQRResults;
        //First Thread Right Scanner
        private CogID firstRightOneDScanner;
        private CogID firstRightQRScanner;
        private CogID firstRightDataMatrixScanner;
        private CogIDResults firstRightOneDResults;
        private CogIDResults firstRightDataMatrixResults;
        private CogIDResults firstRightQRResults;
        //First Thread Center Scanner
        private CogID firstCenterOneDScanner;
        private CogID firstCenterQRScanner;
        private CogID firstCenterDataMatrixScanner;
        private CogIDResults firstCenterOneDResults;
        private CogIDResults firstCenterDataMatrixResults;
        private CogIDResults firstCenterQRResults;
        //Second Thread Left Scanner
        private CogID secondLeftOneDScanner;
        private CogID secondLeftQRScanner;
        private CogID secondLeftDataMatrixScanner;
        private CogIDResults secondLeftOneDResults;
        private CogIDResults secondLeftDataMatrixResults;
        private CogIDResults secondLeftQRResults;
        //Second Thread Right Scanner
        private CogID secondRightOneDScanner;
        private CogID secondRightQRScanner;
        private CogID secondRightDataMatrixScanner;
        private CogIDResults secondRightOneDResults;
        private CogIDResults secondRightDataMatrixResults;
        private CogIDResults secondRightQRResults;
        //Second Thread Center Scanner
        private CogID secondCenterOneDScanner;
        private CogID secondCenterQRScanner;
        private CogID secondCenterDataMatrixScanner;
        private CogIDResults secondCenterOneDResults;
        private CogIDResults secondCenterDataMatrixResults;
        private CogIDResults secondCenterQRResults;

        //Lists for keeping Reel barcodes
        //List<string> oneDList = new List<string>();
        //List<string> dataMatrixList = new List<string>();
        //List<string> QRList = new List<string>();
        List<string> erpReturnList = new List<string>();
        List<string> awaitVerifyList = new List<string>();
        List<string> firstBarcodeList = new List<string>();
        List<string> secondBarcodeList = new List<string>();

        //Basler
        private static string centerCamera = "23191741";
        private static string leftCamera = "22560961";
        private static string rightCamera = "22560958";
        private Camera cCamera = new Camera(centerCamera);
        private Camera lCamera = new Camera(leftCamera);
        private Camera rCamera = new Camera(rightCamera);


        //Sensor Inputs
        private Thread firstSensorThread;
        private Thread secondSensorThread;
        private Thread thirdSensorThread;
        private Thread fourthSensorThread;

        //Save Barcode data and ERP data using delimiter (comma for multiple barcode, | for different Reel)
        string allERPData = "";
        string erpToSend = "";

        //Threads to capture and decode 
        private Thread captureDecodeFirstLeftThread;
        private Thread captureDecodeFirstRightThread;
        private Thread captureDecodeFirstCenterThread;
        private Thread captureDecodeSecondLeftThread;
        private Thread captureDecodeSecondRightThread;
        private Thread captureDecodeSecondCenterThread;
        private bool captureDecodeFirstLeftState = false;
        private bool captureDecodeFirstRightState = false;
        private bool captureDecodeFirstCenterState = false;
        private bool captureDecodeSecondLeftState = false;
        private bool captureDecodeSecondRightState = false;
        private bool captureDecodeSecondCenterState = false;
        private int threadNo = 1;
        private bool firstLeftGotResult = false;
        private bool firstRightGotResult = false;
        private bool firstCenterGotResult = false;
        private bool secondLeftGotResult = false;
        private bool secondRightGotResult = false;
        private bool secondCenterGotResult = false;

        //vendor info
        int delimiterCount = 3;

        //load settings (luvo, ngmes)
        int config_count = 0;
        int cfg_count = 0;

        //manual typed vendor information
        private string manual_part_no = "";
        private string manual_manufacturer = "";
        private string manual_quantity = "";
        private string manual_vendor_code = "";
        private string manual_date_code = "";
        private string manual_lot_code = "";
        private string manual_po_no = "";
        private string manual_packing_no = "";
        private string manual_expire_date = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            setting_config();
            load_scanner();
            load_printer();
            load_vendor();
            load_pcie();
            setupCognex();
            setupBasler();
            cognex_warmup();
            startMonitorSensorOne();
            startMonitorSensorTwo();
            //startMonitorSensorThree();
            //startMonitorSensorFour();
            string cc_val = cfg[cc], un_val = cfg[un], token_val = cfg[token];
            cl = new CMES4DLL();
            if (cl == null)
            {
                txt_rtnCMESDLL.Text = "Connection Failed";
            }
            else if (cl.Init(cc_val, un_val, token_val))
            {
                txt_rtnCMESDLL.Text = "Connected";
                txt_rtnCMESDLL.ForeColor = Color.Green;
                isconnect = true;
            }
            else
            {
                txt_rtnCMESDLL.Text = "Invalid Token";
            }
            onGreenTowerLight();
            WriteToLogFile("Start Program");
        }

        private void WriteToLogFile(string content)
        {
            using (StreamWriter sw = File.AppendText($".\\Log\\{System.DateTime.Today.ToString("yyyyMdd")}.txt"))
            {
                sw.WriteLine($"[{System.DateTime.Now}]:{content}");
            }
        }

        private void cognex_warmup()
        {
            //Load Image File into Cognex
            CogImageFile aImageFile = new CogImageFile();
            try
            {
                aImageFile.Open($"C:\\AutoGRN_config\\initialphoto\\left.bmp", CogImageFileModeConstants.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load image file " + ex);
                captureDecodeFirstLeftState = false;
                return;
            }
            //Load into CogImage8Grey for Barcode Detection
            CogImage8Grey aImage = (CogImage8Grey)aImageFile[0];

            aImageFile.Close();
            firstLeftQRResults = firstLeftQRScanner.Execute(aImage, null);
        }
        private void setting_config()
        {
            string pathRoot = Path.Combine(@"C:\AutoGRN_config");
            DirectoryInfo directoryinfo = new DirectoryInfo(pathRoot);
            string json_config = File.ReadAllText(pathRoot + @"\luvo\config.txt");

            var dictionary = JsonConvert.DeserializeObject<IDictionary>(json_config);
            foreach (DictionaryEntry entry in dictionary)
            {
                config[config_count] = Convert.ToString(entry.Value);
                config_count++;
            }
            config_count = 0;

            string json_cfg2 = File.ReadAllText(pathRoot + @"\ngmes4-dll.cfg");

            var dictionary2 = JsonConvert.DeserializeObject<IDictionary>(json_cfg2);
            foreach (DictionaryEntry entry in dictionary2)
            {
                cfg[cfg_count] = Convert.ToString(entry.Value);
                cfg_count++;
            }
            cfg_count = 0;

            txtStation.Text = config[station];
            txtProcess.Text = config[process];
            txtCC.Text = cfg[cc];
            txtUsername.Text = cfg[un];
            txtToken.Text = cfg[token];
            txtIP.Text = cfg[ip];
        }
        private void load_pcie()
        {
            initDO();
            diCtrl.LoadProfile("C:\\Advantech\\public\\NavigatorFramework\\profile.xml");
            diCtrl.SelectedDevice = new DeviceInformation(0);
        }
        public void initDO()
        {
            doCtrl.LoadProfile("C:\\Advantech\\public\\NavigatorFramework\\profile.xml");
            doCtrl.SelectedDevice = new DeviceInformation(0);
            if (!doCtrl.Initialized)
            {
                MessageBox.Show("No device be selected or device open failed!", "StaticDO");
                this.Close();
                return;
            }
        }
        public void startConveyor()
        {
            doCtrl.WriteBit(3, 6, 1);
        }

        private void stopConveyor()
        {
            doCtrl.WriteBit(3, 6, 0);
        }

        private void onRedTowerLight()
        {
            doCtrl.WriteBit(3, 3, 1);
            doCtrl.WriteBit(3, 2, 0);
            gotError = true;
        }
        private void onGreenTowerLight()
        {
            doCtrl.WriteBit(3, 3, 0);
            doCtrl.WriteBit(3, 2, 1);
            gotError = false;
        }

        private void offTowerLight()
        {
            doCtrl.WriteBit(3, 3, 0);
            doCtrl.WriteBit(3, 2, 0);
            gotError = false;
        }

        //Get input from PCIE-1730 Port 0 every second
        private void startMonitorSensorOne()
        {
            firstSensorThread = new Thread(new ThreadStart(monitorSensorOne));
            firstSensorThread.IsBackground = true;
            firstSensorThread.Start();
        }
        private void monitorSensorOne()
        {
            byte portData;
            ErrorCode value;
            while (true)
            {
                if (gotError == false)
                {
                    value = diCtrl.Read(0, out portData);
                    string inputBinary = Convert.ToString(portData, 2);
                    if (inputBinary.Length < 8)
                    {
                        inputBinary = inputBinary.PadLeft(8, '0');
                    }
                    for (int i = 0; i < inputBinary.Length; i++)
                    {
                        portZeroStates[i] = inputBinary[i].ToString();
                    }

                    if (portZeroStates[7] == "1")
                    {
                        if (previousSensorOneState == "0" && threadNo == 1)
                        {

                            Debug.WriteLine($"Thread No: {threadNo}");
                            if (v_capturetime == null)
                            {
                                MessageBox.Show("Please choose vendor.");
                                return;
                            }
                            Debug.WriteLine(v_capturetime);
                            Thread.Sleep(Int32.Parse(v_capturetime));
                            startCaptureDecodeFirst();
                            Thread.Sleep(100);

                            threadNo = 2;
                        }
                        else if (previousSensorOneState == "0" && threadNo == 2)
                        {
                            if (v_capturetime == null)
                            {
                                MessageBox.Show("Please choose vendor.");
                                return;
                            }
                            Thread.Sleep(Int32.Parse(v_capturetime));
                            startCaptureDecodeSecond();
                            Thread.Sleep(100);

                            //}
                            threadNo = 1;
                        }
                        previousSensorOneState = "1";
                    }
                    else
                    {
                        previousSensorOneState = "0";
                    }

                }
            }
        }
        private void startMonitorSensorTwo()
        {
            secondSensorThread = new Thread(new ThreadStart(monitorSensorTwo));
            secondSensorThread.IsBackground = true;
            secondSensorThread.Start();
        }
        private void monitorSensorTwo()
        {
            byte portData;
            ErrorCode value;

            while (true)
            {
                value = diCtrl.Read(0, out portData);
                string inputBinary = Convert.ToString(portData, 2);
                if (inputBinary.Length < 8)
                {
                    inputBinary = inputBinary.PadLeft(8, '0');
                }
                if (portZeroStates[6] == "1")
                {
                    if (!startingJob && v_printtime != null)
                    {
                        Thread.Sleep(Int32.Parse(v_printtime));
                        StartPrintJob();
                    }
                }
            }
        }

        private void startMonitorSensorThree()
        {
            thirdSensorThread = new Thread(new ThreadStart(monitorSensorThree));
            thirdSensorThread.IsBackground = true;
            thirdSensorThread.Start();
        }
        private void monitorSensorThree()
        {
            byte portData;
            ErrorCode value;

            while (true)
            {
                value = diCtrl.Read(0, out portData);
                string inputBinary = Convert.ToString(portData, 2);
                if (inputBinary.Length < 8)
                {
                    inputBinary = inputBinary.PadLeft(8, '0');
                }
                if (portZeroStates[5] == "1")
                {
                    if (!startingJob)
                    {
                        Thread.Sleep(Int32.Parse(v_printtime));
                        StartPrintJob();
                    }
                }
            }
        }

        private void startMonitorSensorFour()
        {
            fourthSensorThread = new Thread(new ThreadStart(monitorSensorFour));
            fourthSensorThread.IsBackground = true;
            fourthSensorThread.Start();
        }
        private void monitorSensorFour()
        {
            byte portData;
            ErrorCode value;
            while (true)
            {
                value = diCtrl.Read(0, out portData);
                string inputBinary = Convert.ToString(portData, 2);
                if (inputBinary.Length < 8)
                {
                    inputBinary = inputBinary.PadLeft(8, '0');
                }
                //fourthSensorValue.Invoke((MethodInvoker)delegate
                //{
                //    if (portZeroStates[4] == "1")
                //    {
                //        fourthSensorValue.Text = "On";
                //        listBox3.Invoke((MethodInvoker)delegate
                //        {
                //            listBox3.Items.Clear();
                //        });
                //        listBox4.Invoke((MethodInvoker)delegate
                //        {
                //            listBox4.Items.Clear();
                //            if (resultText.Length > 0)
                //            {
                //                listBox4.Items.Add("Out");
                //                listBox4.Items.Add(resultText);
                //            }
                //        });
                //    }
                //    else
                //    {
                //        fourthSensorValue.Text = "Off";
                //    }

                //});
                Thread.Sleep(500);
            }
        }
        private void load_printer()
        {
            if (labelApp == null)
            {
                labelApp = new LabelManager2.Application();
                labelApp.EnableEvents = true;
            }
            if (codeSoftApp != null)
                return;
            try
            {
                codeSoftObject = System.Runtime.InteropServices.Marshal.GetActiveObject(LppxLT_id);
                codeSoftApp = (LppxLT.ILppxLT)codeSoftObject;
            }
            catch (Exception)
            {
                try
                {
                    codeSoftObjectPersistent = new LppxLT.ILppxLT();
                    codeSoftObject = System.Runtime.InteropServices.Marshal.GetActiveObject(LppxLT_id);

                }
                catch (Exception e)
                {
                    codeSoftObject = null;
                    string szerror = e.Message.ToString();
                    MessageBox.Show(szerror);
                }
            }
        }
        private void SendPrintJob()
        {
            sendingJob = true;
            sendPrintJobThread = new Thread(new ThreadStart(RunSendJobThread));
            sendPrintJobThread.IsBackground = true;
            sendPrintJobThread.Start();
        }
        private void RunSendJobThread()
        {
            codeSoftApp.OpenDocument("C:\\Users\\Public\\Documents\\Teklynx\\CODESOFT 2015\\Samples\\Labels\\RG-Nationgate.lab", true);
            if (codeSoftApp == null)
            {
                return;
            }

            if (codeSoftApp.GetActiveDocument().Equals(""))
            {
                return;
            }
            //split the first erp return string in list here, then move the string to await verify list
            string[] print_job_values = erpReturnList[0].Split(',');
            if (print_job_values.Length <= 1)
            {
                MessageBox.Show("Error: ERP Return Invalid");
                return;
            }
            codeSoftApp.SetVariableValue("STOCK", print_job_values[0]);
            codeSoftApp.SetVariableValue("PART", print_job_values[1]);
            codeSoftApp.SetVariableValue("QTY", print_job_values[2]);
            codeSoftApp.SetVariableValue("RACK", print_job_values[3]);
            codeSoftApp.SetVariableValue("QR", "S" + print_job_values[0]);

            try
            {
                if (codeSoftApp.PrintDocument(1))
                {
                    logListbox.Invoke((MethodInvoker)delegate
                    {
                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] Sent Print Job: {erpReturnList[0]}");
                    });
                    erpReturnList.RemoveAt(0);
                    awaitVerifyList.Add("S" + print_job_values[0]);
                }
                else
                {
                    logListbox.Invoke((MethodInvoker)delegate
                    {
                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] Sent Print Job Fail: {erpReturnList[0]}");
                    });
                    stopConveyor();
                    onRedTowerLight();
                }
                sendingJob = false;
                WriteToLogFile($"Sent Print Job: S:{print_job_values[0]}, P:{print_job_values[1]}, Q:{print_job_values[2]}, R: {print_job_values[3]}");
            }
            catch (Exception e)
            {
                WriteToLogFile("Codesoft Error when sending print job." + e.ToString());
            }

        }
        private void StartPrintJob()
        {
            startPrintJobThread = new Thread(new ThreadStart(RunPrintJobThread));
            startPrintJobThread.IsBackground = true;
            startPrintJobThread.Start();
        }
        private void RunPrintJobThread()
        {
            startingJob = true;
            doCtrl.WriteBit(3, 0, 1);
            Thread.Sleep(1000);
            doCtrl.WriteBit(3, 0, 0);
            Thread.Sleep(1000); //Make sure no double send print start
            startingJob = false;
        }

        private void load_scanner()
        {
            serialThread = new Thread(new ThreadStart(RunSerialThread));
            serialThread.IsBackground = true;
            serialThread.Start();
        }

        private void RunSerialThread()
        {
            // Open COM4 port
            com4Port = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
            com4Port.DataReceived += new SerialDataReceivedEventHandler(Com4Port_DataReceived);
            com4Port.ReadTimeout = 3000;
            com4Port.WriteTimeout = 500;
            com4Port.Open();
        }

        private void Com4Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] data = new byte[1024];
            int bytesRead = com4Port.Read(data, 0, data.Length);
            string _message = Encoding.ASCII.GetString(data, 0, bytesRead);
            this.BeginInvoke(new SetTextDeleg(BackScanner_DataReceived), new object[] { _message });
            Thread.Sleep(1000);
        }

        Reg_Form Register_Form = new Reg_Form();
        private string previous_scanned = "";
        System.DateTime previous_setting_scan_time = System.DateTime.Now;
        private void BackScanner_DataReceived(string data)
        {

            string raw_string = data;
            Debug.WriteLine($"rfp {register_form_opened}");
            //skip double/triple scan
            if (register_form_opened == false)
            {

                if (raw_string == previous_scanned)
                {
                    return;
                }
                if (awaitVerifyList.Contains(raw_string.Trim()))
                {
                    if (awaitVerifyList[0] == raw_string.Trim())
                    {
                        logListbox.Invoke((MethodInvoker)delegate
                        {
                            logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] Verified: {raw_string}");
                        });
                        awaitVerifyList.RemoveAt(0);
                        previous_scanned = raw_string;
                        WriteToLogFile($"Verified: {raw_string}");
                        return;
                    }
                    else
                    {
                        logListbox.Invoke((MethodInvoker)delegate
                        {
                            logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] Verified: {raw_string}");
                        });
                        awaitVerifyList.Remove(raw_string.Trim());
                        WriteToLogFile($"Verified: {raw_string}");
                        logListbox.Invoke((MethodInvoker)delegate
                        {
                            logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] Error: {awaitVerifyList[0]} not verified!");
                        });
                        WriteToLogFile($"Error: {awaitVerifyList[0]} not verified!");
                        stopConveyor();
                        onRedTowerLight();
                        previous_scanned = raw_string;
                        MessageBox.Show($"{awaitVerifyList[0]} not verified!");
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                string message_without_alphanumeric = "";
                if ((System.DateTime.Now - previous_setting_scan_time).TotalSeconds >= 3)
                {
                    message_without_alphanumeric = Regex.Replace(raw_string, @"[\d-]", string.Empty);
                    message_without_alphanumeric = Regex.Replace(message_without_alphanumeric, @"[A-Za-z]+", string.Empty);
                    message_without_alphanumeric = message_without_alphanumeric.Replace("?", string.Empty);
                    var charMap = message_without_alphanumeric.Distinct().ToDictionary(c => c, c => message_without_alphanumeric.Count(s => s == c));
                    char most_occured = charMap.OrderByDescending(kvp => kvp.Value).First().Key;
                    //message_without_alphanumeric = message.Replace('\u2194', '|');
                    //byte charByte = Convert.ToByte(most_occured);
                    //string [] splitted_message = message.Split(message[6]);
                    //Console.WriteLine(most_occured);

                    string[] splitted_message = raw_string.Split(most_occured);
                    WriteToLogFile("Scan Reel Into Register/Edit Form.");
                    if (splitted_message[0].StartsWith("S") && splitted_message[0].Length == 13)
                    {
                        return;
                    }
                    else
                    {
                        Register_Form.registerScanListbox.Items.Clear();
                        if (Register_Form.codeTypeCombobox.Text == "BorgWarner")
                        {
                            if (raw_string.Length < 29)
                            {
                                return;
                            }
                            raw_string = raw_string.Substring(0, 29);
                            Register_Form.txt_ManufacturerName.Text = raw_string.Substring(8, 6);
                            Register_Form.registerScanListbox.Items.Add(raw_string);
                            Register_Form.registerScanListbox.Items.Add("Supplier: " + Register_Form.txt_ManufacturerName.Text);
                            WriteToLogFile("Detected BorgWarner Supplier: " + Register_Form.txt_ManufacturerName.Text);
                        }
                        else
                        {
                            for (int i = 0; i < splitted_message.Length; i++)
                            {
                                Register_Form.registerScanListbox.Items.Add($"{i + 1}: {splitted_message[i]}");
                                WriteToLogFile($"{i + 1}: {splitted_message[i]}");
                            }
                            if (most_occured == '\r')
                            {
                                Register_Form.txt_Spliter.Text = "CR";
                            }
                            else if (most_occured == '\n')
                            {
                                Register_Form.txt_Spliter.Text = "LF";
                            }
                            else
                            {
                                Register_Form.txt_Spliter.Text = most_occured.ToString();
                            }
                            previous_setting_scan_time = System.DateTime.Now;

                        }

                    }
                }
            }

        }

        private void load_vendor()
        {
            FileStream fs = null;
            StreamReader fr = null;
            Dictionary<string, string> vendor_name = new Dictionary<string, string>();
            vendor_name.Add("", "");
            vendor_dtl_count = 0;
            try
            {
                string pathRoot = Path.Combine(@"C:\AutoGRN_config");
                fs = new FileStream(pathRoot + @"\vendor.txt", FileMode.Open, FileAccess.Read);
                fr = new StreamReader(fs);
                String s = null;
                s = fr.ReadLine();

                while (s != null)
                {

                    string[] strsplit = s.Split('|');//Note:Make sure there is a space between name and password, if it is other characters, you can do the appropriate substitute for
                    //Debug.WriteLine(strsplit[24].ToString());
                    if (strsplit[1] == "")
                    {
                        strsplit[1] = strsplit[0];
                    }
                    vendor_name.Add(strsplit[0], strsplit[0]);
                    vendor_name_dtl[vendor_dtl_count] = strsplit[0];
                    vendor_code_dtl[vendor_dtl_count] = strsplit[1];

                    Debug.WriteLine(manual_vendor_code);
                    vendor_part_dtl[vendor_dtl_count] = strsplit[2];
                    vendor_qty_dtl[vendor_dtl_count] = strsplit[4];
                    vendor_manu_dtl[vendor_dtl_count] = strsplit[6];
                    if (strsplit[8] == "CR")
                    {
                        vendor_spliter[vendor_dtl_count] = "\r";
                    }
                    else if (strsplit[8] == "LF")
                    {
                        vendor_spliter[vendor_dtl_count] = "\n";
                    }
                    else
                    {
                        vendor_spliter[vendor_dtl_count] = strsplit[8];
                    }
                    vendor_capturetime[vendor_dtl_count] = strsplit[9];
                    vendor_printtime[vendor_dtl_count] = strsplit[13];
                    vendor_codetype_dtl[vendor_dtl_count] = strsplit[10];
                    part_start_dtl[vendor_dtl_count] = strsplit[3];
                    qty_start_dtl[vendor_dtl_count] = strsplit[5];
                    manu_start_dtl[vendor_dtl_count] = strsplit[7];
                    vendor_code_order_dtl[vendor_dtl_count] = strsplit[11];
                    code_order_start_dtl[vendor_dtl_count] = strsplit[12];
                    vendor_datecode_dtl[vendor_dtl_count] = strsplit[14];
                    datecode_start_dtl[vendor_dtl_count] = strsplit[15];
                    vendor_lotcode_dtl[vendor_dtl_count] = strsplit[16];
                    lotcode_start_dtl[vendor_dtl_count] = strsplit[17];
                    vendor_pono_dtl[vendor_dtl_count] = strsplit[18];
                    pono_start_dtl[vendor_dtl_count] = strsplit[19];
                    vendor_packingno_dtl[vendor_dtl_count] = strsplit[20];
                    packingno_start_dtl[vendor_dtl_count] = strsplit[21];
                    vendor_expiredate_dtl[vendor_dtl_count] = strsplit[22];
                    expiredate_start_dtl[vendor_dtl_count] = strsplit[23];
                    part_end_dtl[vendor_dtl_count] = strsplit[24];
                    qty_end_dtl[vendor_dtl_count] = strsplit[25];
                    manu_end_dtl[vendor_dtl_count] = strsplit[26];
                    code_order_end_dtl[vendor_dtl_count] = strsplit[27];
                    datecode_end_dtl[vendor_dtl_count] = strsplit[28];
                    lotcode_end_dtl[vendor_dtl_count] = strsplit[29];
                    pono_end_dtl[vendor_dtl_count] = strsplit[30];
                    packingno_end_dtl[vendor_dtl_count] = strsplit[31];
                    expiredate_end_dtl[vendor_dtl_count] = strsplit[32];

                    manual_date_code_dtl[vendor_dtl_count] = strsplit[33];
                    manual_lot_code_dtl[vendor_dtl_count] = strsplit[34];
                    manual_po_no_dtl[vendor_dtl_count] = strsplit[35];
                    manual_packing_no_dtl[vendor_dtl_count] = strsplit[36];
                    manual_expire_date_dtl[vendor_dtl_count] = strsplit[37];

                    manual_part_no_dtl[vendor_dtl_count] = strsplit[38];
                    manual_quantity_dtl[vendor_dtl_count] = strsplit[39];
                    manual_manufacturer_dtl[vendor_dtl_count] = strsplit[40];

                    manufacturer_name_dtl[vendor_dtl_count] = strsplit[41];


                    vendor_dtl_count++;
                    s = fr.ReadLine();
                }
                fr.Close();
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(e.ToString());
                return;
            }
            vendor_dropdown.DataSource = new BindingSource(vendor_name, null); ;
            vendor_dropdown.ValueMember = "Key"; //bind username
            vendor_dropdown.DisplayMember = "Value";//bind passwork
            homeVendorCombobox.DataSource = new BindingSource(vendor_name, null); ;
            homeVendorCombobox.ValueMember = "Key"; //bind username
            homeVendorCombobox.DisplayMember = "Value";//bind passwork
        }

        private void setupCognex()
        {
            // First Thread - Left
            // Create a CogID for Barcode Scanner
            firstLeftOneDScanner = new CogID();
            //Set Max number of code to find
            firstLeftOneDScanner.NumToFind = 15;
            //Set UTF-8 to include Chinese Character
            firstLeftOneDScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            firstLeftOneDScanner.DisableAll2DCodes();
            // Create a CogID for data matrix Scanner
            firstLeftDataMatrixScanner = new CogID();
            firstLeftDataMatrixScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            firstLeftDataMatrixScanner.NumToFind = 15;
            firstLeftDataMatrixScanner.DisableAllCodes();
            firstLeftDataMatrixScanner.DataMatrix.Enabled = true;
            // Create a CogID for QR Scanner
            firstLeftQRScanner = new CogID();
            firstLeftQRScanner.NumToFind = 15;
            firstLeftQRScanner.DisableAllCodes();
            firstLeftQRScanner.QRCode.Enabled = true;
            firstLeftQRScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;

            // First Thread - Right
            // Create a CogID for Barcode Scanner
            firstRightOneDScanner = new CogID();
            //Set Max number od code to find
            firstRightOneDScanner.NumToFind = 15;
            //Set UTF-8 to include Chinese Character
            firstRightOneDScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            firstRightOneDScanner.DisableAll2DCodes();
            // Create a CogID for data matrix Scanner
            firstRightDataMatrixScanner = new CogID();
            firstRightDataMatrixScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            firstRightDataMatrixScanner.NumToFind = 15;
            firstRightDataMatrixScanner.DisableAllCodes();
            firstRightDataMatrixScanner.DataMatrix.Enabled = true;
            // Create a CogID for QR Scanner
            firstRightQRScanner = new CogID();
            firstRightQRScanner.NumToFind = 15;
            firstRightQRScanner.DisableAllCodes();
            firstRightQRScanner.QRCode.Enabled = true;
            firstRightQRScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;

            // First Thread - Center
            // Create a CogID for Barcode Scanner
            firstCenterOneDScanner = new CogID();
            //Set Max number od code to find
            firstCenterOneDScanner.NumToFind = 15;
            //Set UTF-8 to include Chinese Character
            firstCenterOneDScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            firstCenterOneDScanner.DisableAll2DCodes();
            // Create a CogID for data matrix Scanner
            firstCenterDataMatrixScanner = new CogID();
            firstCenterDataMatrixScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            firstCenterDataMatrixScanner.NumToFind = 15;
            firstCenterDataMatrixScanner.DisableAllCodes();
            firstCenterDataMatrixScanner.DataMatrix.Enabled = true;
            // Create a CogID for QR Scanner
            firstCenterQRScanner = new CogID();
            firstCenterQRScanner.NumToFind = 15;
            firstCenterQRScanner.DisableAllCodes();
            firstCenterQRScanner.QRCode.Enabled = true;
            firstCenterQRScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;

            // Second Thread - Left
            // Create a CogID for Barcode Scanner
            secondLeftOneDScanner = new CogID();
            //Set Max number od code to find
            secondLeftOneDScanner.NumToFind = 15;
            //Set UTF-8 to include Chinese Character
            secondLeftOneDScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            
            
            // Create a CogID for data matrix Scanner
            secondLeftDataMatrixScanner = new CogID();
            secondLeftDataMatrixScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            secondLeftDataMatrixScanner.NumToFind = 15;
            secondLeftDataMatrixScanner.DisableAllCodes();
            secondLeftDataMatrixScanner.DataMatrix.Enabled = true;
            secondLeftOneDScanner.DisableAll2DCodes();
            // Create a CogID for QR Scanner
            secondLeftQRScanner = new CogID();
            secondLeftQRScanner.NumToFind = 15;
            secondLeftQRScanner.DisableAllCodes();
            secondLeftQRScanner.QRCode.Enabled = true;
            secondLeftQRScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;

            // Second Thread - Right
            // Create a CogID for Barcode Scanner
            secondRightOneDScanner = new CogID();
            //Set Max number od code to find
            secondRightOneDScanner.NumToFind = 15;            
            //Set UTF-8 to include Chinese Character
            secondRightOneDScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            secondRightOneDScanner.DisableAll2DCodes();
            // Create a CogID for data matrix Scanner
            secondRightDataMatrixScanner = new CogID();
            secondRightDataMatrixScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            secondRightDataMatrixScanner.NumToFind = 15;
            secondRightDataMatrixScanner.DisableAllCodes();
            secondRightDataMatrixScanner.DataMatrix.Enabled = true;
            // Create a CogID for QR Scanner
            secondRightQRScanner = new CogID();
            secondRightQRScanner.NumToFind = 15;
            secondRightQRScanner.DisableAllCodes();
            secondRightQRScanner.QRCode.Enabled = true;
            secondRightQRScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;

            // Second Thread - Center
            // Create a CogID for Barcode Scanner
            secondCenterOneDScanner = new CogID();
            //Set Max number od code to find
            secondCenterOneDScanner.NumToFind = 15;
            //Set UTF-8 to include Chinese Character
            secondCenterOneDScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            secondCenterOneDScanner.DisableAll2DCodes();
            // Create a CogID for data matrix Scanner
            secondCenterDataMatrixScanner = new CogID();
            secondCenterDataMatrixScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
            secondCenterDataMatrixScanner.NumToFind = 15;
            secondCenterDataMatrixScanner.DisableAllCodes();
            secondCenterDataMatrixScanner.DataMatrix.Enabled = true;
            // Create a CogID for QR Scanner
            secondCenterQRScanner = new CogID();
            secondCenterQRScanner.NumToFind = 15;
            secondCenterQRScanner.DisableAllCodes();
            secondCenterQRScanner.QRCode.Enabled = true;
            secondCenterQRScanner.DecodedStringCodePage = CogIDCodePageConstants.UTF8;
        }

        private void setupBasler()
        {
            // Set the acquisition mode to free running continuous acquisition when the camera is opened.
            lCamera.CameraOpened += Configuration.AcquireContinuous;

            // Open the connection to the camera device.
            lCamera.Open();

            // The parameter MaxNumBuffer can be used to control the amount of buffers
            // allocated for grabbing. The default value of this parameter is 10.
            lCamera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(30);
            lCamera.Parameters[PLCamera.ExposureTime].SetValue(200);

            //right camera
            rCamera.CameraOpened += Configuration.AcquireContinuous;
            rCamera.Open();
            rCamera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(30);
            rCamera.Parameters[PLCamera.ExposureTime].SetValue(150);

            //center camera
            cCamera.CameraOpened += Configuration.AcquireContinuous;
            cCamera.Open();
            cCamera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(30);
            cCamera.Parameters[PLCamera.ExposureTime].SetValue(800);
        }

        private void startCaptureDecodeFirst()
        {
            firstBarcodeList.Clear();
            firstLeftGotResult = false;
            firstRightGotResult = false;
            firstCenterGotResult = false;

            captureDecodeFirstLeftState = true;
            captureDecodeFirstLeftThread = new Thread(new ThreadStart(captureDecodeFirstLeft));
            captureDecodeFirstLeftThread.IsBackground = true;
            captureDecodeFirstLeftThread.Start();
            captureDecodeFirstRightState = true;
            captureDecodeFirstRightThread = new Thread(new ThreadStart(captureDecodeFirstRight));
            captureDecodeFirstRightThread.IsBackground = true;
            captureDecodeFirstRightThread.Start();
            captureDecodeFirstCenterState = true;
            captureDecodeFirstCenterThread = new Thread(new ThreadStart(captureDecodeFirstCenter));
            captureDecodeFirstCenterThread.IsBackground = true;
            captureDecodeFirstCenterThread.Start();
        }
        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        private void captureDecodeFirstLeft()
        {
            string scanned_vc, result, pn, qty, mpn;
            string profile = cfg[cc], sta = config[station];
            Debug.WriteLine("Thread 1 Left Start");
            // Start grabbing.
            lCamera.StreamGrabber.Start();
            // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
            IGrabResult grabResult = lCamera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            lCamera.StreamGrabber.Stop();
            using (grabResult)
            {
                // Image grabbed successfully?
                if (grabResult.GrabSucceeded)
                {
                    //Save Image
                    ImagePersistence.Save(ImageFileFormat.Bmp, $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-left.bmp", grabResult);
                }
                else
                {
                    Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                }
            }

            using (Bitmap bitmap = new Bitmap($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-left.bmp"))
            {
                // Define the new size
                int newWidth = bitmap.Width / 2;
                int newHeight = bitmap.Height / 2;

                // Resize the bitmap
                using (Bitmap resizedBitmap = ResizeBitmap(bitmap, newWidth, newHeight))
                {
                    // Save the resized bitmap to a file stream
                    using (FileStream fs = new FileStream("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\first-left.bmp", FileMode.Create))
                    {
                        resizedBitmap.Save(fs, ImageFormat.Bmp);
                    }
                }

            }
                File.Delete($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-left.bmp");
                File.Move("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\first-left.bmp", $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-left.bmp");
            Debug.WriteLine("Thread 1 Left Pic Saved");
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.ImageLocation = $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-left.bmp";
            
            //Load Image File into Cognex
            CogImageFile aImageFile = new CogImageFile();
            try
            {
                aImageFile.Open($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-left.bmp", CogImageFileModeConstants.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load image file " + ex);
                captureDecodeFirstLeftState = false;
                return;
            }
            //Load into CogImage8Grey for Barcode Detection
            CogImage8Grey aImage = (CogImage8Grey)aImageFile[0];

            aImageFile.Close();

            if (v_codetype == "QR")
            {
                //Check QR Code
                firstLeftQRResults = firstLeftQRScanner.Execute(aImage, null);
                for (int QRIndex = 0; QRIndex < firstLeftQRResults.Count; QRIndex++)
                {
                    int count = firstLeftQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        Debug.WriteLine(firstLeftQRResults[QRIndex].DecodedData.DecodedString);
                        if (!firstBarcodeList.Contains(firstLeftQRResults[QRIndex].DecodedData.DecodedString))
                        {
                            firstBarcodeList.Add(firstLeftQRResults[QRIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";

                            //Split string and get info
                            string[] split_scaned = firstLeftQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");

                                    return;
                                }
                            }

                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");

                                    return;
                                }
                            }


                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");

                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");

                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");

                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");

                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }

                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");

                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {


                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (QRIndex == firstLeftQRResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");

                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        firstLeftGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");

                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }

                            captureDecodeFirstLeftState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeFirstLeftState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "Datamatrix")
            {
                //Check Data Matrix
                firstLeftDataMatrixResults = firstLeftDataMatrixScanner.Execute(aImage, null);
                for (int dataMatrixIndex = 0; dataMatrixIndex < firstLeftDataMatrixResults.Count; dataMatrixIndex++)
                {
                    int count = firstLeftDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        if (!firstBarcodeList.Contains(firstLeftDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString))
                        {
                            firstBarcodeList.Add(firstLeftDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString);

                            string[] split_scaned = firstLeftDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }
                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }


                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }
                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (dataMatrixIndex == firstLeftDataMatrixResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        firstLeftGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }
                            captureDecodeFirstLeftState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeFirstLeftState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "BorgWarner")
            {
                //Check BorgWarner 1D Code
                firstLeftOneDResults = firstLeftOneDScanner.Execute(aImage, null);
                for (int OneDIndex = 0; OneDIndex < firstLeftOneDResults.Count; OneDIndex++)
                {
                    string decoded_content = firstLeftOneDResults[OneDIndex].DecodedData.DecodedString;
                    if (decoded_content.Length < 29)
                    {
                        continue;
                    }
                    else
                    {
                        decoded_content = decoded_content.Substring(0, 29);
                        //get details and break loop
                        if (p_id == 999)
                        {
                            pn = manual_part_no;
                        }
                        else
                        {
                            try
                            {
                                if (p_end == "")
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1);
                                }
                                else
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                }
                                if (pn.Length < 8 && pn.StartsWith("9"))
                                {
                                    pn = pn.PadLeft(8, '0');
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Part Number setting.");
                                WriteToLogFile("Error: Please check Part Number setting.");

                                return;
                            }
                        }
                        if (q_id == 999)
                        {
                            qty = manual_quantity;
                            if (short.Parse(qty) == 0)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                return;
                            }
                        }
                        else
                        {
                            try
                            {
                                if (q_end == "")
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1);
                                }
                                else
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                }
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                                qty = qty.TrimStart('0');
                                qty = qty.TrimEnd();
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting.");
                                WriteToLogFile("Error: Please check Quantity setting.");

                                return;
                            }
                        }
                        if (m_id == 999)
                        {
                            mpn = manual_manufacturer;
                        }
                        else
                        {
                            try
                            {
                                if (m_end == "")
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1);
                                }
                                else
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Manufacturer setting.");
                                WriteToLogFile("Error: Please check Manufacturer setting.");

                                return;
                            }
                        }
                        if (vc_id == 999)
                        {
                            scanned_vc = manual_vendor_code;
                        }
                        else
                        {
                            try
                            {
                                if (vc_end == "")
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1);
                                }
                                else
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Vendor Code setting.");
                                WriteToLogFile("Error: Please check Vendor Code setting.");

                                return;
                            }
                        }
                        if (vdc_id == 999)
                        {
                            mdt = manual_date_code;
                        }
                        else
                        {
                            try
                            {
                                if (vdc_end == "")
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1);
                                }
                                else
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Date Code setting.");
                                WriteToLogFile("Error: Please check Date Code setting.");

                                return;
                            }

                        }
                        if (vlc_id == 999)
                        {
                            lot = manual_lot_code;
                        }
                        else
                        {
                            try
                            {
                                if (vlc_end == "")
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1);
                                }
                                else
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Lot Code setting.");
                                WriteToLogFile("Error: Please check Lot Code setting.");

                                return;
                            }

                        }
                        if (vpono_id == 999)
                        {
                            po = manual_po_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpono_end == "")
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1);
                                }
                                else
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check PO No setting.");
                                WriteToLogFile("Error: Please check PO No setting.");
                                return;
                            }

                        }
                        if (vpackingno_id == 999)
                        {
                            packid = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpackingno_end == "")
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1);
                                }
                                else
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Packing No setting.");
                                WriteToLogFile("Error: Please check Packing No setting.");
                                return;
                            }
                        }
                        if (ved_id == 999)
                        {
                            expdt = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (ved_end == "")
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1);
                                }
                                else
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Expire Date setting.");
                                WriteToLogFile("Error: Please check Expire Date setting.");
                                return;
                            }
                        }
                        logListbox.Invoke((MethodInvoker)delegate
                        {
                            logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                        });
                        WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");

                        if (cl != null && isconnect)
                        {
                            result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                            var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                            result = jsdat.d.ToString();
                            if (result != "")
                            {


                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                });
                                WriteToLogFile($"ERP Return:{result}");

                                if (result.Contains("INVALID") && (OneDIndex == firstLeftOneDResults.Count - 1))
                                {
                                    onRedTowerLight();
                                    stopConveyor();
                                    MessageBox.Show(result);
                                    WriteToLogFile($"{result}");

                                    return;
                                }
                                else if (result.Contains("INVALID"))
                                {
                                    continue;
                                }
                                else
                                {
                                    erpReturnList.Add(result);
                                    SendPrintJob();
                                    firstLeftGotResult = true;
                                }
                            }
                            else
                            {
                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                });
                                WriteToLogFile($"ERP Return: No Result");

                                //stop conveyor if no ERP return here
                                //light up red if no ERP return here
                            }
                        }
                        captureDecodeFirstLeftState = false;
                        break;
                    }
                    
                }
            }


            if (firstLeftGotResult == false && firstCenterGotResult == false && firstRightGotResult == false && captureDecodeFirstRightThread.IsAlive == false && captureDecodeFirstCenterThread.IsAlive == false)
            {
                onRedTowerLight();
                stopConveyor();
                MessageBox.Show("No result on last reel.");
                WriteToLogFile("No result on last reel.");
            }
        }
        private void captureDecodeFirstRight()
        {
            string scanned_vc, result, pn, qty, mpn;
            string profile = cfg[cc], sta = config[station];
            Debug.WriteLine("Thread 1 Right Start");
            // Start grabbing.
            rCamera.StreamGrabber.Start();
            // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
            IGrabResult grabResult = rCamera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            rCamera.StreamGrabber.Stop();
            using (grabResult)
            {
                // Image grabbed successfully?
                if (grabResult.GrabSucceeded)
                {
                    //Save Image
                    ImagePersistence.Save(ImageFileFormat.Bmp, $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-right.bmp", grabResult);
                }
                else
                {
                    Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                }
            }

            using (Bitmap bitmap = new Bitmap($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-right.bmp"))
            {
                // Define the new size
                int newWidth = bitmap.Width / 2;
                int newHeight = bitmap.Height / 2;

                // Resize the bitmap
                using (Bitmap resizedBitmap = ResizeBitmap(bitmap, newWidth, newHeight))
                {
                    // Save the resized bitmap to a file stream
                    using (FileStream fs = new FileStream("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\first-right.bmp", FileMode.Create))
                    {
                        resizedBitmap.Save(fs, ImageFormat.Bmp);
                    }
                }

            }
                File.Delete($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-right.bmp");
                File.Move("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\first-right.bmp", $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-right.bmp");
            Debug.WriteLine("Thread 1 Right Pic Saved");
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.ImageLocation = $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-right.bmp";
            //Load Image File into Cognex
            CogImageFile aImageFile = new CogImageFile();
            try
            {
                aImageFile.Open($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-right.bmp", CogImageFileModeConstants.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load image file " + ex);
                captureDecodeFirstRightState = false;
                return;
            }
            //Load into CogImage8Grey for Barcode Detection
            CogImage8Grey aImage = (CogImage8Grey)aImageFile[0];

            aImageFile.Close();
            if (v_codetype == "QR")
            {
                //Check QR Code
                firstRightQRResults = firstRightQRScanner.Execute(aImage, null);
                for (int QRIndex = 0; QRIndex < firstRightQRResults.Count; QRIndex++)
                {
                    int count = firstRightQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        Debug.WriteLine(firstRightQRResults[QRIndex].DecodedData.DecodedString);
                        if (!firstBarcodeList.Contains(firstRightQRResults[QRIndex].DecodedData.DecodedString))
                        {
                            firstBarcodeList.Add(firstRightQRResults[QRIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            //Split string and get info
                            string[] split_scaned = firstRightQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }
                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }


                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = "";
                            }
                            else
                            {
                                if (ved_end == "")
                                {
                                    expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                }
                                else
                                {
                                    expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                }

                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                Debug.WriteLine(result);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (QRIndex == firstRightQRResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        firstRightGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }
                            captureDecodeFirstRightState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeFirstRightState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "Datamatrix")
            {
                //Check Data Matrix
                firstRightDataMatrixResults = firstRightDataMatrixScanner.Execute(aImage, null);
                for (int dataMatrixIndex = 0; dataMatrixIndex < firstRightDataMatrixResults.Count; dataMatrixIndex++)
                {
                    int count = firstRightDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        if (!firstBarcodeList.Contains(firstRightDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString))
                        {
                            firstBarcodeList.Add(firstRightDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            string[] split_scaned = firstRightDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }

                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }

                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }

                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (dataMatrixIndex == firstRightDataMatrixResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        firstRightGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }
                            captureDecodeFirstRightState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeFirstRightState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "BorgWarner")
            {
                //Check BorgWarner 1D Code
                firstRightOneDResults = firstRightOneDScanner.Execute(aImage, null);
                Debug.WriteLine("Result Count: " + firstRightOneDResults.Count);
                for (int OneDIndex = 0; OneDIndex < firstRightOneDResults.Count; OneDIndex++)
                {
                    string decoded_content = firstRightOneDResults[OneDIndex].DecodedData.DecodedString;
                    Debug.WriteLine(decoded_content);
                    if (decoded_content.Length < 29)
                    {
                        continue;
                    }
                    else
                    {
                        decoded_content = decoded_content.Substring(0, 29);
                        //get details and break loop
                        if (p_id == 999)
                        {
                            pn = manual_part_no;
                        }
                        else
                        {
                            try
                            {
                                if (p_end == "")
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1);
                                }
                                else
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                }
                                if (pn.Length < 8 && pn.StartsWith("9"))
                                {
                                    pn = pn.PadLeft(8, '0');
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Part Number setting.");
                                WriteToLogFile("Error: Please check Part Number setting.");

                                return;
                            }
                        }
                        if (q_id == 999)
                        {
                            qty = manual_quantity;
                            if (short.Parse(qty) == 0)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                return;
                            }
                        }
                        else
                        {
                            try
                            {
                                if (q_end == "")
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1);
                                }
                                else
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                }
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                                qty = qty.TrimStart('0');
                                qty = qty.TrimEnd();
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting.");
                                WriteToLogFile("Error: Please check Quantity setting.");

                                return;
                            }
                        }
                        if (m_id == 999)
                        {
                            mpn = manual_manufacturer;
                        }
                        else
                        {
                            try
                            {
                                if (m_end == "")
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1);
                                }
                                else
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Manufacturer setting.");
                                WriteToLogFile("Error: Please check Manufacturer setting.");

                                return;
                            }
                        }
                        if (vc_id == 999)
                        {
                            scanned_vc = manual_vendor_code;
                        }
                        else
                        {
                            try
                            {
                                if (vc_end == "")
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1);
                                }
                                else
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Vendor Code setting.");
                                WriteToLogFile("Error: Please check Vendor Code setting.");

                                return;
                            }
                        }
                        if (vdc_id == 999)
                        {
                            mdt = manual_date_code;
                        }
                        else
                        {
                            try
                            {
                                if (vdc_end == "")
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1);
                                }
                                else
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Date Code setting.");
                                WriteToLogFile("Error: Please check Date Code setting.");

                                return;
                            }

                        }
                        if (vlc_id == 999)
                        {
                            lot = manual_lot_code;
                        }
                        else
                        {
                            try
                            {
                                if (vlc_end == "")
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1);
                                }
                                else
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Lot Code setting.");
                                WriteToLogFile("Error: Please check Lot Code setting.");

                                return;
                            }

                        }
                        if (vpono_id == 999)
                        {
                            po = manual_po_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpono_end == "")
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1);
                                }
                                else
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check PO No setting.");
                                WriteToLogFile("Error: Please check PO No setting.");
                                return;
                            }

                        }
                        if (vpackingno_id == 999)
                        {
                            packid = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpackingno_end == "")
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1);
                                }
                                else
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Packing No setting.");
                                WriteToLogFile("Error: Please check Packing No setting.");
                                return;
                            }
                        }
                        if (ved_id == 999)
                        {
                            expdt = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (ved_end == "")
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1);
                                }
                                else
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Expire Date setting.");
                                WriteToLogFile("Error: Please check Expire Date setting.");
                                return;
                            }
                        }
                        logListbox.Invoke((MethodInvoker)delegate
                        {
                            logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                        });
                        WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");

                        if (cl != null && isconnect)
                        {
                            result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                            var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                            result = jsdat.d.ToString();
                            if (result != "")
                            {


                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                });
                                WriteToLogFile($"ERP Return:{result}");

                                if (result.Contains("INVALID") && (OneDIndex == firstRightOneDResults.Count - 1))
                                {
                                    onRedTowerLight();
                                    stopConveyor();
                                    MessageBox.Show(result);
                                    WriteToLogFile($"{result}");

                                    return;
                                }
                                else if (result.Contains("INVALID"))
                                {
                                    continue;
                                }
                                else
                                {
                                    erpReturnList.Add(result);
                                    SendPrintJob();
                                    firstRightGotResult = true;
                                }
                            }
                            else
                            {
                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                });
                                WriteToLogFile($"ERP Return: No Result");

                                //stop conveyor if no ERP return here
                                //light up red if no ERP return here
                            }
                        }
                        captureDecodeFirstRightState = false;
                        break;
                    }

                }
            }
            if (firstLeftGotResult == false && firstCenterGotResult == false && firstRightGotResult == false && captureDecodeFirstLeftThread.IsAlive == false && captureDecodeFirstCenterThread.IsAlive == false)
            {
                onRedTowerLight();
                stopConveyor();
                MessageBox.Show("No result on last reel.");
                WriteToLogFile("No result on last reel.");
            }

        }
        static Bitmap ResizeBitmap(Bitmap bitmap, int newWidth, int newHeight)
        {
            Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(resizedBitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, newWidth, newHeight);
            }
            return resizedBitmap;
        }
        private void captureDecodeFirstCenter()
        {
            string scanned_vc, result, pn, qty, mpn;
            string profile = cfg[cc], sta = config[station];
            Debug.WriteLine("Thread 1 Center Start");
            // Start grabbing.
            cCamera.StreamGrabber.Start();
            // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
            IGrabResult grabResult = cCamera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            cCamera.StreamGrabber.Stop();
            using (grabResult)
            {
                // Image grabbed successfully?
                if (grabResult.GrabSucceeded)
                {
                    //Save Image
                    ImagePersistence.Save(ImageFileFormat.Bmp, $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-center.bmp", grabResult);
                }
                else
                {
                    Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                }
            }

            using (Bitmap bitmap = new Bitmap($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-center.bmp"))
            {
                // Define the new size
                int newWidth = bitmap.Width / 2;
                int newHeight = bitmap.Height / 2;

                // Resize the bitmap
                using (Bitmap resizedBitmap = ResizeBitmap(bitmap, newWidth, newHeight))
                {
                    // Save the resized bitmap to a file stream
                    using (FileStream fs = new FileStream("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\first-center.bmp", FileMode.Create))
                    {
                        resizedBitmap.Save(fs, ImageFormat.Bmp);
                    }
                }
            }
                    File.Delete($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-center.bmp");
                    File.Move("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\first-center.bmp", $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-center.bmp");
            Debug.WriteLine("Thread 1 Center Pic Saved");
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.ImageLocation = $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-center.bmp";
            //Load Image File into Cognex
            CogImageFile aImageFile = new CogImageFile();
            
            try
            {
                aImageFile.Open($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\first-center.bmp", CogImageFileModeConstants.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load image file " + ex);
                captureDecodeFirstCenterState = false;
                return;
            }
            //Load into CogImage8Grey for Barcode Detection
            CogImage8Grey aImage = (CogImage8Grey)aImageFile[0];

            aImageFile.Close();

            if (v_codetype == "QR")
            {
                //Check QR Code
                Console.WriteLine("QR");
                try
                {
                    firstCenterQRResults = firstCenterQRScanner.Execute(aImage, null);
                }
                catch (Exception e)
                {
                    WriteToLogFile(e.ToString());
                    stopConveyor();
                    onRedTowerLight();
                    
                    MessageBox.Show("Unknown error. Please try again.");
                    return;
                }
                for (int QRIndex = 0; QRIndex < firstCenterQRResults.Count; QRIndex++)
                {
                    int count = firstCenterQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        Debug.WriteLine(firstCenterQRResults[QRIndex].DecodedData.DecodedString);
                        if (!firstBarcodeList.Contains(firstCenterQRResults[QRIndex].DecodedData.DecodedString))
                        {
                            firstBarcodeList.Add(firstCenterQRResults[QRIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            //Split string and get info
                            string[] split_scaned = firstCenterQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }
                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }


                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (QRIndex == firstCenterQRResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        firstCenterGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }
                            SendPrintJob();
                            captureDecodeFirstCenterState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeFirstCenterState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "Datamatrix")
            {
                //Check Data Matrix
                firstCenterDataMatrixResults = firstCenterDataMatrixScanner.Execute(aImage, null);
                for (int dataMatrixIndex = 0; dataMatrixIndex < firstCenterDataMatrixResults.Count; dataMatrixIndex++)
                {
                    int count = firstCenterDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        if (!firstBarcodeList.Contains(firstCenterDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString))
                        {
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            firstBarcodeList.Add(firstCenterDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString);
                            string[] split_scaned = firstCenterDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }

                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }


                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (dataMatrixIndex == firstCenterDataMatrixResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        firstCenterGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }

                            captureDecodeFirstCenterState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeFirstCenterState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "BorgWarner")
            {
                //Check BorgWarner 1D Code
                firstCenterOneDResults = firstCenterOneDScanner.Execute(aImage, null);
                for (int OneDIndex = 0; OneDIndex < firstCenterOneDResults.Count; OneDIndex++)
                {
                    string decoded_content = firstCenterOneDResults[OneDIndex].DecodedData.DecodedString;
                    if (decoded_content.Length < 29)
                    {
                        continue;
                    }
                    else
                    {                       
                        decoded_content = decoded_content.Substring(0, 29);
                        //get details and break loop
                        if (p_id == 999)
                        {
                            pn = manual_part_no;
                        }
                        else
                        {
                            try
                            {
                                if (p_end == "")
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1);
                                }
                                else
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                }
                                if (pn.Length < 8 && pn.StartsWith("9"))
                                {
                                    pn = pn.PadLeft(8, '0');
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Part Number setting.");
                                WriteToLogFile("Error: Please check Part Number setting.");

                                return;
                            }
                        }
                        if (q_id == 999)
                        {
                            qty = manual_quantity;
                            if (short.Parse(qty) == 0)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                return;
                            }
                        }
                        else
                        {
                            try
                            {
                                if (q_end == "")
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1);
                                }
                                else
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                }
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                                qty = qty.TrimStart('0');
                                qty = qty.TrimEnd();
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting.");
                                WriteToLogFile("Error: Please check Quantity setting.");

                                return;
                            }
                        }
                        if (m_id == 999)
                        {
                            mpn = manual_manufacturer;
                        }
                        else
                        {
                            try
                            {
                                if (m_end == "")
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1);
                                }
                                else
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Manufacturer setting.");
                                WriteToLogFile("Error: Please check Manufacturer setting.");

                                return;
                            }
                        }
                        if (vc_id == 999)
                        {
                            scanned_vc = manual_vendor_code;
                        }
                        else
                        {
                            try
                            {
                                if (vc_end == "")
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1);
                                }
                                else
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Vendor Code setting.");
                                WriteToLogFile("Error: Please check Vendor Code setting.");

                                return;
                            }
                        }
                        if (vdc_id == 999)
                        {
                            mdt = manual_date_code;
                        }
                        else
                        {
                            try
                            {
                                if (vdc_end == "")
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1);
                                }
                                else
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Date Code setting.");
                                WriteToLogFile("Error: Please check Date Code setting.");

                                return;
                            }

                        }
                        if (vlc_id == 999)
                        {
                            lot = manual_lot_code;
                        }
                        else
                        {
                            try
                            {
                                if (vlc_end == "")
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1);
                                }
                                else
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Lot Code setting.");
                                WriteToLogFile("Error: Please check Lot Code setting.");

                                return;
                            }

                        }
                        if (vpono_id == 999)
                        {
                            po = manual_po_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpono_end == "")
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1);
                                }
                                else
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check PO No setting.");
                                WriteToLogFile("Error: Please check PO No setting.");
                                return;
                            }

                        }
                        if (vpackingno_id == 999)
                        {
                            packid = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpackingno_end == "")
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1);
                                }
                                else
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Packing No setting.");
                                WriteToLogFile("Error: Please check Packing No setting.");
                                return;
                            }
                        }
                        if (ved_id == 999)
                        {
                            expdt = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (ved_end == "")
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1);
                                }
                                else
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Expire Date setting.");
                                WriteToLogFile("Error: Please check Expire Date setting.");
                                return;
                            }
                        }
                        logListbox.Invoke((MethodInvoker)delegate
                        {
                            logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                        });
                        WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");

                        if (cl != null && isconnect)
                        {
                            result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                            var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                            result = jsdat.d.ToString();
                            if (result != "")
                            {


                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                });
                                WriteToLogFile($"ERP Return:{result}");

                                if (result.Contains("INVALID") && (OneDIndex == firstCenterOneDResults.Count - 1))
                                {
                                    onRedTowerLight();
                                    stopConveyor();
                                    MessageBox.Show(result);
                                    WriteToLogFile($"{result}");

                                    return;
                                }
                                else if (result.Contains("INVALID"))
                                {
                                    continue;
                                }
                                else
                                {
                                    erpReturnList.Add(result);
                                    SendPrintJob();
                                    firstCenterGotResult = true;
                                }
                            }
                            else
                            {
                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                });
                                WriteToLogFile($"ERP Return: No Result");

                                //stop conveyor if no ERP return here
                                //light up red if no ERP return here
                            }
                        }
                        captureDecodeFirstCenterState = false;
                        break;
                    }

                }
            }
            if (firstLeftGotResult == false && firstCenterGotResult == false && firstRightGotResult == false && captureDecodeFirstLeftThread.IsAlive == false && captureDecodeFirstRightThread.IsAlive == false)
            {
                onRedTowerLight();
                stopConveyor();
                MessageBox.Show("No result on last reel.");
                WriteToLogFile("No result on last reel.");
            }
        }

        private void startCaptureDecodeSecond()
        {
            secondBarcodeList.Clear();
            secondLeftGotResult = false;
            secondRightGotResult = false;
            secondCenterGotResult = false;

            captureDecodeSecondLeftState = true;
            captureDecodeSecondLeftThread = new Thread(new ThreadStart(captureDecodeSecondLeft));
            captureDecodeSecondLeftThread.IsBackground = true;
            captureDecodeSecondLeftThread.Start();
            captureDecodeSecondRightState = true;
            captureDecodeSecondRightThread = new Thread(new ThreadStart(captureDecodeSecondRight));
            captureDecodeSecondRightThread.IsBackground = true;
            captureDecodeSecondRightThread.Start();
            captureDecodeSecondCenterState = true;
            captureDecodeSecondCenterThread = new Thread(new ThreadStart(captureDecodeSecondCenter));
            captureDecodeSecondCenterThread.IsBackground = true;
            captureDecodeSecondCenterThread.Start();
        }

        private void captureDecodeSecondLeft()
        {
            string scanned_vc, result, pn, qty, mpn;
            string profile = cfg[cc], sta = config[station];
            Debug.WriteLine("Thread 2 Left Start");
            // Start grabbing.
            lCamera.StreamGrabber.Start();
            // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
            IGrabResult grabResult = lCamera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            lCamera.StreamGrabber.Stop();
            using (grabResult)
            {
                // Image grabbed successfully?
                if (grabResult.GrabSucceeded)
                {
                    //Save Image
                    ImagePersistence.Save(ImageFileFormat.Bmp, $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-left.bmp", grabResult);
                }
                else
                {
                    Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                }
            }

            using (Bitmap bitmap = new Bitmap($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-left.bmp"))
            {
                // Define the new size
                int newWidth = bitmap.Width / 2;
                int newHeight = bitmap.Height / 2;

                // Resize the bitmap
                using (Bitmap resizedBitmap = ResizeBitmap(bitmap, newWidth, newHeight))
                {
                    // Save the resized bitmap to a file stream
                    using (FileStream fs = new FileStream("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\second-left.bmp", FileMode.Create))
                    {
                        resizedBitmap.Save(fs, ImageFormat.Bmp);
                    }
                }

            }
                File.Delete($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-left.bmp");
                File.Move("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\second-left.bmp", $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-left.bmp");
            Debug.WriteLine("Thread 2 Left Pic Saved");
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.ImageLocation = $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-left.bmp";
            //Load Image File into Cognex
            CogImageFile aImageFile = new CogImageFile();
            try
            {
                aImageFile.Open($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-left.bmp", CogImageFileModeConstants.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load image file " + ex);
                captureDecodeSecondLeftState = false;
                return;
            }
            //Load into CogImage8Grey for Barcode Detection
            CogImage8Grey aImage = (CogImage8Grey)aImageFile[0];

            aImageFile.Close();
            if (v_codetype == "QR")
            {
                //Check QR Code
                firstLeftQRResults = firstLeftQRScanner.Execute(aImage, null);
                for (int QRIndex = 0; QRIndex < firstLeftQRResults.Count; QRIndex++)
                {
                    int count = firstLeftQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        Debug.WriteLine(firstLeftQRResults[QRIndex].DecodedData.DecodedString);
                        if (!firstBarcodeList.Contains(firstLeftQRResults[QRIndex].DecodedData.DecodedString))
                        {
                            firstBarcodeList.Add(firstLeftQRResults[QRIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            //Split string and get info
                            string[] split_scaned = firstLeftQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }
                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }

                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (QRIndex == secondLeftQRResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        secondLeftGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }

                            captureDecodeSecondLeftState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeSecondLeftState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "Datamatrix")
            {
                //Check Data Matrix
                firstLeftDataMatrixResults = firstLeftDataMatrixScanner.Execute(aImage, null);
                for (int dataMatrixIndex = 0; dataMatrixIndex < firstLeftDataMatrixResults.Count; dataMatrixIndex++)
                {
                    int count = firstLeftDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        if (!firstBarcodeList.Contains(firstLeftDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString))
                        {
                            firstBarcodeList.Add(firstLeftDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            string[] split_scaned = firstLeftDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }
                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }


                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (dataMatrixIndex == secondLeftDataMatrixResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        secondLeftGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }
                            captureDecodeSecondLeftState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeSecondLeftState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "BorgWarner")
            {
                //Check BorgWarner 1D Code
                secondLeftOneDResults = secondLeftOneDScanner.Execute(aImage, null);
                for (int OneDIndex = 0; OneDIndex < secondLeftOneDResults.Count; OneDIndex++)
                {
                    string decoded_content = secondLeftOneDResults[OneDIndex].DecodedData.DecodedString;
                    if (decoded_content.Length < 29)
                    {
                        continue;
                    }
                    else
                    {
                        decoded_content = decoded_content.Substring(0, 29);
                        //get details and break loop
                        if (p_id == 999)
                        {
                            pn = manual_part_no;
                        }
                        else
                        {
                            try
                            {
                                if (p_end == "")
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1);
                                }
                                else
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                }
                                if (pn.Length < 8 && pn.StartsWith("9"))
                                {
                                    pn = pn.PadLeft(8, '0');
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Part Number setting.");
                                WriteToLogFile("Error: Please check Part Number setting.");

                                return;
                            }
                        }
                        if (q_id == 999)
                        {
                            qty = manual_quantity;
                            if (short.Parse(qty) == 0)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                return;
                            }
                        }
                        else
                        {
                            try
                            {
                                if (q_end == "")
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1);
                                }
                                else
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                }
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                                qty = qty.TrimStart('0');
                                qty = qty.TrimEnd();
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting.");
                                WriteToLogFile("Error: Please check Quantity setting.");

                                return;
                            }
                        }
                        if (m_id == 999)
                        {
                            mpn = manual_manufacturer;
                        }
                        else
                        {
                            try
                            {
                                if (m_end == "")
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1);
                                }
                                else
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Manufacturer setting.");
                                WriteToLogFile("Error: Please check Manufacturer setting.");

                                return;
                            }
                        }
                        if (vc_id == 999)
                        {
                            scanned_vc = manual_vendor_code;
                        }
                        else
                        {
                            try
                            {
                                if (vc_end == "")
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1);
                                }
                                else
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Vendor Code setting.");
                                WriteToLogFile("Error: Please check Vendor Code setting.");

                                return;
                            }
                        }
                        if (vdc_id == 999)
                        {
                            mdt = manual_date_code;
                        }
                        else
                        {
                            try
                            {
                                if (vdc_end == "")
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1);
                                }
                                else
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Date Code setting.");
                                WriteToLogFile("Error: Please check Date Code setting.");

                                return;
                            }

                        }
                        if (vlc_id == 999)
                        {
                            lot = manual_lot_code;
                        }
                        else
                        {
                            try
                            {
                                if (vlc_end == "")
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1);
                                }
                                else
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Lot Code setting.");
                                WriteToLogFile("Error: Please check Lot Code setting.");

                                return;
                            }

                        }
                        if (vpono_id == 999)
                        {
                            po = manual_po_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpono_end == "")
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1);
                                }
                                else
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check PO No setting.");
                                WriteToLogFile("Error: Please check PO No setting.");
                                return;
                            }

                        }
                        if (vpackingno_id == 999)
                        {
                            packid = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpackingno_end == "")
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1);
                                }
                                else
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Packing No setting.");
                                WriteToLogFile("Error: Please check Packing No setting.");
                                return;
                            }
                        }
                        if (ved_id == 999)
                        {
                            expdt = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (ved_end == "")
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1);
                                }
                                else
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Expire Date setting.");
                                WriteToLogFile("Error: Please check Expire Date setting.");
                                return;
                            }
                        }
                        logListbox.Invoke((MethodInvoker)delegate
                        {
                            logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                        });
                        WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");

                        if (cl != null && isconnect)
                        {
                            result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                            var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                            result = jsdat.d.ToString();
                            if (result != "")
                            {


                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                });
                                WriteToLogFile($"ERP Return:{result}");

                                if (result.Contains("INVALID") && (OneDIndex == secondLeftOneDResults.Count - 1))
                                {
                                    onRedTowerLight();
                                    stopConveyor();
                                    MessageBox.Show(result);
                                    WriteToLogFile($"{result}");

                                    return;
                                }
                                else if (result.Contains("INVALID"))
                                {
                                    continue;
                                }
                                else
                                {
                                    erpReturnList.Add(result);
                                    SendPrintJob();
                                    secondLeftGotResult = true;
                                }
                            }
                            else
                            {
                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                });
                                WriteToLogFile($"ERP Return: No Result");

                                //stop conveyor if no ERP return here
                                //light up red if no ERP return here
                            }
                        }
                        captureDecodeSecondLeftState = false;
                        break;
                    }

                }
            }
            if (secondLeftGotResult == false && secondCenterGotResult == false && secondRightGotResult == false && captureDecodeSecondCenterThread.IsAlive == false && captureDecodeSecondRightThread.IsAlive == false)
            {
                onRedTowerLight();
                stopConveyor();
                MessageBox.Show("No result on last reel.");
                WriteToLogFile("No result on last reel.");

            }
        }
        private void captureDecodeSecondRight()
        {
            string scanned_vc, result, pn, qty, mpn;
            string profile = cfg[cc], sta = config[station];
            Debug.WriteLine("Thread 2 Right Start");
            // Start grabbing.
            rCamera.StreamGrabber.Start();
            // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
            IGrabResult grabResult = rCamera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            rCamera.StreamGrabber.Stop();
            using (grabResult)
            {
                // Image grabbed successfully?
                if (grabResult.GrabSucceeded)
                {
                    //Save Image
                    ImagePersistence.Save(ImageFileFormat.Bmp, $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-right.bmp", grabResult);
                }
                else
                {
                    Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                }
            }

            using (Bitmap bitmap = new Bitmap($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-right.bmp"))
            {
                // Define the new size
                int newWidth = bitmap.Width / 2;
                int newHeight = bitmap.Height / 2;

                // Resize the bitmap
                using (Bitmap resizedBitmap = ResizeBitmap(bitmap, newWidth, newHeight))
                {
                    // Save the resized bitmap to a file stream
                    using (FileStream fs = new FileStream("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\second-right.bmp", FileMode.Create))
                    {
                        resizedBitmap.Save(fs, ImageFormat.Bmp);
                    }
                }

            }
                File.Delete($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-right.bmp");
                File.Move("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\second-right.bmp", $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-right.bmp");
            Debug.WriteLine("Thread 2 Right Pic Saved");
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.ImageLocation = $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-right.bmp";
            //Load Image File into Cognex
            CogImageFile aImageFile = new CogImageFile();
            try
            {
                aImageFile.Open($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-right.bmp", CogImageFileModeConstants.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load image file " + ex);
                captureDecodeSecondRightState = false;
                return;
            }
            //Load into CogImage8Grey for Barcode Detection
            CogImage8Grey aImage = (CogImage8Grey)aImageFile[0];

            aImageFile.Close();
            if (v_codetype == "QR")
            {
                //Check QR Code
                secondRightQRResults = secondRightQRScanner.Execute(aImage, null);
                for (int QRIndex = 0; QRIndex < secondRightQRResults.Count; QRIndex++)
                {
                    int count = secondRightQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        Debug.WriteLine(secondRightQRResults[QRIndex].DecodedData.DecodedString);
                        if (!secondBarcodeList.Contains(secondRightQRResults[QRIndex].DecodedData.DecodedString))
                        {
                            secondBarcodeList.Add(secondRightQRResults[QRIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            //Split string and get info
                            string[] split_scaned = secondRightQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }
                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }

                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (QRIndex == secondRightQRResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        secondRightGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }

                            captureDecodeSecondRightState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeSecondRightState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "Datamatrix")
            {
                //Check Data Matrix
                secondRightDataMatrixResults = secondRightDataMatrixScanner.Execute(aImage, null);
                for (int dataMatrixIndex = 0; dataMatrixIndex < secondRightDataMatrixResults.Count; dataMatrixIndex++)
                {
                    int count = secondRightDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        if (!secondBarcodeList.Contains(secondRightDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString))
                        {
                            secondBarcodeList.Add(secondRightDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            string[] split_scaned = secondRightDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }
                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }

                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (dataMatrixIndex == secondRightDataMatrixResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        secondRightGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }
                            captureDecodeSecondRightState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeSecondRightState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "BorgWarner")
            {
                //Check BorgWarner 1D Code
                secondRightOneDResults = secondRightOneDScanner.Execute(aImage, null);
                for (int OneDIndex = 0; OneDIndex < secondRightOneDResults.Count; OneDIndex++)
                {
                    
                    string decoded_content = secondRightOneDResults[OneDIndex].DecodedData.DecodedString;
                    if (decoded_content.Length < 29)
                    {
                        continue;
                    }
                    else
                    {
                        decoded_content = decoded_content.Substring(0, 29);
                        //get details and break loop
                        if (p_id == 999)
                        {
                            pn = manual_part_no;
                        }
                        else
                        {
                            try
                            {
                                
                                if (p_end == "")
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1);
                                }
                                else
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                }
                                if (pn.Length < 8 && pn.StartsWith("9"))
                                {
                                    pn = pn.PadLeft(8, '0');
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Part Number setting.");
                                WriteToLogFile("Error: Please check Part Number setting.");

                                return;
                            }
                        }
                        if (q_id == 999)
                        {
                            qty = manual_quantity;
                            if (short.Parse(qty) == 0)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                return;
                            }
                        }
                        else
                        {
                            try
                            {
                                if (q_end == "")
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1);
                                }
                                else
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                }
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                                qty = qty.TrimStart('0');
                                qty = qty.TrimEnd();
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting.");
                                WriteToLogFile("Error: Please check Quantity setting.");

                                return;
                            }
                        }
                        if (m_id == 999)
                        {
                            mpn = manual_manufacturer;
                        }
                        else
                        {
                            try
                            {
                                if (m_end == "")
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1);
                                }
                                else
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Manufacturer setting.");
                                WriteToLogFile("Error: Please check Manufacturer setting.");

                                return;
                            }
                        }
                        if (vc_id == 999)
                        {
                            scanned_vc = manual_vendor_code;
                        }
                        else
                        {
                            try
                            {
                                if (vc_end == "")
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1);
                                }
                                else
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Vendor Code setting.");
                                WriteToLogFile("Error: Please check Vendor Code setting.");

                                return;
                            }
                        }
                        if (vdc_id == 999)
                        {
                            mdt = manual_date_code;
                        }
                        else
                        {
                            try
                            {
                                if (vdc_end == "")
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1);
                                }
                                else
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Date Code setting.");
                                WriteToLogFile("Error: Please check Date Code setting.");

                                return;
                            }

                        }
                        if (vlc_id == 999)
                        {
                            lot = manual_lot_code;
                        }
                        else
                        {
                            try
                            {
                                if (vlc_end == "")
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1);
                                }
                                else
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Lot Code setting.");
                                WriteToLogFile("Error: Please check Lot Code setting.");

                                return;
                            }

                        }
                        if (vpono_id == 999)
                        {
                            po = manual_po_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpono_end == "")
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1);
                                }
                                else
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check PO No setting.");
                                WriteToLogFile("Error: Please check PO No setting.");
                                return;
                            }

                        }
                        if (vpackingno_id == 999)
                        {
                            packid = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpackingno_end == "")
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1);
                                }
                                else
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Packing No setting.");
                                WriteToLogFile("Error: Please check Packing No setting.");
                                return;
                            }
                        }
                        if (ved_id == 999)
                        {
                            expdt = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (ved_end == "")
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1);
                                }
                                else
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Expire Date setting.");
                                WriteToLogFile("Error: Please check Expire Date setting.");
                                return;
                            }
                        }
                        logListbox.Invoke((MethodInvoker)delegate
                        {
                            logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                        });
                        WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");

                        if (cl != null && isconnect)
                        {
                            result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                            Debug.WriteLine(result);
                            var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                            result = jsdat.d.ToString();
                            if (result != "")
                            {


                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                });
                                WriteToLogFile($"ERP Return:{result}");

                                if (result.Contains("INVALID") && (OneDIndex == secondRightOneDResults.Count - 1))
                                {
                                    onRedTowerLight();
                                    stopConveyor();
                                    MessageBox.Show(result);
                                    WriteToLogFile($"{result}");

                                    return;
                                }
                                else if (result.Contains("INVALID"))
                                {
                                    continue;
                                }
                                else
                                {
                                    erpReturnList.Add(result);
                                    SendPrintJob();
                                    secondRightGotResult = true;
                                }
                            }
                            else
                            {
                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                });
                                WriteToLogFile($"ERP Return: No Result");

                                //stop conveyor if no ERP return here
                                //light up red if no ERP return here
                            }
                        }
                        captureDecodeSecondRightState = false;
                        break;
                    }

                }
            }

            if (secondLeftGotResult == false && secondCenterGotResult == false && secondRightGotResult == false && captureDecodeSecondCenterThread.IsAlive == false && captureDecodeSecondLeftThread.IsAlive == false)
            {
                onRedTowerLight();
                stopConveyor();
                MessageBox.Show("No result on last reel.");
                WriteToLogFile("No result on last reel.");
            }
        }
        private void captureDecodeSecondCenter()
        {
            string scanned_vc, result, pn, qty, mpn;
            string profile = cfg[cc], sta = config[station];
            Debug.WriteLine("Thread 2 Center Start");
            // Start grabbing.
            cCamera.StreamGrabber.Start();
            // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
            IGrabResult grabResult = cCamera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            cCamera.StreamGrabber.Stop();
            using (grabResult)
            {
                // Image grabbed successfully?
                if (grabResult.GrabSucceeded)
                {
                    //Save Image
                    ImagePersistence.Save(ImageFileFormat.Bmp, $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-center.bmp", grabResult);
                }
                else
                {
                    Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                }
            }

            using (Bitmap bitmap = new Bitmap($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-center.bmp"))
            {
                // Define the new size
                int newWidth = bitmap.Width / 2;
                int newHeight = bitmap.Height / 2;

                // Resize the bitmap
                using (Bitmap resizedBitmap = ResizeBitmap(bitmap, newWidth, newHeight))
                {
                    // Save the resized bitmap to a file stream
                    using (FileStream fs = new FileStream("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\second-center.bmp", FileMode.Create))
                    {
                        resizedBitmap.Save(fs, ImageFormat.Bmp);
                    }
                }

            }
                File.Delete($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-center.bmp");
                File.Move("C:\\Users\\HP EliteDesk 800G3ED\\Documents\\second-center.bmp", $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-center.bmp");
            Debug.WriteLine("Thread 2 Center Pic Saved");
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.ImageLocation = $"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-center.bmp";
            //Load Image File into Cognex
            CogImageFile aImageFile = new CogImageFile();
            try
            {
                aImageFile.Open($"C:\\Users\\HP EliteDesk 800G3ED\\Desktop\\second-center.bmp", CogImageFileModeConstants.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load image file " + ex);
                captureDecodeSecondCenterState = false;
                return;
            }
            //Load into CogImage8Grey for Barcode Detection
            CogImage8Grey aImage = (CogImage8Grey)aImageFile[0];

            aImageFile.Close();
            if (v_codetype == "QR")
            {
                //Check QR Code
                try
                {
                    secondCenterQRResults = secondCenterQRScanner.Execute(aImage, null);
                }
                catch (Exception e)
                {
                    WriteToLogFile(e.ToString());
                    stopConveyor();
                    onRedTowerLight();
                    MessageBox.Show("Unknown error. Please try again.");
                    return;
                }
                for (int QRIndex = 0; QRIndex < secondCenterQRResults.Count; QRIndex++)
                {
                    int count = secondCenterQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        Debug.WriteLine(secondCenterQRResults[QRIndex].DecodedData.DecodedString);
                        if (!secondBarcodeList.Contains(secondCenterQRResults[QRIndex].DecodedData.DecodedString))
                        {
                            secondBarcodeList.Add(secondCenterQRResults[QRIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            //Split string and get info
                            string[] split_scaned = secondCenterQRResults[QRIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }
                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }


                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (QRIndex == secondCenterQRResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        secondCenterGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }
                            captureDecodeSecondCenterState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeSecondCenterState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "Datamatrix")
            {
                //Check Data Matrix
                secondCenterDataMatrixResults = secondCenterDataMatrixScanner.Execute(aImage, null);
                for (int dataMatrixIndex = 0; dataMatrixIndex < secondCenterDataMatrixResults.Count; dataMatrixIndex++)
                {
                    int count = secondCenterDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]).Length - 1;
                    if (count >= delimiterCount)
                    {
                        if (!secondBarcodeList.Contains(secondCenterDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString))
                        {
                            secondBarcodeList.Add(secondCenterDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString);
                            scanned_vc = "";
                            pn = "";
                            qty = "";
                            mpn = "";
                            string[] split_scaned = secondCenterDataMatrixResults[dataMatrixIndex].DecodedData.DecodedString.Split(scanner_spliter[0]);
                            if (p_id == 999)
                            {
                                pn = manual_part_no;
                            }
                            else
                            {
                                try
                                {
                                    if (p_end == "")
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1);
                                    }
                                    else
                                    {
                                        pn = split_scaned[p_id].Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                    }
                                    if (pn.Length < 8 && pn.StartsWith("9"))
                                    {
                                        pn = pn.PadLeft(8, '0');
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Part Number setting.");
                                    WriteToLogFile("Error: Please check Part Number setting.");
                                    return;
                                }
                            }
                            if (q_id == 999)
                            {
                                qty = manual_quantity;
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (q_end == "")
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1);
                                    }
                                    else
                                    {
                                        qty = split_scaned[q_id].Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                    }
                                    if (short.Parse(qty) == 0)
                                    {
                                        stopConveyor();
                                        onRedTowerLight();
                                        MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting.");
                                    WriteToLogFile("Error: Please check Quantity setting.");
                                    return;
                                }
                            }


                            if (m_id == 999)
                            {
                                mpn = manual_manufacturer;
                            }
                            else
                            {
                                try
                                {
                                    if (m_end == "")
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1);
                                    }
                                    else
                                    {
                                        mpn = split_scaned[m_id].Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Manufacturer setting.");
                                    WriteToLogFile("Error: Please check Manufacturer setting.");
                                    return;
                                }
                            }
                            if (vc_id == 999)
                            {
                                scanned_vc = manual_vendor_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vc_end == "")
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1);
                                    }
                                    else
                                    {
                                        scanned_vc = split_scaned[vc_id].Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Vendor Code setting.");
                                    WriteToLogFile("Error: Please check Vendor Code setting.");
                                    return;
                                }
                            }
                            if (vdc_id == 999)
                            {
                                mdt = manual_date_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vdc_end == "")
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1);
                                    }
                                    else
                                    {
                                        mdt = split_scaned[vdc_id].Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Date Code setting.");
                                    WriteToLogFile("Error: Please check Date Code setting.");
                                    return;
                                }

                            }
                            if (vlc_id == 999)
                            {
                                lot = manual_lot_code;
                            }
                            else
                            {
                                try
                                {
                                    if (vlc_end == "")
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1);
                                    }
                                    else
                                    {
                                        lot = split_scaned[vlc_id].Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Lot Code setting.");
                                    WriteToLogFile("Error: Please check Lot Code setting.");
                                    return;
                                }

                            }
                            if (vpono_id == 999)
                            {
                                po = manual_po_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpono_end == "")
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1);
                                    }
                                    else
                                    {
                                        po = split_scaned[vpono_id].Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check PO No setting.");
                                    WriteToLogFile("Error: Please check PO No setting.");
                                    return;
                                }

                            }
                            if (vpackingno_id == 999)
                            {
                                packid = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (vpackingno_end == "")
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1);
                                    }
                                    else
                                    {
                                        packid = split_scaned[vpackingno_id].Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Packing No setting.");
                                    WriteToLogFile("Error: Please check Packing No setting.");
                                    return;
                                }
                            }
                            if (ved_id == 999)
                            {
                                expdt = manual_packing_no;
                            }
                            else
                            {
                                try
                                {
                                    if (ved_end == "")
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1);
                                    }
                                    else
                                    {
                                        expdt = split_scaned[ved_id].Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                    }
                                }
                                catch (Exception e)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Expire Date setting.");
                                    WriteToLogFile("Error: Please check Expire Date setting.");
                                    return;
                                }
                            }
                            logListbox.Invoke((MethodInvoker)delegate
                            {
                                logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            });
                            WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                            if (cl != null && isconnect)
                            {
                                result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                                result = jsdat.d.ToString();
                                if (result != "")
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                    });
                                    WriteToLogFile($"ERP Return:{result}");

                                    if (result.Contains("INVALID") && (dataMatrixIndex == secondCenterDataMatrixResults.Count - 1))
                                    {
                                        onRedTowerLight();
                                        stopConveyor();
                                        MessageBox.Show(result);
                                        WriteToLogFile($"{result}");
                                        return;
                                    }
                                    else if (result.Contains("INVALID"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        erpReturnList.Add(result);
                                        SendPrintJob();
                                        secondCenterGotResult = true;
                                    }
                                }
                                else
                                {
                                    logListbox.Invoke((MethodInvoker)delegate
                                    {
                                        logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                    });
                                    WriteToLogFile($"ERP Return: No Result");
                                    //stop conveyor if no ERP return here
                                    //light up red if no ERP return here
                                }
                            }
                            captureDecodeSecondCenterState = false;
                            return;
                        }
                        else
                        {
                            captureDecodeSecondCenterState = false;
                            return;
                        }
                    }
                }

            }

            if (v_codetype == "BorgWarner")
            {
                //Check BorgWarner 1D Code
                secondCenterOneDResults = secondCenterOneDScanner.Execute(aImage, null);
                Debug.WriteLine("Result Count: " + secondCenterOneDResults.Count);
                for (int OneDIndex = 0; OneDIndex < secondCenterOneDResults.Count; OneDIndex++)
                {
                    string decoded_content = secondCenterOneDResults[OneDIndex].DecodedData.DecodedString;
                    Debug.WriteLine(decoded_content);

                    if (decoded_content.Length < 29)
                    {
                        continue;
                    }
                    else
                    {
                        decoded_content = decoded_content.Substring(0, 29);
                        //get details and break loop
                        if (p_id == 999)
                        {
                            pn = manual_part_no;
                        }
                        else
                        {
                            try
                            {
                                if (p_end == "")
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1);
                                }
                                else
                                {
                                    pn = decoded_content.Substring(int.Parse(p_start) - 1, int.Parse(p_end) - int.Parse(p_start) + 1);
                                }
                                if (pn.Length < 8 && pn.StartsWith("9"))
                                {
                                    pn = pn.PadLeft(8, '0');
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Part Number setting.");
                                WriteToLogFile("Error: Please check Part Number setting.");

                                return;
                            }
                        }
                        if (q_id == 999)
                        {
                            qty = manual_quantity;
                            if (short.Parse(qty) == 0)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                return;
                            }
                        }
                        else
                        {
                            try
                            {
                                if (q_end == "")
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1);
                                }
                                else
                                {
                                    qty = decoded_content.Substring(int.Parse(q_start) - 1, int.Parse(q_end) - int.Parse(q_start) + 1);
                                }
                                if (short.Parse(qty) == 0)
                                {
                                    stopConveyor();
                                    onRedTowerLight();
                                    MessageBox.Show("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    WriteToLogFile("Error: Please check Quantity setting. Quantity cannot be 0.");
                                    return;
                                }
                                qty = qty.TrimStart('0');
                                qty = qty.TrimEnd();
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Quantity setting.");
                                WriteToLogFile("Error: Please check Quantity setting.");

                                return;
                            }
                        }
                        if (m_id == 999)
                        {
                            mpn = manual_manufacturer;
                        }
                        else
                        {
                            try
                            {
                                if (m_end == "")
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1);
                                }
                                else
                                {
                                    mpn = decoded_content.Substring(int.Parse(m_start) - 1, int.Parse(m_end) - int.Parse(m_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Manufacturer setting.");
                                WriteToLogFile("Error: Please check Manufacturer setting.");

                                return;
                            }
                        }
                        if (vc_id == 999)
                        {
                            scanned_vc = manual_vendor_code;
                        }
                        else
                        {
                            try
                            {
                                if (vc_end == "")
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1);
                                }
                                else
                                {
                                    scanned_vc = decoded_content.Substring(int.Parse(vc_start) - 1, int.Parse(vc_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Vendor Code setting.");
                                WriteToLogFile("Error: Please check Vendor Code setting.");

                                return;
                            }
                        }
                        if (vdc_id == 999)
                        {
                            mdt = manual_date_code;
                        }
                        else
                        {
                            try
                            {
                                if (vdc_end == "")
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1);
                                }
                                else
                                {
                                    mdt = decoded_content.Substring(int.Parse(vdc_start) - 1, int.Parse(vdc_end) - int.Parse(vdc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Date Code setting.");
                                WriteToLogFile("Error: Please check Date Code setting.");

                                return;
                            }

                        }
                        if (vlc_id == 999)
                        {
                            lot = manual_lot_code;
                        }
                        else
                        {
                            try
                            {
                                if (vlc_end == "")
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1);
                                }
                                else
                                {
                                    lot = decoded_content.Substring(int.Parse(vlc_start) - 1, int.Parse(vlc_end) - int.Parse(vlc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Lot Code setting.");
                                WriteToLogFile("Error: Please check Lot Code setting.");

                                return;
                            }

                        }
                        if (vpono_id == 999)
                        {
                            po = manual_po_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpono_end == "")
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1);
                                }
                                else
                                {
                                    po = decoded_content.Substring(int.Parse(vpono_start) - 1, int.Parse(vpono_end) - int.Parse(vpono_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check PO No setting.");
                                WriteToLogFile("Error: Please check PO No setting.");
                                return;
                            }

                        }
                        if (vpackingno_id == 999)
                        {
                            packid = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (vpackingno_end == "")
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1);
                                }
                                else
                                {
                                    packid = decoded_content.Substring(int.Parse(vpackingno_start) - 1, int.Parse(vpackingno_end) - int.Parse(vc_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Packing No setting.");
                                WriteToLogFile("Error: Please check Packing No setting.");
                                return;
                            }
                        }
                        if (ved_id == 999)
                        {
                            expdt = manual_packing_no;
                        }
                        else
                        {
                            try
                            {
                                if (ved_end == "")
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1);
                                }
                                else
                                {
                                    expdt = decoded_content.Substring(int.Parse(ved_start) - 1, int.Parse(ved_end) - int.Parse(ved_start) + 1);
                                }
                            }
                            catch (Exception e)
                            {
                                stopConveyor();
                                onRedTowerLight();
                                MessageBox.Show("Error: Please check Expire Date setting.");
                                WriteToLogFile("Error: Please check Expire Date setting.");
                                return;
                            }
                        }
                        logListbox.Invoke((MethodInvoker)delegate
                        {
                            logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");
                        });
                        WriteToLogFile($"VC:{scanned_vc} PN:{pn}, Qty:{qty}, MPN:{mpn}, DateCode: {mdt}, PO No.: {po}, Lot No: {lot}, Pack ID: {packid}, Exp. Date: {expdt} ");

                        if (cl != null && isconnect)
                        {
                            result = cl.GetStockCodeToPrint_V206(profile, sta, pn, qty, mpn, mdt, scanned_vc, po, expdt, packid, lot, mnm, mark, prog, bin, dat1, dat2, dat3, composite, fn);
                            var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                            result = jsdat.d.ToString();
                            if (result != "")
                            {
                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return:{result}");
                                });
                                WriteToLogFile($"ERP Return:{result}");

                                if (result.Contains("INVALID") && (OneDIndex == secondCenterOneDResults.Count - 1))
                                {
                                    onRedTowerLight();
                                    stopConveyor();
                                    MessageBox.Show(result);
                                    WriteToLogFile($"{result}");

                                    return;
                                }
                                else if (result.Contains("INVALID"))
                                {
                                    continue;
                                }
                                else
                                {
                                    erpReturnList.Add(result);
                                    SendPrintJob();
                                    secondCenterGotResult = true;
                                }
                            }
                            else
                            {
                                logListbox.Invoke((MethodInvoker)delegate
                                {
                                    logListbox.Items.Insert(0, $"[{System.DateTime.Now.ToShortTimeString()}] ERP Return: No Result");
                                });
                                WriteToLogFile($"ERP Return: No Result");

                                //stop conveyor if no ERP return here
                                //light up red if no ERP return here
                            }
                        }
                        captureDecodeSecondCenterState = false;
                        break;
                    }

                }
            }

            if (secondLeftGotResult == false && secondCenterGotResult == false && secondRightGotResult == false && captureDecodeSecondLeftThread.IsAlive == false && captureDecodeSecondRightThread.IsAlive == false)
            {
                onRedTowerLight();
                stopConveyor();
                MessageBox.Show("No result on last reel.");
                WriteToLogFile("No result on last reel.");
            }
        }

        private void Form1_FormClosing(object sender, FormClosedEventArgs e)
        {
            lCamera.Close();
            rCamera.Close();
            cCamera.Close();
            stopConveyor();
            offTowerLight();
            WriteToLogFile("Close Program.");
        }


        private void btn_setting1_Click_1(object sender, EventArgs e)
        {
            this.tabControl1.SelectedTab = tabPage2;
        }

        private void btn_graphic1_Click(object sender, EventArgs e)
        {
            this.tabControl1.SelectedTab = tabPage1;
        }

        private void btn_emg1_Click_1(object sender, EventArgs e)
        {
            stopConveyor();
            onRedTowerLight();
            WriteToLogFile("Emergency Stop Conveyor.");
        }
        private void btn_restart_Click(object sender, EventArgs e)
        {
            startConveyor();
            startConveyor();
            onGreenTowerLight();
            WriteToLogFile("Restart Conveyor.");
        }
        private void btn_exit1_Click_1(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_RegVC_Click(object sender, EventArgs e)
        {
            register_form_opened = true;
            Register_Form.registerScanListbox.Items.Clear();
            Register_Form.click_source = "register";

            Register_Form.MEVendorCodeEntry.Text = "";
            Register_Form.txt_VendorName.Text = "";
            Register_Form.txt_ManufacturerName.Text = "";
            Register_Form.codeTypeCombobox.Text = "";
            Register_Form.P_Index.Text = "";
            Register_Form.Q_Index.Text = "";
            Register_Form.M_Index.Text = "";
            Register_Form.txt_P_Start.Text = "";
            Register_Form.txt_Q_Start.Text = "";
            Register_Form.txt_M_Start.Text = "";
            Register_Form.txt_P_End.Text = "";
            Register_Form.txt_Q_End.Text = "";
            Register_Form.txt_M_End.Text = "";
            Register_Form.txt_Spliter.Text = "";
            Register_Form.txt_capturetime.Text = "";
            Register_Form.txt_printtime.Text = "";
            Register_Form.txt_expdate_start.Text = "";
            Register_Form.vendorCodeOrderCombobox.Text = "";
            Register_Form.dateCode_index.Text = "";
            Register_Form.lotCode_index.Text = "";
            Register_Form.PONo_index.Text = "";
            Register_Form.packingNo_index.Text = "";
            Register_Form.expireDate_index.Text = "";
            Register_Form.txt_vendorcode_start.Text = "";
            Register_Form.txt_datecodestart.Text = "";
            Register_Form.txt_lotcode_start.Text = "";
            Register_Form.txt_pono_start.Text = "";
            Register_Form.txt_packingno_start.Text = "";
            Register_Form.txt_vendorcode_end.Text = "";
            Register_Form.txt_datecode_end.Text = "";
            Register_Form.txt_lotcode_end.Text = "";
            Register_Form.txt_pono_end.Text = "";
            Register_Form.txt_packingno_end.Text = "";
            Register_Form.txt_expdate_end.Text = "";
            Register_Form.M_Index.SelectedIndex = -1;
            Register_Form.Q_Index.SelectedIndex = -1;
            Register_Form.P_Index.SelectedIndex = -1;
            Register_Form.vendorCodeOrderCombobox.SelectedIndex = -1;
            Register_Form.dateCode_index.SelectedIndex = -1;
            Register_Form.lotCode_index.SelectedIndex = -1;
            Register_Form.PONo_index.SelectedIndex = -1;
            Register_Form.packingNo_index.SelectedIndex = -1;
            Register_Form.expireDate_index.SelectedIndex = -1;
            Register_Form.MEDateCodeEntry.Text = "";
            Register_Form.MELotCodeEntry.Text = "";
            Register_Form.MEPONoEntry.Text = "";
            Register_Form.MEPackingNoEntry.Text = "";
            Register_Form.MEExpireDateEntry.Text = "";

            var result = Register_Form.ShowDialog();
            if (result == DialogResult.OK)
            {
                load_vendor();
                this.tabControl1.SelectedTab = tabPage2;
            }
            register_form_opened = false;
        }

        private void btn_EditVC_Click(object sender, EventArgs e)
        {
            string selected_vendor = vendor_dropdown.SelectedValue.ToString();
            if (selected_vendor == "")
            {
                MessageBox.Show("Please Select Proper Vendor", "Vendor Empty!");
                vendor_dropdown.Focus();
                return;
            }
            register_form_opened = true;
            Register_Form.registerScanListbox.Items.Clear();
            Register_Form.click_source = "edit";
            for (int check_vendor = 0; check_vendor < vendor_dtl_count; check_vendor++)
            {
                if (selected_vendor == vendor_name_dtl[check_vendor])
                {
                    Register_Form.MEVendorCodeEntry.Text = vendor_code_dtl[check_vendor];
                    Register_Form.txt_VendorName.Text = vendor_name_dtl[check_vendor];
                    Register_Form.txt_ManufacturerName.Text = manufacturer_name_dtl[check_vendor];
                    Register_Form.codeTypeCombobox.Text = vendor_codetype_dtl[check_vendor];
                    Register_Form.P_Index.Text = vendor_part_dtl[check_vendor];
                    Register_Form.Q_Index.Text = vendor_qty_dtl[check_vendor];
                    Register_Form.M_Index.Text = vendor_manu_dtl[check_vendor];
                    Register_Form.txt_P_Start.Text = part_start_dtl[check_vendor];
                    Register_Form.txt_Q_Start.Text = qty_start_dtl[check_vendor];
                    Register_Form.txt_M_Start.Text = manu_start_dtl[check_vendor];
                    Register_Form.txt_P_End.Text = part_end_dtl[check_vendor];
                    Register_Form.txt_Q_End.Text = qty_end_dtl[check_vendor];
                    Register_Form.txt_M_End.Text = manu_end_dtl[check_vendor];
                    if (vendor_spliter[check_vendor] == "\r")
                    {
                        Register_Form.txt_Spliter.Text = "CR";
                    }
                    else if (vendor_spliter[check_vendor] == "\n")
                    {
                        Register_Form.txt_Spliter.Text = "LF";
                    }
                    else
                    {
                        Register_Form.txt_Spliter.Text = vendor_spliter[check_vendor];
                    }

                    Register_Form.txt_capturetime.Text = vendor_capturetime[check_vendor];
                    Register_Form.txt_printtime.Text = vendor_printtime[check_vendor];
                    Register_Form.txt_expdate_start.Text = expiredate_start_dtl[check_vendor];
                    Register_Form.vendorCodeOrderCombobox.Text = vendor_code_order_dtl[check_vendor];
                    Register_Form.dateCode_index.Text = vendor_datecode_dtl[check_vendor];
                    Register_Form.lotCode_index.Text = vendor_lotcode_dtl[check_vendor];
                    Register_Form.PONo_index.Text = vendor_pono_dtl[check_vendor];
                    Register_Form.packingNo_index.Text = vendor_packingno_dtl[check_vendor];
                    Register_Form.expireDate_index.Text = vendor_expiredate_dtl[check_vendor];
                    Register_Form.txt_vendorcode_start.Text = code_order_start_dtl[check_vendor];
                    Register_Form.txt_datecodestart.Text = datecode_start_dtl[check_vendor];
                    Register_Form.txt_lotcode_start.Text = lotcode_start_dtl[check_vendor];
                    Register_Form.txt_pono_start.Text = pono_start_dtl[check_vendor];
                    Register_Form.txt_packingno_start.Text = packingno_start_dtl[check_vendor];
                    Register_Form.txt_vendorcode_end.Text = code_order_end_dtl[check_vendor];
                    Register_Form.txt_datecode_end.Text = datecode_end_dtl[check_vendor];
                    Register_Form.txt_lotcode_end.Text = lotcode_end_dtl[check_vendor];
                    Register_Form.txt_pono_end.Text = pono_end_dtl[check_vendor];
                    Register_Form.txt_packingno_end.Text = packingno_end_dtl[check_vendor];
                    Register_Form.txt_expdate_end.Text = expiredate_end_dtl[check_vendor];
                    Register_Form.MEDateCodeEntry.Text = manual_date_code_dtl[check_vendor];
                    Register_Form.MELotCodeEntry.Text = manual_lot_code_dtl[check_vendor];
                    Register_Form.MEPONoEntry.Text = manual_po_no_dtl[check_vendor];
                    Register_Form.MEPackingNoEntry.Text = manual_packing_no_dtl[check_vendor];
                    Register_Form.MEExpireDateEntry.Text = manual_expire_date_dtl[check_vendor];
                    Register_Form.MEPartNoEntry.Text = manual_part_no_dtl[check_vendor];
                    Register_Form.MEQuantityEntry.Text = manual_quantity_dtl[check_vendor];
                    Register_Form.MEManufacturerEntry.Text = manual_manufacturer_dtl[check_vendor];

                    Register_Form.originalVendorNameLabel.Text = vendor_name_dtl[check_vendor];
                }
            }
            var result = Register_Form.ShowDialog();
            if (result == DialogResult.OK)
            {
                load_vendor();
                homeVendorCombobox.Text = selected_vendor;
                vendor_dropdown.Text = selected_vendor;
                this.tabControl1.SelectedTab = tabPage2;
            }
            register_form_opened = false;
        }

        //this function is not used and the button is hidden
        private void btn_SaveVC_Click(object sender, EventArgs e)
        {
            string selected_vendor = vendor_dropdown.Text;
            if (selected_vendor == "")
            {
                MessageBox.Show("Please Select Proper Vendor", "Vendor Empty!");
                vendor_dropdown.Focus();
                return;
            }
            for (int check_vendor = 0; check_vendor < vendor_dtl_count; check_vendor++)
            {
                if (selected_vendor == vendor_name_dtl[check_vendor])
                {
                    vc = vendor_code_dtl[check_vendor];
                    txt_VendorCode.Text = vendor_code_dtl[check_vendor];

                    if (vendor_part_dtl[check_vendor] == "N/A" || vendor_part_dtl[check_vendor] == "")
                    {
                        p_id = 999;
                    }
                    else
                    {
                        p_id = int.Parse(vendor_part_dtl[check_vendor]) - 1;
                    }
                    if (vendor_qty_dtl[check_vendor] == "N/A" || vendor_qty_dtl[check_vendor] == "")
                    {
                        q_id = 999;
                    }
                    else
                    {
                        MessageBox.Show(q_id.ToString());
                        q_id = int.Parse(vendor_qty_dtl[check_vendor]) - 1;
                    }
                    if (vendor_manu_dtl[check_vendor] == "N/A" || vendor_manu_dtl[check_vendor] == "")
                    {
                        m_id = 999;
                    }
                    else
                    {
                        m_id = int.Parse(vendor_manu_dtl[check_vendor]) - 1;
                    }
                    if (vendor_code_order_dtl[check_vendor] == "N/A" || vendor_code_order_dtl[check_vendor] == "")
                    {
                        vc_id = 999;
                    }
                    else
                    {
                        vc_id = int.Parse(vendor_code_order_dtl[check_vendor]) - 1;
                    }
                    if (vendor_datecode_dtl[check_vendor] == "N/A")
                    {
                        vdc_id = 999;
                    }
                    else
                    {
                        vdc_id = int.Parse(vendor_datecode_dtl[check_vendor]) - 1;
                    }
                    if (vendor_lotcode_dtl[check_vendor] == "N/A")
                    {
                        vlc_id = 999;
                    }
                    else
                    {
                        vlc_id = int.Parse(vendor_lotcode_dtl[check_vendor]) - 1;
                    }
                    if (vendor_pono_dtl[check_vendor] == "N/A")
                    {
                        vpono_id = 999;
                    }
                    else
                    {
                        vpono_id = int.Parse(vendor_pono_dtl[check_vendor]) - 1;
                    }
                    if (vendor_packingno_dtl[check_vendor] == "N/A")
                    {
                        vpackingno_id = 999;
                    }
                    else
                    {
                        vpackingno_id = int.Parse(vendor_packingno_dtl[check_vendor]) - 1;
                    }
                    if (vendor_expiredate_dtl[check_vendor] == "N/A")
                    {
                        ved_id = 999;
                    }
                    else
                    {
                        ved_id = int.Parse(vendor_expiredate_dtl[check_vendor]) - 1;
                    }
                    manufacturer_name = manufacturer_name_dtl[check_vendor];
                    v_spliter = ",";
                    v_capturetime = vendor_capturetime[check_vendor];
                    v_printtime = vendor_printtime[check_vendor];
                    v_codetype = vendor_codetype_dtl[check_vendor];
                    scanner_spliter = vendor_spliter[check_vendor];
                    check_spliter = vendor_spliter[check_vendor];
                    p_start = part_start_dtl[check_vendor];
                    
                    q_start = qty_start_dtl[check_vendor];
                    m_start = manu_start_dtl[check_vendor];
                    vc_start = code_order_start_dtl[check_vendor];
                    vdc_start = datecode_start_dtl[check_vendor];
                    vlc_start = lotcode_start_dtl[check_vendor];
                    vpono_start = pono_start_dtl[check_vendor];
                    vpackingno_start = packingno_start_dtl[check_vendor];
                    ved_start = expiredate_start_dtl[check_vendor];
                    p_end = part_end_dtl[check_vendor];
                    q_end = qty_end_dtl[check_vendor];
                    m_end = manu_end_dtl[check_vendor];
                    vc_end = code_order_end_dtl[check_vendor];
                    vdc_end = datecode_end_dtl[check_vendor];
                    vlc_end = lotcode_end_dtl[check_vendor];
                    vpono_end = pono_end_dtl[check_vendor];
                    vpackingno_end = packingno_end_dtl[check_vendor];
                    ved_end = expiredate_end_dtl[check_vendor];


                    if (check_spliter == "T")
                    {
                        scanner_spliter = "\t";
                    }
                    return;
                }
            }
        }

        private void btn_SaveField_Click(object sender, EventArgs e)
        {

            mark = txt_ICMark.Text.ToString();
            prog = txt_ICProg.Text.ToString();
            bin = txt_ICBin.Text.ToString();
            dat1 = txt_Data1.Text.ToString();
            dat2 = txt_Data2.Text.ToString();
            dat3 = txt_Data3.Text.ToString();
            composite = txt_Composite.Text.ToString();
            fn = txt_FileName.Text.ToString();
        }

        private void btn_ResetField_Click(object sender, EventArgs e)
        {

            txt_ICMark.Text = mark;
            txt_ICProg.Text = prog;
            txt_ICBin.Text = bin;
            txt_Data1.Text = dat1;
            txt_Data2.Text = dat2;
            txt_Data3.Text = dat3;
            txt_Composite.Text = composite;
            txt_FileName.Text = fn;
        }

        private void btn_adv_Setting_Click(object sender, EventArgs e)
        {
            using (Password passwordForm = new Password())
            {
                var result = passwordForm.ShowDialog();
                if (result == DialogResult.OK)
                {
                    this.tabControl1.SelectedTab = tabPage3;
                }
            }
        }

        private void btnWorkSave_Click(object sender, EventArgs e)
        {
            work_dtl[work_order] = txtWorkOrderNumber.Text;
            work_dtl[project_code] = txtProjectCode.Text;
            work_dtl[program_recipe] = txtProgramRecipe.Text;
            work_dtl[employee_name] = txtEmployee.Text;

            MessageBox.Show("Work Order Saved!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnWorkReset_Click(object sender, EventArgs e)
        {
            txtWorkOrderNumber.Text = work_dtl[work_order];
            txtProjectCode.Text = work_dtl[project_code];
            txtProgramRecipe.Text = work_dtl[program_recipe];
            txtEmployee.Text = work_dtl[employee_name];
        }

        private void btnSetConfig_Click(object sender, EventArgs e)
        {
            try
            {
                string temp_station;
                string temp_process;
                temp_station = txtStation.Text;
                temp_process = txtProcess.Text;

                string[] config_txt = new string[1] { "{\r\n\tSTATION:\"" + temp_station +
                    "\",\r\n\tPROCESS:\"" + temp_process + "\"\r\n}" };

                string pathRoot = Path.Combine(@"C:\AutoGRN_config\luvo");
                DirectoryInfo directoryinfo = new DirectoryInfo(pathRoot);
                if (!directoryinfo.Exists)
                {
                    directoryinfo.Create();
                }

                File.WriteAllLines(pathRoot + @"\config.txt", config_txt);
                MessageBox.Show("Config Details Saved!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnResetConfig_Click(object sender, EventArgs e)
        {
            txtStation.Text = config[station];
            txtProcess.Text = config[process];
        }

        private void btnSetInit_Click(object sender, EventArgs e)
        {
            try
            {
                string temp_cc;
                string temp_un;
                string temp_token;
                string temp_ip;
                temp_cc = txtCC.Text;
                temp_un = txtUsername.Text;
                temp_token = txtToken.Text;
                temp_ip = txtIP.Text;


                string[] initial_txt = new string[1] {"{\r\n\tcc:\"" + temp_cc +
                    "\",\r\n\tun:\"" + temp_un +
                    "\",\r\n\ttoken:\"" + temp_token +
                    "\",\r\n\tip:\"" + temp_ip + "\"\r\n}" };

                string pathRoot = Path.Combine(@"C:\AutoGRN_config");
                DirectoryInfo directoryinfo = new DirectoryInfo(pathRoot);
                if (!directoryinfo.Exists)
                {
                    directoryinfo.Create();
                }

                File.WriteAllLines(pathRoot + @"\ngmes4-dll.cfg", initial_txt);
                MessageBox.Show("Initial Details Saved!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnResetInit_Click(object sender, EventArgs e)
        {
            txtCC.Text = cfg[cc];
            txtUsername.Text = cfg[un];
            txtToken.Text = cfg[token];
            txtIP.Text = cfg[ip];
        }

        private void btn_graphic2_Click(object sender, EventArgs e)
        {
            this.tabControl1.SelectedTab = tabPage1;
        }

        private void btn_exit3_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_exit2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_cntCMESDLL_Click(object sender, EventArgs e)
        {
            string cc_val = cfg[cc], un_val = cfg[un], token_val = cfg[token];
            cl = new CMES4DLL();
            if (cl == null)
            {
                txt_rtnCMESDLL.Text = "Connection Failed";
            }
            else if (cl.Init(cc_val, un_val, token_val))
            {
                txt_rtnCMESDLL.Text = "Connected";
                txt_rtnCMESDLL.ForeColor = Color.Green;
                isconnect = true;
            }
            else
            {
                txt_rtnCMESDLL.Text = "Invalid Token";
            }
        }

        private void btn_datetimeCMESDLL_Click(object sender, EventArgs e)
        {
            if (cl != null && isconnect)
            {
                string result = cl.GetDateTimeInServer();
                var jsdat = JsonConvert.DeserializeAnonymousType(result, new { d = "" });
                txt_rtnCMESDLL.Text = jsdat.d.ToString();
            }
            else
            {
                txt_rtnCMESDLL.Text = "Not Connected";
                txt_rtnCMESDLL.ForeColor = Color.Red;
            }
        }
        private void Home_Vendor_Changed(object sender, EventArgs e)
        {
            for (int check_vendor = 0; check_vendor < vendor_dtl_count; check_vendor++)
            {
                if (homeVendorCombobox.Text == vendor_name_dtl[check_vendor])
                {
                    WriteToLogFile($"Chosen {homeVendorCombobox.Text} profile.");
                    vc = vendor_code_dtl[check_vendor];
                    txt_VendorCode.Text = vendor_code_dtl[check_vendor];
                    if (vendor_part_dtl[check_vendor] == "N/A" || vendor_part_dtl[check_vendor] == "")
                    {
                        p_id = 999;
                        manual_part_no = manual_part_no_dtl[check_vendor];
                    }
                    else
                    {
                        p_id = int.Parse(vendor_part_dtl[check_vendor]) - 1;
                    }
                    if (vendor_qty_dtl[check_vendor] == "N/A" || vendor_qty_dtl[check_vendor] == "")
                    {
                        q_id = 999;
                        manual_quantity = manual_quantity_dtl[check_vendor];
                    }
                    else
                    {
                        q_id = int.Parse(vendor_qty_dtl[check_vendor]) - 1;
                    }
                    if (vendor_manu_dtl[check_vendor] == "N/A" || vendor_manu_dtl[check_vendor] == "")
                    {
                        m_id = 999;
                        manual_manufacturer = manual_manufacturer_dtl[check_vendor];
                    }
                    else
                    {
                        m_id = int.Parse(vendor_manu_dtl[check_vendor]) - 1;
                    }
                    if (vendor_code_order_dtl[check_vendor] == "N/A" || vendor_code_order_dtl[check_vendor] == "")
                    {
                        vc_id = 999;
                        manual_vendor_code = vendor_code_dtl[check_vendor];
                    }
                    else
                    {
                        vc_id = int.Parse(vendor_code_order_dtl[check_vendor]) - 1;
                    }
                    if (vendor_datecode_dtl[check_vendor] == "N/A")
                    {
                        vdc_id = 999;
                        manual_date_code = manual_date_code_dtl[check_vendor];
                    }
                    else
                    {
                        vdc_id = int.Parse(vendor_datecode_dtl[check_vendor]) - 1;
                    }
                    if (vendor_lotcode_dtl[check_vendor] == "N/A")
                    {
                        vlc_id = 999;
                        manual_lot_code = manual_lot_code_dtl[check_vendor];
                    }
                    else
                    {
                        vlc_id = int.Parse(vendor_lotcode_dtl[check_vendor]) - 1;
                    }
                    if (vendor_pono_dtl[check_vendor] == "N/A")
                    {
                        vpono_id = 999;
                        manual_po_no = manual_po_no_dtl[check_vendor];
                    }
                    else
                    {
                        vpono_id = int.Parse(vendor_pono_dtl[check_vendor]) - 1;
                    }
                    if (vendor_packingno_dtl[check_vendor] == "N/A")
                    {
                        vpackingno_id = 999;
                        manual_packing_no = manual_packing_no_dtl[check_vendor];
                    }
                    else
                    {
                        vpackingno_id = int.Parse(vendor_packingno_dtl[check_vendor]) - 1;
                    }
                    if (vendor_expiredate_dtl[check_vendor] == "N/A")
                    {
                        ved_id = 999;
                        manual_expire_date = manual_expire_date_dtl[check_vendor];
                    }
                    else
                    {
                        ved_id = int.Parse(vendor_expiredate_dtl[check_vendor]) - 1;
                    }
                    v_spliter = ",";
                    manufacturer_name = manufacturer_name_dtl[check_vendor];
                    v_capturetime = vendor_capturetime[check_vendor];
                    v_printtime = vendor_printtime[check_vendor];
                    v_codetype = vendor_codetype_dtl[check_vendor];
                    scanner_spliter = vendor_spliter[check_vendor];
                    check_spliter = vendor_spliter[check_vendor];
                    p_start = part_start_dtl[check_vendor];
                    q_start = qty_start_dtl[check_vendor];
                    m_start = manu_start_dtl[check_vendor];
                    vc_start = code_order_start_dtl[check_vendor];
                    vdc_start = datecode_start_dtl[check_vendor];
                    vlc_start = lotcode_start_dtl[check_vendor];
                    vpono_start = pono_start_dtl[check_vendor];
                    vpackingno_start = packingno_start_dtl[check_vendor];
                    ved_start = expiredate_start_dtl[check_vendor];
                    p_end = part_end_dtl[check_vendor];
                    q_end = qty_end_dtl[check_vendor];
                    m_end = manu_end_dtl[check_vendor];
                    vc_end = code_order_end_dtl[check_vendor];
                    vdc_end = datecode_end_dtl[check_vendor];
                    vlc_end = lotcode_end_dtl[check_vendor];
                    vpono_end = pono_end_dtl[check_vendor];
                    vpackingno_end = packingno_end_dtl[check_vendor];
                    ved_end = expiredate_end_dtl[check_vendor];

                    homeVendorCombobox.Text = vendor_name_dtl[check_vendor];

                    if (check_spliter == "T")
                    {
                        scanner_spliter = "\t";
                    }
                    return;
                }
            }
        }

        private void gp_RequiredField_Enter(object sender, EventArgs e)
        {

        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (vendor_dropdown.Text == "")
            {
                MessageBox.Show("Please Select Proper Vendor", "Vendor Empty!");
                vendor_dropdown.Focus();
                return;
            }

            DialogResult dialogResult = MessageBox.Show($"Confirm Delete {vendor_dropdown.Text}?", "Delete", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                string tempFile = Path.GetTempFileName();
                string pathRoot = Path.Combine(@"C:\AutoGRN_config");
                using (var sr = new StreamReader(pathRoot + @"\vendor.txt"))
                using (var sw = new StreamWriter(tempFile))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!line.StartsWith(vendor_dropdown.Text))
                            sw.WriteLine(line);
                    }
                }

                File.Delete(pathRoot + @"\vendor.txt");
                File.Move(tempFile, pathRoot + @"\vendor.txt");
                File.AppendAllText(pathRoot + @"\log.txt", $"{System.DateTime.Now.ToString()} Delete" + Environment.NewLine);
                load_vendor();
                WriteToLogFile($"Deleted Profile: {vendor_dropdown.Text}");
            }
        }
    }
}
