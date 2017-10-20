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

using RedRat;
using RedRat.IR;
using RedRat.RedRat3;
using RedRat.Util;
using RedRat.AVDeviceMngmt;

namespace RedRatDatabaseViewer
{
    public partial class RedRatDBViewer : Form
    {
        private AVDeviceDB SignalDB;
        private AVDevice SelectedDevice;
        private IRPacket SelectedSignal;

        private double RC_ModutationFreq;
        private double[] RC_Lengths;
        private byte[] RC_SigData;
        private int RC_NoRepeats;
        private double RC_IntraSigPause;
        private byte[] RC_MainSignal;
        private byte[] RC_RepeatSignal;
        private ToggleBit[] RC_ToggleData;
        private string RC_Description;
        private string RC_Name;
        private ModulatedSignal.PauseRepeatType RC_PauseRepeatMode;
        private double RC_RepeatPause;
        //private 
        private bool RC_MainRepeatIdentical;        // result of calling bool MainRepeatIdentical(ModulatedSignal sig) 
        private bool RC_Select2ndSignalForDoubleSignal = false;
        //private for Double Signal or Toggle Bit
        private bool RC_SendNext_Indicator_1st_Bit_or_Signalal;

        private int Previous_Device = -1;
        private int Previous_Key = -1;

        public RedRatDBViewer()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
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
                    Console.WriteLine("Searching for RedRat...");
                    // using (var rr3 = FindRedRat3())
                    {
                        Console.WriteLine("RedRat Found. Loading DB file...");
                        SignalDB = LoadSignalDB(openFileDialog1.FileName);
                        Console.WriteLine("DB file is OK.");
                        foreach (var AVDevice in SignalDB.AVDevices)
                        {
                            listboxAVDeviceList.Items.Add(AVDevice.Name);
                        }
                        listboxAVDeviceList.Enabled = true;
                        listboxAVDeviceList.SelectedIndex = 0;
                        listboxRCKey.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

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

        private void Displaying_RC_Signal_Array(double[] rc_length, byte[] rc_main_sig_array, byte[] rc_repeat_sig_array, int rc_prepeat, double rc_intra_sig_pause, int Repeat_Tx_Times=0)
        {
            int repeat_cnt = rc_prepeat, pulse_high;
            const int time_ratio = 1000;
            
            // Main signal
            pulse_high = 1;
            foreach (var sig in rc_main_sig_array)
            {
                rtbDecodeRCSignal.AppendText(pulse_high.ToString() + ":" + (rc_length[sig]* time_ratio).ToString() + "\n");
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

        private void SendSingleRC()
        {
            SelectedSignal = SelectedDevice.Signals[listboxRCKey.SelectedIndex];
            lbModulationType.Text = SelectedSignal.GetType().ToString();

            if (SelectedSignal.GetType() == typeof(DoubleSignal))
            {
                DoubleSignal tempDoubleSignal = (DoubleSignal)SelectedSignal;
                chkSelectDoubleSignal.Enabled = true;
                rbDoubleSignalLED.Checked = true;
                ProcessDoubleSignalData(tempDoubleSignal);
            }
            else
            {
                chkSelectDoubleSignal.Enabled = false;
                rbDoubleSignalLED.Checked = false;
                ProcessSingleSignalData(SelectedSignal);
            }

        }


        //
        // Form Events
        //
        private void SingleOutput_Click(object sender, EventArgs e)
        {
            if((Previous_Device<0)||(Previous_Key<0))
            {
                // return immediately when No Selected Device or no Selected Signal
                return;
            }
            //
            // Display signal
            //
            IRPacket TxSignal;

            // 
            // Get the to-sent signal out of Double Signal
            //
            if (SelectedSignal.GetType() == typeof(DoubleSignal))
            {
                DoubleSignal tempDoubleSignal = (DoubleSignal)SelectedSignal;
                TxSignal = (RC_SendNext_Indicator_1st_Bit_or_Signalal==true)?(tempDoubleSignal.Signal1):(tempDoubleSignal.Signal2);
            }
            else
            {
                TxSignal = SelectedSignal;
            }
            //
            // Type conversion
            //
            if (TxSignal.GetType() == typeof(ModulatedSignal))
            {
                ModulatedSignal sig = (ModulatedSignal)TxSignal;
                GetRCData(sig);
            }
            else if (TxSignal.GetType() == typeof(RedRat3ModulatedSignal))
            {
                RedRat3ModulatedSignal sig = (RedRat3ModulatedSignal)TxSignal;
                GetRCData(sig);
            }
            else if (TxSignal.GetType() == typeof(FlashCodeSignal))
            {
                FlashCodeSignal sig = (FlashCodeSignal)TxSignal;
                GetRCData(sig);
            }
            else if (TxSignal.GetType() == typeof(RedRat3FlashCodeSignal))
            {
                RedRat3FlashCodeSignal sig = (RedRat3FlashCodeSignal)TxSignal;
                GetRCData(sig);
            }
            else
            {
                return; // not suppport so return immediately
            }
            //
            // Pre-processing done, start to Tx
            //
            int repeat_cnt = RC_NoRepeats, pulse_high, pulse_index, toggle_bit_index;
            const int time_ratio = 1000;
            rtbDecodeRCSignal.Text = "Tx Mod-Freq: " + RC_ModutationFreq.ToString() + "\n";
            // Tx Main signal
            toggle_bit_index = 0;
            pulse_index = 0;
            pulse_high = 1;
            foreach (var sig in RC_MainSignal)
            {
                double signal_width;
                //
                //  Update Toggle Bits
                //
                if ((toggle_bit_index < RC_ToggleData.Length)&&(pulse_index == RC_ToggleData[toggle_bit_index].bitNo))
                {
                    signal_width = RC_Lengths[(RC_SendNext_Indicator_1st_Bit_or_Signalal == true) ? (RC_ToggleData[toggle_bit_index].len1) : (RC_ToggleData[toggle_bit_index].len2)];
                    toggle_bit_index++;
                }
                else
                {
                    signal_width = RC_Lengths[sig];
                }
                rtbDecodeRCSignal.AppendText(pulse_high.ToString() + ":" + (signal_width * time_ratio).ToString() + "\n");
                pulse_high = (pulse_high != 0) ? 0 : 1;
                pulse_index++;
            }

            // Tx the rest of signal (2nd/3rd/...etc)
            while (repeat_cnt-- > 0)
            {
                rtbDecodeRCSignal.AppendText("0" + ":" + (RC_IntraSigPause * time_ratio).ToString() + "\n");
                pulse_index++;
                pulse_high = 1;
                foreach (var sig in RC_RepeatSignal)
                {
                    double signal_width;
                    //
                    //  Update Toggle Bits
                    //
                    if ((toggle_bit_index < RC_ToggleData.Length) && (pulse_index == RC_ToggleData[toggle_bit_index].bitNo))
                    {
                        signal_width = RC_Lengths[(RC_SendNext_Indicator_1st_Bit_or_Signalal == true) ? (RC_ToggleData[toggle_bit_index].len1) : (RC_ToggleData[toggle_bit_index].len2)];
                        toggle_bit_index++;
                    }
                    else
                    {
                        signal_width = RC_Lengths[sig];
                    }
                    rtbDecodeRCSignal.AppendText(pulse_high.ToString() + ":" + (signal_width * time_ratio).ToString() + "\n");
                    pulse_high = (pulse_high != 0) ? 0 : 1;
                    pulse_index++;
                }
            }
            RC_SendNext_Indicator_1st_Bit_or_Signalal = !RC_SendNext_Indicator_1st_Bit_or_Signalal;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // To be implemented
        }

        private void listboxAVDeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int Current_Device = listboxAVDeviceList.SelectedIndex;
            if (Previous_Device != Current_Device)
            {
                string devicename = listboxAVDeviceList.SelectedItem.ToString();
                SelectedDevice = SignalDB.GetAVDevice(devicename);
                listboxRCKey.Items.Clear();
                foreach (var Signal in SelectedDevice.Signals)
                {
                    listboxRCKey.Items.Add(Signal.Name);
                }
                Previous_Device = Current_Device;
                Previous_Key = -1;
                listboxRCKey.SelectedIndex = 0;  // Make sure (Previous_Key!= listboxRCKey.SelectedIndex) at next SelectedIndexChanged event
                RC_SendNext_Indicator_1st_Bit_or_Signalal = true;
            }
        }

        private void listboxRCKey_SelectedIndexChanged(object sender, EventArgs ev)
        {
            int Current_Key = listboxRCKey.SelectedIndex;

            if( Previous_Key != Current_Key)
            {
                SelectedSignal = SelectedDevice.Signals[Current_Key];
                lbModulationType.Text = SelectedSignal.GetType().ToString();

                if (SelectedSignal.GetType() == typeof(DoubleSignal))
                {
                    DoubleSignal tempDoubleSignal = (DoubleSignal)SelectedSignal;
                    chkSelectDoubleSignal.Enabled = true;
                    rbDoubleSignalLED.Checked = true;
                    ProcessDoubleSignalData(tempDoubleSignal);
                }
                else
                {
                    chkSelectDoubleSignal.Enabled = false;
                    rbDoubleSignalLED.Checked = false;
                    ProcessSingleSignalData(SelectedSignal);
                }

                rtbDecodeRCSignal.Text = "Modulation Frequency: " + RC_ModutationFreq.ToString() + "\n";
                lbFreq.Text = RC_ModutationFreq.ToString() + " Hz";
                Displaying_RC_Signal_Array(RC_Lengths, RC_MainSignal, RC_RepeatSignal, RC_NoRepeats, RC_IntraSigPause, 0);

                Previous_Key = Current_Key;
                RC_SendNext_Indicator_1st_Bit_or_Signalal = true;
            }
        }

        private void UpdateRCDataOnForm()
        {
            dgvPulseData.Rows.Clear();
            int index = 0;
            foreach (var len in RC_Lengths)
            {
                string []str = { len.ToString() };
                dgvPulseData.Rows.Add(str);
                dgvPulseData.Rows[index].HeaderCell.Value = String.Format("{0}", index);
                index++;
            }
            dgvToggleBits.Rows.Clear();
            index = 0;
            foreach (var toggle_bit in RC_ToggleData)
            {
                string[] str = { RC_Lengths[toggle_bit.len1].ToString(), RC_Lengths[toggle_bit.len2].ToString() };
                int bit_no = toggle_bit.bitNo;
                if ( (index>0) && (bit_no<Convert.ToInt64(dgvToggleBits.Rows[index-1].HeaderCell.Value)) )
                {
                    dgvToggleBits.Rows.Insert(index - 1, str);
                    dgvToggleBits.Rows[index-1].HeaderCell.Value = String.Format("{0}", bit_no);
                }
                else
                {
                    dgvToggleBits.Rows.Add(str);
                    dgvToggleBits.Rows[index].HeaderCell.Value = String.Format("{0}", bit_no);
                }
                index++;
            }
        }

        private void UpdateRCDoubleSignalCheckBoxValue(bool if_checked)
        {
            RC_Select2ndSignalForDoubleSignal = if_checked;
            chkSelectDoubleSignal.Checked = if_checked;
        }

        private void ClearRCData()
        {
            RC_ModutationFreq = 0;
            RC_Lengths = new double[1];
            RC_SigData = new byte[1];
            RC_NoRepeats = 0;
            RC_IntraSigPause = 0;
            RC_MainSignal = new byte[1];
            RC_RepeatSignal = new byte[1];
            RC_ToggleData = new ToggleBit[1];
            RC_Description = "";
            RC_Name = "";
            // RC_PauseRepeatMode = ;
            RC_RepeatPause = 0;
            RC_MainRepeatIdentical = false;
        }

        private void GetRCData(ModulatedSignal sig)
        {
            RC_ModutationFreq = sig.ModulationFreq;
            RC_Lengths = sig.Lengths;
            RC_SigData = sig.SigData;
            RC_NoRepeats = sig.NoRepeats;
            RC_IntraSigPause = sig.IntraSigPause;
            RC_MainSignal = sig.MainSignal;
            RC_RepeatSignal = sig.RepeatSignal;
            RC_ToggleData = sig.ToggleData;
            RC_Description = sig.Description;
            RC_Name = sig.Name;
            RC_PauseRepeatMode = sig.PauseRepeatMode;
            RC_RepeatPause = sig.RepeatPause;
            RC_MainRepeatIdentical = ModulatedSignal.MainRepeatIdentical(sig);
         }

        private void GetRCData(RedRat3ModulatedSignal sig)
        {
            RC_ModutationFreq = sig.ModulationFreq;
            RC_Lengths = sig.Lengths;
            RC_SigData = sig.SigData;
            RC_NoRepeats = sig.NoRepeats;
            RC_IntraSigPause = sig.IntraSigPause;
            RC_MainSignal = sig.MainSignal;
            RC_RepeatSignal = sig.RepeatSignal;
            RC_ToggleData = sig.ToggleData;
            RC_Description = sig.Description;
            RC_Name = sig.Name;
            RC_PauseRepeatMode = sig.PauseRepeatMode;
            RC_RepeatPause = sig.RepeatPause;
            RC_MainRepeatIdentical = RedRat3ModulatedSignal.MainRepeatIdentical(sig);
        }

        private void GetRCData(FlashCodeSignal sig)
        {
            RC_ModutationFreq = 0;
            RC_Lengths = sig.Lengths;
            RC_SigData = sig.SigData;
            RC_NoRepeats = sig.NoRepeats;
            RC_IntraSigPause = sig.IntraSigPause;
            RC_MainSignal = sig.MainSignal;
            RC_RepeatSignal = sig.RepeatSignal;
            RC_ToggleData = new ToggleBit[1];
            RC_Description = sig.Description;
            RC_Name = sig.Name;
            RC_MainRepeatIdentical = false; // No such function, need to compare
        }

        private void GetRCData(RedRat3FlashCodeSignal sig)
        {
            RC_ModutationFreq = 0;
            RC_Lengths = sig.Lengths;
            RC_SigData = sig.SigData;
            RC_NoRepeats = sig.NoRepeats;
            RC_IntraSigPause = sig.IntraSigPause;
            RC_MainSignal = sig.MainSignal;
            RC_RepeatSignal = sig.RepeatSignal;
            RC_ToggleData = new ToggleBit[1];
            RC_Description = sig.Description;
            RC_Name = sig.Name;
            RC_MainRepeatIdentical = false; // No such function, need to compare
        }


        private void ProcessSingleSignalData(IRPacket sgl_signal)
        {
            if (sgl_signal.GetType() == typeof(ModulatedSignal))
            {
                ModulatedSignal sig = (ModulatedSignal)sgl_signal;
                rtbSignalData.Text = sig.ToString();
                GetRCData(sig);
                UpdateRCDataOnForm();
            }
            else if (sgl_signal.GetType() == typeof(RedRat3ModulatedSignal))
            {
                RedRat3ModulatedSignal sig = (RedRat3ModulatedSignal)sgl_signal;
                rtbSignalData.Text = sig.ToString();
                GetRCData(sig);
                UpdateRCDataOnForm();
            }
            else if (sgl_signal.GetType() == typeof(FlashCodeSignal))
            {
                FlashCodeSignal sig = (FlashCodeSignal)sgl_signal;
                rtbSignalData.Text = sgl_signal.ToString();
                GetRCData(sig);
                UpdateRCDataOnForm();
            }
            else if (sgl_signal.GetType() == typeof(RedRat3FlashCodeSignal))
            {
                RedRat3FlashCodeSignal sig = (RedRat3FlashCodeSignal)sgl_signal;
                rtbSignalData.Text = sgl_signal.ToString();
                GetRCData(sig);
                UpdateRCDataOnForm();
            }
            //else if (Signal.GetType() == typeof(ProntoModulatedSignal))
            //{
            //    rtbSignalData.Text = Signal.ToString();
            //}
            //else if (Signal.GetType() == typeof(RedRat3ModulatedKeyboardSignal))
            //{
            //    rtbSignalData.Text = Signal.ToString();
            //}
            //else if (Signal.GetType() == typeof(RedRat3IrDaPacket))
            //{
            //    rtbSignalData.Text = Signal.ToString();
            //}
            //else if (Signal.GetType() == typeof(IrDaPacket))
            //{
            //    rtbSignalData.Text = Signal.ToString();
            //}
            else if (sgl_signal.GetType() == typeof(DoubleSignal))
            {
                rtbSignalData.Text = sgl_signal.ToString() + "\nNot supported in this funciton\n";
                ClearRCData();
                UpdateRCDataOnForm();
            }
            else
            {
                rtbSignalData.Text = sgl_signal.ToString();
                ClearRCData();
                UpdateRCDataOnForm();
            }
        }

        private void ProcessDoubleSignalData(DoubleSignal dbl_signal)
        {
            IRPacket tempSingleSignal;

            if (RC_Select2ndSignalForDoubleSignal == false)
            {
                tempSingleSignal = dbl_signal.Signal1;
            }
            else
            {
                tempSingleSignal = dbl_signal.Signal2;
            }

            //var tempSignal = typeof()
            ProcessSingleSignalData(tempSingleSignal);
        }

        private void chkSelectDoubleSignal_CheckedChanged(object sender, EventArgs e)
        {
            RC_Select2ndSignalForDoubleSignal = chkSelectDoubleSignal.Checked;
            var Signal = SelectedDevice.Signals[listboxRCKey.SelectedIndex];
            if (Signal.GetType() == typeof(DoubleSignal))
            {
                ProcessDoubleSignalData((DoubleSignal)Signal);
            }
            rtbDecodeRCSignal.Text = "Modulation Frequency: " + RC_ModutationFreq.ToString() + "\n";
            lbFreq.Text = RC_ModutationFreq.ToString() + " Hz";
            Displaying_RC_Signal_Array(RC_Lengths, RC_MainSignal, RC_RepeatSignal, RC_NoRepeats, RC_IntraSigPause, 0);
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

        private void dgvPulseData_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void lbFreq_Click(object sender, EventArgs e)
        {

        }

        private void dgvToggleBits_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }


}
