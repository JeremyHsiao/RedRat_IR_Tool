using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics.Contracts;

using RedRat;
using RedRat.IR;
using RedRat.RedRat3;
using RedRat.Util;
using RedRat.AVDeviceMngmt;

namespace RedRatDatabaseViewer
{
    public partial class RedRatDBViewer : Form
    {

        private int Previous_Device = -1;
        private int Previous_Key = -1;
        private bool RC_Select1stSignalForDoubleOrToggleSignal =  true;

        private RedRatDBParser RedRatData;

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
                ret = false;
            }
            return ret;
        }

        private Boolean Serial_ClosePort()
        {
            Boolean ret = false;
            try
            {
                _serialPort.Close();
                ret = true;
            }
            catch (Exception ex232)
            {
                ret = false;
            }
            return ret;
        }

        static bool _continue_serial_read_write = false;
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
                    string message = _serialPort.ReadLine();
                    {
                        UART_READ_MSG_QUEUE.Enqueue(message);
                        AppendSerialMessageLog(message);
                    }
                }
                catch (Exception ex)
                {
                    //AppendSerialMessageLog(ex.ToString());
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
                AppendSerialMessageLog("\n========Tx:" + Tx_CNT.ToString() + " ");
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
                    AppendSerialMessageLog(ex.ToString() + " ");
                    return_value = false;
                }
            }
            else
            {
                //AppendSerialMessageLog("COM is closed and cannot send byte data\n");
                return_value = false;
            }
            return return_value;
        }

        private void EnableRCFunctionButton()
        {
            if ((_serialPort.IsOpen == true) && (RedRatData !=null) && (RedRatData.SignalDB != null))
            {
                btnSingleRCPressed.Enabled = true;
                btnCheckHeartBeat.Enabled = true;
                btnStopRCButton.Enabled = true;
            }
        }

        private void DisableSingleRCButton()
        {
            btnSingleRCPressed.Enabled = false;
            btnCheckHeartBeat.Enabled = false;
            btnStopRCButton.Enabled = false;
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
                        EnableRCFunctionButton();
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
                        DisableSingleRCButton();
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

        private List<byte> Convert_data_to_Byte(uint width_value)
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

        public List<byte> Prepare_Do_Nothing_CMD()
        {
            List<byte> data_to_sent = new List<byte>();

            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            data_to_sent.Add(0x00);
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            return data_to_sent;
        }

        public List<byte> Prepare_STOP_CMD()
        {
            List<byte> data_to_sent = new List<byte>();

            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xfe);
            data_to_sent.Add(0xfe);
            return data_to_sent;
        }

        public bool SendOneRC()
        {
            // Precondition
            //   1. Load RC Database by RedRatLoadSignalDB()
            //   2. Select Device by RedRatSelectDevice() using device_name or index_no
            //   3. Select RC Signal by RedRatSelectRCSignal() using rc_name or index_no --> specify false at 2nd input parameter if need to Tx 2nd signal of Double signal / Toggle Bits Signal
            Contract.Requires(RedRatData != null);
            Contract.Requires(RedRatData.SignalDB != null); 
            Contract.Requires(RedRatData.SelectedDevice != null);
            Contract.Requires(RedRatData.SelectedSignal != null);

            // Exection in this function
            //   4. Get complete pulse width data by GetTxPulseWidth()
            //   5. Combine pulse width data with other RC information into one array
            //   6. UART Tx this array
            //   7. For Double signal or Toggle Bit signal, switch to next one

            // Step 4
            List<byte> data_to_sent = new List<byte>();
            List<byte> pulse_packet = new List<byte>();
            List<double> pulse_width = RedRatData.GetTxPulseWidth();
            foreach (var val in pulse_width)
            {
                pulse_packet.AddRange(Convert_data_to_Byte(Convert.ToUInt32(val)));
            }

            // Step 5
            Byte CheckSum = 0, temp_byte, duty_cycle = 50, default_repeat_cnt = 0;
            double RC_ModutationFreq = RedRatData.RC_ModutationFreq();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            data_to_sent.Add(default_repeat_cnt);        // Repeat_No
            CheckSum = default_repeat_cnt;
            data_to_sent.Add(duty_cycle);       // Duty-cycle is currently fixed to 50
            CheckSum ^= duty_cycle;
            UInt16 period = (RC_ModutationFreq == 0) ? (UInt16)0 : (Convert.ToUInt16(8000000 / RC_ModutationFreq));
            temp_byte = Convert.ToByte(period / 256);
            data_to_sent.Add(temp_byte);
            CheckSum ^= temp_byte;
            temp_byte = Convert.ToByte(period % 256);
            data_to_sent.Add(temp_byte);
            CheckSum ^= temp_byte;
            // Add RC Signal
            foreach (var val in pulse_packet)
            {
                CheckSum ^= val;
            }
            data_to_sent.AddRange(pulse_packet);
            data_to_sent.Add(0xff);
            CheckSum ^= 0xff;
            data_to_sent.Add(CheckSum);

            // Step 6
            SendToSerial_v2(data_to_sent.ToArray());

            // Step 7
            if ((RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (RedRatData.RC_ToggleData().Length > 0))
            {
                RC_Select1stSignalForDoubleOrToggleSignal = !RC_Select1stSignalForDoubleOrToggleSignal;
                RedRatData.RedRatSelectRCSignal(listboxRCKey.SelectedIndex, RC_Select1stSignalForDoubleOrToggleSignal);
            }

            return true; // To-be-implemented: implement error detection in the future
        }

        //
        // Form Events
        //

        bool ThisTimeDoNotUpdateMessageBox = false;

        private void SingleOutput_Click(object sender, EventArgs e)
        {
            if((Previous_Device<0)||(Previous_Key<0))
            {
                // return immediately when No Selected Device or no Selected Signal
                return;
            }

            btnSingleRCPressed.Enabled = false;
            btnCheckHeartBeat.Enabled = false;
            btnStopRCButton.Enabled = false;
            btnConnectionControl.Enabled = false;
            btnGetRCFile.Enabled = false;

            // Use UART to transmit RC signal
            SendOneRC(); 

            // Update 2nd Signal checkbox
            if ((RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (RedRatData.RC_ToggleData().Length > 0) )
            {
                // Switch to the other signal in display
                ThisTimeDoNotUpdateMessageBox = true;
                chkSelect2ndSignal.Checked = RC_Select1stSignalForDoubleOrToggleSignal;
            }
            //
            // End of Tx 
            //

            //if(UART_READ_MSG_QUEUE.Count>0)
            //{
            //    string temp_str = UART_READ_MSG_QUEUE.Dequeue();
            //    int value_in = Convert.ToInt16(temp_str);
            //    temp_str = UART_READ_MSG_QUEUE.Dequeue();
            //    int value_out = Convert.ToInt16(temp_str);
            //    if(value_in<value_out)
            //    {
            //        value_in += 250;
            //    }
            //    if((value_in-value_out)== pulse_width.Count)
            //    {
            //        AppendSerialMessageLog("OK\n");
            //    }
            //}

            //
            //
            //
            btnConnectionControl.Enabled = true;
            btnGetRCFile.Enabled = true;
            btnCheckHeartBeat.Enabled = true;
            btnStopRCButton.Enabled = true;
            btnSingleRCPressed.Enabled = true;
        }

        private void btnCheckHeartBeat_Click(object sender, EventArgs e)
        {
            btnSingleRCPressed.Enabled = false;
            btnCheckHeartBeat.Enabled = false;
            btnStopRCButton.Enabled = false;
            SendToSerial_v2(Prepare_Do_Nothing_CMD().ToArray());
            btnCheckHeartBeat.Enabled = true;
            btnStopRCButton.Enabled = true;
            btnSingleRCPressed.Enabled = true;
        }

        private void StopCMDButton_Click(object sender, EventArgs e)
        {
            btnSingleRCPressed.Enabled = false;
            btnCheckHeartBeat.Enabled = false;
            btnStopRCButton.Enabled = false;
            SendToSerial_v2(Prepare_STOP_CMD().ToArray());
            btnCheckHeartBeat.Enabled = true;
            btnStopRCButton.Enabled = true;
            btnSingleRCPressed.Enabled = true;
        }

        private void listboxAVDeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int Current_Device = listboxAVDeviceList.SelectedIndex;
            if (Previous_Device != Current_Device)
            {
                string devicename = listboxAVDeviceList.SelectedItem.ToString();
                if(RedRatData.RedRatSelectDevice(devicename))
                {
                    listboxRCKey.Items.Clear();
                    listboxRCKey.Items.AddRange(RedRatData.RedRatGetRCNameList().ToArray());
                    Previous_Device = Current_Device;
                    Previous_Key = -1;
                    listboxRCKey.SelectedIndex = 0;  // Make sure (Previous_Key!= listboxRCKey.SelectedIndex) at next SelectedIndexChanged event
                    RC_Select1stSignalForDoubleOrToggleSignal = true;
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
 
            if ( Previous_Key != Current_Key)
            {
                string rcname = listboxRCKey.SelectedItem.ToString();
                if (RedRatData.RedRatSelectRCSignal(rcname, RC_Select1stSignalForDoubleOrToggleSignal))
                {
                    Type temp_type = RedRatData.RedRatSelectedSignalType();
                    lbModulationType.Text = temp_type.ToString();
                    if (temp_type == typeof(DoubleSignal))
                    {
                        chkSelect2ndSignal.Enabled = true;
                        rbDoubleSignalLED.Checked = true;
                    }
                    else if (RedRatData.RC_ToggleData().Length > 0)
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

                    Previous_Key = Current_Key;
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
            if(rbDoubleSignalLED.Checked==true)
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
                        RedRatData = new RedRatDBParser();
                        RedRatData.RedRatLoadSignalDB(openFileDialog1.FileName); // Device 0 Signal 0 will be selected after RC database loaded

                        //
                        // Update Form Display Data according to content of RedRatData.SelectedSignal
                        //
                        listboxAVDeviceList.Items.Clear();
                        listboxRCKey.Items.Clear();
                        listboxAVDeviceList.Items.AddRange(RedRatData.RedRatGetDBDeviceNameList().ToArray());
                        listboxRCKey.Items.AddRange(RedRatData.RedRatGetRCNameList().ToArray());
                        UpdateRCDataOnForm();

                        Previous_Device = -1;
                        listboxAVDeviceList.SelectedIndex = 0;
                        this.listboxAVDeviceList_SelectedIndexChanged(sender, e); // Force to update both list selection box
                        listboxAVDeviceList.Enabled = true;
                        listboxRCKey.Enabled = true;
                        EnableRCFunctionButton();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }


    }

}
