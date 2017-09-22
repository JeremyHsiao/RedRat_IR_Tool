﻿using System;
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

using RedRat;
using RedRat.IR;
using RedRat.RedRat3;
using RedRat.Util;
using RedRat.AVDeviceMngmt;

namespace WindowsFormsApp1
{
    public partial class RedRatDBViewer : Form
    {
        private AVDeviceDB SignalDB;
        private AVDevice SelectedDevice;

        private double RC_ModutationFreq;
        private double[] RC_Lengths;
        private byte[] RC_SigData;
        private int RC_NoRepeats;
        private double RC_IntraSigPause;
        private byte[] RC_MainSignal;
        private byte[] RC_RepeatSignal;
        private ToggleBit[] RC_ToggleData;
        private bool RC_MainRepeatIdentical;        // result of calling bool MainRepeatIdentical(ModulatedSignal sig)  

        public RedRatDBViewer()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = ".\\";
            openFileDialog1.Filter = "RedRat Device files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

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

        private void SetupPulseView()
        {
            dgvPulseData.ColumnCount = 2;
            dgvPulseData.Columns[0].Name = "Pulse";
            dgvPulseData.Columns[1].Name = "Duration";

            string[] row0 = { "0", "N/A" };
            string[] row1 = { "0", "N/A" };
            string[] row2 = { "0", "N/A" };
            string[] row3 = { "0", "N/A" };
            dgvPulseData.Rows.Add(row0);
            dgvPulseData.Rows.Add(row1);
            dgvPulseData.Rows.Add(row2);
            dgvPulseData.Rows.Add(row3);
        }

        //
        // Form Events
        //
        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void listboxAVDeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            string devicename = listboxAVDeviceList.SelectedItem.ToString();
            SelectedDevice = SignalDB.GetAVDevice(devicename);
            listboxRCKey.Items.Clear();
            foreach (var Signal in SelectedDevice.Signals)
            {
                listboxRCKey.Items.Add(Signal.Name);
            }
            listboxRCKey.SelectedIndex = 0;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupPulseView();
        }

        private void listboxRCKey_SelectedIndexChanged(object sender, EventArgs e)
        {
            var Signal = SelectedDevice.Signals[listboxRCKey.SelectedIndex];

            if (Signal.GetType()==typeof(ModulatedSignal))
            {
                ModulatedSignal sig = (ModulatedSignal)Signal;
                rtbSignalData.Text = sig.ToString();
                RC_ModutationFreq = sig.ModulationFreq;
                txtFreq.Text = RC_ModutationFreq.ToString();
            }
            else if (Signal.GetType() == typeof(RedRat3ModulatedSignal))
            {
                RedRat3ModulatedSignal sig = (RedRat3ModulatedSignal)Signal;
            }
            //else if (Signal.GetType() == typeof(DoubleSignal))
            //{
            //    rtbSignalData.Text = Signal.ToString();
            //}
            //else if (Signal.GetType() == typeof(FlashCodeSignal))
            //{
            //    rtbSignalData.Text = Signal.ToString();
            //}
            //else if (Signal.GetType() == typeof(RedRat3FlashCodeSignal))
            //{
            //    rtbSignalData.Text = Signal.ToString();
            //}
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
            else
            {
                rtbSignalData.Text = Signal.ToString();
            }
        }

        private void dgvPulseData_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }


}
