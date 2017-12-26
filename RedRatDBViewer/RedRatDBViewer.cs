using RedRat;
using RedRat.IR;
using RedRat.RedRat3;
using RedRat.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Timers;

namespace RedRatDatabaseViewer
{
    public partial class RedRatDBViewer : Form
    {

        private int Previous_Device = -1;
        private int Previous_Key = -1;
        private bool RC_Select1stSignalForDoubleOrToggleSignal = true;

        private RedRatDBParser RedRatData = new RedRatDBParser();

        //
        // Add UART Part
        //
        static SerialPort _serialPort;

        private void Serial_InitialSetting()
        {
            // Allow the user to set the appropriate properties.
            _serialPort.PortName = "COM14";
            _serialPort.BaudRate = 115200; // as default;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Encoding = Encoding.UTF8;

            // Set the read/write timeouts
            _serialPort.ReadTimeout = 2000;
            _serialPort.WriteTimeout = 2000;
        }

        private void Serial_UpdatePortName()
        {
            listBox1.Items.Clear();
            foreach (string comport_s in SerialPort.GetPortNames())
            {
                listBox1.Items.Add(comport_s);
            }
            if (listBox1.Items.Count > 0)
            {
                listBox1.SelectedIndex = 0;     // this can be modified to preferred default
                EnableConnectButton();
                UpdateToConnectButton();
            }
            else
            {
                DisableConnectButton();
                UpdateToConnectButton();
            }
        }

        private Boolean Serial_OpenPort(string PortName)
        {
            Boolean ret = false;
            try
            {
                _serialPort.PortName = PortName;
                _serialPort.Open();
                ret = true;
            }
            catch (Exception ex232)
            {
                Console.WriteLine("Serial_OpenPort Exception at PORT: " + PortName + " - " + ex232);
                ret = false;
            }
            return ret;
        }

        private Boolean Serial_ClosePort()
        {
            Boolean ret = false;
            string PortName = "Invalid _serialPort.PortName";
            try
            {
                PortName = _serialPort.PortName;
                _serialPort.Close();
                ret = true;
            }
            catch (Exception ex232)
            {
                Console.WriteLine("Serial_ClosePort Exception at PORT: " + PortName + " - " + ex232);
                ret = false;
            }
            return ret;
        }

        static bool _continue_serial_read_write = false;
        static uint Get_UART_Input = 0;
        static Thread readThread = null;
        private Queue<string> UART_READ_MSG_QUEUE = new Queue<string>();

        private void Start_SerialReadThread()
        {
            _continue_serial_read_write = true;
            readThread = new Thread(ReadSerialPortThread);
            readThread.Start();
        }
        private void Stop_SerialReadThread()
        {
            _continue_serial_read_write = false;
            if (readThread != null)
            {
                if (readThread.IsAlive)
                {
                    readThread.Join();
                }
            }
        }

        public void ReadSerialPortThread()
        {
            while (_continue_serial_read_write)
            {
                try
                {
                    if (_serialPort.IsOpen == true)
                    {
                        if (_serialPort.BytesToRead > 0)
                        {
                            _serialPort.ReadTimeout = 500;
                            string message = _serialPort.ReadLine();
                            {
                                if (Get_UART_Input > 0)
                                {
                                    Get_UART_Input--;
                                    UART_READ_MSG_QUEUE.Enqueue(message);
                                }
                                AppendSerialMessageLog(message);
                            }
                        }
                    }
                    else
                    {
                        _continue_serial_read_write = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ReadSerialPortThread - " + ex);
                    //AppendSerialMessageLog(ex.ToString());
                    //_continue_serial_read_write = false;
                }
            }
        }

        private void SendToSerial(byte[] byte_to_sent)
        {
            if (_serialPort.IsOpen == true)
            {
                AppendSerialMessageLog("Start Tx\n");
                Application.DoEvents();
                try
                {
                    // _serialPort.Write("This is a Test\n");
                    _serialPort.Write(byte_to_sent, 0, byte_to_sent.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SendToSerial - " + ex);
                    AppendSerialMessageLog(ex.ToString());
                }
            }
            else
            {
                //AppendSerialMessageLog("COM is closed and cannot send byte data\n");
            }
        }

        private int Tx_CNT = 0;

        private bool SendToSerial_v2(byte[] byte_to_sent)
        {
            bool return_value = false;

            if (_serialPort.IsOpen == true)
            {
                Tx_CNT++;
                Application.DoEvents();
                try
                {
                    int temp_index = 0;
                    const int fixed_length = 16;

                    while (temp_index < byte_to_sent.Length)
                    {
                        if ((temp_index + fixed_length) < byte_to_sent.Length)
                        {
                            _serialPort.Write(byte_to_sent, temp_index, fixed_length);
                            temp_index += fixed_length;
                        }
                        else
                        {
                            _serialPort.Write(byte_to_sent, temp_index, (byte_to_sent.Length - temp_index));
                            temp_index = byte_to_sent.Length;
                        }
                    }
                    return_value = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SendToSerial_v2 - " + ex);
                    return_value = false;
                }
            }
            else
            {
                //AppendSerialMessageLog("COM is closed and cannot send byte data\n");
                return_value = false;
            }
            AppendSerialMessageLog("\n===Tx:" + Tx_CNT.ToString() + " ");
            return return_value;
        }

        private void UpdateRCFunctionButtonAfterConnection()
        {
            if ((_serialPort.IsOpen == true))
            {
                if ((RedRatData != null) && (RedRatData.SignalDB != null) && (RedRatData.SelectedDevice != null) && (RedRatData.SelectedSignal != null))
                {
                    btnSingleRCPressed.Enabled = true;
                }
                else
                {
                    btnSingleRCPressed.Enabled = false;
                }
                btnCheckHeartBeat.Enabled = true;
                btnStopRCButton.Enabled = true;
                btnRepeatRC.Enabled = true;
            }
        }

        private void UpdateRCFunctionButtonAfterDisconnection()
        {
            btnSingleRCPressed.Enabled = false;
            btnCheckHeartBeat.Enabled = false;
            btnStopRCButton.Enabled = false;
        }

        private void UndoTemoparilyDisbleAllRCFunctionButtons()
        {
            if ((RedRatData != null) && (RedRatData.SignalDB != null) && (RedRatData.SelectedDevice != null) && (RedRatData.SelectedSignal != null))
            {
                btnSingleRCPressed.Enabled = true;
            }
            else
            {
                btnSingleRCPressed.Enabled = false;
            }
            btnCheckHeartBeat.Enabled = true;
            btnStopRCButton.Enabled = true;
            btnRepeatRC.Enabled = true;
            btnConnectionControl.Enabled = true;
        }

        private void TemoparilyDisbleAllRCFunctionButtons()
        {
            btnSingleRCPressed.Enabled = false;
            btnCheckHeartBeat.Enabled = false;
            btnStopRCButton.Enabled = false;
            btnRepeatRC.Enabled = false;
            btnConnectionControl.Enabled = false;
        }

        private void EnableRefreshCOMButton()
        {
            btnFreshCOMNo.Enabled = true;
        }

        private void DisableRefreshCOMButton()
        {
            btnFreshCOMNo.Enabled = false;
        }

        private void EnableConnectButton()
        {
            btnConnectionControl.Enabled = true;
        }

        private void DisableConnectButton()
        {
            btnConnectionControl.Enabled = false;
        }

        const string CONNECT_UART_STRING_ON_BUTTON = "Connect UART";
        const string DISCONNECT_UART_STRING_ON_BUTTON = "Disconnect UART";

        private void UpdateToConnectButton()
        {
            btnConnectionControl.Text = CONNECT_UART_STRING_ON_BUTTON;
        }

        private void UpdateToDisconnectButton()
        {
            btnConnectionControl.Text = DISCONNECT_UART_STRING_ON_BUTTON;
        }

        private void btnFreshCOMNo_Click(object sender, System.EventArgs e)
        {
            Serial_UpdatePortName();
        }

        //
        // Print Serial Port Message on RichTextBox
        //
        delegate void AppendSerialMessageCallback(string text);
        public void AppendSerialMessageLog(string my_str)
        {
            if (this.rtbSignalData.InvokeRequired)
            {
                AppendSerialMessageCallback d = new AppendSerialMessageCallback(AppendSerialMessageLog);
                this.Invoke(d, new object[] { my_str });
            }
            else
            {
                this.rtbSignalData.AppendText(my_str);
                this.rtbSignalData.ScrollToCaret();
            }
        }

        private void btnConnectionControl_Click(object sender, EventArgs e)
        {
            if (btnConnectionControl.Text.Equals(CONNECT_UART_STRING_ON_BUTTON, StringComparison.Ordinal)) // Check if button is showing "Connect" at this moment.
            {   // User to connect
                if (_serialPort.IsOpen == false)
                {
                    string curItem = listBox1.SelectedItem.ToString();
                    if (Serial_OpenPort(curItem) == true)
                    {
                        UpdateToDisconnectButton();
                        DisableRefreshCOMButton();
                        UpdateRCFunctionButtonAfterConnection();
                        Start_SerialReadThread();
                    }
                    else
                    {
                        rtbSignalData.AppendText(DateTime.Now.ToString("h:mm:ss tt") + " - Cannot connect to RS232.\n");
                    }
                }
            }
            else
            {   // User to disconnect
                if (_serialPort.IsOpen == true)
                {
                    Stop_SerialReadThread();
                    if (Serial_ClosePort() == true)
                    {
                        UpdateToConnectButton();
                        EnableRefreshCOMButton();
                        UpdateRCFunctionButtonAfterDisconnection();
                    }
                    else
                    {
                        rtbSignalData.AppendText(DateTime.Now.ToString("h:mm:ss tt") + " - Cannot disconnect from RS232.\n");
                    }
                }
            }
        }
        //
        // End of UART part
        //

        /// <summary>
        /// Simply finds the first RedRat3 attached to this computer.
        /// </summary>
        private IRedRat3 FindRedRat3()
        {
            var rr3li = RRUtil.FindRedRats(LocationInfo.RedRatType.RedRat3).FirstOrDefault();

            if (rr3li == null)
            {
                throw new Exception("Unable to find any RedRat3 devices on this computer.");
            }
            return rr3li.GetRedRat() as IRedRat3; ;
        }

        private void Update_RC_Signal_Display_Content()
        {
            int pulse_high;
            string temp_freq_str = RedRatData.RC_ModutationFreq().ToString();
            rtbDecodeRCSignal.Text = "Carrier Frequency:" + temp_freq_str + " Hz\n";
            lbFreq.Text = temp_freq_str + " Hz";
            List<double> pulse_width = RedRatData.GetTxPulseWidth();
            pulse_high = 1;
            foreach (var val in pulse_width)
            {
                rtbDecodeRCSignal.AppendText(pulse_high.ToString() + ":" + Convert.ToInt32(val).ToString() + "\n");
                pulse_high = (pulse_high != 0) ? 0 : 1;
            }
        }

        //
        // Form Events
        //

        bool ThisTimeDoNotUpdateMessageBox = false;

        private void SingleOutput_Click(object sender, EventArgs e)
        {
            if (RedRatData.Signal_Type_Supported != true)
            {
                return;
            }

            if ((Previous_Device < 0) || (Previous_Key < 0))
            {
                // return immediately when No Selected Device or no Selected Signal
                return;
            }

            TemoparilyDisbleAllRCFunctionButtons();

            //btnSingleRCPressed.Enabled = false;
            //btnCheckHeartBeat.Enabled = false;
            //btnStopRCButton.Enabled = false;
            //btnConnectionControl.Enabled = false;
            btnGetRCFile.Enabled = false;

            // Use UART to transmit RC signal
            int rc_duration = SendOneRC() / 1000 + 1;
            HomeMade_Delay(rc_duration);

            // Update 2nd Signal checkbox
            if ((RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (RedRatData.RC_ToggleData_Length_Value() > 0))
            {
                // Switch to the other signal in display
                ThisTimeDoNotUpdateMessageBox = true;
                RC_Select1stSignalForDoubleOrToggleSignal = !RC_Select1stSignalForDoubleOrToggleSignal;
                RedRatData.RedRatSelectRCSignal(listboxRCKey.SelectedIndex, RC_Select1stSignalForDoubleOrToggleSignal);
                chkSelect2ndSignal.Checked = RC_Select1stSignalForDoubleOrToggleSignal;
            }
            //
            // End of Tx 
            //

            btnGetRCFile.Enabled = true;
            UndoTemoparilyDisbleAllRCFunctionButtons();
            //btnConnectionControl.Enabled = true;
            //btnCheckHeartBeat.Enabled = true;
            //btnStopRCButton.Enabled = true;
            //btnSingleRCPressed.Enabled = true;
        }

        private void btnCheckHeartBeat_Click(object sender, EventArgs e)
        {
            //btnSingleRCPressed.Enabled = false;
            //btnCheckHeartBeat.Enabled = false;
            //btnStopRCButton.Enabled = false;
            TemoparilyDisbleAllRCFunctionButtons();
            SendToSerial_v2(Prepare_Say_HI_CMD().ToArray());
            UndoTemoparilyDisbleAllRCFunctionButtons();
            //btnCheckHeartBeat.Enabled = true;
            //btnStopRCButton.Enabled = true;
            //btnSingleRCPressed.Enabled = true;
        }

        private void StopCMDButton_Click(object sender, EventArgs e)
        {
            //btnSingleRCPressed.Enabled = false;
            //btnCheckHeartBeat.Enabled = false;
            //btnStopRCButton.Enabled = false;
            TemoparilyDisbleAllRCFunctionButtons();
            SendToSerial_v2(Prepare_STOP_CMD().ToArray());
            UndoTemoparilyDisbleAllRCFunctionButtons();
            //btnCheckHeartBeat.Enabled = true;
            //btnStopRCButton.Enabled = true;
            //btnSingleRCPressed.Enabled = true;
        }

        private void listboxAVDeviceList_SelectedIndexChanged(object sender, EventArgs ev)
        {
            int Current_Device = listboxAVDeviceList.SelectedIndex;
            if (Previous_Device != Current_Device)
            {
                string devicename = listboxAVDeviceList.SelectedItem.ToString();
                if (RedRatData.RedRatSelectDevice(devicename))
                {
                    listboxRCKey.Items.Clear();
                    listboxRCKey.Items.AddRange(RedRatData.RedRatGetRCNameList().ToArray());
                    listboxRCKey.SelectedIndex = 0;
                    RedRatData.RedRatSelectRCSignal(0, RC_Select1stSignalForDoubleOrToggleSignal);
                    Previous_Key = -1;
                    this.listboxRCKey_SelectedIndexChanged(sender, ev);
                    Previous_Device = Current_Device;
                }
                else
                {
                    Console.WriteLine("Select Device Error: " + devicename);
                }
            }
        }

        private void listboxRCKey_SelectedIndexChanged(object sender, EventArgs ev)
        {
            int Current_Key = listboxRCKey.SelectedIndex;

            if (Previous_Key != Current_Key)
            {
                Previous_Key = Current_Key;
                string rcname = listboxRCKey.SelectedItem.ToString();
                if (RedRatData.RedRatSelectRCSignal(rcname, RC_Select1stSignalForDoubleOrToggleSignal))
                {
                    Type temp_type = RedRatData.RedRatSelectedSignalType();
                    lbModulationType.Text = temp_type.ToString();
                    if (RedRatData.Signal_Type_Supported == false)
                    {
                        chkSelect2ndSignal.Enabled = false;
                        rbDoubleSignalLED.Checked = false;
                        //Update_RC_Signal_Display_Content();
                        rtbDecodeRCSignal.Text = temp_type + " is not supported, or signal data is corrupted.";
                        //UpdateRCDataOnForm();
                        dgvPulseData.Rows.Clear();
                        dgvToggleBits.Rows.Clear();
                        rtbSignalData.Text = RedRatData.TxSignal.ToString();
                    }
                    else
                    {
                        if (temp_type == typeof(DoubleSignal))
                        {
                            chkSelect2ndSignal.Enabled = true;
                            rbDoubleSignalLED.Checked = true;
                        }
                        else if (RedRatData.RC_ToggleData_Length_Value() > 0)
                        {
                            chkSelect2ndSignal.Enabled = true;
                            rbDoubleSignalLED.Checked = false;
                        }
                        else
                        {
                            chkSelect2ndSignal.Enabled = false;
                            rbDoubleSignalLED.Checked = false;
                        }
                        Update_RC_Signal_Display_Content();
                        UpdateRCDataOnForm();
                        if (ThisTimeDoNotUpdateMessageBox)
                        {
                            ThisTimeDoNotUpdateMessageBox = false;
                        }
                        else
                        {
                            rtbSignalData.Text = RedRatData.TxSignal.ToString();
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Select RC Error: " + rcname);
                }
            }
        }

        private void UpdateRCDataOnForm()
        {
            double[] RC_Lengths = RedRatData.RC_Lengths();
            ToggleBit[] RC_ToggleData = RedRatData.RC_ToggleData();

            dgvPulseData.Rows.Clear();
            if (RC_Lengths != null)
            {
                int index = 0;
                foreach (var len in RC_Lengths)
                {
                    string[] str = { len.ToString() };
                    dgvPulseData.Rows.Add(str);
                    dgvPulseData.Rows[index].HeaderCell.Value = String.Format("{0}", index);
                    index++;
                }
            }

            dgvToggleBits.Rows.Clear();
            if (RC_ToggleData != null)
            {
                int index = 0;
                foreach (var toggle_bit in RC_ToggleData)
                {
                    string[] str = { RC_Lengths[toggle_bit.len1].ToString(), RC_Lengths[toggle_bit.len2].ToString() };
                    int bit_no = toggle_bit.bitNo;
                    if ((index > 0) && (bit_no < Convert.ToInt64(dgvToggleBits.Rows[index - 1].HeaderCell.Value)))
                    {
                        dgvToggleBits.Rows.Insert(index - 1, str);
                        dgvToggleBits.Rows[index - 1].HeaderCell.Value = String.Format("{0}", bit_no);
                    }
                    else
                    {
                        dgvToggleBits.Rows.Add(str);
                        dgvToggleBits.Rows[index].HeaderCell.Value = String.Format("{0}", bit_no);
                    }
                    index++;
                }
            }
        }

        private void UpdateRCDoubleSignalCheckBoxValue(bool if_checked)
        {
            chkSelect2ndSignal.Checked = if_checked;
        }

        private void chkSelect2ndSignal_CheckedChanged(object sender, EventArgs e)
        {
            RC_Select1stSignalForDoubleOrToggleSignal = chkSelect2ndSignal.Checked;
            RedRatData.RedRatSelectRCSignal(listboxRCKey.SelectedIndex, RC_Select1stSignalForDoubleOrToggleSignal);

            //double RC_ModutationFreq = RedRatData.RC_ModutationFreq();
            //double[] RC_Lengths = RedRatData.RC_Lengths();
            //byte[] RC_MainSignal = RedRatData.RC_MainSignal();
            //byte[] RC_RepeatSignal = RedRatData.RC_RepeatSignal() ;
            //int RC_NoRepeats = RedRatData.RC_NoRepeats();
            //double RC_IntraSigPause = RedRatData.RC_IntraSigPause();
            Update_RC_Signal_Display_Content();
            UpdateRCDataOnForm();
            if (ThisTimeDoNotUpdateMessageBox)
            {
                ThisTimeDoNotUpdateMessageBox = false;
            }
            else
            {
                rtbSignalData.Text = RedRatData.TxSignal.ToString();
            }
        }

        private void rbDoubleSignalLED_CheckedChanged(object sender, EventArgs e)
        {
            if (rbDoubleSignalLED.Checked == true)
            {
                rbDoubleSignalLED.ForeColor = System.Drawing.Color.Blue;
            }
            else
            {
                rbDoubleSignalLED.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void RedRatDBViewer_Load(object sender, EventArgs e)
        {
            _serialPort = new SerialPort();
            Serial_InitialSetting();
            Serial_UpdatePortName();
        }

        private void RedRatDBViewer_Closing(Object sender, FormClosingEventArgs e)
        {
            if (_serialPort.IsOpen == true)
            {
                Stop_SerialReadThread();
            }
        }

        public RedRatDBViewer()
        {
            InitializeComponent();
        }

        private void btnGetRCFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = ".\\",
                Filter = "RedRat Device files (*.xml)|*.xml|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    //Console.WriteLine("Searching for RedRat...");
                    // using (var rr3 = FindRedRat3())
                    {
                        //Console.WriteLine("RedRat Found. Loading DB file...");
                        //RedRatData = new RedRatDBParser();
                        RedRatData.RedRatLoadSignalDB(openFileDialog1.FileName); // Device 0 Signal 0 will be selected after RC database loaded
                        HomeMade_Delay(16);
                        //
                        // Update Form Display Data according to content of RedRatData.SelectedSignal
                        //
                        rtbSignalData.Text = "";
                        listboxAVDeviceList.Items.Clear();
                        listboxRCKey.Items.Clear();
                        if (RedRatData.SignalDB != null)
                        {
                            Previous_Device = -1;
                            listboxAVDeviceList.Items.AddRange(RedRatData.RedRatGetDBDeviceNameList().ToArray());
                            if (listboxAVDeviceList.Items.Count > 0)
                            {
                                listboxAVDeviceList.SelectedIndex = 0;
                                HomeMade_Delay(16);
                                RedRatData.RedRatSelectDevice(0);
                            }
                            if (RedRatData.SelectedDevice != null)
                            {
                                this.listboxAVDeviceList_SelectedIndexChanged(sender, e);
                                Previous_Key = -1;
                                listboxRCKey.Items.AddRange(RedRatData.RedRatGetRCNameList().ToArray());
                                if (listboxRCKey.Items.Count > 0)
                                {
                                    listboxRCKey.SelectedIndex = 0;
                                    HomeMade_Delay(16);
                                    RedRatData.RedRatSelectRCSignal(0);
                                    if (RedRatData.SelectedSignal != null)
                                    {
                                        UpdateRCDataOnForm();
                                        this.listboxRCKey_SelectedIndexChanged(sender, e); // Force to update Device list selection box (RC selection box will be forced to updated within listboxAVDeviceList_SelectedIndexChanged()
                                        listboxAVDeviceList.Enabled = true;
                                        listboxRCKey.Enabled = true;
                                    }
                                }
                            }
                            else
                            {
                                rtbDecodeRCSignal.Text = "RC data file data is corrupted!";
                                UpdateRCDataOnForm();
                                Previous_Device = -1;
                                Previous_Key = -1;
                                listboxAVDeviceList.Enabled = true;
                                listboxRCKey.Enabled = false;
                            }
                        }
                        else
                        {
                            rtbDecodeRCSignal.Text = "RC data file data is corrupted!";
                            UpdateRCDataOnForm();
                            Previous_Device = -1;
                            Previous_Key = -1;
                            listboxAVDeviceList.Enabled = false;
                            listboxRCKey.Enabled = false;
                        }

                        UpdateRCFunctionButtonAfterConnection();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        //
        // 跟小藍鼠有關係的程式代碼與範例程式區 -- 開始
        //
        enum ENUM_CMD_STATUS
        {
            ENUM_CMD_IDLE = 0,
            ENUM_CMD_UNKNOWN_LAST = 0x7e,
            ENUM_CMD_INPUT_TX_SIGNAL = 0x7f,
            ENUM_CMD_ADD_REPEAT_COUNT = 0x80,
            ENUM_CMD_CODE_0X81 = 0x81,
            ENUM_CMD_CODE_0X82 = 0x82,
            ENUM_CMD_CODE_0X83 = 0x83,
            ENUM_CMD_CODE_0X84 = 0x84,
            ENUM_CMD_CODE_0X85 = 0x85,
            ENUM_CMD_CODE_0X86 = 0x86,
            ENUM_CMD_CODE_0X87 = 0x87,
            ENUM_CMD_CODE_0X88 = 0x88,
            ENUM_CMD_CODE_0X89 = 0x89,
            ENUM_CMD_CODE_0X8A = 0x8a,
            ENUM_CMD_CODE_0X8B = 0x8b,
            ENUM_CMD_CODE_0X8C = 0x8c,
            ENUM_CMD_CODE_0X8D = 0x8d,
            ENUM_CMD_CODE_0X8E = 0x8e,
            ENUM_CMD_CODE_0X8F = 0x8f,
            ENUM_CMD_CODE_0X90 = 0x90,
            ENUM_CMD_CODE_0X91 = 0x91,
            ENUM_CMD_CODE_0X92 = 0x92,
            ENUM_CMD_CODE_0X93 = 0x93,
            ENUM_CMD_CODE_0X94 = 0x94,
            ENUM_CMD_CODE_0X95 = 0x95,
            ENUM_CMD_CODE_0X96 = 0x96,
            ENUM_CMD_CODE_0X97 = 0x97,
            ENUM_CMD_CODE_0X98 = 0x98,
            ENUM_CMD_CODE_0X99 = 0x99,
            ENUM_CMD_CODE_0X9A = 0x9a,
            ENUM_CMD_CODE_0X9B = 0x9b,
            ENUM_CMD_CODE_0X9C = 0x9c,
            ENUM_CMD_CODE_0X9D = 0x9d,
            ENUM_CMD_CODE_0X9E = 0x9e,
            ENUM_CMD_ENTER_ISP_MODE = 0x9f,
            ENUM_CMD_SET_GPIO_SINGLE_BIT = 0xa0,    // End of command with 2-byte parameter
            ENUM_CMD_CODE_0XA1 = 0xa1,
            ENUM_CMD_CODE_0XA2 = 0xa2,
            ENUM_CMD_CODE_0XA3 = 0xa3,
            ENUM_CMD_CODE_0XA4 = 0xa4,
            ENUM_CMD_CODE_0XA5 = 0xa5,
            ENUM_CMD_CODE_0XA6 = 0xa6,
            ENUM_CMD_CODE_0XA7 = 0xa7,
            ENUM_CMD_CODE_0XA8 = 0xa8,
            ENUM_CMD_CODE_0XA9 = 0xa9,
            ENUM_CMD_CODE_0XAA = 0xaa,
            ENUM_CMD_CODE_0XAB = 0xab,
            ENUM_CMD_CODE_0XAC = 0xac,
            ENUM_CMD_CODE_0XAD = 0xad,
            ENUM_CMD_CODE_0XAE = 0xae,
            ENUM_CMD_CODE_0XAF = 0xaf,
            ENUM_CMD_CODE_0XB0 = 0xb0,
            ENUM_CMD_CODE_0XB1 = 0xb1,
            ENUM_CMD_CODE_0XB2 = 0xb2,
            ENUM_CMD_CODE_0XB3 = 0xb3,
            ENUM_CMD_CODE_0XB4 = 0xb4,
            ENUM_CMD_CODE_0XB5 = 0xb5,
            ENUM_CMD_CODE_0XB6 = 0xb6,
            ENUM_CMD_CODE_0XB7 = 0xb7,
            ENUM_CMD_CODE_0XB8 = 0xb8,
            ENUM_CMD_CODE_0XB9 = 0xb9,
            ENUM_CMD_CODE_0XBA = 0xba,
            ENUM_CMD_CODE_0XBB = 0xbb,
            ENUM_CMD_CODE_0XBC = 0xbc,
            ENUM_CMD_CODE_0XBD = 0xbd,
            ENUM_CMD_CODE_0XBE = 0xbe,
            ENUM_CMD_CODE_0XBF = 0xbf,
            ENUM_CMD_SET_GPIO_ALL_BIT = 0xc0,       // End of command with byte parameter
            ENUM_CMD_CODE_0XC1 = 0xc1,
            ENUM_CMD_CODE_0XC2 = 0xc2,
            ENUM_CMD_CODE_0XC3 = 0xc3,
            ENUM_CMD_CODE_0XC4 = 0xc4,
            ENUM_CMD_CODE_0XC5 = 0xc5,
            ENUM_CMD_CODE_0XC6 = 0xc6,
            ENUM_CMD_CODE_0XC7 = 0xc7,
            ENUM_CMD_CODE_0XC8 = 0xc8,
            ENUM_CMD_CODE_0XC9 = 0xc9,
            ENUM_CMD_CODE_0XCA = 0xca,
            ENUM_CMD_CODE_0XCB = 0xcb,
            ENUM_CMD_CODE_0XCC = 0xcc,
            ENUM_CMD_CODE_0XCD = 0xcd,
            ENUM_CMD_CODE_0XCE = 0xce,
            ENUM_CMD_CODE_0XCF = 0xcf,
            ENUM_CMD_CODE_0XD0 = 0xd0,
            ENUM_CMD_CODE_0XD1 = 0xd1,
            ENUM_CMD_CODE_0XD2 = 0xd2,
            ENUM_CMD_CODE_0XD3 = 0xd3,
            ENUM_CMD_CODE_0XD4 = 0xd4,
            ENUM_CMD_CODE_0XD5 = 0xd5,
            ENUM_CMD_CODE_0XD6 = 0xd6,
            ENUM_CMD_CODE_0XD7 = 0xd7,
            ENUM_CMD_CODE_0XD8 = 0xd8,
            ENUM_CMD_CODE_0XD9 = 0xd9,
            ENUM_CMD_CODE_0XDA = 0xda,
            ENUM_CMD_CODE_0XDB = 0xdb,
            ENUM_CMD_CODE_0XDC = 0xdc,
            ENUM_CMD_CODE_0XDD = 0xdd,
            ENUM_CMD_CODE_0XDE = 0xde,
            ENUM_CMD_CODE_0XDF = 0xdf,
            ENUM_CMD_GET_GPIO_INPUT = 0xe0,         // End of command only code
            ENUM_CMD_GET_SENSOR_VALUE = 0xe1,
            ENUM_CMD_CODE_0XE2 = 0xe2,
            ENUM_CMD_CODE_0XE3 = 0xe3,
            ENUM_CMD_CODE_0XE4 = 0xe4,
            ENUM_CMD_CODE_0XE5 = 0xe5,
            ENUM_CMD_CODE_0XE6 = 0xe6,
            ENUM_CMD_CODE_0XE7 = 0xe7,
            ENUM_CMD_CODE_0XE8 = 0xe8,
            ENUM_CMD_CODE_0XE9 = 0xe9,
            ENUM_CMD_CODE_0XEA = 0xea,
            ENUM_CMD_CODE_0XEB = 0xeb,
            ENUM_CMD_CODE_0XEC = 0xec,
            ENUM_CMD_CODE_0XED = 0xed,
            ENUM_CMD_CODE_0XEE = 0xee,
            ENUM_CMD_CODE_0XEF = 0xef,
            ENUM_CMD_CODE_0XF0 = 0xf0,
            ENUM_CMD_CODE_0XF1 = 0xf1,
            ENUM_CMD_CODE_0XF2 = 0xf2,
            ENUM_CMD_CODE_0XF3 = 0xf3,
            ENUM_CMD_CODE_0XF4 = 0xf4,
            ENUM_CMD_CODE_0XF5 = 0xf5,
            ENUM_CMD_CODE_0XF6 = 0xf6,
            ENUM_CMD_CODE_0XF7 = 0xf7,
            ENUM_CMD_GET_TX_CURRENT_REPEAT_COUNT = 0xf8,
            ENUM_CMD_GET_TX_RUNNING_STATUS = 0xf9,
            ENUM_CMD_RETURN_SW_VER = 0xfa,
            ENUM_CMD_RETURN_BUILD_TIME = 0xfb,
            ENUM_CMD_RETURN_CMD_VERSION = 0xfc,
            ENUM_CMD_SAY_HI = 0xfd,
            ENUM_CMD_STOP_ALL = 0xfe,
            ENUM_SYNC_BYTE_VALUE = 0xff,
            ENUM_CMD_VERSION_V100 = 0x100,
            ENUM_CMD_VERSION_V200 = 0x200,
            ENUM_CMD_VERSION_V201 = 0x201,
            ENUM_CMD_VERSION_V202 = 0x202,
            ENUM_CMD_VERSION_CURRENT_PLUS_1,
            ENUM_CMD_STATE_MAX
        };

        const uint CMD_CODE_LOWER_LIMIT = (0x80);
        const uint CMD_SEND_COMMAND_CODE_WITH_DOUBLE_WORD = (0x80);
        const uint CMD_SEND_COMMAND_CODE_WITH_WORD = (0xa0);
        const uint CMD_SEND_COMMAND_CODE_WITH_BYTE = (0xc0);
        const uint CMD_SEND_COMMAND_CODE_ONLY = (0xe0);
        const uint CMD_CODE_UPPER_LIMIT = (0xfe);
        const uint ISP_PASSWORD = (0x46574154);

        //
        // Input parameter is 32-bit unsigned data
        //
        private List<byte> Convert_data_to_Byte(UInt32 input_data)
        {
            Stack<Byte> byte_data = new Stack<Byte>();
            UInt32 value = input_data;
            byte_data.Push(Convert.ToByte(value & 0xff));
            value >>= 8;
            byte_data.Push(Convert.ToByte(value & 0xff));
            value >>= 8;
            byte_data.Push(Convert.ToByte(value & 0xff));
            value >>= 8;
            byte_data.Push(Convert.ToByte(value & 0xff));
            List<byte> data_to_sent = new List<byte>();
            foreach (var single_byte in byte_data)
            {
                data_to_sent.Add(single_byte);
            }
            return data_to_sent;
        }

        //
        // Input parameter is 16-bit unsigned data
        //
        private List<byte> Convert_data_to_Byte(UInt16 input_data)
        {
            Stack<Byte> byte_data = new Stack<Byte>();
            UInt16 value = input_data;
            byte_data.Push(Convert.ToByte(value & 0xff));
            value >>= 8;
            byte_data.Push(Convert.ToByte(value & 0xff));
            List<byte> data_to_sent = new List<byte>();
            foreach (var single_byte in byte_data)
            {
                data_to_sent.Add(single_byte);
            }
            return data_to_sent;
        }

        //
        // Input parameter is 8-bit unsigned data
        //
        private List<byte> Convert_data_to_Byte(byte input_data)
        {
            List<byte> data_to_sent = new List<byte>();
            data_to_sent.Add(input_data);
            return data_to_sent;
        }

        //
        // This is dedicated for witdh-data of IR signal
        //
        private List<byte> Convert_data_to_Byte_modified(uint width_value)
        {
            Stack<Byte> byte_data = new Stack<Byte>();
            if (width_value > 0x7fff)
            {
                UInt32 value = width_value | 0x80000000;            // Specify this is 4 bytes data in our protocol
                byte_data.Push(Convert.ToByte(value & 0xff));
                value >>= 8;
                byte_data.Push(Convert.ToByte(value & 0xff));
                value >>= 8;
                byte_data.Push(Convert.ToByte(value & 0xff));
                value >>= 8;
                byte_data.Push(Convert.ToByte(value & 0xff));
            }
            else
            {
                UInt32 value = width_value;
                byte_data.Push(Convert.ToByte(value & 0xff));
                value >>= 8;
                byte_data.Push(Convert.ToByte(value & 0xff));
            }
            List<byte> data_to_sent = new List<byte>();
            foreach (var single_byte in byte_data)
            {
                data_to_sent.Add(single_byte);
            }
            return data_to_sent;
        }

        //
        // Checksum is currently XOR all data (excluding sync header)
        //
        private Byte CheckSum = 0;
        public void ClearCheckSum()
        {
            CheckSum = 0;
        }

        public void UpdateCheckSum(byte value)
        {
            CheckSum ^= value;
        }

        public byte GetCheckSum()
        {
            return CheckSum;
        }

        public bool CompareCheckSum()
        {
            return (CheckSum == 0) ? true : false;
        }

        //
        // To get UART data byte for each command
        //
        public List<byte> Prepare_STOP_CMD()
        {
            List<byte> data_to_sent = new List<byte>();

            ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for header
            data_to_sent.Add((Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_STOP_ALL)));
            UpdateCheckSum((Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_STOP_ALL)));
            data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

        public List<byte> Prepare_Say_HI_CMD()
        {
            List<byte> data_to_sent = new List<byte>();

            ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SAY_HI));
            UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SAY_HI));
            data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

        public List<byte> Prepare_Get_RC_Repeat_Count()
        {
            List<byte> data_to_sent = new List<byte>();

            ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_TX_CURRENT_REPEAT_COUNT));
            UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_TX_CURRENT_REPEAT_COUNT));
            data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

         public List<byte> Prepare_Get_RC_Current_Running_Status()
        {
            List<byte> data_to_sent = new List<byte>();

            ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_TX_RUNNING_STATUS));
            UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_TX_RUNNING_STATUS));
            data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

        public List<byte> Prepare_Enter_ISP_CMD()
        {
            List<byte> data_to_sent = new List<byte>();

            ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_ENTER_ISP_MODE));
            UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_ENTER_ISP_MODE));
            List<byte> input_param_in_byte = Convert_data_to_Byte(ISP_PASSWORD);
            foreach (byte temp in input_param_in_byte)
            {
                data_to_sent.Add(temp);
                UpdateCheckSum(temp);
            }
            data_to_sent.Add(GetCheckSum());
            //data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

        public List<byte> Prepare_Send_Repeat_Cnt_Add_CMD(UInt32 cnt = 0)
        {
            List<byte> data_to_sent = new List<byte>();

            ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_ADD_REPEAT_COUNT));
            UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_ADD_REPEAT_COUNT));
            List<byte> input_param_in_byte = Convert_data_to_Byte(cnt);
            foreach (byte temp in input_param_in_byte)
            {
                data_to_sent.Add(temp);
                UpdateCheckSum(temp);
            }
            data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

        public List<byte> Prepare_Send_Input_CMD(byte input_cmd, UInt32 input_param = 0)
        {
            if ((input_cmd < CMD_CODE_LOWER_LIMIT) || (input_cmd > CMD_CODE_UPPER_LIMIT))
            {
                return Prepare_Say_HI_CMD();
            }

            List<byte> data_to_sent = new List<byte>();
            ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for header
            data_to_sent.Add(input_cmd);
            UpdateCheckSum(input_cmd);
            if ((input_cmd >= CMD_SEND_COMMAND_CODE_WITH_BYTE) && (input_cmd < CMD_SEND_COMMAND_CODE_ONLY))
            {
                byte temp = Convert.ToByte(input_param & 0xff);
                data_to_sent.Add(Convert.ToByte(temp & 0xff));
                UpdateCheckSum(temp);
            }
            else if ((input_cmd >= CMD_SEND_COMMAND_CODE_WITH_WORD) && (input_cmd < CMD_SEND_COMMAND_CODE_WITH_BYTE))
            {
                List<byte> input_param_in_byte = Convert_data_to_Byte(Convert.ToUInt16(input_param & 0xffff));
                foreach (byte temp in input_param_in_byte)
                {
                    data_to_sent.Add(temp);
                    UpdateCheckSum(temp);
                }
            }
            else if ((input_cmd >= CMD_SEND_COMMAND_CODE_WITH_DOUBLE_WORD) && (input_cmd < CMD_SEND_COMMAND_CODE_WITH_WORD))
            {
                List<byte> input_param_in_byte = Convert_data_to_Byte(Convert.ToUInt32(input_param & 0xffffffff));
                foreach (byte temp in input_param_in_byte)
                {
                    data_to_sent.Add(temp);
                    UpdateCheckSum(temp);
                }
            }
            data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

        public int SendOneRC(byte default_repeat_cnt = 0)
        {
            // Precondition
            //   1. Load RC Database by RedRatLoadSignalDB()
            //   2. Select Device by RedRatSelectDevice() using device_name or index_no
            //   3. Select RC Signal by RedRatSelectRCSignal() using rc_name or index_no --> specify false at 2nd input parameter if need to Tx 2nd signal of Double signal / Toggle Bits Signal
            Contract.Requires(RedRatData != null);
            Contract.Requires(RedRatData.SignalDB != null);
            Contract.Requires(RedRatData.SelectedDevice != null);
            Contract.Requires(RedRatData.SelectedSignal != null);
            Contract.Requires(RedRatData.Signal_Type_Supported == true);

            if ((RedRatData == null))
            {
                return 0;
            }
            // Execution in this function
            //   4. Get complete pulse width data by GetTxPulseWidth()
            //   5. Combine pulse width data with other RC information into one array
            //   6. UART Tx this array
            //   7. For Double signal or Toggle Bit signal, switch to next one

            // Step 4
            List<byte> data_to_sent = new List<byte>();
            List<byte> pulse_packet = new List<byte>();
            List<double> pulse_width = RedRatData.GetTxPulseWidth();
            int total_us = 0;
            foreach (var val in pulse_width)
            {
                pulse_packet.AddRange(Convert_data_to_Byte_modified(Convert.ToUInt32(val)));
                total_us += Convert.ToInt32(val);
            }

            // Step 5
            Byte temp_byte, duty_cycle = 33;
            double RC_ModutationFreq = RedRatData.RC_ModutationFreq();

            // (1) Packet header -- must start with at least 2 times 0xff - no need to calcuate checksum for header
            {
                data_to_sent.Add(0xff);
                data_to_sent.Add(0xff);
                ClearCheckSum();
            }

            // (2) Command
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_INPUT_TX_SIGNAL));
            UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_INPUT_TX_SIGNAL));

            // (3) how many times to repeat RC (max 0xff)
            {
                const byte repeat_count_max = 0xff;
                if (default_repeat_cnt <= repeat_count_max)
                {
                    data_to_sent.Add(default_repeat_cnt);        // Repeat_No
                    UpdateCheckSum(default_repeat_cnt);
                    total_us *= (default_repeat_cnt > 0) ? (default_repeat_cnt + 1) : 1;
                }
                else
                {
                    Console.WriteLine("Repeat Count is out of range (>" + repeat_count_max.ToString() + "), using " + repeat_count_max.ToString() + " instead.");
                    data_to_sent.Add(repeat_count_max);        // Repeat_No
                    UpdateCheckSum(repeat_count_max);
                    total_us *= (repeat_count_max > 0) ? (repeat_count_max + 1) : 1;
                }
            }
            // (4) Duty Cycle range is 0-100, other values are reserved
            {
                const byte default_duty_cycle = 33, max_duty_cycle = 100;
                if (duty_cycle <= max_duty_cycle)
                {
                    data_to_sent.Add(duty_cycle);
                    UpdateCheckSum(duty_cycle);
                }
                else
                {
                    Console.WriteLine("Duty Cycle is out of range (>" + max_duty_cycle.ToString() + "), using  " + default_duty_cycle.ToString() + "  instead.");
                    data_to_sent.Add(default_duty_cycle);
                    UpdateCheckSum(default_duty_cycle);
                }
            }
            // (5) Frequency is between 200 KHz - 20Hz, or 0 Hz (no carrier)
            {
                const double max_freq = 200000, min_freq = 20, default_freq = 38000;
                UInt16 period;
                if (RC_ModutationFreq > max_freq)
                {
                    Console.WriteLine("Duty Cycle is out of range (> " + max_freq.ToString() + " Hz), using " + max_freq.ToString() + " instead.");
                    period = Convert.ToUInt16(8000000 / max_freq);
                }
                else if (RC_ModutationFreq >= min_freq)
                {
                    period = Convert.ToUInt16(8000000 / RC_ModutationFreq);
                }
                else if (RC_ModutationFreq == 0)
                {
                    period = 0;
                }
                else
                {
                    Console.WriteLine("Duty Cycle is out of range (< " + min_freq.ToString() + " Hz), using " + default_freq.ToString() + " instead.");
                    period = Convert.ToUInt16(8000000 / default_freq);
                }
                temp_byte = Convert.ToByte(period / 256);
                data_to_sent.Add(temp_byte);
                UpdateCheckSum(temp_byte);
                temp_byte = Convert.ToByte(period % 256);
                data_to_sent.Add(temp_byte);
                UpdateCheckSum(temp_byte);
            }
            // (6) Add RC width data
            {
                foreach (var val in pulse_packet)
                {
                    UpdateCheckSum(val);
                }
                data_to_sent.AddRange(pulse_packet);
            }
            // (7) Add 0xff as last data byte
            {
                data_to_sent.Add(0xff);
                UpdateCheckSum(0xff);
            }
            // (8) Finally add checksum at end of packet
            data_to_sent.Add(GetCheckSum());

            // Step 6
            SendToSerial_v2(data_to_sent.ToArray());

            return total_us; // return total_rc_time_duration
        }

        static bool TimeOutIndicator = false;

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            TimeOutIndicator = true;
        }

        private void ClearTimeOutIndicator()
        {
            TimeOutIndicator = false;
        }

        private bool GetTimeOutIndicator()
        {
            return TimeOutIndicator;
        }

        static bool HomeMade_TimeOutIndicator = false;
        private static void HomeMade_Delay_OnTimedEvent(object source, ElapsedEventArgs e)
        {
            HomeMade_TimeOutIndicator = true;
        }
        private void HomeMade_Delay(int delay_ms)
        {
            System.Timers.Timer aTimer = new System.Timers.Timer(delay_ms);
            aTimer.Elapsed += new ElapsedEventHandler(HomeMade_Delay_OnTimedEvent);
            HomeMade_TimeOutIndicator = false;
            aTimer.Enabled = true;
            while (HomeMade_TimeOutIndicator == false) { Application.DoEvents(); }
            aTimer.Stop();
            aTimer.Dispose();
        }

        private void TEST_WalkThroughAllRCKeys()
        {
            // 1. Open RC database file to load it in advance
            foreach (var temp_device in RedRatData.RedRatGetDBDeviceNameList())
            {
                RedRatData.RedRatSelectDevice(temp_device);
                foreach (var temp_rc in RedRatData.RedRatGetRCNameList())
                {
                    RedRatData.RedRatSelectRCSignal(temp_rc, true);
                    if (RedRatData.Signal_Type_Supported == true)
                    {

                        // Use UART to transmit RC signal
                        int rc_duration = SendOneRC()/1000 + 1;
                        HomeMade_Delay(rc_duration);

                        // Update 2nd Signal checkbox
                        if ((RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (RedRatData.RC_ToggleData_Length_Value() > 0))
                        {
                            RedRatData.RedRatSelectRCSignal(temp_rc, false);
                            // Use UART to transmit RC signal
                            rc_duration = SendOneRC() / 1000 + 1;
                            HomeMade_Delay(rc_duration);
                        }
                    }
                }
            }
        }

        private void TEST_WalkThroughAllCMDwithData()
        {
            // Testing: send all CMD with input parameter
            for (byte cmd = Convert.ToByte(CMD_CODE_UPPER_LIMIT); cmd >= Convert.ToByte(CMD_CODE_LOWER_LIMIT) ; cmd--)
            //byte cmd = 0xdf;
            {
                SendToSerial_v2(Prepare_Send_Input_CMD(cmd, 0x1010101U * cmd).ToArray());
                HomeMade_Delay(32);
            }
        }

        private void TEST_StressSendingAlreadySelectedRC()
        {
            // Testing: send "stress_cnt" times Single RC
            // 
            // Precondition
            //   1. Load RC Database by RedRatLoadSignalDB()
            //   2. Select Device by RedRatSelectDevice() using device_name or index_no
            //   3. Select RC Signal by RedRatSelectRCSignal() using rc_name or index_no --> specify false at 2nd input parameter if need to Tx 2nd signal of Double signal / Toggle Bits Signal
            Contract.Requires(RedRatData != null);
            Contract.Requires(RedRatData.SignalDB != null);
            Contract.Requires(RedRatData.SelectedDevice != null);
            Contract.Requires(RedRatData.SelectedSignal != null);
            Contract.Requires(RedRatData.Signal_Type_Supported == true);

            byte stress_cnt = 250;

            while (stress_cnt-- > 0)
            {
                // Use UART to transmit RC signal
                int rc_duration = SendOneRC() / 1000 + 1;
                HomeMade_Delay(rc_duration);

                // Update 2nd Signal checkbox
                if ((RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (RedRatData.RC_ToggleData_Length_Value() > 0))
                {
                    // Switch to the other signal in display
                    ThisTimeDoNotUpdateMessageBox = true;
                    RC_Select1stSignalForDoubleOrToggleSignal = !RC_Select1stSignalForDoubleOrToggleSignal;
                    RedRatData.RedRatSelectRCSignal(listboxRCKey.SelectedIndex, RC_Select1stSignalForDoubleOrToggleSignal);
                    chkSelect2ndSignal.Checked = RC_Select1stSignalForDoubleOrToggleSignal;
                }
            }
        }

        private void TEST_StressSendingRepeatCount()
        {
            // Testing: send "repeat_cnt" times Single RC
            // Precondition
            //   1. Load RC Database by RedRatLoadSignalDB()
            //   2. Select Device by RedRatSelectDevice() using device_name or index_no
            //   3. Select RC Signal by RedRatSelectRCSignal() using rc_name or index_no --> specify false at 2nd input parameter if need to Tx 2nd signal of Double signal / Toggle Bits Signal
            Contract.Requires(RedRatData != null);
            Contract.Requires(RedRatData.SignalDB != null);
            Contract.Requires(RedRatData.SelectedDevice != null);
            Contract.Requires(RedRatData.SelectedSignal != null);
            Contract.Requires(RedRatData.Signal_Type_Supported == true);
            const byte repeat_count_threshold = 5;
            int repeat_cnt = 300;

            if (RedRatData.Signal_Type_Supported != true)
                return;

            while (repeat_cnt > 0)
            {
                int rc_duration;
                if (repeat_cnt >= repeat_count_threshold)
                {
                    rc_duration = SendOneRC(repeat_count_threshold - 1);
                    repeat_cnt -= repeat_count_threshold;
                }
                else
                {
                    rc_duration = SendOneRC(Convert.ToByte(repeat_cnt));
                    repeat_cnt = 0;
                }

                rc_duration /= 1000 + 1;
                HomeMade_Delay(rc_duration);
            }
        }

        private void TEST_GPIO_Output()
        {
            const int delay_time = 50;
            // Testing: send GPIO output with byte parameter -- Set output port value at once
            for (uint output_value = 0; output_value <= 0xff; output_value++)
            {
                SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), output_value).ToArray());
                HomeMade_Delay(delay_time/2);
            }

            int run_time = 10;
            const UInt32 IO_value_mask = 0x0, reverse_IO_value_mask = 0x1;

            SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), ~reverse_IO_value_mask).ToArray());
            HomeMade_Delay(delay_time);

            while (run_time-- > 0)
            {
                for (uint output_bit = 0; output_bit < 7;)
                {
                    UInt32 temp_parameter = (output_bit << 8) | reverse_IO_value_mask;
                    SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    HomeMade_Delay(16);
                    output_bit++;
                    temp_parameter = (((output_bit) << 8) | IO_value_mask);
                    SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    HomeMade_Delay(delay_time);
                }
                for (uint output_bit = 7; output_bit > 0;)
                {
                    UInt32 temp_parameter = (output_bit << 8) | reverse_IO_value_mask;
                    SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    HomeMade_Delay(16);
                    output_bit--;
                    temp_parameter = (((output_bit) << 8) | IO_value_mask);
                    SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    HomeMade_Delay(delay_time);
                }
            }
        }

        private void TEST_GPIO_Input()
        {
            const int delay_time = 500;
            UInt32 GPIO_Read_Data = 0;

            // For reading an UART input, please make sure previous return data has been already received

            int run_time = 20;
            while (run_time-- > 0)
            {
                Get_UART_Input = 4;
                SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_GPIO_INPUT)).ToArray());
                HomeMade_Delay(16);
                while(UART_READ_MSG_QUEUE.Count>0)
                {
                    String in_str = UART_READ_MSG_QUEUE.Dequeue();
                    if (in_str.Contains("IN:"))
                    {
                        string value_str = in_str.Substring(in_str.IndexOf(":")+1);
                        GPIO_Read_Data = Convert.ToUInt32(value_str, 16);
                    }
                }
                SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), ~GPIO_Read_Data).ToArray());
                HomeMade_Delay(delay_time);
                SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), 0xff).ToArray());
                HomeMade_Delay(delay_time);
            }
        }

        private void TEST_Return_Repeat_Count_and_Tx_Status()
        {
            int repeat = 300; // max value is 4,294,967,295 (0xffffffff)
            const byte recommended_first_repeat_cnt_value = 100;    // must be <= 0xff
            // Load RedRat database - 載入資料庫
            RedRatData.RedRatLoadSignalDB("C:\\Users\\jeremy.hsiao\\Downloads\\SDK-V4-Samples\\Samples\\RC DB\\DeviceDB - 複製.xml");
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            HomeMade_Delay(16);
            // Select Device  - 選擇RC Device
            RedRatData.RedRatSelectDevice("HP-MCE");
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            //HomeMade_Delay(16);
            // Select RC - 選擇RC (使用名稱或Index No)
            RedRatData.RedRatSelectRCSignal("1", true);
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            //HomeMade_Delay(16);
            // Check if this RC code is supported -- 如果此訊號資料OK可以發射,就發射
            if (RedRatData.Signal_Type_Supported == true)
            {
                // Use UART to transmit RC signal -- repeat (recommended_first_repeat_cnt_value-1) times == total transmit (recommended_first_repeat_cnt_value) times
                int rc_duration = SendOneRC(recommended_first_repeat_cnt_value - 1) / 1000 + 1;
                // Delay to wait for RC Tx finished
                HomeMade_Delay(1);
                // 將剩下的Repeat_Count輸出
                SendToSerial_v2(Prepare_Send_Repeat_Cnt_Add_CMD(Convert.ToUInt32(repeat - recommended_first_repeat_cnt_value)).ToArray());
                rc_duration = ((rc_duration * repeat) / Convert.ToInt32(recommended_first_repeat_cnt_value)) - 1;

                //HomeMade_Delay(rc_duration-1);
                // 這裏是另一種delay的做法,如果需要在等待時,讓系統做一些別的事情
                System.Timers.Timer aTimer = new System.Timers.Timer(rc_duration);
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                ClearTimeOutIndicator();
                aTimer.Enabled = true;
                // 一直循環等到 GetTimeOutIndicator() == true為止
                // 在這等待的同時,就可以安排其它要做的事情
                // 這裏至少要放Applicaiton.DoEvents()讓其它event有機會完成
                while (GetTimeOutIndicator() == false)
                {
                    HomeMade_Delay(500);
                    Example_Get_Remaining_Repeat_Count();
                    HomeMade_Delay(500);
                    Console.WriteLine(Example_Get_Current_Tx_Status().ToString());         
                }
                aTimer.Stop();
                aTimer.Dispose();

                Example_Get_Remaining_Repeat_Count();        
                Example_Get_Current_Tx_Status();            
            }
        }

        // 發射一個信號的範例 - 在此不repeat,所以可以忽略傳入參數
        private void Example_to_Send_RC_without_Repeat_Count()
        {
            // Load RedRat database - 載入資料庫
            RedRatData.RedRatLoadSignalDB("C:\\Users\\jeremy.hsiao\\Downloads\\SDK-V4-Samples\\Samples\\RC DB\\DeviceDB - 複製.xml");
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            HomeMade_Delay(16);
            // Select Device  - 選擇RC Device
            RedRatData.RedRatSelectDevice("HP-MCE");
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            //HomeMade_Delay(16);
            // Select RC - 選擇RC (使用名稱或Index No)
            RedRatData.RedRatSelectRCSignal("1", true);
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            //HomeMade_Delay(16); 
            // Check if this RC code is supported -- 如果此訊號資料OK可以發射,就發射
            if (RedRatData.Signal_Type_Supported == true)
            {
                // Use UART to transmit RC signal -- repeat (SendOneRC_default_cnt) times == total transmit (SendOneRC_default_cnt+1) times
                int rc_duration = SendOneRC() / 1000 + 1;
                // Delay to wait for RC Tx finished
                HomeMade_Delay(rc_duration);

                // If you need to send double signal or toggle bit signal at next IR transmission -- 這裡是示範如何發射Double Signal或Toggle Signal的第二個信號
                if ((RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (RedRatData.RC_ToggleData_Length_Value() > 0))
                {
                    // Use UART to transmit RC signal -- repeat 10 times
                    RedRatData.RedRatSelectRCSignal("1", false);
                    rc_duration = SendOneRC() / 1000 + 1;
                    // Delay to wait for RC Tx finished
                    HomeMade_Delay(rc_duration);
                }
            }
        }

        // 如果一個RC要repeat不超過255次,可以使用一個byte來傳入repeat次數,就能夠直接傳入repeat次數
        private void Example_to_Send_RC_with_Repeat_Count() // repeat count <= 0xff
        {
            const byte SendOneRC_default_cnt = 2;
            // Load RedRat database - 載入資料庫
            RedRatData.RedRatLoadSignalDB("C:\\Users\\jeremy.hsiao\\Downloads\\SDK-V4-Samples\\Samples\\RC DB\\DeviceDB - 複製.xml");
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            HomeMade_Delay(16);
            // Select Device  - 選擇RC Device
            RedRatData.RedRatSelectDevice("HP-MCE");
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            //HomeMade_Delay(16);
            // Select RC - 選擇RC (使用名稱或Index No)
            RedRatData.RedRatSelectRCSignal("1", true);
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            //HomeMade_Delay(16);
            // Check if this RC code is supported -- 如果此訊號資料OK可以發射,就發射
            if (RedRatData.Signal_Type_Supported == true)
            {
                // Use UART to transmit RC signal -- repeat (SendOneRC_default_cnt) times == total transmit (SendOneRC_default_cnt+1) times
                int rc_duration = SendOneRC(SendOneRC_default_cnt) / 1000 + 1;
                // Delay to wait for RC Tx finished
                HomeMade_Delay(rc_duration);

                // If you need to send double signal or toggle bit signal at next IR transmission -- 這裡是示範如何發射Double Signal或Toggle Signal的第二個信號
                if ((RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (RedRatData.RC_ToggleData_Length_Value() > 0))
                {
                    // Use UART to transmit RC signal -- repeat 10 times
                    RedRatData.RedRatSelectRCSignal("1", false);
                    rc_duration = SendOneRC(SendOneRC_default_cnt) / 1000 + 1;
                    // Delay to wait for RC Tx finished
                    HomeMade_Delay(rc_duration);
                }
            }
        }

        // 如果一個RC要repeat很多次(過255次),無法像前面使用一個byte來傳入repeat次數,
        // 就要在後面使用另一個指令,來追加要repeat的次數,
        // 該指令的傳入參數為4-byte (0~4,294,967,295 (0xffffffff))
        private void Example_to_Send_RC_with_Large_Repeat_Count()
        {
            int repeat = 300; // max value is 4,294,967,295 (0xffffffff)
            const byte recommended_first_repeat_cnt_value = 100;    // must be <= 0xff
            // Load RedRat database - 載入資料庫
            RedRatData.RedRatLoadSignalDB("C:\\Users\\jeremy.hsiao\\Downloads\\SDK-V4-Samples\\Samples\\RC DB\\DeviceDB - 複製.xml");
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            HomeMade_Delay(16);
            // Select Device  - 選擇RC Device
            RedRatData.RedRatSelectDevice("HP-MCE");
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            //HomeMade_Delay(16);
            // Select RC - 選擇RC (使用名稱或Index No)
            RedRatData.RedRatSelectRCSignal("1", true);
            // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
            //HomeMade_Delay(16);
            // Check if this RC code is supported -- 如果此訊號資料OK可以發射,就發射
            if (RedRatData.Signal_Type_Supported == true)
            {
                // Use UART to transmit RC signal -- repeat (recommended_first_repeat_cnt_value-1) times == total transmit (recommended_first_repeat_cnt_value) times
                int rc_duration = SendOneRC(recommended_first_repeat_cnt_value-1) / 1000 + 1;                                                                                                                                                                                                                                                           
                // Delay to wait for RC Tx finished
                HomeMade_Delay(1);
                // 將剩下的Repeat_Count輸出
                SendToSerial_v2(Prepare_Send_Repeat_Cnt_Add_CMD(Convert.ToUInt32(repeat-recommended_first_repeat_cnt_value)).ToArray());
                rc_duration = ((rc_duration * repeat) / Convert.ToInt32(recommended_first_repeat_cnt_value)) - 1;

                //HomeMade_Delay(rc_duration-1);
                // 這裏是另一種delay的做法,如果需要在等待時,讓系統做一些別的事情
                System.Timers.Timer aTimer = new System.Timers.Timer(rc_duration);
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                ClearTimeOutIndicator();
                aTimer.Enabled = true;
                // 一直循環等到 GetTimeOutIndicator() == true為止
                // 在這等待的同時,就可以安排其它要做的事情
                // 這裏至少要放Applicaiton.DoEvents()讓其它event有機會完成
                while (GetTimeOutIndicator() == false)
                {
                    // 這裏至少要放Applicaiton.DoEvents()讓其它event有機會完成
                    Application.DoEvents();
                }
                aTimer.Stop();
                aTimer.Dispose();

                // If you need to send double signal or toggle bit signal at next IR transmission -- 這裡是示範如何發射Double Signal或Toggle Signal的第二個信號
                if ((RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (RedRatData.RC_ToggleData_Length_Value() > 0))
                {
                    // Use UART to transmit RC signal -- repeat 10 times
                    RedRatData.RedRatSelectRCSignal("1", false);
                    rc_duration = SendOneRC(recommended_first_repeat_cnt_value-1) / 1000 + 1;
                    // Delay to wait for RC Tx finished
                    HomeMade_Delay(1);
                    SendToSerial_v2(Prepare_Send_Repeat_Cnt_Add_CMD(Convert.ToUInt32(repeat - recommended_first_repeat_cnt_value)).ToArray());
                    rc_duration = ((rc_duration * repeat) / Convert.ToInt32(recommended_first_repeat_cnt_value));
                    HomeMade_Delay(rc_duration-1);
                }
            }
        }

        // 強迫停止信號發射的指令 -- 也可以用來PC端程式開啟時,用來將小藍鼠的狀態設定為預設狀態
        private void Example_to_Stop_Running()
        {
            SendToSerial_v2(Prepare_STOP_CMD().ToArray());
        }

        // 單純回應"HI"的指令,可用來試試看系統是否還有在接受指令
        private void Example_to_Test_If_Still_Alive()
        {
            SendToSerial_v2(Prepare_Say_HI_CMD().ToArray());
        }

        // 讓系統進入等待軟體更新的狀態
        private void Example_Entering_ISP()
        {
            SendToSerial_v2(Prepare_Enter_ISP_CMD().ToArray());
        }

        private int Example_Get_Remaining_Repeat_Count()
        {
            int repeat_cnt = 0;
            Get_UART_Input = 1;
            SendToSerial_v2(Prepare_Get_RC_Repeat_Count().ToArray());
            HomeMade_Delay(5);
            if (UART_READ_MSG_QUEUE.Count > 0)
            {
                String in_str = UART_READ_MSG_QUEUE.Dequeue();
                if (in_str.Contains("CNT:"))
                {

                    string value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                    repeat_cnt = Convert.ToInt32(value_str, 16);
                    return repeat_cnt;
                }
            }
            else
            {
                // Should not reach here unless system is abnormal
            }
            return 0;
        }

        private bool Example_Get_Current_Tx_Status()
        {
            Get_UART_Input = 1;
            SendToSerial_v2(Prepare_Get_RC_Current_Running_Status().ToArray());
            HomeMade_Delay(5);
            if (UART_READ_MSG_QUEUE.Count > 0)
            {
                String in_str = UART_READ_MSG_QUEUE.Dequeue();
                if (in_str.Contains("TX:"))
                {
                    string value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                    if (Convert.ToInt32(value_str, 16) != 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                // Should not reach here unless system is abnormal
            }
            return false;
        }

        private void Example_Get_All_GPIO_Input()
        {
            UInt32 GPIO_Read_Data = 0;
            Get_UART_Input = 1;
            SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_GPIO_INPUT)).ToArray());
            HomeMade_Delay(5);
            if (UART_READ_MSG_QUEUE.Count > 0)
            {
                String in_str = UART_READ_MSG_QUEUE.Dequeue();
                if (in_str.Contains("IN:"))
                {
                    string value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                    GPIO_Read_Data = Convert.ToUInt32(value_str, 16);
                    Console.WriteLine(GPIO_Read_Data.ToString());           // output to console
                }
            }
        }

        private void Example_Check_System_Alive()
        {
            Get_UART_Input = 1;
            SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SAY_HI)).ToArray());
            HomeMade_Delay(5);
            if (UART_READ_MSG_QUEUE.Count > 0)
            {
                String in_str = UART_READ_MSG_QUEUE.Dequeue();
                if (in_str.Equals("HI"))
                {
                    Console.WriteLine("Alive");          
                }
                else 
                {
                    Console.WriteLine("?????");
                }
            }
        }

        //
        // 跟小藍鼠有關係的程式代碼與範例程式區--結尾
        //

        private void btnRepeatRC_Click(object sender, EventArgs e)
        {
            TemoparilyDisbleAllRCFunctionButtons();

            //
            // Example
            //

            TEST_Return_Repeat_Count_and_Tx_Status();
            Example_to_Stop_Running(); // 順便將可能因為測試而正在執行的動作中斷
            Example_Check_System_Alive();
            Example_Get_All_GPIO_Input();
            Example_to_Test_If_Still_Alive();
            Example_to_Send_RC_without_Repeat_Count();
            Example_to_Test_If_Still_Alive();
            Example_to_Send_RC_with_Repeat_Count();
            Example_to_Test_If_Still_Alive();
            Example_to_Send_RC_with_Large_Repeat_Count();
            Example_to_Test_If_Still_Alive();

            ////
            // Self-testing code
            //

            TEST_Return_Repeat_Count_and_Tx_Status();
            Example_to_Test_If_Still_Alive();
            TEST_WalkThroughAllCMDwithData();
            Example_to_Test_If_Still_Alive();
            TEST_GPIO_Output();
            Example_to_Test_If_Still_Alive();
            TEST_GPIO_Input();
            Example_to_Test_If_Still_Alive();

            if ((RedRatData != null) && (RedRatData.SignalDB != null))
            {
                TEST_WalkThroughAllRCKeys();
                Example_to_Test_If_Still_Alive();
                TEST_StressSendingRepeatCount();
            }

            // Example_Entering_ISP();
            UndoTemoparilyDisbleAllRCFunctionButtons();
        }
    }
}
