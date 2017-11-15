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
                // listBox1.SelectedIndex = listBox1.Items.Count - 1;
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

        private void SendToSerial_v2(byte[] byte_to_sent)
        {
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
                }
                catch (Exception ex)
                {
                    AppendSerialMessageLog(ex.ToString() + " ");
                }
            }
            else
            {
                //AppendSerialMessageLog("COM is closed and cannot send byte data\n");
            }
        }

        private void EnableSingleRCButton()
        {
            if ((_serialPort.IsOpen == true) && (RedRatData.SignalDB != null))
            {
                btnSingleRCPressed.Enabled = true;
            }
        }

        private void DisableSingleRCButton()
        {
            btnSingleRCPressed.Enabled = false;
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
                        EnableSingleRCButton();
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

        /// <summary>
        /// Loads the signal database XML file.
        /// </summary>
        private AVDeviceDB LoadSignalDB(string dbFileName)
        {
            var ser = new XmlSerializer(typeof(AVDeviceDB));
            var fs = new FileStream((new FileInfo(dbFileName)).FullName, FileMode.Open);
            var avDeviceDB = (AVDeviceDB)ser.Deserialize(fs);
            return avDeviceDB;
        }

        /// <summary>
        /// Returns an IR signal object from the signal DB file using the deviceName and signalName to
        /// look it up.
        /// </summary>
        private IRPacket GetSignal(AVDeviceDB signalDB, string deviceName, string signalName)
        {
            var device = signalDB.GetAVDevice(deviceName);
            if (device == null)
            {
                throw new Exception(
                    string.Format("No device of name '{0}' found in the signal database.", deviceName));
            }
            var signal = device.GetSignal(signalName);
            if (signal == null)
            {
                throw new Exception(
                    string.Format("No signal of name '{0}' found for device '{1}' in the signal database.",
                        signalName, deviceName));
            }
            return signal;
        }

        private void Displaying_RC_Signal_Array(double[] rc_length, byte[] rc_main_sig_array, byte[] rc_repeat_sig_array, int rc_repeat, double rc_intra_sig_pause, int Repeat_Tx_Times=0)
        {
            Contract.Requires(rc_main_sig_array != null);
            Contract.Requires((rc_repeat <= 0) || ((rc_repeat > 0) && (rc_repeat_sig_array != null)));

            int repeat_cnt = rc_repeat, pulse_high;
            const int time_ratio = 1000;
            
            // Main signal
            pulse_high = 1;
            foreach (var sig in rc_main_sig_array)
            {
                rtbDecodeRCSignal.AppendText(pulse_high.ToString() + ":" + (rc_length[sig] * time_ratio).ToString() + "\n");
                pulse_high = (pulse_high != 0) ? 0 : 1;
            }

            while (repeat_cnt-- > 0) 
            {
                rtbDecodeRCSignal.AppendText("0" + ":" + (rc_intra_sig_pause * time_ratio).ToString() + "\n");
                pulse_high = 1;
                foreach (var sig in rc_repeat_sig_array)
                {
                    rtbDecodeRCSignal.AppendText(pulse_high.ToString() + ":" + (rc_length[sig]* time_ratio).ToString() + "\n");
                    pulse_high = (pulse_high != 0) ? 0 : 1;
                }
            }
            //
            // To be implemented: make use of Repeat_Tx_Times
            //
        }

        //private void RedRatClearRCData()
        //{
        //    RC_ModutationFreq = 0;
        //    RC_Lengths = null;
        //    RC_SigData = null;
        //    RC_NoRepeats = 0;
        //    RC_IntraSigPause = 0;
        //    RC_MainSignal = null;
        //    RC_RepeatSignal = null;
        //    RC_ToggleData = null;
        //    RC_Description = "";
        //    RC_Name = "";
        //    // RC_PauseRepeatMode = ;
        //    RC_RepeatPause = 0;
        //    RC_MainRepeatIdentical = false;
        //}

        //private void Verify_Toggle_Bit_Data()
        //{
        //    List<ToggleBit> temp_toggle_data = new List<ToggleBit>();

        //    foreach (var toggle_data in RC_ToggleData)
        //    {
        //        if ((toggle_data.len1 < RC_Lengths.Length) && (toggle_data.len2 < RC_Lengths.Length))
        //        {
        //            temp_toggle_data.Add(toggle_data);
        //        }
        //        else
        //        {
        //            Console.WriteLine("Toggle Bit Data Error at bit:" + toggle_data.bitNo + " (" + toggle_data.len1 + "," + toggle_data.len2 + ")");
        //        }
        //    }

        //    RC_ToggleData = temp_toggle_data.ToArray();
        //}

        //private void GetRCData(ModulatedSignal sig)
        //{
        //    RC_ModutationFreq = sig.ModulationFreq;
        //    RC_Lengths = sig.Lengths;
        //    RC_SigData = sig.SigData;
        //    RC_NoRepeats = sig.NoRepeats;
        //    RC_IntraSigPause = sig.IntraSigPause;
        //    RC_MainSignal = sig.MainSignal;
        //    RC_RepeatSignal = sig.RepeatSignal;
        //    RC_ToggleData = sig.ToggleData;
        //    Verify_Toggle_Bit_Data();
        //    RC_Description = sig.Description;
        //    RC_Name = sig.Name;
        //    RC_PauseRepeatMode = sig.PauseRepeatMode;
        //    RC_RepeatPause = sig.RepeatPause;
        //    RC_MainRepeatIdentical = ModulatedSignal.MainRepeatIdentical(sig);
        //}

        //private void GetRCData(RedRat3ModulatedSignal sig)
        //{
        //    RC_ModutationFreq = sig.ModulationFreq;
        //    RC_Lengths = sig.Lengths;
        //    RC_SigData = sig.SigData;
        //    RC_NoRepeats = sig.NoRepeats;
        //    RC_IntraSigPause = sig.IntraSigPause;
        //    RC_MainSignal = sig.MainSignal;
        //    RC_RepeatSignal = sig.RepeatSignal;
        //    RC_ToggleData = sig.ToggleData;
        //    Verify_Toggle_Bit_Data();
        //    RC_Description = sig.Description;
        //    RC_Name = sig.Name;
        //    RC_PauseRepeatMode = sig.PauseRepeatMode;
        //    RC_RepeatPause = sig.RepeatPause;
        //    RC_MainRepeatIdentical = RedRat3ModulatedSignal.MainRepeatIdentical(sig);
        //}

        //private void GetRCData(FlashCodeSignal sig)
        //{
        //    RC_ModutationFreq = 0;
        //    RC_Lengths = sig.Lengths;
        //    RC_SigData = sig.SigData;
        //    RC_NoRepeats = sig.NoRepeats;
        //    RC_IntraSigPause = sig.IntraSigPause;
        //    RC_MainSignal = sig.MainSignal;
        //    RC_RepeatSignal = sig.RepeatSignal;
        //    RC_ToggleData = null;
        //    RC_Description = sig.Description;
        //    RC_Name = sig.Name;
        //    RC_MainRepeatIdentical = false; // No such function, need to compare
        //}

        //private void GetRCData(RedRat3FlashCodeSignal sig)
        //{
        //    RC_ModutationFreq = 0;
        //    RC_Lengths = sig.Lengths;
        //    RC_SigData = sig.SigData;
        //    RC_NoRepeats = sig.NoRepeats;
        //    RC_IntraSigPause = sig.IntraSigPause;
        //    RC_MainSignal = sig.MainSignal;
        //    RC_RepeatSignal = sig.RepeatSignal;
        //    RC_ToggleData = null;
        //    RC_Description = sig.Description;
        //    RC_Name = sig.Name;
        //    RC_MainRepeatIdentical = false; // No such function, need to compare
        //}

        //private void ProcessSingleSignalData(IRPacket sgl_signal)
        //{
        //    if (sgl_signal.GetType() == typeof(ModulatedSignal))
        //    {
        //        ModulatedSignal sig = (ModulatedSignal)sgl_signal;
        //        rtbSignalData.Text = sig.ToString();
        //        GetRCData(sig);
        //        UpdateRCDataOnForm();
        //    }
        //    else if (sgl_signal.GetType() == typeof(RedRat3ModulatedSignal))
        //    {
        //        RedRat3ModulatedSignal sig = (RedRat3ModulatedSignal)sgl_signal;
        //        rtbSignalData.Text = sig.ToString();
        //        GetRCData(sig);
        //        UpdateRCDataOnForm();
        //    }
        //    else if (sgl_signal.GetType() == typeof(FlashCodeSignal))
        //    {
        //        FlashCodeSignal sig = (FlashCodeSignal)sgl_signal;
        //        rtbSignalData.Text = sgl_signal.ToString();
        //        GetRCData(sig);
        //        UpdateRCDataOnForm();
        //    }
        //    else if (sgl_signal.GetType() == typeof(RedRat3FlashCodeSignal))
        //    {
        //        RedRat3FlashCodeSignal sig = (RedRat3FlashCodeSignal)sgl_signal;
        //        rtbSignalData.Text = sgl_signal.ToString();
        //        RedRatData.GetRCData(sig);
        //        UpdateRCDataOnForm();
        //    }
        //    //else if (Signal.GetType() == typeof(ProntoModulatedSignal))
        //    //{
        //    //    rtbSignalData.Text = Signal.ToString();
        //    //}
        //    //else if (Signal.GetType() == typeof(RedRat3ModulatedKeyboardSignal))
        //    //{
        //    //    rtbSignalData.Text = Signal.ToString();
        //    //}
        //    //else if (Signal.GetType() == typeof(RedRat3IrDaPacket))
        //    //{
        //    //    rtbSignalData.Text = Signal.ToString();
        //    //}
        //    //else if (Signal.GetType() == typeof(IrDaPacket))
        //    //{
        //    //    rtbSignalData.Text = Signal.ToString();
        //    //}
        //    else if (sgl_signal.GetType() == typeof(DoubleSignal))
        //    {
        //        rtbSignalData.Text = sgl_signal.ToString() + "\nNot supported in this funciton\n";
        //        RedRatClearRCData();
        //        UpdateRCDataOnForm();
        //    }
        //    else
        //    {
        //        rtbSignalData.Text = sgl_signal.ToString();
        //        RedRatClearRCData();
        //        UpdateRCDataOnForm();
        //    }
        //}

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

        private double High_Pulse_Width_Adjustment(double signal_width)
        {
            double RC_ModutationFreq = RedRatData.RC_ModutationFreq();
            double high_pulse_compensation;
            ToggleBit[] RC_ToggleData = RedRatData.RC_ToggleData();

            if (RC_ModutationFreq == 0)
            {
                const double min_width = 50;
                if (signal_width < min_width)
                {
                    high_pulse_compensation = 50 - signal_width;
                }
                else
                {
                    high_pulse_compensation = 0;
                }
            }
            else
            {
                const double min_carrier_width_ratio = 3;
                double carrier_width = (1000000 / RC_ModutationFreq);
                double min_width = carrier_width * (min_carrier_width_ratio);
                if ( (signal_width+carrier_width) >= min_width)
                {
                    high_pulse_compensation = carrier_width;
                }
                else
                {
                    high_pulse_compensation = min_width - signal_width;
                }
            }
            return high_pulse_compensation;
        }

        public List<byte> Prepare_RC_Data_Packet(bool IsFirstSignal)
        {
            List<byte> data_to_sent = new List<byte>();
            //IRPacket TxSignal = SelectedSignal;
            double high_pulse_compensation = 0;

            if(RedRatData.RedRatSelectRCSignal(listboxRCKey.SelectedIndex, IsFirstSignal) ==false)
            {
                return data_to_sent;
            }

            // DEBUG PURPOSE ONLY
            //rtbDecodeRCSignal.Text = "Tx Mod-Freq: " + RC_ModutationFreq.ToString() + "\n";
            // END

            //
            // Pre-processing done, start to prepare pulse-width data for a single Tx
            //
            double RC_ModutationFreq = RedRatData.RC_ModutationFreq();
            double[] RC_Lengths = RedRatData.RC_Lengths();
            byte[] RC_MainSignal = RedRatData.RC_MainSignal();
            byte[] RC_RepeatSignal = RedRatData.RC_RepeatSignal();
            int RC_NoRepeats = RedRatData.RC_NoRepeats();
            double RC_IntraSigPause = RedRatData.RC_IntraSigPause();
            double RC_RepeatPause = RedRatData.RC_RepeatPause();
            ToggleBit[] RC_ToggleData = RedRatData.RC_ToggleData();

            int repeat_cnt = RC_NoRepeats, pulse_index, toggle_bit_index;
            bool pulse_high;
            double time_ratio = RedRatData.Time_Factor_to_uS;

            // Tx Main signal
            toggle_bit_index = 0;
            pulse_index = 0;
            pulse_high = true;
            foreach (var sig in RC_MainSignal)
            {
                double signal_width;
                //
                //  Update Toggle Bits
                //
                if ((toggle_bit_index < RC_ToggleData.Length) && (pulse_index == RC_ToggleData[toggle_bit_index].bitNo))
                {
                    int toggle_bit_no = (IsFirstSignal == true) ? (RC_ToggleData[toggle_bit_index].len1) : (RC_ToggleData[toggle_bit_index].len2);
                    signal_width = RC_Lengths[toggle_bit_no];
                    toggle_bit_index++;
                }
                else
                {
                    signal_width = RC_Lengths[sig];
                }
                //rtbDecodeRCSignal.AppendText(pulse_high.ToString() + ":" + (signal_width * time_ratio).ToString() + "\n");
                signal_width *= time_ratio;

                //
                // high_pulse period must extended a bit to compensate shorted-period due to detection mechanism
                //
                if (pulse_high)
                {
                    high_pulse_compensation = High_Pulse_Width_Adjustment(signal_width);
                    signal_width += high_pulse_compensation;
                }
                else
                {
                    signal_width -= high_pulse_compensation;
                }

                data_to_sent.AddRange(Convert_data_to_Byte(Convert.ToUInt32(signal_width)));
                // DEBUG PURPOSE ONLY
                //rtbDecodeRCSignal.AppendText((pulse_high==true?"1":"0") + ":" + Convert.ToUInt32(signal_width).ToString() + "\n");
                // END

                pulse_high = !pulse_high;
                pulse_index++;
            }

            // Tx the rest of signal (2nd/3rd/...etc)
            while (repeat_cnt-- > 0)
            {
                uint temp_value = Convert.ToUInt32(RC_IntraSigPause * time_ratio - high_pulse_compensation);
                data_to_sent.AddRange(Convert_data_to_Byte(temp_value));
                // DEBUG PURPOSE ONLY
                //rtbDecodeRCSignal.AppendText((pulse_high == true ? "1" : "0") + ":" + temp_value.ToString() + "\n");
                // END

                pulse_index++;
                pulse_high = true;

                foreach (var sig in RC_RepeatSignal)
                {
                    double signal_width;
                    //
                    //  Update Toggle Bits
                    //
                    if ((toggle_bit_index < RC_ToggleData.Length) && (pulse_index == RC_ToggleData[toggle_bit_index].bitNo))
                    {
                        signal_width = RC_Lengths[(IsFirstSignal == true) ? (RC_ToggleData[toggle_bit_index].len1) : (RC_ToggleData[toggle_bit_index].len2)];
                        toggle_bit_index++;
                    }
                    else
                    {
                        signal_width = RC_Lengths[sig];
                    }
                    //rtbDecodeRCSignal.AppendText(pulse_high.ToString() + ":" + (signal_width * time_ratio).ToString() + "\n");
                    //pulse_high = (pulse_high != 0) ? 0 : 1;
                    signal_width *= time_ratio;
                    if (pulse_high)
                    {
                        high_pulse_compensation = High_Pulse_Width_Adjustment(signal_width);
                        signal_width += high_pulse_compensation;
                    }
                    else
                    {
                        signal_width -= high_pulse_compensation;
                    }
                    data_to_sent.AddRange(Convert_data_to_Byte(Convert.ToUInt32(signal_width)));

                    // DEBUG PURPOSE ONLY
                    //rtbDecodeRCSignal.AppendText((pulse_high == true ? "1" : "0") + ":" + Convert.ToUInt32(signal_width).ToString() + "\n");
                    // END

                    pulse_high = !pulse_high;
                    pulse_index++;
                }
            }
            if (RC_RepeatPause > 0)
            {
                data_to_sent.AddRange(Convert_data_to_Byte(Convert.ToUInt32((RC_RepeatPause * time_ratio) - high_pulse_compensation)));
            }
            else if (RC_IntraSigPause > 0)
            {
                data_to_sent.AddRange(Convert_data_to_Byte(Convert.ToUInt32((RC_IntraSigPause * time_ratio) - high_pulse_compensation)));
            }
            pulse_index++;
            //
            // End of Tx preprocessing
            //

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

            List<byte> data_to_sent = new List<byte>();
            Byte CheckSum = 0, temp_byte;
            double RC_ModutationFreq = RedRatData.RC_ModutationFreq();

            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            data_to_sent.Add(0);        // Repeat_No
            CheckSum = 0;
            data_to_sent.Add(50);       // Duty-cycle
            CheckSum ^= 50;
            UInt16 period = (RC_ModutationFreq == 0) ? (UInt16)0 : (Convert.ToUInt16(8000000 / RC_ModutationFreq));
            temp_byte = Convert.ToByte(period / 256);
            data_to_sent.Add(temp_byte);
            CheckSum ^= temp_byte;
            temp_byte = Convert.ToByte(period % 256);
            data_to_sent.Add(temp_byte);
            CheckSum ^= temp_byte;

            List<byte> pulse_packet = Prepare_RC_Data_Packet(RC_Select1stSignalForDoubleOrToggleSignal);
            foreach (var val in pulse_packet)
            {
                CheckSum ^= val;
            }
            data_to_sent.AddRange(pulse_packet);
            data_to_sent.Add(0xff);
            CheckSum ^= 0xff;
            data_to_sent.Add(CheckSum);

            SendToSerial_v2(data_to_sent.ToArray());

            if (RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal)))
            {
                RC_Select1stSignalForDoubleOrToggleSignal = !RC_Select1stSignalForDoubleOrToggleSignal;
                ThisTimeDoNotUpdateMessageBox = true;
                chkSelectDoubleSignal.Checked = RC_Select1stSignalForDoubleOrToggleSignal;
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
            btnSingleRCPressed.Enabled = true;
        }

        private void btnCheckHeartBeat_Click(object sender, EventArgs e)
        {
            btnStopRCButton.Enabled = false;
            SendToSerial_v2(Prepare_Do_Nothing_CMD().ToArray());
            btnStopRCButton.Enabled = true;
        }

        private void StopCMDButton_Click(object sender, EventArgs e)
        {
            btnStopRCButton.Enabled = false;
            SendToSerial_v2(Prepare_STOP_CMD().ToArray());
            btnStopRCButton.Enabled = true;
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
                    listboxRCKey.Items.AddRange(RedRatData.RedRageGetRCNameList().ToArray());
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
                    if ((temp_type == typeof(DoubleSignal))||(RedRatData.RC_ToggleData().Length>0))
                    {
                        chkSelectDoubleSignal.Enabled = true;
                        rbDoubleSignalLED.Checked = true;
                    }
                    else
                    {
                        chkSelectDoubleSignal.Enabled = false;
                        rbDoubleSignalLED.Checked = false;
                    }
                    string temp_freq = RedRatData.RC_ModutationFreq().ToString();
                    rtbDecodeRCSignal.Text = "Modulation Frequency: " + temp_freq + " Hz\n";
                    lbFreq.Text = temp_freq + " Hz";
                    Displaying_RC_Signal_Array(RedRatData.RC_Lengths(), RedRatData.RC_MainSignal(), RedRatData.RC_RepeatSignal(), RedRatData.RC_NoRepeats(), RedRatData.RC_IntraSigPause(), 0);
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
            //RC_Select2ndSignalForDoubleSignal = if_checked;
            chkSelectDoubleSignal.Checked = if_checked;
        }

        private void chkSelect2ndSignal_CheckedChanged(object sender, EventArgs e)
        {
            RC_Select1stSignalForDoubleOrToggleSignal = chkSelectDoubleSignal.Checked;
            RedRatData.RedRatSelectRCSignal(listboxRCKey.SelectedIndex, RC_Select1stSignalForDoubleOrToggleSignal);

            double RC_ModutationFreq = RedRatData.RC_ModutationFreq();
            double[] RC_Lengths = RedRatData.RC_Lengths();
            byte[] RC_MainSignal = RedRatData.RC_MainSignal();
            byte[] RC_RepeatSignal = RedRatData.RC_RepeatSignal() ;
            int RC_NoRepeats = RedRatData.RC_NoRepeats();
            double RC_IntraSigPause = RedRatData.RC_IntraSigPause();
            rtbDecodeRCSignal.Text = "Modulation Frequency: " + RC_ModutationFreq.ToString() + "\n";
            lbFreq.Text = RC_ModutationFreq.ToString() + " Hz";
            Displaying_RC_Signal_Array(RC_Lengths, RC_MainSignal, RC_RepeatSignal, RC_NoRepeats, RC_IntraSigPause, 0);
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
                        listboxRCKey.Items.AddRange(RedRatData.RedRageGetRCNameList().ToArray());
                        UpdateRCDataOnForm();

                        Previous_Device = -1;
                        listboxAVDeviceList.SelectedIndex = 0;
                        this.listboxAVDeviceList_SelectedIndexChanged(sender, e); // Force to update both list selection box
                        listboxAVDeviceList.Enabled = true;
                        listboxRCKey.Enabled = true;
                        EnableSingleRCButton();
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
