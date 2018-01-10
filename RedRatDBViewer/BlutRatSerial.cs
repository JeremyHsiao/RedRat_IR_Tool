using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;

namespace RedRatDatabaseViewer
{
    class BlueRatSerial
    {
        //
        // Add UART Part
        //
        public const int Serial_BaudRate = 115200;
        public const Parity Serial_Parity = Parity.None;
        public const int Serial_DataBits = 8;
        public const StopBits Serial_StopBits = StopBits.One;
        private SerialPort _serialPort;

        public BlueRatSerial() { _serialPort = new SerialPort(); _serialPort.BaudRate = Serial_BaudRate; _serialPort.Parity = Serial_Parity; _serialPort.DataBits = Serial_DataBits; _serialPort.StopBits = Serial_StopBits; }
        public BlueRatSerial(string com_port) { _serialPort = new SerialPort(com_port, Serial_BaudRate, Serial_Parity, Serial_DataBits, Serial_StopBits); }
        public string GetPortName() { return _serialPort.PortName; }


        public Boolean Serial_OpenPort()
        {
            Boolean bRet = false;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Encoding = Encoding.UTF8;
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            try
            {
                _serialPort.Open();
                Start_SerialReadThread();
                //_system_IO_exception = false;
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
            if ((_serialPort.IsOpen == true) && (readThread.IsAlive))
            {
                bRet = true;
            }
            return bRet;
        }

        public Boolean ReadLine_Ready() { return (UART_READ_MSG_QUEUE.Count > 0) ? true : false; }
        public string ReadLine_Result() { return UART_READ_MSG_QUEUE.Dequeue(); }

        //static bool _continue_serial_read_write = false;
        //static uint Get_UART_Input = 0;
        //static Thread readThread = null
        Thread readThread = null;
        private Queue<bool> Wait_UART_Input = new Queue<bool>();
        private Queue<string> Temp_MSG_QUEUE = new Queue<string>();
        private Queue<string> UART_READ_MSG_QUEUE = new Queue<string>();
        public Queue<string> LOG_QUEUE = new Queue<string>();
        //static bool _system_IO_exception = false;

        private void Start_SerialReadThread()
        {
            //_continue_serial_read_write = true;
            readThread = new Thread(ReadSerialPortThread);
            readThread.Start();
        }
        private void Stop_SerialReadThread()
        {
            //_continue_serial_read_write = false;
            if (readThread != null)
            {
                if (readThread.IsAlive)
                {
                    readThread.Abort();
                    readThread.Join();
                }
            }
        }

        public void Start_ReadLine()
        {
            Wait_UART_Input.Enqueue(true);
        }

        /*
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            Console.WriteLine("Data Received:");
            Console.Write(indata);
        }
        */

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
                                //AppendSerialMessageLog(message);
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
                    //AppendSerialMessageLog(ex.ToString());
                    //_continue_serial_read_write = false;
                }
            }
        }

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
