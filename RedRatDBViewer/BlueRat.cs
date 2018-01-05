using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace RedRatDatabaseViewer
{
    class BlueRat
    {
        // Static private variable
        private static int BlueRatInstanceNumber = 0;
        private static List<string> BlueRatCOMPortString = new List<string>();

        //
        // Function for external use
        //
        public BlueRat() { Serial_InitialSetting(); BlueRatInstanceNumber++; }
        ~BlueRat() { Serial_ClosePort(); BlueRatInstanceNumber--; }

        /*
        static public bool CheckBlueRatExisting(string com_port)
        {
            bool ret = false;
            if (_serialPort.IsOpen == true)
            {
                if (this.Test_If_System_Can_Say_HI() == true)
                {
                    SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SAY_HI)).ToArray());
                    HomeMade_Delay(5);
                    if (UART_READ_MSG_QUEUE.Count > 0)
                    {
                        String in_str = UART_READ_MSG_QUEUE.Dequeue();
                        if (in_str.Contains(_CMD_SAY_HI_RETURN_HEADER_))
                        {
                            ret = true;
                        }
                        else
                        {
                            Console.WriteLine("BlueRat no resonse to HI Command");
                        }
                    }
                }
                else
                {
                }

            }
            else
            {
                if (Serial_OpenPort(com_name) == true)
                {
                    ret = Connect_BlueRat_Protocol();
                }
                else
                {
                    Console.WriteLine("Cannot open serial port:" + com_name);
                }
            }
            if (ret == false)
            {
                Serial_ClosePort();
            }
            return ret;
        }
        */
        private UInt32 BlueRatCMDVersion = 0;
        private string MyBlueRatCOMPort = "";
/*
        public string GetCOMPort(string com_port) { return MyBlueRatCOMPort; }
        public bool SetCOMPort(string com_port)
        {
            bool bRet = false;

            if(com_port!="")
            {
                if(_serialPort.PortName!=com_port)
                {
                    if (Serial_OpenPort(com_port) == true)
                    {
                        MyBlueRatCOMPort = com_port;
                        Serial_ClosePort();
                        bRet = true;
                    }
                }
                else
                {
                    bRet = true;
                }
            }
            return bRet;
        }
*/

        public UInt32 GetCommandVersion()
        {
            if(BlueRatCMDVersion==0)
            {
                if (this.CheckConnection() == true)
                {
                    this.Get_Command_Version();
                }
                else
                {
                    Console.WriteLine("Need to setup connection to BlueRat to get CMD_VER");
                }
            }
            return BlueRatCMDVersion;
        }

        private bool Connect_BlueRat_Protocol()
        {
            bool ret = false;
            if (this.CheckConnection() == true)
            {
                ret = true;
            }
            else
            {
                HomeMade_Delay(10);
                this.Force_Init_BlueRat();
                HomeMade_Delay(10);
                if (this.CheckConnection() == true)
                {
                    this.Get_Command_Version();
                    ret = true;
                }
            }
            return ret;
        }

        public bool Connect(string com_name)
        {
            bool ret = false;
            if (this.SerialPortConnection()==true)
            {
                ret = Connect_BlueRat_Protocol();
            }
            else
            {
                if (Serial_OpenPort(com_name) == true)
                {
                    ret = Connect_BlueRat_Protocol();
                }
                else
                {
                    Console.WriteLine("Cannot open serial port:" + com_name);
                }
            }
            if(ret==false)
            {
                Serial_ClosePort();
            }
            return ret;
        }

        public bool Disconnect()
        {
            bool ret = false;
            if (this.SerialPortConnection() == true)
            {
                Stop_Current_Tx();
                HomeMade_Delay(300);
                Force_Init_BlueRat();
            }
            if (Serial_ClosePort() == true)
            {
                ret = true;
            }
            else
            {
                Console.WriteLine("Cannot close serial port for BlueRat.");
            }
            return ret;
        }

        public bool CheckConnection()
        {
            bool ret = false;
            if (this.SerialPortConnection() == true)
            {
                if (this.Test_If_System_Can_Say_HI() == true)
                {
                    ret = true;
                }
                else
                {
                    Console.WriteLine("BlueRat no resonse to HI Command");
                }
            }
            return ret;
        }

        // 強迫立刻停止信號發射的指令 -- 例如在PC端程式開啟時,可以用來將小藍鼠的狀態設定為預設狀態
        public bool Force_Init_BlueRat()
        {
            bool ret = false;
            ret = SendToSerial_v2(Prepare_FORCE_RESTART_CMD().ToArray());
            HomeMade_TimeOutIndicator = true;
            return ret;
        }

        // 強迫目前這一次信號發射結束後立刻停止(清除repeat count)的指令
        public bool Stop_Current_Tx()
        {
            bool ret = false;
            ret = SendToSerial_v2(Prepare_STOP_CMD().ToArray());
            //HomeMade_TimeOutIndicator = true;
            return ret;
        }

        public bool Add_Repeat_Count(UInt32 add_count)
        {
            bool ret = false;
            ret = SendToSerial_v2(Prepare_Send_Repeat_Cnt_Add_CMD(add_count).ToArray());
            return ret;
        }

        // 讓系統進入等待軟體更新的狀態
        public bool Enter_ISP_Mode()
        {
            bool ret = false;
            ret = SendToSerial_v2(Prepare_Enter_ISP_CMD().ToArray());
            return ret;
        }

        private void HomeMade_Delay(int delay_ms)
        {
            if (delay_ms <= 0) return;
            System.Timers.Timer aTimer = new System.Timers.Timer(delay_ms);
            aTimer.Elapsed += new ElapsedEventHandler(HomeMade_Delay_OnTimedEvent);
            HomeMade_TimeOutIndicator = false;
            aTimer.Enabled = true;
            while ((this.SerialPortConnection() == true)&&(HomeMade_TimeOutIndicator == false)) { Application.DoEvents(); }
            aTimer.Stop();
            aTimer.Dispose();
        }

        public int Get_Remaining_Repeat_Count()
        {
            int repeat_cnt = 0;
            Get_UART_Input = 1;
            if (SendToSerial_v2(Prepare_Get_RC_Repeat_Count().ToArray()))
            {
                HomeMade_Delay(5);
                if (UART_READ_MSG_QUEUE.Count > 0)
                {
                    String in_str = UART_READ_MSG_QUEUE.Dequeue();
                    if (_CMD_GET_TX_CURRENT_REPEAT_COUNT_RETURN_HEADER_ == "")
                    {
                        repeat_cnt = Convert.ToInt32(in_str, 16);
                    }
                    else if (in_str.Contains(_CMD_GET_TX_CURRENT_REPEAT_COUNT_RETURN_HEADER_))
                    {
                        string value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                        repeat_cnt = Convert.ToInt32(value_str, 16);
                    }
                }
                else
                {
                    Console.WriteLine("Check Get_Remaining_Repeat_Count()");
                }
            }
            return repeat_cnt;
        }

        public bool Get_Current_Tx_Status()
        {
            bool ret = false;

            Get_UART_Input = 1;
            if (SendToSerial_v2(Prepare_Get_RC_Current_Running_Status().ToArray()))
            {
                HomeMade_Delay(10);
                if (UART_READ_MSG_QUEUE.Count > 0)
                {
                    String in_str = UART_READ_MSG_QUEUE.Dequeue();
                    if (_CMD_GET_TX_RUNNING_STATUS_HEADER_ == "") 
                    {
                        if (Convert.ToInt32(in_str, 16) != 0)
                        {
                            ret = true;
                        }
                    }
                    else if (in_str.Contains(_CMD_GET_TX_RUNNING_STATUS_HEADER_))
                    { 
                        string value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                        if (Convert.ToInt32(value_str, 16) != 0)
                        {
                            ret = true;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Check Get_Current_Tx_Status()");
                }
            }
            return ret;
        }

        public string Get_SW_Version()
        {
            string value_str = "0";
        
            Get_UART_Input = 1;
            if (SendToSerial_v2(Prepare_Send_Input_CMD_without_Parameter(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_RETURN_SW_VER)).ToArray()))
            {
                HomeMade_Delay(20);
                if (UART_READ_MSG_QUEUE.Count > 0)
                {
                    String in_str = UART_READ_MSG_QUEUE.Dequeue();
                    if (_CMD_RETURN_SW_VER_RETURN_HEADER_ == "")
                    {
                        value_str = in_str;
                    }
                    else if (in_str.Contains(_CMD_RETURN_SW_VER_RETURN_HEADER_))
                    {
                        value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                    }
                }
                else
                {
                    Console.WriteLine("Check Get_SW_Version()");
                }
            }
            return value_str;
        }

        public string Get_BUILD_TIME()
        {
            string value_str = "";

            Get_UART_Input = 1;
            if (SendToSerial_v2(Prepare_Send_Input_CMD_without_Parameter(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_RETURN_BUILD_TIME)).ToArray()))
            {
                HomeMade_Delay(200);
                if (UART_READ_MSG_QUEUE.Count > 0)
                {
                    String in_str = UART_READ_MSG_QUEUE.Dequeue();
                    if (_CMD_BUILD_TIME_RETURN_HEADER_ == "")
                    {
                        value_str = in_str;
                    }
                    else if (in_str.Contains(_CMD_BUILD_TIME_RETURN_HEADER_))
                    {
                        value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                    }
                }
                else
                {
                    Console.WriteLine("Check Get_BUILD_TIME()");
                }
            }
            return value_str;
        }

        public string Get_Command_Version()
        {
            string value_str = "";

            Get_UART_Input = 1;
            if (SendToSerial_v2(Prepare_Send_Input_CMD_without_Parameter(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_RETURN_CMD_VERSION)).ToArray()))
            {
                HomeMade_Delay(20);
                if (UART_READ_MSG_QUEUE.Count > 0)
                {
                    String in_str = UART_READ_MSG_QUEUE.Dequeue();
                    if (_CMD_RETURN_CMD_VERSION_RETURN_HEADER_ == "")
                    {
                        value_str = in_str;
                        BlueRatCMDVersion = Convert.ToUInt32(value_str);
                    }
                    else if (in_str.Contains(_CMD_RETURN_CMD_VERSION_RETURN_HEADER_))
                    {
                        value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                        BlueRatCMDVersion = Convert.ToUInt32(value_str);
                    }
                }
                else
                {
                    Console.WriteLine("Check Get_Command_Version()");
                }
            }
            return value_str;
        }

        public UInt32 Get_GPIO_Input()
        {
            UInt32 GPIO_Read_Data = 0xffffffff;
            Get_UART_Input = 1;
            if (SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_GPIO_INPUT)).ToArray()))
            {
                HomeMade_Delay(5);
                if (UART_READ_MSG_QUEUE.Count > 0)
                {
                    String in_str = UART_READ_MSG_QUEUE.Dequeue();
                    if (_CMD_GPIO_INPUT_RETURN_HEADER_ == "")
                    {
                        GPIO_Read_Data = Convert.ToUInt32(in_str, 16);
                    }
                    else if (in_str.Contains(_CMD_GPIO_INPUT_RETURN_HEADER_))
                    {
                        string value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                        GPIO_Read_Data = Convert.ToUInt32(value_str, 16);
                        Console.WriteLine(GPIO_Read_Data.ToString());           // output to console
                    }
                }
                else
                {
                    Console.WriteLine("Check Get_GPIO_Input()");
                }
            }
            return GPIO_Read_Data;
        }

        public bool Set_GPIO_Output(byte output_value)
        {
            bool ret = false;

            if(SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), output_value).ToArray()))
            {
                ret = true;
            }
            return ret;
        }

        public bool Set_GPIO_Output_SinglePort(byte port_no, byte output_value)
        {
            bool ret = false;

            UInt32 temp_parameter;
            if (output_value != 0) { temp_parameter = 1; } else { temp_parameter = 0; }
            temp_parameter |= Convert.ToUInt32(port_no) << 8;
            if (SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), temp_parameter).ToArray()))
            {
                ret = true;
            }
            return ret;
        }
        // For testing purpose
        public void TEST_WalkThroughAllCMDwithData()
        {
            // Testing: send all CMD with input parameter
            for (byte cmd = Convert.ToByte(CMD_CODE_UPPER_LIMIT); cmd >= Convert.ToByte(CMD_CODE_LOWER_LIMIT); cmd--)
            //byte cmd = 0xdf;
            {
                SendToSerial_v2(Prepare_Send_Input_CMD(cmd, 0x1010101U * cmd).ToArray());
                HomeMade_Delay(32);
            }
        }

        // For testing purpose
        public void TEST_GPIO_Output()
        {
            const int delay_time = 100;
            // Testing: send GPIO output with byte parameter -- Set output port value at once
            for (byte output_value = 0; output_value <= 0xff; output_value++)
            {
                Set_GPIO_Output(output_value);
                HomeMade_Delay(delay_time / 2);
            }

            int run_time = 10;
            const UInt32 IO_value_mask = 0x0, reverse_IO_value_mask = 0x1;

            Set_GPIO_Output(Convert.ToByte((~reverse_IO_value_mask)&0xff));
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

        // For testing purpose
        public void TEST_GPIO_Input()
        {
            const int delay_time = 500;
            UInt32 GPIO_Read_Data = 0;

            // For reading an UART input, please make sure previous return data has been already received

            int run_time = 20;
            while (run_time-- > 0)
            {
                GPIO_Read_Data = Get_GPIO_Input();
                SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), ~GPIO_Read_Data).ToArray());
                HomeMade_Delay(delay_time);
                SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), 0xff).ToArray());
                HomeMade_Delay(delay_time);
            }
        }

        //
        // 跟小藍鼠有關係的程式代碼與範例程式區--結尾
        //

        //
        // Function for external use -- END
        //

        //
        // Add UART Part
        //
        static SerialPort _serialPort = new SerialPort();

        private void Serial_InitialSetting()
        {
            // Allow the user to set the appropriate properties.
            //_serialPort.PortName = "COM14";
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

        private Boolean Serial_OpenPort(string PortName)
        {
            Boolean ret = false;
            try
            {
                _serialPort.PortName = PortName;
                _serialPort.Open();
                Start_SerialReadThread();
                _system_IO_exception = false;
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
                Stop_SerialReadThread();
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

        private Boolean SerialPortConnection()
        {
            Boolean ret = false;
            if (_serialPort.IsOpen == true)
            {
                ret = true;
            }
            return ret;
        }

        static bool _continue_serial_read_write = false;
        static uint Get_UART_Input = 0;
        static Thread readThread = null;
        private Queue<string> UART_READ_MSG_QUEUE = new Queue<string>();
        public Queue<string> BlueRat_LOG_QUEUE = new Queue<string>();
        static bool _system_IO_exception = false;

        class BlueRatReadThreadClass
        {
            public double Base;
            public double Height;
            public double Area;
            public void CalcArea()
            {
                Area = 0.5 * Base * Height;
                MessageBox.Show("The area is: " + Area.ToString());
            }
        }

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
                            string message = _serialPort.ReadLine();
                            BlueRat_LOG_QUEUE.Enqueue(message);
                            {
                                if (Get_UART_Input > 0)
                                {
                                    Get_UART_Input--;
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
                    if (ex.TargetSite.Name=="WinIOError")
                    {
                        _system_IO_exception = true;
                        _continue_serial_read_write = false;
                        _serialPort.Close();
                        OnUARTException(EventArgs.Empty);
                    }
                    Console.WriteLine("ReadSerialPortThread - " + ex);
                    //AppendSerialMessageLog(ex.ToString());
                    //_continue_serial_read_write = false;
                }
            }
        }
/*
        private void SendToSerial(byte[] byte_to_sent)
        {
            if (_serialPort.IsOpen == true)
            {
                //AppendSerialMessageLog("Start Tx\n");
                Application.DoEvents();
                try
                {
                    // _serialPort.Write("This is a Test\n");
                    _serialPort.Write(byte_to_sent, 0, byte_to_sent.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SendToSerial - " + ex);
                    //AppendSerialMessageLog(ex.ToString());
                }
            }
            else
            {
                //AppendSerialMessageLog("COM is closed and cannot send byte data\n");
            }
        }
*/
        public static bool BlueRatSendToSerial(SerialPort _serialPort, byte[] byte_to_sent)
        {
            bool return_value = false;

            if (_serialPort.IsOpen == true)
            {
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

        private int Tx_CNT = 0;
        private bool SendToSerial_v2(byte[] byte_to_sent)
        {
            bool return_value = false;

            return_value = BlueRatSendToSerial(_serialPort, byte_to_sent);
            // Console.WriteLine("\n===Tx:" + Tx_CNT.ToString() + " ");
            // Tx_CNT++;
/*
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
            //AppendSerialMessageLog("\n===Tx:" + Tx_CNT.ToString() + " ");
            */
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
            ENUM_CMD_FORCE_RESTART = 0x9e,
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
        const uint RESTART_PASSWORD = (0x46535050);

        const string _CMD_SAY_HI_RETURN_HEADER_ = "HI";
        //const string _CMD_RETURN_SW_VER_RETURN_HEADER_ = "SW:"; 
        const string _CMD_RETURN_SW_VER_RETURN_HEADER_ = ""; // for compatibility of firmware: BlueRat - 20171221_001.bin -- please update it whenever we have chance to upgrade firmware.
        //const string _CMD_BUILD_TIME_RETURN_HEADER_ = "AT";      
        const string _CMD_BUILD_TIME_RETURN_HEADER_ = "";       //  please update it whenever we have chance to upgrade firmware.
        //const string _CMD_RETURN_CMD_VERSION_RETURN_HEADER_ = "CMD_VER:"; 
        const string _CMD_RETURN_CMD_VERSION_RETURN_HEADER_ = ""; // for compatibility of firmware: BlueRat - 20171221_001.bin -- please update it whenever we have chance to upgrade firmware.
        //        const string _CMD_GET_TX_RUNNING_STATUS_HEADER_ = "TX:";
        const string _CMD_GET_TX_RUNNING_STATUS_HEADER_ = ""; // for compatibility of firmware: BlueRat - 20171221_001.bin -- please update it whenever we have chance to upgrade firmware.
        //const string _CMD_GET_TX_CURRENT_REPEAT_COUNT_RETURN_HEADER_ = "CNT:";
        const string _CMD_GET_TX_CURRENT_REPEAT_COUNT_RETURN_HEADER_ = ""; // for compatibility of firmware: BlueRat - 20171221_001.bin -- please update it whenever we have chance to upgrade firmware.
        //const string _CMD_GPIO_INPUT_RETURN_HEADER_ = "IN:"; 
        const string _CMD_GPIO_INPUT_RETURN_HEADER_ = "";       //  please update it whenever we have chance to upgrade firmware.
        //const string _CMD_SENSOR_INPUT_RETURN_HEADER_ = "SS:";
        const string _CMD_SENSOR_INPUT_RETURN_HEADER_ = "";  //  please update it whenever we have chance to upgrade firmware.

        //
        // Input parameter is 32-bit unsigned data
        //
        static private List<byte> Convert_data_to_Byte(UInt32 input_data)
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
        static private List<byte> Convert_data_to_Byte(UInt16 input_data)
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
        static private List<byte> Convert_data_to_Byte(byte input_data)
        {
            List<byte> data_to_sent = new List<byte>();
            data_to_sent.Add(input_data);
            return data_to_sent;
        }

        //
        // This is dedicated for witdh-data of IR signal
        //
        static private List<byte> Convert_data_to_Byte_modified(uint width_value)
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
        private void ClearCheckSum()
        {
            CheckSum = 0;
        }

        private void UpdateCheckSum(byte value)
        {
            CheckSum ^= value;
        }

        private byte GetCheckSum()
        {
            return CheckSum;
        }

        private bool CompareCheckSum()
        {
            return (CheckSum == 0) ? true : false;
        }

        //
        // To get UART data byte for each command
        //
        private List<byte> Prepare_STOP_CMD()
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

        private List<byte> Prepare_FORCE_RESTART_CMD()
        {
            List<byte> data_to_sent = new List<byte>();

            ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_FORCE_RESTART));
            UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_FORCE_RESTART));
            List<byte> input_param_in_byte = Convert_data_to_Byte(RESTART_PASSWORD);
            foreach (byte temp in input_param_in_byte)
            {
                data_to_sent.Add(temp);
                UpdateCheckSum(temp);
            }
            data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

        private List<byte> Prepare_Say_HI_CMD()
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

        private List<byte> Prepare_Get_RC_Repeat_Count()
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

        private List<byte> Prepare_Get_RC_Current_Running_Status()
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

        private List<byte> Prepare_Enter_ISP_CMD()
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

        private List<byte> Prepare_Send_Repeat_Cnt_Add_CMD(UInt32 cnt = 0)
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

        private List<byte> Prepare_Send_Input_CMD_without_Parameter(byte input_cmd)
        {
            List<byte> data_to_sent = new List<byte>();

            ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(input_cmd);
            UpdateCheckSum(input_cmd);
            data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

        private List<byte> Prepare_Send_Input_CMD(byte input_cmd, UInt32 input_param = 0)
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

        public int SendOneRC(RedRatDBParser RedRatData, byte default_repeat_cnt = 0)
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

            if ((RedRatData == null)||(this.SerialPortConnection()==false))
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

        // 小藍鼠專用的delay的內部資料與function
        static bool HomeMade_TimeOutIndicator = false;
        private static void HomeMade_Delay_OnTimedEvent(object source, ElapsedEventArgs e)
        {
            HomeMade_TimeOutIndicator = true;
        }

        // 單純回應"HI"的指令,可用來試試看系統是否還有在接受指令
        private Boolean Test_If_System_Can_Say_HI()
        {
            Boolean ret = false;
            Get_UART_Input = 1;
            SendToSerial_v2(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SAY_HI)).ToArray());
            HomeMade_Delay(5);
            if (UART_READ_MSG_QUEUE.Count > 0)
            {
                String in_str = UART_READ_MSG_QUEUE.Dequeue();
                if (in_str.Contains(_CMD_SAY_HI_RETURN_HEADER_))
                {
                    ret = true;
                }
                else
                {
                    Console.WriteLine("BlueRat no resonse to HI Command");
                }
            }
            return ret;
        }

        //
        ///
        ///
    }
}