using System;
using System.Collections.Generic;
using RedRat;
using RedRat.IR;
using RedRat.RedRat3;
using RedRat.Util;
using RedRat.AVDeviceMngmt;
using System.Diagnostics.Contracts;
using System.IO;
using System.Xml.Serialization;

namespace BlueRatLibrary
{
    public class RedRatDBParser
    {
        // Private Member
        private AVDeviceDB signal_db;
        private AVDevice selected_device;
        private IRPacket selected_signal;
        private IRPacket tx_signal;
        private bool if_use_1st_signal;
        private double time_factor_to_us;
        private bool sig_type_supported;
        // Property Function
        public AVDeviceDB SignalDB { get { return signal_db; } }
        public AVDevice SelectedDevice { get { return selected_device; } }
        public IRPacket SelectedSignal { get { return selected_signal; } }
        public IRPacket TxSignal { get { return tx_signal; } }
        public bool If_Use_First_Signal { get { return if_use_1st_signal; } }
        public double Time_Factor_to_uS { get { return time_factor_to_us; } }
        public bool Signal_Type_Supported { get { return sig_type_supported; } }
        //
        // Public Function
        //
        public RedRatDBParser() { Initialize(); }

        public bool RedRatLoadSignalDB(string dbFileName)
        {
            try
            {
                var ser = new XmlSerializer(typeof(AVDeviceDB));
                var fs = new FileStream((new FileInfo(dbFileName)).FullName, FileMode.Open);
                var avDeviceDB = (AVDeviceDB)ser.Deserialize(fs);
                fs.Close();
                signal_db = avDeviceDB;
                //
                // Use device 0 signal 0 as default after RC database is loaded
                //
                //RedRatSelectDevice(0);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("RedRatLoadSignalDB exception at filename: " + dbFileName + " - " + ex);
                return false;
            }
        }

        public bool RedRatSelectDevice(string DeviceName)
        {
            Contract.Requires(signal_db != null);
            try
            {
                selected_device = signal_db.GetAVDevice(DeviceName);
                //RedRatSelectRCSignal(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("RedRatLoadSignalDB exception at device: " + DeviceName + " - " + ex);
                return false;
            }
            return true;
        }

        public bool RedRatSelectDevice(int DeviceIndex)
        {
            Contract.Requires(DeviceIndex >= 0);
            Contract.Requires(DeviceIndex < signal_db.AVDevices.Length);
            try
            {
                selected_device = signal_db.AVDevices[DeviceIndex];
                //RedRatSelectRCSignal(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("RedRatLoadSignalDB exception at device number: " + DeviceIndex.ToString() + " - " + ex);
                return false;
            }
            return true;
        }

        public bool RedRatSelectRCSignal(string rcName, bool Use_1st_Signal = true)
        {
            Contract.Requires(selected_device != null);
            try
            {
                selected_signal = selected_device.GetSignal(rcName);
                ProcessSignalData(selected_signal, Use_1st_Signal);
                if_use_1st_signal = Use_1st_Signal;
            }
            catch (Exception ex)
            {
                Console.WriteLine("RedRatSelectRCSignal exception at RC Key: " + rcName + " - " + ex);
                return false;
            }
            return true;
        }

        public bool RedRatSelectRCSignal(int rc_index, bool Use_1st_Signal = true)
        {
            Contract.Requires(selected_device != null);
            Contract.Requires(rc_index >= 0);
            Contract.Requires(rc_index < selected_device.Signals.Length);
            try
            {
                selected_signal = selected_device.Signals[rc_index];
                ProcessSignalData(selected_signal, Use_1st_Signal);
                if_use_1st_signal = Use_1st_Signal;
            }
            catch (Exception ex)
            {
                Console.WriteLine("RedRatSelectRCSignal exception at RC Key number: " + rc_index.ToString() + " - " + ex);
                return false;
            }
            return true;
        }

        public Type RedRatSelectedSignalType()
        {
            return selected_signal.GetType();
        }

        public List<string> RedRatGetDBDeviceNameList()
        {
            Contract.Requires(signal_db != null);
            return new List<string>(signal_db.GetAVDeviceNames());
        }

        public List<string> RedRatGetRCNameList()
        {
            Contract.Requires(signal_db != null);
            Contract.Requires(selected_device != null);
            return new List<string>(selected_device.GetSignalNames());
        }

        public List<double> GetTxPulseWidth()
        {
            Contract.Requires(signal_db != null);
            Contract.Requires(selected_device != null);
            Contract.Requires(selected_signal != null);

            List<double> data_to_sent = new List<double>();
            int repeat_cnt, pulse_index, toggle_bit_index;
            bool pulse_high;
            double signal_width, high_pulse_compensation;

            // Tx Main signal
            high_pulse_compensation = 0;
            toggle_bit_index = 0;
            pulse_index = 0;
            pulse_high = true;
            foreach (var sig in rc_main_signal)
            {
                //
                //  Update Toggle Bits
                //
                if ((toggle_bit_index < RC_ToggleData_Length_Value()) && (pulse_index == rc_toggle_data[toggle_bit_index].bitNo))
                {
                    int toggle_bit_no = (if_use_1st_signal == true) ? (rc_toggle_data[toggle_bit_index].len1) : (rc_toggle_data[toggle_bit_index].len2);
                    signal_width = rc_lengths[toggle_bit_no];
                    toggle_bit_index++;
                }
                else
                {
                    signal_width = rc_lengths[sig];
                }
                signal_width *= time_factor_to_us;

                //
                // high_pulse period must extended a bit to compensate shorted-period due to detection mechanism
                //
                if (pulse_high)
                {
                    high_pulse_compensation = High_Pulse_Width_Adjustment(rc_modulation_freq, signal_width);
                    signal_width += high_pulse_compensation;
                }
                else
                {
                    signal_width -= high_pulse_compensation;
                }
                data_to_sent.Add(signal_width);

                pulse_high = !pulse_high;
                pulse_index++;
            }

            // Tx the rest of signal (2nd/3rd/...etc)
            repeat_cnt = rc_repeats_number;
            while (repeat_cnt-- > 0)
            {
                // Insert a blank
                signal_width = (rc_intra_sig_pause * time_factor_to_us) - high_pulse_compensation;
                data_to_sent.Add(signal_width);
                pulse_index++;
                pulse_high = true;

                foreach (var sig in rc_repeat_signal)
                {
                    //
                    //  Update Toggle Bits
                    //
                    if ((toggle_bit_index < RC_ToggleData_Length_Value()) && (pulse_index == rc_toggle_data[toggle_bit_index].bitNo))
                    {
                        signal_width = rc_lengths[(if_use_1st_signal == true) ? (rc_toggle_data[toggle_bit_index].len1) : (rc_toggle_data[toggle_bit_index].len2)];
                        toggle_bit_index++;
                    }
                    else
                    {
                        signal_width = rc_lengths[sig];
                    }
                    signal_width *= time_factor_to_us;
                    if (pulse_high)
                    {
                        high_pulse_compensation = High_Pulse_Width_Adjustment(rc_modulation_freq, signal_width);
                        signal_width += high_pulse_compensation;
                    }
                    else
                    {
                        signal_width -= high_pulse_compensation;
                    }
                    data_to_sent.Add(signal_width);
                    pulse_high = !pulse_high;
                    pulse_index++;
                }
            }

            //
            //  Insert end-of-packet blank.
            //
            if (rc_repeat_pause > 0)
            {
                data_to_sent.Add((rc_repeat_pause * time_factor_to_us) - high_pulse_compensation);
            }
            else if (rc_intra_sig_pause > 0)
            {
                data_to_sent.Add((rc_intra_sig_pause * time_factor_to_us) - high_pulse_compensation);
            }
            else
            {
                data_to_sent.Add((1000000 / rc_modulation_freq * 32) - high_pulse_compensation);            // min 32 pulse low
            }
            //pulse_index++;
            //
            // End of Tx preprocessing
            //

            return data_to_sent;
        }

        public double RC_ModutationFreq() { return rc_modulation_freq; }
        public double[] RC_Lengths() { return rc_lengths; }
        public byte[] RC_SigData() { return rc_sig_data; }
        public int RC_NoRepeats() { return rc_repeats_number; }
        public double RC_IntraSigPause() { return rc_intra_sig_pause; }
        public byte[] RC_MainSignal() { return rc_main_signal; }
        public byte[] RC_RepeatSignal() { return rc_repeat_signal; }
        public ToggleBit[] RC_ToggleData() { return rc_toggle_data; }
        public string RC_Description() { return rc_description; }
        public string RC_Name() { return rc_name; }
        public ModulatedSignal.PauseRepeatType RC_PauseRepeatMode() { return rc_pause_repeat_mode; }
        public double RC_RepeatPause() { return rc_repeat_pause; }
        public bool RC_MainRepeatIdentical() { return rc_main_repeat_identical; }

        // to avoid null pointer access when getting toggle bit array size
        public int RC_ToggleData_Length_Value() { return (rc_toggle_data!=null)?(rc_toggle_data.Length):0; }

    //
    // Private Function
    //
    private double rc_modulation_freq;
        private double[] rc_lengths;
        private byte[] rc_sig_data;
        private int rc_repeats_number;
        private double rc_intra_sig_pause;
        private byte[] rc_main_signal;
        private byte[] rc_repeat_signal;
        private ToggleBit[] rc_toggle_data;
        private string rc_description;
        private string rc_name;
        private ModulatedSignal.PauseRepeatType rc_pause_repeat_mode;
        private double rc_repeat_pause;
        private bool rc_main_repeat_identical; // result of calling bool MainRepeatIdentical(ModulatedSignal sig) 

        private void Initialize()
        {
            signal_db = null;
            selected_device = null;
            selected_signal = null;
            if_use_1st_signal = true;
            time_factor_to_us = 1000;
            sig_type_supported = false;
        }

        private void Verify_Toggle_Bit_Data_with_RC_Length_Array()
        {
            List<ToggleBit> temp_toggle_data = new List<ToggleBit>();

            //Contract.Requires<ArgumentNullException> (rc_lengths != null, "rc_lengths is null!");
            Contract.Requires(rc_lengths != null);
            foreach (var toggle_data in rc_toggle_data)
            {
                if ((toggle_data.len1 < rc_lengths.Length) && (toggle_data.len2 < rc_lengths.Length))
                {
                    // Only keep valid toggle bit data
                    temp_toggle_data.Add(toggle_data);
                }
                else
                {
                    Console.WriteLine("Toggle Bit Data Error at bit:" + toggle_data.bitNo + " (" + toggle_data.len1 + "," + toggle_data.len2 + ")");
                }
            }

            rc_toggle_data = temp_toggle_data.ToArray();
        }

        private void RedRatClearRCData()
        {
            rc_modulation_freq = 0;
            rc_lengths = null;
            rc_sig_data = null;
            rc_repeats_number = 0;
            rc_intra_sig_pause = 0;
            rc_main_signal = null;
            rc_repeat_signal = null;
            rc_toggle_data = null;
            rc_description = "";
            rc_name = "";
            rc_pause_repeat_mode = ModulatedSignal.PauseRepeatType.ConstantGap;
            rc_repeat_pause = 0;
            rc_main_repeat_identical = false;
         }

        private void ProcessSignalData(IRPacket process_signal, bool RC_Select1StSignal = true)
        {
            IRPacket sgl_signal;

            //
            // Select desired signal in Double Signal IR
            //
            if (process_signal.GetType() == typeof(DoubleSignal))
            {
                DoubleSignal temp_signal = (DoubleSignal)process_signal;
                if (RC_Select1StSignal == true)
                {
                    sgl_signal = temp_signal.Signal1;
                }
                else
                {
                    sgl_signal = temp_signal.Signal2;
                }
            }
            else
            {
                sgl_signal = process_signal;
            }

            if (sgl_signal.GetType() == typeof(ModulatedSignal))
            {
                ModulatedSignal sig = (ModulatedSignal)sgl_signal;
                rc_modulation_freq = sig.ModulationFreq;
                rc_lengths = sig.Lengths;
                rc_sig_data = sig.SigData;
                rc_repeats_number = sig.NoRepeats;
                rc_intra_sig_pause = sig.IntraSigPause;
                rc_main_signal = sig.MainSignal;
                rc_repeat_signal = sig.RepeatSignal;
                rc_toggle_data = sig.ToggleData;
                Verify_Toggle_Bit_Data_with_RC_Length_Array();
                rc_description = sig.Description;
                rc_name = sig.Name;
                rc_pause_repeat_mode = sig.PauseRepeatMode;
                rc_repeat_pause = sig.RepeatPause;
                rc_main_repeat_identical = ModulatedSignal.MainRepeatIdentical(sig);
                tx_signal = sig;
                sig_type_supported = true;
                if (rc_lengths == null)
                {
                    sig_type_supported = false;
                }
           }
           else if (sgl_signal.GetType() == typeof(RedRat3ModulatedSignal))
           {
                RedRat3ModulatedSignal sig = (RedRat3ModulatedSignal)sgl_signal;
                rc_modulation_freq = sig.ModulationFreq;
                rc_lengths = sig.Lengths;
                rc_sig_data = sig.SigData;
                rc_repeats_number = sig.NoRepeats;
                rc_intra_sig_pause = sig.IntraSigPause;
                rc_main_signal = sig.MainSignal;
                rc_repeat_signal = sig.RepeatSignal;
                rc_toggle_data = sig.ToggleData;
                Verify_Toggle_Bit_Data_with_RC_Length_Array();
                rc_description = sig.Description;
                rc_name = sig.Name;
                rc_pause_repeat_mode = sig.PauseRepeatMode;
                rc_repeat_pause = sig.RepeatPause;
                rc_main_repeat_identical = RedRat3ModulatedSignal.MainRepeatIdentical(sig);
                tx_signal = sig;
                sig_type_supported = true;
                if (rc_lengths == null)
                {
                    sig_type_supported = false;
                }
            }
/*
            else if (sgl_signal.GetType() == typeof(FlashCodeSignal))
            {
                FlashCodeSignal sig = (FlashCodeSignal)sgl_signal;
                rc_modulation_freq = 0;
                rc_lengths = sig.Lengths;
                rc_sig_data = sig.SigData;
                rc_repeats_number = sig.NoRepeats;
                rc_intra_sig_pause = sig.IntraSigPause;
                rc_main_signal = sig.MainSignal;
                rc_repeat_signal = sig.RepeatSignal;
                rc_toggle_data = null;
                rc_description = sig.Description;
                rc_name = sig.Name;
                rc_main_repeat_identical = false; // No such function, need to compare
                tx_signal = sig;
                sig_type_supported = true;
            }
            else if (sgl_signal.GetType() == typeof(RedRat3FlashCodeSignal))
            {
                RedRat3FlashCodeSignal sig = (RedRat3FlashCodeSignal)sgl_signal;
                rc_modulation_freq = 0;
                rc_lengths = sig.Lengths;
                rc_sig_data = sig.SigData;
                rc_repeats_number = sig.NoRepeats;
                rc_intra_sig_pause = sig.IntraSigPause;
                rc_main_signal = sig.MainSignal;
                rc_repeat_signal = sig.RepeatSignal;
                rc_toggle_data = null;
                rc_description = sig.Description;
                rc_name = sig.Name;
                rc_main_repeat_identical = false; // No such function, need to compare
                tx_signal = sig;
                sig_type_supported = true;
            }
*/
            //else if (Signal.GetType() == typeof(ProntoModulatedSignal))
            //{
            //}
            //else if (Signal.GetType() == typeof(RedRat3ModulatedKeyboardSignal))
            //{
            //}
            //else if (Signal.GetType() == typeof(RedRat3IrDaPacket))
            //{
            //}
            //else if (Signal.GetType() == typeof(IrDaPacket))
            //{
            //}
            else
            {
                tx_signal = sgl_signal;
                sig_type_supported = false;
                RedRatClearRCData();
            }
        }

        private double High_Pulse_Width_Adjustment(double RC_ModutationFreq, double signal_width)
        {
            //double RC_ModutationFreq = RedRatData.RC_ModutationFreq();
            double high_pulse_compensation;
            //ToggleBit[] RC_ToggleData = RedRatData.RC_ToggleData();

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
                if ((signal_width + carrier_width) >= min_width)
                {
                    high_pulse_compensation = 0; // carrier_width;
                }
                else
                {
                    high_pulse_compensation = min_width - signal_width;
                }
            }
            return high_pulse_compensation;
        }

    }
}

