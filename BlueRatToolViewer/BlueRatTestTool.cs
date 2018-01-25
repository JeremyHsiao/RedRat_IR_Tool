using RedRat;
using RedRat.IR;
using RedRat.RedRat3;
using RedRat.Util;
using RedRat.AVDeviceMngmt;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Timers;

namespace BlueRatViewer
{
    public partial class BlueRatDevViewer : Form
    {

        private int Previous_Device = -1;
        private int Previous_Key = -1;
        private bool RC_Select1stSignalForDoubleOrToggleSignal = true;
        private bool FormIsClosing = false;

        private RedRatDBParser RedRatData = new RedRatDBParser();
        private BlueRat MyBlueRat = new BlueRat();

        static bool BlueRat_UART_Exception_status = false;

        static void BlueRat_UARTException(Object sender, EventArgs e)
        {
            BlueRat_UART_Exception_status = true;
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

        private void Serial_UpdatePortName()
        {
            lstBlueRatComPort.Items.Clear();
            /*
            foreach (string comport_s in SerialPort.GetPortNames())
            {
                listBox1.Items.Add(comport_s);
            }
            */
            List<string> bluerat_com = BlueRat.FindAllBlueRat();
            foreach (string com_port in bluerat_com)
            {
                lstBlueRatComPort.Items.Add(com_port);
            }

            if (lstBlueRatComPort.Items.Count > 0)
            {
                lstBlueRatComPort.SelectedIndex = 0;     // this can be modified to preferred default
                EnableConnectButton();
                UpdateToConnectButton();
            }
            else
            {
                DisableConnectButton();
                UpdateToConnectButton();
            }
        }

        private void UpdateRCFunctionButtonAfterConnection()
        {
            if ((MyBlueRat.CheckConnection() == true))
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

        const string CONNECT_UART_STRING_ON_BUTTON = "Connect";
        const string DISCONNECT_UART_STRING_ON_BUTTON = "Disconnect";

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
        public void AppendDBViewerMessageLog(string my_str)
        {
            if (this.rtbSignalData.InvokeRequired)
            {
                AppendSerialMessageCallback d = new AppendSerialMessageCallback(AppendDBViewerMessageLog);
                this.Invoke(d, new object[] { my_str });
            }
            else
            {
                this.rtbSignalData.AppendText(my_str);
                this.rtbSignalData.ScrollToCaret();
            }
        }

        // 這個主程式專用的delay的內部資料與function
        static bool BlueRatDevViewer_Delay_TimeOutIndicator = false;
        private static void BlueRatDevViewer_Delay_OnTimedEvent(object source, ElapsedEventArgs e)
        {
            BlueRatDevViewer_Delay_TimeOutIndicator = true;
        }

        private void BlueRatDevViewer_Delay(int delay_ms)
        {
            if (delay_ms <= 0) return;
            System.Timers.Timer aTimer = new System.Timers.Timer(delay_ms);
            aTimer.Elapsed += new ElapsedEventHandler(BlueRatDevViewer_Delay_OnTimedEvent);
            BlueRatDevViewer_Delay_TimeOutIndicator = false;
            aTimer.Enabled = true;
            while ((FormIsClosing == false) && (BlueRatDevViewer_Delay_TimeOutIndicator == false)) { Application.DoEvents(); Thread.Sleep(1); }
            aTimer.Stop();
            aTimer.Dispose();
        }

        private void btnConnectionControl_Click(object sender, EventArgs e)
        {
            if (btnConnectionControl.Text.Equals(CONNECT_UART_STRING_ON_BUTTON, StringComparison.Ordinal)) // Check if button is showing "Connect" at this moment.
            {   // User to connect
                string curItem = lstBlueRatComPort.SelectedItem.ToString();
                if (MyBlueRat.Connect(curItem) == true)
                {
                    BlueRat_UART_Exception_status = false;
                    UpdateToDisconnectButton();
                    DisableRefreshCOMButton();
                    UpdateRCFunctionButtonAfterConnection();
                }
                else
                {
                    rtbSignalData.AppendText(DateTime.Now.ToString("h:mm:ss tt") + " - Cannot connect to BlueRat.\n");
                }
            }
            else
            {   // User to disconnect
                if (MyBlueRat.Disconnect() == true)
                {
                    UpdateToConnectButton();
                    EnableRefreshCOMButton();
                    UpdateRCFunctionButtonAfterDisconnection();
                    if (BlueRat_UART_Exception_status)
                    { Serial_UpdatePortName(); }
                    BlueRat_UART_Exception_status = false;
                }
                else
                {
                    rtbSignalData.AppendText(DateTime.Now.ToString("h:mm:ss tt") + " - Cannot disconnect from RS232.\n");
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

            //if ((Previous_Device < 0) || (Previous_Key < 0))
            //{
            //    // return immediately when No Selected Device or no Selected Signal
            //    return;
            //}

            TemoparilyDisbleAllRCFunctionButtons();

            //btnSingleRCPressed.Enabled = false;
            //btnCheckHeartBeat.Enabled = false;
            //btnStopRCButton.Enabled = false;
            //btnConnectionControl.Enabled = false;
            btnGetRCFile.Enabled = false;

            // Use UART to transmit RC signal
            int rc_duration = MyBlueRat.SendOneRC(RedRatData) / 1000 + 1;
            Console.WriteLine("Tx: " + RedRatData.SelectedDevice.Name + " - " + RedRatData.SelectedSignal.Name);
            BlueRatDevViewer_Delay(rc_duration);

            // Update 2nd Signal checkbox
            if ((RedRatData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (RedRatData.RC_ToggleData_Length_Value() > 0))
            {
                // Switch to the other signal in display
                chkSelect2ndSignal.Enabled = false;
                RC_Select1stSignalForDoubleOrToggleSignal = !RC_Select1stSignalForDoubleOrToggleSignal;
                //RedRatData.RedRatSelectRCSignal(listboxRCKey.SelectedIndex, RC_Select1stSignalForDoubleOrToggleSignal);
                //ThisTimeDoNotUpdateMessageBox = true;
                chkSelect2ndSignal.Checked = !RC_Select1stSignalForDoubleOrToggleSignal;
                chkSelect2ndSignal.Enabled = true;
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
            if (MyBlueRat.CheckConnection() == true)
            {
                AppendDBViewerMessageLog("BlueRat available\n");
            }
            else
            {
                AppendDBViewerMessageLog("BlueRat not found\n");
            }
            //SendToSerial_v2(Prepare_Say_HI_CMD().ToArray());
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
            if (MyBlueRat.Stop_Current_Tx() == true)
            {
                this.rtbSignalData.AppendText("Stop Command Sent\n");
            }
            else
            {
                AppendDBViewerMessageLog("Command not sent\n");
            }

            string temp_string;
            temp_string = MyBlueRat.FW_VER.ToString();
            this.rtbSignalData.AppendText("Get SW ver: " + temp_string + "\n");
            //temp_string2 = MyBlueRat.Get_Command_Version();
            temp_string = MyBlueRat.CMD_VER.ToString();
            this.rtbSignalData.AppendText("Get CMD ver: " + temp_string + "\n");
            temp_string = MyBlueRat.BUILD_TIME.ToString();
            this.rtbSignalData.AppendText("Get Build time: " + temp_string + "\n");
            this.rtbSignalData.ScrollToCaret();

            //SendToSerial_v2(Prepare_STOP_CMD().ToArray());
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
                    listboxAVDeviceList.Enabled = false;
                    Previous_Device = Current_Device;
                    // Clear RC window
                    ClearRCWindow();
                    rtbSignalData.Text = "";

                    // Update RC Window and set 0 as selected
                    UpdateRCWindowAfterDeviceSelected();
                    Previous_Key = -1;
                    listboxRCKey.SelectedIndex = 0;
                    RedRatData.RedRatSelectRCSignal(0);

                    //
                    // Update Form Display Data according to content of RedRatData.SelectedSignal
                    //
                    UpdateRCDataOnForm();
                    listboxAVDeviceList.Enabled = true;
                    listboxRCKey.Enabled = true;

                    // Wait for data to refresh
                    BlueRatDevViewer_Delay(16);
                    listboxAVDeviceList.Enabled = true;
                }
                else
                {
                    Console.WriteLine("SelectDeviceError: " + devicename);
                }
            }
        }

        private void listboxRCKey_SelectedIndexChanged(object sender, EventArgs ev)
        {
            int Current_Key = listboxRCKey.SelectedIndex;

            if (Previous_Key != Current_Key)
            {
                listboxRCKey.Enabled = false;
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
                        rtbSignalData.Text = RedRatData.TxSignal.ToString() + "\n";
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
                            rtbSignalData.Text = RedRatData.TxSignal.ToString() + "\n";
                        }
                    }
                }
                else
                {
                    Console.WriteLine("SelectRCError: " + rcname);
                }
                listboxRCKey.Enabled = true;
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
            RC_Select1stSignalForDoubleOrToggleSignal = !chkSelect2ndSignal.Checked;
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

        private void BlueRatDevViewer_Load(object sender, EventArgs e)
        {
            //_serialPort = new SerialPort();
            //Serial_InitialSetting();
            Serial_UpdatePortName();
            //MyBlueRat.UARTException += BlueRat_UARTException;
        }

        private void BlueRatDevViewer_Closing(Object sender, FormClosingEventArgs e)
        {
            Console.WriteLine("BlueRatDevViewer_Closing");
            MyApplicationNeedToStopNow = true;
            FormIsClosing = true;
            //MyBlueRat.Stop_Current_Tx();
            //MyBlueRat.Force_Init_BlueRat();
            MyBlueRat.Disconnect();
        }

        public BlueRatDevViewer()
        {
            InitializeComponent();
        }

        private void ClearDeviceWindow()
        {
            listboxAVDeviceList.Items.Clear();
        }

        private void UpdateDeviceWindowAfterLoadDB()
        {
            listboxAVDeviceList.Items.Clear();
            listboxAVDeviceList.Items.AddRange(RedRatData.RedRatGetDBDeviceNameList().ToArray());
        }

        private void ClearRCWindow()
        {
            listboxRCKey.Items.Clear();
        }

        private void UpdateRCWindowAfterDeviceSelected()
        {
            listboxRCKey.Items.AddRange(RedRatData.RedRatGetRCNameList().ToArray());
        }

        private void btnGetRCFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = ".\\",
                Filter = "RC Device files (*.xml)|*.xml|All files (*.*)|*.*",
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
                        if (RedRatData.RedRatLoadSignalDB(openFileDialog1.FileName)) // Device 0 Signal 0 will be selected after RC database loaded
                        {
                            // Clear window after loading RC database
                            BlueRatDevViewer_Delay(16);
                            ClearDeviceWindow();
                            ClearRCWindow();
                            rtbSignalData.Text = "";

                            // Update Device Window and set 0 as selected
                            UpdateDeviceWindowAfterLoadDB();
                            Previous_Device = -1;
                            listboxAVDeviceList.SelectedIndex = 0;
                            RedRatData.RedRatSelectDevice(0);

                            // Update RC Window and set 0 as selected
                            UpdateRCWindowAfterDeviceSelected();
                            Previous_Key = -1;
                            listboxRCKey.SelectedIndex = 0;
                            RedRatData.RedRatSelectRCSignal(0);

                            //
                            // Update Form Display Data according to content of RedRatData.SelectedSignal
                            //
                            UpdateRCDataOnForm();
                            listboxAVDeviceList.Enabled = true;
                            listboxRCKey.Enabled = true;

                            // Wait for data to refresh
                            BlueRatDevViewer_Delay(16);
                        }
                        else
                        {
                            rtbDecodeRCSignal.Text = "Cannot load RC file or RC data file is corrupted";
                            UpdateRCDataOnForm();
                            Previous_Device = -1;
                            Previous_Key = -1;
                            listboxAVDeviceList.Enabled = true;
                            listboxRCKey.Enabled = false;
                        }
                        UpdateRCFunctionButtonAfterConnection();
                    }
                }
                catch (Exception ex)
                {
                    rtbDecodeRCSignal.Text = "RC data file data is corrupted!";
                    Console.WriteLine("btnGetRCFile_Click - " + ex.ToString());
                    UpdateRCDataOnForm();
                    Previous_Device = -1;
                    Previous_Key = -1;
                    listboxAVDeviceList.Enabled = true;
                    listboxRCKey.Enabled = false;
                }
            }
        }

        private void TEST_WalkThroughAllRCKeys(BlueRat TestBlueRat, RedRatDBParser TestDBData)
        {
            // 1. Open RC database file to load it in advance
            foreach (var temp_device in TestDBData.RedRatGetDBDeviceNameList())
            {
                Console.WriteLine("RC--" + temp_device);
                TestDBData.RedRatSelectDevice(temp_device);
                foreach (var temp_rc in TestDBData.RedRatGetRCNameList())
                {
                    if (FormIsClosing == true)
                    {
                        return;
                    }
                    TestDBData.RedRatSelectRCSignal(temp_rc, true);
                    if (TestDBData.Signal_Type_Supported == true)
                    {
                        Console.WriteLine(temp_rc);

                        // Use UART to transmit RC signal
                        int rc_duration = TestBlueRat.SendOneRC(TestDBData) / 1000 + 1;
                        BlueRatDevViewer_Delay(rc_duration);

                        if (FormIsClosing == true)
                        {
                            return;
                        }
                        // Update 2nd Signal checkbox
                        if ((TestDBData.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (TestDBData.RC_ToggleData_Length_Value() > 0))
                        {
                            TestDBData.RedRatSelectRCSignal(temp_rc, false);
                            // Use UART to transmit RC signal
                            rc_duration = TestBlueRat.SendOneRC(RedRatData) / 1000 + 1;
                            BlueRatDevViewer_Delay(rc_duration);
                        }
                    }
                }
            }
        }

        private void TEST_StressSendingAlreadySelectedRC(BlueRat my_blue_rat, RedRatDBParser redrat_rc_db)
        {
            // Testing: send "stress_cnt" times Single RC
            // 
            // Precondition
            //   1. Load RC Database by RedRatLoadSignalDB()
            //   2. Select Device by RedRatSelectDevice() using device_name or index_no
            //   3. Select RC Signal by RedRatSelectRCSignal() using rc_name or index_no --> specify false at 2nd input parameter if need to Tx 2nd signal of Double signal / Toggle Bits Signal
            Contract.Requires(redrat_rc_db != null);
            Contract.Requires(redrat_rc_db.SignalDB != null);
            Contract.Requires(redrat_rc_db.SelectedDevice != null);
            Contract.Requires(redrat_rc_db.SelectedSignal != null);
            Contract.Requires(redrat_rc_db.Signal_Type_Supported == true);

            byte stress_cnt = 250;

            while (stress_cnt-- > 0)
            {
                // Use UART to transmit RC signal
                int rc_duration = my_blue_rat.SendOneRC(redrat_rc_db) / 1000 + 1;
                BlueRatDevViewer_Delay(rc_duration);

                // Update 2nd Signal checkbox
                if ((redrat_rc_db.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (redrat_rc_db.RC_ToggleData_Length_Value() > 0))
                {
                    // Switch to the other signal in display
                    chkSelect2ndSignal.Enabled = false;
                    RC_Select1stSignalForDoubleOrToggleSignal = !RC_Select1stSignalForDoubleOrToggleSignal;
                    //redrat_rc_db.RedRatSelectRCSignal(listboxRCKey.SelectedIndex, RC_Select1stSignalForDoubleOrToggleSignal);
                    ThisTimeDoNotUpdateMessageBox = true;
                    chkSelect2ndSignal.Checked = !RC_Select1stSignalForDoubleOrToggleSignal;
                    chkSelect2ndSignal.Enabled = true;
                }
            }
        }

        private void TEST_StressSendingRepeatCount(BlueRat my_blue_rat)
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
                    rc_duration = my_blue_rat.SendOneRC(RedRatData, repeat_count_threshold - 1);
                    repeat_cnt -= repeat_count_threshold;
                }
                else
                {
                    rc_duration = my_blue_rat.SendOneRC(RedRatData, Convert.ToByte(repeat_cnt));
                    repeat_cnt = 0;
                }

                rc_duration /= 1000 + 1;
                BlueRatDevViewer_Delay(rc_duration);
            }
        }

        private void TEST_Return_Repeat_Count_and_Tx_Status(BlueRat my_blue_rat, RedRatDBParser redrat_rc_db)
        {
            int repeat = 300; // max value is 4,294,967,295 (0xffffffff)
            const byte recommended_first_repeat_cnt_value = 100;    // must be <= 0xff
            // Load RedRat database - 載入資料庫
            if (!(redrat_rc_db.RedRatLoadSignalDB(@".\DeviceDB.xml")))
            {
                return;     // return if loading RC fails
            }
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            BlueRatDevViewer_Delay(16);
            // Select Device  - 選擇RC Device
            redrat_rc_db.RedRatSelectDevice("HP-MCE");
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            //BlueRatDevViewer_Delay(16);
            // Select RC - 選擇RC (使用名稱或Index No)
            redrat_rc_db.RedRatSelectRCSignal("1", true);
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            //BlueRatDevViewer_Delay(16);
            // Check if this RC code is supported -- 如果此訊號資料OK可以發射,就發射
            if (redrat_rc_db.Signal_Type_Supported == true)
            {
                // Use UART to transmit RC signal -- repeat (recommended_first_repeat_cnt_value-1) times == total transmit (recommended_first_repeat_cnt_value) times
                int rc_duration = my_blue_rat.SendOneRC(redrat_rc_db, recommended_first_repeat_cnt_value - 1) / 1000 + 1;
                // Delay to wait for RC Tx finished
                BlueRatDevViewer_Delay(1);
                // 將剩下的Repeat_Count輸出
                my_blue_rat.Add_Repeat_Count(Convert.ToUInt32(repeat - recommended_first_repeat_cnt_value));
                rc_duration = ((rc_duration * repeat) / Convert.ToInt32(recommended_first_repeat_cnt_value)) - 1;

                //BlueRatDevViewer_Delay(rc_duration-1);
                // 這裏是另一種delay的做法,如果需要在等待時,讓系統做一些別的事情
                System.Timers.Timer aTimer = new System.Timers.Timer(rc_duration);
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                ClearTimeOutIndicator();
                aTimer.Enabled = true;
                // 一直循環等到 GetTimeOutIndicator() == true為止
                // 在這等待的同時,就可以安排其它要做的事情
                // 這裏至少要放Applicaiton.DoEvents()讓其它event有機會完成
                while ((GetTimeOutIndicator() == false) && (FormIsClosing == false))
                {
                    bool cmd_ok_status;

                    cmd_ok_status = my_blue_rat.Get_Remaining_Repeat_Count(out int temp_repeat_cnt);
                    if (cmd_ok_status)
                    {
                        Console.WriteLine(temp_repeat_cnt.ToString());
                    }
                    else
                    {
                        Console.WriteLine("remaining_cnt_err");
                    }
                    if ((temp_repeat_cnt <= 4) || (cmd_ok_status == false) || (FormIsClosing == true)) break;
                    BlueRatDevViewer_Delay(360);      // better >=360

                    if (FormIsClosing == true) break;
                    cmd_ok_status = my_blue_rat.Get_Current_Tx_Status(out bool temp_tx_status);
                    if (cmd_ok_status)
                    {
                        Console.WriteLine(temp_tx_status.ToString());
                    }
                    else
                    {
                        Console.WriteLine("tx_status_err");
                    }
                    if ((temp_tx_status == false) || (cmd_ok_status == false) || (FormIsClosing == true)) break;
                    BlueRatDevViewer_Delay(200);      // better >= 200
                }

                while ((GetTimeOutIndicator() == false) && (FormIsClosing == false))   // keep looping until timeout
                {
                    BlueRatDevViewer_Delay(32);
                }
                aTimer.Stop();
                aTimer.Dispose();
            }
        }

        // 發射一個信號的範例 - 在此不repeat,所以可以忽略傳入參數
        private void Example_to_Send_RC_without_Repeat_Count(BlueRat my_blue_rat, RedRatDBParser redrat_rc_db)
        {
            // Load RedRat database - 載入資料庫
            if (!(redrat_rc_db.RedRatLoadSignalDB(@".\DeviceDB.xml")))
            {
                return;     // return if loading RC fails
            }
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            BlueRatDevViewer_Delay(16);
            //Application.DoEvents();
            // Select Device  - 選擇RC Device
            redrat_rc_db.RedRatSelectDevice("HP-MCE");
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            //BlueRatDevViewer_Delay(16);
            // Select RC - 選擇RC (使用名稱或Index No)
            redrat_rc_db.RedRatSelectRCSignal("1", true);
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            //BlueRatDevViewer_Delay(16); 
            // Check if this RC code is supported -- 如果此訊號資料OK可以發射,就發射
            if (redrat_rc_db.Signal_Type_Supported == true)
            {
                // Use UART to transmit RC signal -- repeat (SendOneRC_default_cnt) times == total transmit (SendOneRC_default_cnt+1) times
                int rc_duration = my_blue_rat.SendOneRC(redrat_rc_db) / 1000 + 1;
                // Delay to wait for RC Tx finished
                BlueRatDevViewer_Delay(rc_duration);

                // If you need to send double signal or toggle bit signal at next IR transmission -- 這裡是示範如何發射Double Signal或Toggle Signal的第二個信號
                if ((redrat_rc_db.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (redrat_rc_db.RC_ToggleData_Length_Value() > 0))
                {
                    // Use UART to transmit RC signal -- repeat 10 times
                    redrat_rc_db.RedRatSelectRCSignal("1", false);
                    rc_duration = my_blue_rat.SendOneRC(redrat_rc_db) / 1000 + 1;
                    // Delay to wait for RC Tx finished
                    BlueRatDevViewer_Delay(rc_duration);
                }
            }
        }

        // 如果一個RC要repeat不超過255次,可以使用一個byte來傳入repeat次數,就能夠直接傳入repeat次數
        private void Example_to_Send_RC_with_Repeat_Count(BlueRat my_blue_rat, RedRatDBParser redrat_rc_db) // repeat count <= 0xff
        {
            const byte SendOneRC_default_cnt = 2;
            // Load RedRat database - 載入資料庫
            if (!(redrat_rc_db.RedRatLoadSignalDB(@".\DeviceDB.xml")))
            {
                return;     // return if loading RC fails
            }
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            BlueRatDevViewer_Delay(16);
            //Application.DoEvents();
            // Select Device  - 選擇RC Device
            redrat_rc_db.RedRatSelectDevice("HP-MCE");
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            //BlueRatDevViewer_Delay(16);
            // Select RC - 選擇RC (使用名稱或Index No)
            redrat_rc_db.RedRatSelectRCSignal("1", true);
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            //BlueRatDevViewer_Delay(16);
            // Check if this RC code is supported -- 如果此訊號資料OK可以發射,就發射
            if (redrat_rc_db.Signal_Type_Supported == true)
            {
                // Use UART to transmit RC signal -- repeat (SendOneRC_default_cnt) times == total transmit (SendOneRC_default_cnt+1) times
                int rc_duration = my_blue_rat.SendOneRC(redrat_rc_db, SendOneRC_default_cnt) / 1000 + 1;
                // Delay to wait for RC Tx finished
                BlueRatDevViewer_Delay(rc_duration);

                Console.WriteLine("DONE FirstSignal");

                // If you need to send double signal or toggle bit signal at next IR transmission -- 這裡是示範如何發射Double Signal或Toggle Signal的第二個信號
                if ((redrat_rc_db.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (redrat_rc_db.RC_ToggleData_Length_Value() > 0))
                {
                    // Use UART to transmit RC signal -- repeat 10 times
                    redrat_rc_db.RedRatSelectRCSignal("1", false);
                    rc_duration = my_blue_rat.SendOneRC(redrat_rc_db, SendOneRC_default_cnt) / 1000 + 1;
                    // Delay to wait for RC Tx finished
                    BlueRatDevViewer_Delay(rc_duration);
                }
            }
        }

        // 如果一個RC要repeat很多次(過255次),無法像前面使用一個byte來傳入repeat次數,
        // 就要在後面使用另一個指令,來追加要repeat的次數,
        // 該指令的傳入參數為4-byte (0~4,294,967,295 (0xffffffff))
        bool MyApplicationNeedToStopNow = false;
        private void Example_to_Send_RC_with_Large_Repeat_Count(BlueRat my_blue_rat, RedRatDBParser redrat_rc_db)
        {
            int repeat = 300; // max value is 4,294,967,295 (0xffffffff)
            const byte recommended_first_repeat_cnt_value = 100;    // must be <= 0xff
            // Load RedRat database - 載入資料庫
            if (!(redrat_rc_db.RedRatLoadSignalDB(@".\DeviceDB.xml")))
            {
                return;     // return if loading RC fails
            }
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            BlueRatDevViewer_Delay(16);
            //Application.DoEvents();
            // Select Device  - 選擇RC Device
            redrat_rc_db.RedRatSelectDevice("HP-MCE");
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            //BlueRatDevViewer_Delay(16);
            // Select RC - 選擇RC (使用名稱或Index No)
            redrat_rc_db.RedRatSelectRCSignal("1", true);
            // Let main program has time to refresh redrat_rc_db data content -- can be skiped if this code is not running in UI event call-back function
            //BlueRatDevViewer_Delay(16);
            // Check if this RC code is supported -- 如果此訊號資料OK可以發射,就發射
            if (redrat_rc_db.Signal_Type_Supported == true)
            {
                // Use UART to transmit RC signal -- repeat (recommended_first_repeat_cnt_value-1) times == total transmit (recommended_first_repeat_cnt_value) times
                int rc_duration = my_blue_rat.SendOneRC(redrat_rc_db, recommended_first_repeat_cnt_value - 1) / 1000 + 1;
                // Delay to wait for RC Tx finished
                BlueRatDevViewer_Delay(1);
                // 將剩下的Repeat_Count輸出
                my_blue_rat.Add_Repeat_Count(Convert.ToUInt32(repeat - recommended_first_repeat_cnt_value));
                rc_duration = ((rc_duration * repeat) / Convert.ToInt32(recommended_first_repeat_cnt_value)) - 1;

                //BlueRatDevViewer_Delay(rc_duration-1);
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
                    Thread.Sleep(rc_duration / 100);
                    // 如果應用程式需要立刻停止(例如form-closing,或使用者中斷Scheduler的執行),則可以使用break跳出此while-loop
                    if (MyApplicationNeedToStopNow == true)
                    {
                        break;
                    }
                }
                aTimer.Stop();
                aTimer.Dispose();

                Console.WriteLine("DONE FirstSignal");

                // If you need to send double signal or toggle bit signal at next IR transmission -- 這裡是示範如何發射Double Signal或Toggle Signal的第二個信號
                if ((redrat_rc_db.RedRatSelectedSignalType() == (typeof(DoubleSignal))) || (redrat_rc_db.RC_ToggleData_Length_Value() > 0))
                {
                    // Use UART to transmit RC signal -- repeat 10 times
                    redrat_rc_db.RedRatSelectRCSignal("1", false);
                    rc_duration = my_blue_rat.SendOneRC(redrat_rc_db, recommended_first_repeat_cnt_value - 1) / 1000 + 1;
                    // Delay to wait for RC Tx finished
                    BlueRatDevViewer_Delay(1);
                    my_blue_rat.Add_Repeat_Count(Convert.ToUInt32(repeat - recommended_first_repeat_cnt_value));
                    rc_duration = ((rc_duration * repeat) / Convert.ToInt32(recommended_first_repeat_cnt_value));
                    BlueRatDevViewer_Delay(rc_duration - 1);
                }
            }
        }

        private void Test_GPIO_Input(BlueRat my_blue_rat)
        {
            UInt32 GPIO_input_value, retry_cnt;
            bool bRet = false;
            retry_cnt = 3;
            do
            {
                bRet = my_blue_rat.Get_GPIO_Input(out GPIO_input_value);
            }
            while ((bRet == false) && (--retry_cnt > 0) && (FormIsClosing == false));
            if (bRet)
            {
                Console.WriteLine("GPIO_input: " + GPIO_input_value.ToString());
            }
            else
            {
                Console.WriteLine("GPIO_input fail after retry");
            }
        }

        private void TEST_GPIO_Output(BlueRat my_blue_rat)
        {
            const int delay_time = 100;
            // Testing: send GPIO output with byte parameter -- Set output port value at once
            for (uint output_value = 0; output_value <= 0xff; output_value++)
            {
                if (my_blue_rat.Set_GPIO_Output(Convert.ToByte(output_value & 0xff)))
                {
                    BlueRatDevViewer_Delay(delay_time / 2);
                }
                else
                {
                    Console.WriteLine("Set_GPIO_Output-err1");
                }
                if (FormIsClosing == true)
                {
                    return;
                }
            }

            int run_time = 10;
            const UInt32 IO_value_mask = 0x0, reverse_IO_value_mask = 0x1;

            my_blue_rat.Set_GPIO_Output(Convert.ToByte((~reverse_IO_value_mask) & 0xff));
            BlueRatDevViewer_Delay(delay_time);
            if (FormIsClosing == true)
            {
                return;
            }

            while (run_time-- > 0)
            {
                for (Byte output_bit = 0; output_bit < 7;)
                {
                    //UInt32 temp_parameter = (output_bit << 8) | reverse_IO_value_mask;
                    //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    if (my_blue_rat.Set_GPIO_Output_SinglePort(output_bit, Convert.ToByte(reverse_IO_value_mask)))
                    {
                        BlueRatDevViewer_Delay(16);
                        output_bit++;
                    }
                    else
                    {
                        Console.WriteLine("Set_GPIO_Output-err2");
                        return;
                    }
                    if (FormIsClosing == true)
                    {
                        return;
                    }
                    //temp_parameter = (((output_bit) << 8) | IO_value_mask);
                    //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    if (my_blue_rat.Set_GPIO_Output_SinglePort(output_bit, Convert.ToByte(IO_value_mask)))
                    {
                        BlueRatDevViewer_Delay(delay_time);
                    }
                    else
                    {
                        Console.WriteLine("Set_GPIO_Output-err2");
                        return;
                    }

                    if (FormIsClosing == true)
                    {
                        return;
                    }
                }
                for (Byte output_bit = 7; output_bit > 0;)
                {
                    //UInt32 temp_parameter = (output_bit << 8) | reverse_IO_value_mask;
                    //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    if (my_blue_rat.Set_GPIO_Output_SinglePort(output_bit, Convert.ToByte(reverse_IO_value_mask)))
                    {
                        BlueRatDevViewer_Delay(16);
                        output_bit--;
                    }
                    else
                    {
                        Console.WriteLine("Set_GPIO_Output-err3");
                        return;
                    }
                    if (FormIsClosing == true)
                    {
                        return;
                    }
                    //temp_parameter = (((output_bit) << 8) | IO_value_mask);
                    //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    if (my_blue_rat.Set_GPIO_Output_SinglePort(output_bit, Convert.ToByte(IO_value_mask)))
                    {
                        BlueRatDevViewer_Delay(delay_time);
                    }
                    else
                    {
                        Console.WriteLine("Set_GPIO_Output-err3");
                        return;
                    }
                    if (FormIsClosing == true)
                    {
                        return;
                    }
                }
            }
        }

        private void btnRepeatRC_Click(object sender, EventArgs e)
        {
            TemoparilyDisbleAllRCFunctionButtons();

            if (lstBlueRatComPort.Items.Count == 0)
            {
                // Example -- 示範現在如何找出所有的小藍鼠
                List<string> bluerat_com = BlueRat.FindAllBlueRat();
                foreach (string com_port in bluerat_com)
                {
                    lstBlueRatComPort.Items.Add(com_port);
                }
            }
            if ((lstBlueRatComPort.SelectedItem == null) && (lstBlueRatComPort.Items.Count > 0))    // Use first one if none-selected
            {
                lstBlueRatComPort.SelectedIndex = 0;
            }

            if (lstBlueRatComPort.SelectedItem != null)
            {
                // Go through all BlueRat
                int ScanBlueRat_Count = 0;
                do
                {
                    string com_port_name = lstBlueRatComPort.SelectedItem.ToString();
                    //示範現在如何聯接小藍鼠 -- 需傳入COM PORT名稱
                    if (MyBlueRat.Connect(com_port_name))
                    {
                        // 在第一次/或長時間未使用之後,要開始使用BlueRat跑Schedule之前,建議執行這一行,確保BlueRat的起始狀態一致 -- 正常情況下不執行並不影響BlueRat運行,但為了找問題方便,還是請務必執行
                        MyBlueRat.Force_Init_BlueRat();
                        string temp_string1, temp_string2, temp_string3;
                        //temp_string1 = MyBlueRat.Get_SW_Version();
                        temp_string1 = MyBlueRat.FW_VER.ToString();
                        //temp_string2 = MyBlueRat.Get_Command_Version();
                        temp_string2 = MyBlueRat.CMD_VER.ToString();
                        temp_string3 = MyBlueRat.BUILD_TIME;
                        Console.WriteLine("BlueRat at " + com_port_name + ":\n" + "SW: " + temp_string1 + "\n" + "CMD_API: " + temp_string2 + "\n" + "Build time: " + temp_string3 + "\n");

                        if (FormIsClosing == true) break;
                        Test_GPIO_Input(MyBlueRat);
                        Console.WriteLine("DONE - Test_GPIO_Input");

                        if (FormIsClosing == true) break;
                        TEST_Return_Repeat_Count_and_Tx_Status(MyBlueRat, RedRatData);
                        Console.WriteLine("DONE - TEST_Return_Repeat_Count_and_Tx_Status");

                        if (FormIsClosing == true) break;
                        MyBlueRat.Stop_Current_Tx();

                        if (FormIsClosing == false)
                        {
                            Example_to_Send_RC_without_Repeat_Count(MyBlueRat, RedRatData);
                            MyBlueRat.CheckConnection();
                            Console.WriteLine("DONE - Example_to_Send_RC_without_Repeat_Count");
                        }

                        if (FormIsClosing == false)
                        {
                            Example_to_Send_RC_with_Repeat_Count(MyBlueRat, RedRatData);
                            MyBlueRat.CheckConnection();
                            Console.WriteLine("DONE - Example_to_Send_RC_with_Repeat_Count");
                        }
                        if (FormIsClosing == false)
                        {
                            Example_to_Send_RC_with_Large_Repeat_Count(MyBlueRat, RedRatData);
                            MyBlueRat.CheckConnection();
                            Console.WriteLine("DONE - Example_to_Send_RC_with_Large_Repeat_Count");
                        }

                        if (FormIsClosing == false)
                        {
                            TEST_GPIO_Output(MyBlueRat);
                            MyBlueRat.CheckConnection();
                            Console.WriteLine("DONE - TEST_GPIO_Output");
                        }

                        if ((RedRatData != null) && (RedRatData.SignalDB != null))
                        {
                            if (FormIsClosing == false)
                            {
                                TEST_WalkThroughAllRCKeys(MyBlueRat,RedRatData);
                                MyBlueRat.CheckConnection();
                                Console.WriteLine("DONE - TEST_WalkThroughAllRCKeys");
                            }
                            if (FormIsClosing == false)
                            {
                                TEST_StressSendingRepeatCount(MyBlueRat);
                                MyBlueRat.CheckConnection();
                                Console.WriteLine("DONE - TEST_StressSendingRepeatCount");
                            }
                        }

                        ////
                        // Self-testing code
                        //
                        if (FormIsClosing == false)
                        {
                            MyBlueRat.TEST_WalkThroughAllCMDwithData();
                            MyBlueRat.CheckConnection();
                            Console.WriteLine("DONE - TEST_WalkThroughAllCMDwithData");
                        }

                        if (FormIsClosing == false)
                        {
                            MyBlueRat.TEST_SENSOR_Input();
                            MyBlueRat.CheckConnection();
                            Console.WriteLine("DONE - Get_Sensor_Input");
                        }

                        if (FormIsClosing == false)
                        {
                            //MyBlueRat.Enter_ISP_Mode();
                            //Console.WriteLine("DONE - Enter_ISP_Mode");
                        }

                        //示範現在如何結束聯接UART並釋放 
                        MyBlueRat.Disconnect();
                    }
                    if (lstBlueRatComPort.SelectedIndex == (lstBlueRatComPort.Items.Count - 1))
                    {
                        lstBlueRatComPort.SelectedIndex = 0;
                    }
                    else
                    {
                        lstBlueRatComPort.SelectedIndex++;
                    }
                }
                while ((FormIsClosing == false) && (++ScanBlueRat_Count < lstBlueRatComPort.Items.Count));

                // UI update after disconnecting BlueRat
                UpdateToConnectButton();
                EnableRefreshCOMButton();
                UpdateRCFunctionButtonAfterDisconnection();
            }
            UndoTemoparilyDisbleAllRCFunctionButtons();
        }

        private void btnLoadNewFirmware_Click(object sender, EventArgs e)
        {

        }

        private void btnFWUpgrade_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void AutoRunAllRC_Click(object sender, EventArgs e)
        {
            TemoparilyDisbleAllRCFunctionButtons();

            string com_port_name = lstBlueRatComPort.SelectedItem.ToString();
            //示範現在如何聯接小藍鼠 -- 需傳入COM PORT名稱
            if (MyBlueRat.Connect(com_port_name))
            {
                // 在第一次/或長時間未使用之後,要開始使用BlueRat跑Schedule之前,建議執行這一行,確保BlueRat的起始狀態一致 -- 正常情況下不執行並不影響BlueRat運行,但為了找問題方便,還是請務必執行
                MyBlueRat.Force_Init_BlueRat();
                string temp_string1, temp_string2, temp_string3;
                //temp_string1 = MyBlueRat.Get_SW_Version();
                temp_string1 = MyBlueRat.FW_VER.ToString();
                //temp_string2 = MyBlueRat.Get_Command_Version();
                temp_string2 = MyBlueRat.CMD_VER.ToString();
                temp_string3 = MyBlueRat.BUILD_TIME;
                Console.WriteLine("BlueRat at " + com_port_name + ":\n" + "SW: " + temp_string1 + "\n" + "CMD_API: " + temp_string2 + "\n" + "Build time: " + temp_string3 + "\n");

                if ((RedRatData == null)||(RedRatData.SignalDB == null))
                {
                    // Load RedRat database - 載入資料庫
                    if (!(RedRatData.RedRatLoadSignalDB(@"..\..\..\..\RC DB\DeviceDB - 複製.xml")))
                    {
                        return;     // return if loading RC fails
                    }
                    // Let main program has time to refresh RedRatData data content -- can be skiped if this code is not running in UI event call-back function
                    BlueRatDevViewer_Delay(16);
                }

                if ((RedRatData != null) && (RedRatData.SignalDB != null))
                {
                    TEST_WalkThroughAllRCKeys(MyBlueRat, RedRatData);
                 }

                //示範現在如何結束聯接UART並釋放 
                MyBlueRat.Disconnect();
            }
            UndoTemoparilyDisbleAllRCFunctionButtons();

        }
    }
}
