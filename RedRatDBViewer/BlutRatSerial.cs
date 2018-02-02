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
        //private SafeFileHandle hCOM;
        //private uint TimeOutTimer;    

        //private object stream;

        // This function is only inteneded for PL2303 GPIO. Other Serial port hardware shouldn't use it (or should have its own version)
        public SafeFileHandle GetMySafeFileHandle_PL2303()
        {
            object stream = typeof(SerialPort).GetField("internalSerialStream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_serialPort);
            SafeFileHandle hCOM = (SafeFileHandle)stream.GetType().GetField("_handle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(stream);
            return hCOM;
        }
        // End of special function (not Serial-function but need to use serial port information)

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

        enum ENUM_RX_BUFFER_CHAR_STATUS
        {
            EMPTY_BUFFER = 0,
            CHAR_RECEIVED,
            DISCARD_CHAR_RECEIVED,
            MAX_STATUS_NO
        };


        public Boolean ReadLine_Ready() { return (UART_READ_MSG_QUEUE.Count > 0) ? true : false; }
        public string ReadLine_Result() { return UART_READ_MSG_QUEUE.Dequeue(); }

        //static bool _continue_serial_read_write = false;
        //static uint Get_UART_Input = 0;
        //static Thread readThread = null
        ////Thread readThread = null;
        //private Queue<bool> Wait_UART_Input = new Queue<bool>();
        private bool Wait_Serial_Input = false;
        //private Queue<string> Temp_MSG_QUEUE = new Queue<string>();
        private Queue<char> Rx_char_buffer_QUEUE = new Queue<char>();
        private ENUM_RX_BUFFER_CHAR_STATUS RX_Proc_Status = ENUM_RX_BUFFER_CHAR_STATUS.EMPTY_BUFFER;
        private Queue<string> UART_READ_MSG_QUEUE = new Queue<string>();
        public Queue<string> LOG_QUEUE = new Queue<string>();
        //static bool _system_IO_exception = false;

        private void Start_SerialReadThread()
        {
            LOG_QUEUE.Clear();
            UART_READ_MSG_QUEUE.Clear();
            //Temp_MSG_QUEUE.Clear();
            Rx_char_buffer_QUEUE.Clear();
            RX_Proc_Status = ENUM_RX_BUFFER_CHAR_STATUS.EMPTY_BUFFER;
            Wait_Serial_Input = false;
        }

        private void Stop_SerialReadThread()
        {
            LOG_QUEUE.Clear();
            UART_READ_MSG_QUEUE.Clear();
            //Temp_MSG_QUEUE.Clear();
            Rx_char_buffer_QUEUE.Clear();
            RX_Proc_Status = ENUM_RX_BUFFER_CHAR_STATUS.EMPTY_BUFFER;
            Wait_Serial_Input = false;
        }

        public void Start_ReadLine()
        {
            Wait_Serial_Input = true;
        }

        public void Abort_ReadLine()
        {
            Wait_Serial_Input = false;
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            // Find out which serial port --> which bluerat
            SerialPort sp = (SerialPort)sender;
            Object bluerat_serial_obj;
            BlueRatSerialDictionary.TryGetValue(sp.PortName, out bluerat_serial_obj);
            BlueRatSerial bluerat = (BlueRatSerial)bluerat_serial_obj;
            //Rx_char_buffer_QUEUE
            int buf_len = sp.BytesToRead;
            if (buf_len > 0)
            {
                // Read in all char
                char []input_buf = new char[buf_len];
                sp.Read(input_buf, 0, buf_len);

                if (bluerat.BlueRatFWVersion < 102)
                {
                    // processing according to state
                    int ch_index = 0;
                    while (ch_index < buf_len)
                    {
                        char ch = input_buf[ch_index];
                        switch (bluerat.RX_Proc_Status)
                        {
                            case ENUM_RX_BUFFER_CHAR_STATUS.EMPTY_BUFFER:
                                if ((ch == '+')||(ch == '?'))
                                {
                                    bluerat.RX_Proc_Status = ENUM_RX_BUFFER_CHAR_STATUS.DISCARD_CHAR_RECEIVED;
                                }
                                else
                                {
                                    bluerat.Rx_char_buffer_QUEUE.Enqueue(ch);
                                    bluerat.RX_Proc_Status = ENUM_RX_BUFFER_CHAR_STATUS.CHAR_RECEIVED;
                                }
                                break;
                            case ENUM_RX_BUFFER_CHAR_STATUS.DISCARD_CHAR_RECEIVED:
                                if (ch == '\n')
                                {
                                    bluerat.RX_Proc_Status = ENUM_RX_BUFFER_CHAR_STATUS.EMPTY_BUFFER;
                                }
                                break;
                            case ENUM_RX_BUFFER_CHAR_STATUS.CHAR_RECEIVED:
                                if (ch == '\n')
                                {
                                    if (bluerat.Wait_Serial_Input)
                                    {
                                        char[] temp_char_array = new char[bluerat.Rx_char_buffer_QUEUE.Count];
                                        bluerat.Rx_char_buffer_QUEUE.CopyTo(temp_char_array, 0);
                                        bluerat.Rx_char_buffer_QUEUE.Clear();
                                        string temp_str = new string(temp_char_array);
                                        if (string.Compare(temp_str, "S") != 0)
                                        {
                                            bluerat.UART_READ_MSG_QUEUE.Enqueue(temp_str);
                                        }
                                        bluerat.Wait_Serial_Input = false;
                                    }
                                    else
                                    {
                                        bluerat.Rx_char_buffer_QUEUE.Clear();
                                    }
                                    bluerat.RX_Proc_Status = ENUM_RX_BUFFER_CHAR_STATUS.EMPTY_BUFFER;
                                }
                                else if (!((ch == '+')|| (ch == 'S')|| (ch == '?')))        // NOTE: only skip 'S' when it has some char before this 'S'
                                {
                                    bluerat.Rx_char_buffer_QUEUE.Enqueue(ch);
                                }
                                break;
                            default:
                                break;
                        }
                        ch_index++;
                    }
                }
                else
                {
                    // After v1.02
                    int ch_index = 0;
                    while (ch_index < buf_len)
                    {
                        char ch = input_buf[ch_index];
                        if (ch == '\n')
                        {
                            if (bluerat.Wait_Serial_Input)
                            {
                                char[] temp_char_array = new char[bluerat.Rx_char_buffer_QUEUE.Count];
                                bluerat.Rx_char_buffer_QUEUE.CopyTo(temp_char_array, 0);
                                bluerat.Rx_char_buffer_QUEUE.Clear();
                                string temp_str = new string(temp_char_array);
                                bluerat.UART_READ_MSG_QUEUE.Enqueue(temp_str);
                                bluerat.Wait_Serial_Input = false;
                            }
                            else
                            {
                                bluerat.Rx_char_buffer_QUEUE.Clear();
                            }
                        }
                        else
                        {
                            bluerat.Rx_char_buffer_QUEUE.Enqueue(ch);
                        }
                        ch_index++;
                    }
                }
            }
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
