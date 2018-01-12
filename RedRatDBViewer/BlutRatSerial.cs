using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace RedRatDatabaseViewer
{
    class BlueRatSerial
    {
        // static member/function to shared aross all BlueRatSerial
        static private Dictionary<string, Object> BlueRatSerialDictionary = new Dictionary<string, Object>();
        //static private void AddConnectionLUT(string com_port, object obj) { BlueRatSerialDictionary.Add(com_port, obj); }
        //
        // public functions
        //
        public const int Serial_BaudRate = 115200;
        public const Parity Serial_Parity = Parity.None;
        public const int Serial_DataBits = 8;
        public const StopBits Serial_StopBits = StopBits.One;

        public BlueRatSerial() { _serialPort = new SerialPort(); _serialPort.BaudRate = Serial_BaudRate; _serialPort.Parity = Serial_Parity; _serialPort.DataBits = Serial_DataBits; _serialPort.StopBits = Serial_StopBits; }
        public BlueRatSerial(string com_port) { _serialPort = new SerialPort(com_port, Serial_BaudRate, Serial_Parity, Serial_DataBits, Serial_StopBits); }
        public string GetPortName() { return _serialPort.PortName; }
        public void SetBlueRatVersion(UInt32 fw_ver, UInt32 cmd_ver) { BlueRatFWVersion = fw_ver; BlueRatCMDVersion = cmd_ver; }

        private SerialPort _serialPort;
        private UInt32 BlueRatCMDVersion = 0;
        private UInt32 BlueRatFWVersion = 0;
        //private uint TimeOutTimer;    

        //private object stream;

        public SafeFileHandle ReturnSafeFileHandle()
        {
            object stream = typeof(SerialPort).GetField("internalSerialStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_serialPort);
            SafeFileHandle hCOM = (SafeFileHandle)stream.GetType().GetField("_handle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(stream);
            return hCOM;
        }

        public Boolean Serial_OpenPort()
        {
            Boolean bRet = false;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Encoding = Encoding.UTF8;
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            try
            {
                _serialPort.Open();
                Start_SerialReadThread();
                //_system_IO_exception = false;
                BlueRatSerialDictionary.Add(_serialPort.PortName, this);
                bRet = true;
            }
            catch (Exception ex232)
            {
                Console.WriteLine("Serial_OpenPort Exception at PORT: " + _serialPort.PortName + " - " + ex232);
                bRet = false;
            }
            return bRet;
        }

        public Boolean Serial_OpenPort(string PortName)
        {
            Boolean bRet = false;
            _serialPort.PortName = PortName;
            bRet = Serial_OpenPort();
            return bRet;
        }

        public Boolean Serial_ClosePort()
        {
            Boolean bRet = false;
            BlueRatSerialDictionary.Remove(_serialPort.PortName);
            try
            {
                Stop_SerialReadThread();
                _serialPort.Close();
                bRet = true;
            }
            catch (Exception ex232)
            {
                Console.WriteLine("Serial_ClosePort Exception at PORT: " + _serialPort.PortName + " - " + ex232);
                bRet = false;
            }
            return bRet;
        }

        public Boolean Serial_PortConnection()
        {
            Boolean bRet = false;
            //if ((_serialPort.IsOpen == true) && (readThread.IsAlive))
            if (_serialPort.IsOpen == true)
            {
                bRet = true;
            }
            return bRet;
        }

        //
        // Start of read part
        //

        public Boolean ReadLine_Ready() { return (UART_READ_MSG_QUEUE.Count > 0) ? true : false; }
        public string ReadLine_Result() { return UART_READ_MSG_QUEUE.Dequeue(); }

        //static bool _continue_serial_read_write = false;
        //static uint Get_UART_Input = 0;
        //static Thread readThread = null
        ////Thread readThread = null;
        //private Queue<bool> Wait_UART_Input = new Queue<bool>();
        private bool Wait_Serial_Input = false;
        private Queue<string> Temp_MSG_QUEUE = new Queue<string>();
        private Queue<string> UART_READ_MSG_QUEUE = new Queue<string>();
        public Queue<string> LOG_QUEUE = new Queue<string>();
        //static bool _system_IO_exception = false;

        private void Start_SerialReadThread()
        {
            ////_continue_serial_read_write = true;
            //readThread = new Thread(ReadSerialPortThread);
            //readThread.Start();
            LOG_QUEUE.Clear();
            UART_READ_MSG_QUEUE.Clear();
            Temp_MSG_QUEUE.Clear();
            Wait_Serial_Input = false;
            //Wait_UART_Input.Clear();
        }

        private void Stop_SerialReadThread()
        {
            ////_continue_serial_read_write = false;
            //if (readThread != null)
            //{
            //    if (readThread.IsAlive)
            //    {
            //        readThread.Abort();
            //        readThread.Join();
            //    }
            //}
            //Wait_UART_Input.Clear();
            LOG_QUEUE.Clear();
            UART_READ_MSG_QUEUE.Clear();
            Temp_MSG_QUEUE.Clear();
            Wait_Serial_Input = false;
            //Wait_UART_Input.Clear();
        }

        public void Start_ReadLine()
        {
            //Wait_UART_Input.Enqueue(true);
            Wait_Serial_Input = true;
        }

        public void Abort_ReadLine()
        {
            //Wait_UART_Input.Clear();
            Wait_Serial_Input = false;
        }
        /*
        private void ReadSerialPortThread()
        {
            bool _continue_serial_read_write = true;
            while (_continue_serial_read_write)
            {
                try
                {
                    if (_serialPort.IsOpen == true)
                    {
                        if (_serialPort.BytesToRead > 0)
                        {
                            string message = _serialPort.ReadLine();
                            LOG_QUEUE.Enqueue(message);
                            {
                                if (Wait_UART_Input.Count > 0)// (Get_UART_Input > 0)
                                {
                                    //Get_UART_Input--;
                                    Wait_UART_Input.Dequeue();
                                    UART_READ_MSG_QUEUE.Enqueue(message);
                                }
                            }
                            _serialPort.ReadTimeout = 500;
                        }
                    }
                    else
                    {
                        _continue_serial_read_write = false;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.TargetSite.Name == "WinIOError")
                    {
                        //_system_IO_exception = true;
                        //_continue_serial_read_write = false;
                        _serialPort.Close();
                        OnUARTException(EventArgs.Empty);
                    }
                    Console.WriteLine("ReadSerialPortThread - " + ex);
                    //_continue_serial_read_write = false;
                }
            }
        }
        */

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            // Find out which serial port --> which bluerat
            SerialPort sp = (SerialPort)sender;
            Object bluerat_obj;
            BlueRatSerialDictionary.TryGetValue(sp.PortName, out bluerat_obj);
            BlueRatSerial bluerat = (BlueRatSerial)bluerat_obj;

            // Read in available char data and concatenate with previous remaining string;
            string proc_str="", In_Str ="";
            //Console.Write(In_Str);
            while (bluerat.Temp_MSG_QUEUE.Count>0)
            {
                proc_str += bluerat.Temp_MSG_QUEUE.Dequeue();
            }
            /*
           while (sp.BytesToRead>0)
           {
               In_Str += Convert.ToChar(sp.ReadChar());
           }
           */
            int len = sp.BytesToRead;
            if (len > 0)
            {
                byte [] input_byte = new byte[len];
                sp.Read(input_byte, 0, len);
                foreach (var in_byte in input_byte)
                {
                    In_Str += Convert.ToChar(in_byte);
                }
                proc_str += In_Str;
            }

            // Decompose each line message and store to message queue
            int NewLinePos = proc_str.IndexOf(sp.NewLine);
            while (NewLinePos>=0)
            {
                // Capture the string before NewLine char
                string new_line_str;
                if (NewLinePos > 0)
                {
                    new_line_str = proc_str.Substring(0, NewLinePos);
                }
                else
                {
                    new_line_str = "";
                }

                // Debug Log
                //bluerat.LOG_QUEUE.Enqueue(new_line_str);
                //Console.WriteLine(new_line_str); // Debug Purpose
                                                 // End of Debug Log

                // Store thte string without the new line
                if (bluerat.BlueRatFWVersion < 102)
                {
                    if ((string.Compare(new_line_str,"+")!=0) && (string.Compare(new_line_str, "S") != 0)) // This check is for BlueRat v1.00 and before -- skip debug return character.
                    {
                        //if (bluerat.Wait_UART_Input.Count > 0)
                        if (bluerat.Wait_Serial_Input)
                        {
                            //bluerat.Wait_UART_Input.Dequeue();
                            bluerat.Wait_Serial_Input = false;
                            bluerat.UART_READ_MSG_QUEUE.Enqueue(new_line_str);
                        }
                    }
                }
                else
                {
                    //if (bluerat.Wait_UART_Input.Count > 0)
                    if (bluerat.Wait_Serial_Input)
                    {
                        //bluerat.Wait_UART_Input.Dequeue();
                        bluerat.Wait_Serial_Input = false;
                        bluerat.UART_READ_MSG_QUEUE.Enqueue(new_line_str);
                    }
                }
                // get the remaing string for next processing
                if (proc_str.Length > (NewLinePos + 1))
                {
                    proc_str = proc_str.Substring(NewLinePos + 1);
                }
                else
                {
                    proc_str = "";
                }
                NewLinePos = proc_str.IndexOf(sp.NewLine);
            }
            // Store remaining string without a NewLine back to queue
            if (proc_str.Length>0)
            {
                bluerat.Temp_MSG_QUEUE.Enqueue(proc_str);
            }
            //string indata = sp.ReadExisting();
            //Console.WriteLine("Data Received:");
            //Console.Write(indata);
        }
        //
        // End of read part
        //

        public bool BlueRatSendToSerial(byte[] byte_to_sent)
        {
            bool return_value = false;

            if (_serialPort.IsOpen == true)
            {
                //Application.DoEvents();
                try
                {
                    int temp_index = 0;
                    const int fixed_length = 16;

                    while ((temp_index < byte_to_sent.Length) && (_serialPort.IsOpen == true))
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
                    Console.WriteLine("BlueRatSendToSerial - " + ex);
                    return_value = false;
                }
            }
            else
            {
                Console.WriteLine("COM is closed and cannot send byte data\n");
                return_value = false;
            }
            return return_value;
        }

        //
        // To process UART IO Exception
        //
        protected virtual void OnUARTException(EventArgs e)
        {
            EventHandler handler = UARTException;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler UARTException;
    }
}
