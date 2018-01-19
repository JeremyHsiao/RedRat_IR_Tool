using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
using System.Management;
using System.Linq;

namespace RedRatDatabaseViewer
{
    class BlueRatCheckSum
    {
        //
        // Checksum is currently XOR all data (excluding sync header)
        //
        private Byte CheckSum;

        public BlueRatCheckSum() { CheckSum = 0; }
        public void ClearCheckSum()
        {
            CheckSum = 0;
        }

        public void UpdateCheckSum(byte value)
        {
            CheckSum ^= value;
        }

        public byte GetCheckSum()
        {
            return CheckSum;
        }

        public bool CompareCheckSum()
        {
            return (CheckSum == 0) ? true : false;
        }
    }

    class BlueRat
    {
        // Static private variable
        private static int BlueRatInstanceNumber = 0;
        //private static List<string> BlueRatCOMPortString = new List<string>();

        // Private member variable
        private UInt32 BlueRatCMDVersion = 0;
        private UInt32 BlueRatFWVersion = 0;
        private string BlueRatBuildTime = "Unknown";
        //private string BlueRatCOMPortName;
        private BlueRatSerial MyBlueRatSerial;

        // Public member variables
        public UInt32 FW_VER { get { return BlueRatFWVersion; } }
        public UInt32 CMD_VER { get { return BlueRatCMDVersion; } }
        public string BUILD_TIME { get { return BlueRatBuildTime; } }

        // This function is currently only inteneded for PL2303 GPIO. Other Serial port shouldn't use it (or should have its own version)
        public SafeFileHandle ReturnSafeFileHandle() => MyBlueRatSerial.GetMySafeFileHandle_PL2303();
        public SafeFileHandle ReturnSafeFileHandle_PL2303() => MyBlueRatSerial.GetMySafeFileHandle_PL2303();  // For PL2303-GPIO

        //
        // Function for external use
        //
        public BlueRat() { MyBlueRatSerial = new BlueRatSerial(); BlueRatInstanceNumber++; }
        //public BlueRat(string com_port) { Connect(com_port); BlueRatInstanceNumber++; }
        ~BlueRat() { Disconnect(); MyBlueRatSerial = null; BlueRatInstanceNumber--; }

        public bool Connect(string com_name)
        {
            bool bRet = false;
            if (MyBlueRatSerial.Serial_PortConnection() == true)
            {
                bRet = Connect_BlueRat_Protocol();
            }
            else
            {
                if (MyBlueRatSerial.Serial_OpenPort(com_name) == true)
                {
                    bRet = Connect_BlueRat_Protocol();
                }
                else
                {
                    Console.WriteLine("Cannot open serial port:" + com_name);
                }
            }
            if (bRet == true)
            {
                if (BlueRatFWVersion == 0)
                {
                    int retry_cnt;
                    string fw_ver_str;
                    retry_cnt = 3;
                    while ((Get_SW_Version(out fw_ver_str)==false) && (--retry_cnt > 0));
                    BlueRatFWVersion = Convert.ToUInt32(Convert.ToDouble(fw_ver_str) * 100);
                    Update_Header_String_by_SW_Version(this, BlueRatFWVersion);

                    retry_cnt = 3;
                    string cmd_ver_str;
                    while ((Get_Command_Version(out cmd_ver_str) == false) && (--retry_cnt > 0)) ;
                    BlueRatCMDVersion = Convert.ToUInt32(cmd_ver_str);

                    retry_cnt = 3;
                    while ((Get_SW_Build_Time(out BlueRatBuildTime) == false) && (--retry_cnt > 0)) ;
                }
                MyBlueRatSerial.SetBlueRatVersion(BlueRatFWVersion, BlueRatCMDVersion);     // Tell BlueRatSerial about version info for workaround at serial read function.
            }
            else
            {
                MyBlueRatSerial.Serial_ClosePort();
            }
            return bRet;
        }

        public bool Disconnect()
        {
            bool bRet = false;
            //string com_port = MyBlueRatSerial.GetPortName();

            Stop_MyOwn_HomeMade_Delay();
            if (MyBlueRatSerial.Serial_PortConnection() == true)
            {
                MyBlueRatSerial.Abort_ReadLine();
                Stop_Current_Tx();
                HomeMade_Delay(300);
                Force_Init_BlueRat();
            }
            if (MyBlueRatSerial.Serial_ClosePort() == true)
            {
                bRet = true;
            }
            else
            {
                Console.WriteLine("Cannot close serial port for BlueRat.");
            }
            // BlueRatCOMPortString.Remove(com_port);
            // BlueRatCOMPortName = "";
            BlueRatFWVersion = 0;
            BlueRatCMDVersion = 0;
            BlueRatBuildTime = "";
            return bRet;
        }

        public bool CheckConnection()
        {
            bool bRet = false;
            if (MyBlueRatSerial.Serial_PortConnection() == true)
            {
                if (Test_If_System_Can_Say_HI() == true)
                {
                    bRet = true;
                }
                else
                {
                    Console.WriteLine("BlueRat no resonse to HI Command");
                }
            }
            return bRet;
        }

        // 強迫立刻停止信號發射的指令 -- 例如在PC端程式開啟時,可以用來將小藍鼠的狀態設定為預設狀態
        public bool Force_Init_BlueRat()
        {
            bool bRet = false;
            bRet = MyBlueRatSerial.BlueRatSendToSerial(Prepare_FORCE_RESTART_CMD().ToArray());
            HomeMade_Delay(20);
            //HomeMade_TimeOutIndicator = true;
            return bRet;
        }

        // 強迫目前這一次信號發射結束後立刻停止(清除repeat count)的指令
        public bool Stop_Current_Tx()
        {
            bool bRet = false;
            bRet = MyBlueRatSerial.BlueRatSendToSerial(Prepare_STOP_CMD().ToArray());
            //HomeMade_TimeOutIndicator = true;
            return bRet;
        }

        public bool Add_Repeat_Count(UInt32 add_count)
        {
            bool bRet = false;
            bRet = MyBlueRatSerial.BlueRatSendToSerial(Prepare_Send_Repeat_Cnt_Add_CMD(add_count).ToArray());
            return bRet;
        }

        // 讓系統進入等待軟體更新的狀態
        public bool Enter_ISP_Mode()
        {
            bool bRet = false;
            bRet = MyBlueRatSerial.BlueRatSendToSerial(Prepare_Enter_ISP_CMD().ToArray());
            return bRet;
        }

        private ENUM_RETRY_RESULT SendCmd_WaitReadLine(List<byte> cmd_list, out string result_string, int timeout_time = 16)
        {
            ENUM_RETRY_RESULT result_status;
            result_string = "";

            MyBlueRatSerial.Start_ReadLine();
            if (MyBlueRatSerial.BlueRatSendToSerial(cmd_list.ToArray()) == false)
            {
                result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_SEND_COMMAND;
            }
            else
            {
                result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_READLINE;       // readline error will be cleared after line is read.
                int MyRetryTimes = Convert.ToInt32(ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1) - 1;
                int each_delay_time = (timeout_time / Convert.ToInt32(ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1));
                if (each_delay_time < 1) each_delay_time = 1;
                do
                {
                    HomeMade_Delay(each_delay_time);
                    if (MyBlueRatSerial.ReadLine_Ready() == true)
                    {
                        result_string = MyBlueRatSerial.ReadLine_Result();
                        result_status = (ENUM_RETRY_RESULT)MyRetryTimes;
                        break;     // force to exit
                    }
                }
                while (MyRetryTimes-- > 0);
            }
            return result_status;
        }

        public bool Get_Remaining_Repeat_Count(out int repeat_cnt)
        {
            bool bRet = false;
            
            int default_timeout_time = 30;
            string in_str;
            ENUM_RETRY_RESULT result_status;

            if (BlueRatFWVersion < 102)     // This bug is identified and fixed on v1.02
            {
                default_timeout_time = 640;
            }

            repeat_cnt = 0;
            result_status = SendCmd_WaitReadLine((Prepare_Get_RC_Repeat_Count()), out in_str, default_timeout_time);
            if (result_status < ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                if (BlueRatFWVersion < 102)     // This bug is identified and fixed on v1.02
                {

                }
                else if (in_str.Contains(_CMD_GET_TX_CURRENT_REPEAT_COUNT_RETURN_HEADER_))
                {
                    in_str = in_str.Substring(in_str.IndexOf(":") + 1);
                }
                else
                {
                    result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_COMPARE;
                }
            }

            if (result_status < ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                try
                {
                    Int32 temp = Convert.ToInt32(in_str, 16);      // only for testing conversion.
                    repeat_cnt = temp;
                    bRet = true;
                }
                catch (System.FormatException)
                {
                    result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_COMPARE;
                }
            }

            // Debug purpose
            if (result_status >= ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                Console.WriteLine("Get_Remaining_Repeat_Count Error: " + result_status.ToString());
            }

            return bRet;
        }

        public bool Get_Current_Tx_Status(out bool return_tx_status)
        {
            bool bRet = false;
            int default_timeout_time = 16;
            string in_str;
            ENUM_RETRY_RESULT result_status;

            if (BlueRatFWVersion < 102)     // This bug is identified and fixed on v1.02
            {
                default_timeout_time = 280;
            }

            return_tx_status = false;
            result_status = SendCmd_WaitReadLine((Prepare_Get_RC_Current_Running_Status()), out in_str, default_timeout_time);
            if (result_status < ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                if (BlueRatFWVersion < 102)     // This bug is identified and fixed on v1.02
                {
                    if(in_str.Contains("1"))
                    {
                        return_tx_status = true;
                        bRet = true;
                    }
                }
                else if (in_str.Contains(_CMD_GET_TX_RUNNING_STATUS_HEADER_))
                {
                    string value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                    try
                    {
                        Int32 temp = Convert.ToInt32(value_str);      // only for testing conversion.
                        if (temp != 0)
                        {
                            return_tx_status = true;
                        }
                        else
                        {
                            return_tx_status = false;
                        }
                        bRet = true;
                    }
                    catch (System.FormatException)
                    {
                        result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_COMPARE;
                    }
                }
            }

            // Debug purpose
            if (result_status >= ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                Console.WriteLine("Get_Current_Tx_Status Error: " + result_status.ToString());
            }

            return bRet;
        }

        private bool Get_SW_Version(out string result_string)
        {
            bool bRet = false;
            const int default_timeout_time = 16;
            string value_str = "0.01", in_str;
            ENUM_RETRY_RESULT result_status;

            result_string = "0";
            result_status = SendCmd_WaitReadLine(Prepare_Send_Input_CMD_without_Parameter(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_RETURN_SW_VER)), out in_str, default_timeout_time);
            if (result_status < ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                if (in_str.Contains(_SW_VER_STRING_VER_01))         // Check latest version first -- currently 2 version
                {
                    value_str = in_str.Substring(in_str.IndexOf(":") + 1);
                }
                else
                {
                    value_str = in_str;
                }
                // check if this is a number-sring
                try
                {
                    UInt32 temp = Convert.ToUInt32(Convert.ToDouble(value_str) * 100);      // only for testing conversion.
                    result_string = value_str;
                    bRet = true;
                }
                catch (System.FormatException)
                {
                    result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_COMPARE;
                }
            }

            // Debug purpose
            if (result_status >= ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                Console.WriteLine("Get_SW_Version Error: " + result_status.ToString());
            }

            return bRet;
        }

        private bool Get_SW_Build_Time(out string result_string)
        {
            bool bRet = false;
            const int default_timeout_time = 40;
            string in_str;
            ENUM_RETRY_RESULT result_status;

            // Workaround before v1.02
            if (BlueRatFWVersion < 102)     // This bug is identified and fixed on v1.02
            {
                if (BlueRatFWVersion == 100)
                {
                    result_string = "Dec 21 2017" + " " + "13:44:28";
                }
                else
                {
                    result_string = "Dec 2017";
                }
                result_status = ENUM_RETRY_RESULT.ENUM_OK_RETRY_00;
                bRet = true;
            }
            else
            {
                result_string = "0";
                result_status = SendCmd_WaitReadLine(Prepare_Send_Input_CMD_without_Parameter(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_RETURN_BUILD_TIME)), out in_str, default_timeout_time);
                if (result_status < ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
                {
                    if (in_str.Contains(_CMD_BUILD_TIME_RETURN_HEADER_))
                    {
                        result_string = in_str.Substring(in_str.IndexOf(":") + 1);
                        bRet = true;
                    }
                    else
                    {
                        result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_COMPARE;
                    }
                }
                // Debug purpose
                if (result_status >= ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
                {
                    Console.WriteLine("Get_SW_Build_Time Error: " + result_status.ToString());
                }
            }
            return bRet;
        }

        private bool Get_Command_Version(out string result_string)
        {
            bool bRet = false;
            const int default_timeout_time = 30;
            string in_str;
            ENUM_RETRY_RESULT result_status;

            result_string = "0";
            result_status = SendCmd_WaitReadLine(Prepare_Send_Input_CMD_without_Parameter(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_RETURN_CMD_VERSION)), out in_str, default_timeout_time);
            if (result_status < ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {

                if (_CMD_RETURN_CMD_VERSION_RETURN_HEADER_ == "")
                {
                }
                else if (in_str.Contains(_CMD_RETURN_CMD_VERSION_RETURN_HEADER_))
                {
                    in_str = in_str.Substring(in_str.IndexOf(":") + 1);
                }
                else
                {
                    in_str = "ERR:" + in_str;
                    // not correct result - intentionally set in_str="ERR"
                }

                // check if this is a unsigned-number-sring
                try
                {
                    UInt32 temp = Convert.ToUInt32(in_str);
                    result_string = in_str;
                    bRet = true;
                }
                catch (System.FormatException)
                {
                    result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_COMPARE;
                }
            }

            // Debug purpose
            if (result_status >= ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                Console.WriteLine("Get_SW_Version Error: " + result_status.ToString() + " -- " + in_str);
            }

            return bRet;
        }

        public bool Get_GPIO_Input(out UInt32 GPIO_Read_Data)
        {
            bool bRet = false;
            const int default_timeout_time = 16;
            string in_str;
            ENUM_RETRY_RESULT result_status;

            GPIO_Read_Data = 0xffffffff;
            result_status = SendCmd_WaitReadLine(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_GPIO_INPUT)), out in_str, default_timeout_time);
            if (result_status < ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                if (in_str.Contains(_CMD_GPIO_INPUT_RETURN_HEADER_))
                {
                   in_str = in_str.Substring(in_str.IndexOf(":") + 1);
                }
                else if (_CMD_GPIO_INPUT_RETURN_HEADER_ == "")
                {
                    // do nothing here
                }
                else
                {
                    in_str = "ERR:"+ in_str;
                    // not correct result - intentionally set in_str="ERR"
                }

                try
                {
                    UInt32 temp = Convert.ToUInt32(in_str, 16);      // only for testing conversion.
                    GPIO_Read_Data = temp;
                    bRet = true;
                }
                catch (System.FormatException)
                {
                    result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_COMPARE;
                }
            }

            // Debug purpose
            if (result_status >= ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                Console.WriteLine("Get_SW_Version Error: " + result_status.ToString() + " -- " + in_str);
            }

            return bRet;
        }

        public bool Get_Sensor_Input(out UInt32 Sensor_Read_Data)
        {
            bool bRet = false;
            const int default_timeout_time = 16;
            string in_str;
            ENUM_RETRY_RESULT result_status;

            Sensor_Read_Data = 0xffffffff;
            result_status = SendCmd_WaitReadLine(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_SENSOR_VALUE)), out in_str, default_timeout_time);
            if (result_status < ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                if (in_str.Contains(_CMD_SENSOR_INPUT_RETURN_HEADER_))
                {
                    in_str = in_str.Substring(in_str.IndexOf(":") + 1);
                }
                else if (_CMD_SENSOR_INPUT_RETURN_HEADER_ == "")
                {
                    // do nothing here
                }
                else
                {
                    in_str = "ERR:" + in_str;
                    // not correct result - intentionally set in_str="ERR"
                }

                try
                {
                    UInt32 temp = Convert.ToUInt32(in_str, 16);      // only for testing conversion.
                    Sensor_Read_Data = temp;
                    bRet = true;
                }
                catch (System.FormatException)
                {
                    result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_COMPARE;
                }
            }

            // Debug purpose
            if (result_status >= ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1)
            {
                Console.WriteLine("Get_Sensor_Input Error: " + result_status.ToString() + " -- " + in_str);
            }

            return bRet;
        }

        public bool Set_GPIO_Output(byte output_value)
        {
            bool bRet = false;

            if (MyBlueRatSerial.BlueRatSendToSerial(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), output_value).ToArray()))
            {
                bRet = true;
            }
            return bRet;
        }

        public bool Set_GPIO_Output_SinglePort(byte port_no, byte output_value)
        {
            bool bRet = false;

            UInt32 temp_parameter;
            if (output_value != 0) { temp_parameter = 1; } else { temp_parameter = 0; }
            temp_parameter |= Convert.ToUInt32(port_no) << 8;
            if (MyBlueRatSerial.BlueRatSendToSerial(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), temp_parameter).ToArray()))
            {
                bRet = true;
            }
            return bRet;
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

            if ((RedRatData == null) || (MyBlueRatSerial.Serial_PortConnection() == false))
            {
                return 0;
            }

            List<byte> data_to_sent = new List<byte>();
            int total_us;
            data_to_sent = Prepare_RC_Data_CMD(RedRatData, default_repeat_cnt, out total_us);  // prepare RC-data packet for BlueRat
            MyBlueRatSerial.BlueRatSendToSerial(data_to_sent.ToArray());    // Send to BlueRat

            return total_us; // return total_rc_time_duration
        }

        static public List<string> FindAllBlueRat()
        {
            List<string> AllBlueRat = new List<string>();

//            foreach (string comport_s in SerialPort.GetPortNames())
            foreach (string comport_s in BlueRat.Find_BluRat_USBSerial_Port()) 
            {
                BlueRatSerial Checking_Serial = new BlueRatSerial(comport_s);
                if (Checking_Serial.Serial_PortConnection() == true)
                {
                    if (STATIC_Test_If_System_Can_Say_HI(Checking_Serial) == true)
                    {
                        AllBlueRat.Add(comport_s);
                    }
                }
                else
                {
                    // open it only when it is not currently open -- so need to close it after checking
                    Checking_Serial.Serial_OpenPort();
                    if (Checking_Serial.Serial_PortConnection() == true)
                    {
                        if (STATIC_Test_If_System_Can_Say_HI(Checking_Serial) == true)
                        {
                            AllBlueRat.Add(comport_s);
                        }
                    }
                    Checking_Serial.Serial_ClosePort();
                }
            }

            return AllBlueRat;
        }

        // This one is private
        private bool Connect_BlueRat_Protocol()
        {
            bool bRet = false;
            if (this.CheckConnection() == true)
            {
                bRet = true;
            }
            else
            {
                HomeMade_Delay(10);
                Force_Init_BlueRat();
                HomeMade_Delay(10);
                if (CheckConnection() == true)
                {
                    bRet = true;
                }
            }
            return bRet;
        }

        //
        // 跟小藍鼠有關係的程式代碼與範例程式區--結尾
        //

        //
        // Function for external use -- END
        //

        // This function is intended to show Debug_String to console when each UART data is sent
        //private int Tx_CNT = 0;
        private bool SendToSerial_v2(byte[] byte_to_sent)
        {
            bool return_value = false;

            return_value = MyBlueRatSerial.BlueRatSendToSerial(byte_to_sent);
            // Console.WriteLine("\n===Tx:" + Tx_CNT.ToString() + " ");
            // Tx_CNT++;
            return return_value;
        }

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

        // History of all HI String
        const string _HI_STRING_VER_00 = "HI";
        //const string _HI_STRING_VER_01 = "HI!BlueRat!"; // reserved for future extension

        // History of all SW_VER header
        const string _SW_VER_STRING_VER_00 = "";
        const string _SW_VER_STRING_VER_01 = "SW:";

        //string _CMD_SAY_HI_RETURN_HEADER_ = _HI_STRING_VER_00;
        //string _CMD_RETURN_SW_VER_RETURN_HEADER_ ="";
        string _CMD_BUILD_TIME_RETURN_HEADER_ = "";
        string _CMD_RETURN_CMD_VERSION_RETURN_HEADER_ = "";
        string _CMD_GET_TX_RUNNING_STATUS_HEADER_ = "";
        string _CMD_GET_TX_CURRENT_REPEAT_COUNT_RETURN_HEADER_ = "";
        string _CMD_GPIO_INPUT_RETURN_HEADER_ = "";
        string _CMD_SENSOR_INPUT_RETURN_HEADER_ = "";

        static private void Update_Header_String_by_SW_Version(BlueRat blue_rat, uint fw_ver)
        {
            if (fw_ver >= 102)
            {
                //_CMD_SAY_HI_RETURN_HEADER_ = _HI_STRING_VER_00;
                //_CMD_RETURN_SW_VER_RETURN_HEADER_ = "SW:"; 
                blue_rat._CMD_BUILD_TIME_RETURN_HEADER_ = "AT:";
                blue_rat._CMD_RETURN_CMD_VERSION_RETURN_HEADER_ = "CMD_VER:";
                blue_rat._CMD_GET_TX_RUNNING_STATUS_HEADER_ = "TX:";
                blue_rat._CMD_GET_TX_CURRENT_REPEAT_COUNT_RETURN_HEADER_ = "CNT:";
                blue_rat._CMD_GPIO_INPUT_RETURN_HEADER_ = "IN:";
                blue_rat._CMD_SENSOR_INPUT_RETURN_HEADER_ = "SS:";
            }
            else
            {
                //_CMD_SAY_HI_RETURN_HEADER_ = _HI_STRING_VER_00;
                //_CMD_RETURN_SW_VER_RETURN_HEADER_ = "";
                blue_rat._CMD_BUILD_TIME_RETURN_HEADER_ = "";
                blue_rat._CMD_RETURN_CMD_VERSION_RETURN_HEADER_ = "";
                blue_rat._CMD_GET_TX_RUNNING_STATUS_HEADER_ = "";
                blue_rat._CMD_GET_TX_CURRENT_REPEAT_COUNT_RETURN_HEADER_ = "";
                blue_rat._CMD_GPIO_INPUT_RETURN_HEADER_ = "";
                blue_rat._CMD_SENSOR_INPUT_RETURN_HEADER_ = "";
            }
        }

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
        // To get UART data byte for each command
        //
        //
        // Checksum is currently excluding sync header
        //
        static private List<byte> Prepare_STOP_CMD()
        {
            List<byte> data_to_sent = new List<byte>();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();

            MyCheckSum.ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for header
            data_to_sent.Add((Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_STOP_ALL)));
            MyCheckSum.UpdateCheckSum((Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_STOP_ALL)));
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            return data_to_sent;
        }

        static private List<byte> Prepare_FORCE_RESTART_CMD()
        {
            List<byte> data_to_sent = new List<byte>();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();

            MyCheckSum.ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_FORCE_RESTART));
            MyCheckSum.UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_FORCE_RESTART));
            List<byte> input_param_in_byte = Convert_data_to_Byte(RESTART_PASSWORD);
            foreach (byte temp in input_param_in_byte)
            {
                data_to_sent.Add(temp);
                MyCheckSum.UpdateCheckSum(temp);
            }
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            return data_to_sent;
        }

        static private List<byte> Prepare_Say_HI_CMD()
        {
            List<byte> data_to_sent = new List<byte>();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();

            MyCheckSum.ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SAY_HI));
            MyCheckSum.UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SAY_HI));
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            return data_to_sent;
        }

        static private List<byte> Prepare_Get_RC_Repeat_Count()
        {
            List<byte> data_to_sent = new List<byte>();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();

            MyCheckSum.ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_TX_CURRENT_REPEAT_COUNT));
            MyCheckSum.UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_TX_CURRENT_REPEAT_COUNT));
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            return data_to_sent;
        }

        static private List<byte> Prepare_Get_RC_Current_Running_Status()
        {
            List<byte> data_to_sent = new List<byte>();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();

            MyCheckSum.ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_TX_RUNNING_STATUS));
            MyCheckSum.UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_GET_TX_RUNNING_STATUS));
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            return data_to_sent;
        }

        static private List<byte> Prepare_Enter_ISP_CMD()
        {
            List<byte> data_to_sent = new List<byte>();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();

            MyCheckSum.ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_ENTER_ISP_MODE));
            MyCheckSum.UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_ENTER_ISP_MODE));
            List<byte> input_param_in_byte = Convert_data_to_Byte(ISP_PASSWORD);
            foreach (byte temp in input_param_in_byte)
            {
                data_to_sent.Add(temp);
                MyCheckSum.UpdateCheckSum(temp);
            }
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            //data_to_sent.Add(GetCheckSum());
            return data_to_sent;
        }

        static private List<byte> Prepare_Send_Repeat_Cnt_Add_CMD(UInt32 cnt = 0)
        {
            List<byte> data_to_sent = new List<byte>();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();

            MyCheckSum.ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_ADD_REPEAT_COUNT));
            MyCheckSum.UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_ADD_REPEAT_COUNT));
            List<byte> input_param_in_byte = Convert_data_to_Byte(cnt);
            foreach (byte temp in input_param_in_byte)
            {
                data_to_sent.Add(temp);
                MyCheckSum.UpdateCheckSum(temp);
            }
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            return data_to_sent;
        }

        static private List<byte> Prepare_Send_Input_CMD_without_Parameter(byte input_cmd)
        {
            List<byte> data_to_sent = new List<byte>();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();

            MyCheckSum.ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for headers
            data_to_sent.Add(input_cmd);
            MyCheckSum.UpdateCheckSum(input_cmd);
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            return data_to_sent;
        }

        static private List<byte> Prepare_Send_Input_CMD(byte input_cmd, UInt32 input_param = 0)
        {
            if ((input_cmd < CMD_CODE_LOWER_LIMIT) || (input_cmd > CMD_CODE_UPPER_LIMIT))
            {
                return Prepare_Say_HI_CMD();
            }

            List<byte> data_to_sent = new List<byte>();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();
            MyCheckSum.ClearCheckSum();
            data_to_sent.Add(0xff);
            data_to_sent.Add(0xff);
            // No need to calculate checksum for header
            data_to_sent.Add(input_cmd);
            MyCheckSum.UpdateCheckSum(input_cmd);
            if ((input_cmd >= CMD_SEND_COMMAND_CODE_WITH_BYTE) && (input_cmd < CMD_SEND_COMMAND_CODE_ONLY))
            {
                byte temp = Convert.ToByte(input_param & 0xff);
                data_to_sent.Add(Convert.ToByte(temp & 0xff));
                MyCheckSum.UpdateCheckSum(temp);
            }
            else if ((input_cmd >= CMD_SEND_COMMAND_CODE_WITH_WORD) && (input_cmd < CMD_SEND_COMMAND_CODE_WITH_BYTE))
            {
                List<byte> input_param_in_byte = Convert_data_to_Byte(Convert.ToUInt16(input_param & 0xffff));
                foreach (byte temp in input_param_in_byte)
                {
                    data_to_sent.Add(temp);
                    MyCheckSum.UpdateCheckSum(temp);
                }
            }
            else if ((input_cmd >= CMD_SEND_COMMAND_CODE_WITH_DOUBLE_WORD) && (input_cmd < CMD_SEND_COMMAND_CODE_WITH_WORD))
            {
                List<byte> input_param_in_byte = Convert_data_to_Byte(Convert.ToUInt32(input_param & 0xffffffff));
                foreach (byte temp in input_param_in_byte)
                {
                    data_to_sent.Add(temp);
                    MyCheckSum.UpdateCheckSum(temp);
                }
            }
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            return data_to_sent;
        }

        static private List<byte> Prepare_RC_Data_CMD(RedRatDBParser RedRatData, byte default_repeat_cnt, out int total_us)
        {
            // Step 4
            List<byte> data_to_sent = new List<byte>();
            List<byte> pulse_packet = new List<byte>();
            List<double> pulse_width = RedRatData.GetTxPulseWidth();
            BlueRatCheckSum MyCheckSum = new BlueRatCheckSum();

            total_us = 0;
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
                MyCheckSum.ClearCheckSum();
            }

            // (2) Command
            data_to_sent.Add(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_INPUT_TX_SIGNAL));
            MyCheckSum.UpdateCheckSum(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_INPUT_TX_SIGNAL));

            // (3) how many times to repeat RC (max 0xff)
            {
                const byte repeat_count_max = 0xff;
                if (default_repeat_cnt <= repeat_count_max)
                {
                    data_to_sent.Add(default_repeat_cnt);        // Repeat_No
                    MyCheckSum.UpdateCheckSum(default_repeat_cnt);
                    total_us *= (default_repeat_cnt > 0) ? (default_repeat_cnt + 1) : 1;
                }
                else
                {
                    Console.WriteLine("Repeat Count is out of range (>" + repeat_count_max.ToString() + "), using " + repeat_count_max.ToString() + " instead.");
                    data_to_sent.Add(repeat_count_max);        // Repeat_No
                    MyCheckSum.UpdateCheckSum(repeat_count_max);
                    total_us *= (repeat_count_max > 0) ? (repeat_count_max + 1) : 1;
                }
            }
            // (4) Duty Cycle range is 0-100, other values are reserved
            {
                const byte default_duty_cycle = 33, max_duty_cycle = 100;
                if (duty_cycle <= max_duty_cycle)
                {
                    data_to_sent.Add(duty_cycle);
                    MyCheckSum.UpdateCheckSum(duty_cycle);
                }
                else
                {
                    Console.WriteLine("Duty Cycle is out of range (>" + max_duty_cycle.ToString() + "), using  " + default_duty_cycle.ToString() + "  instead.");
                    data_to_sent.Add(default_duty_cycle);
                    MyCheckSum.UpdateCheckSum(default_duty_cycle);
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
                MyCheckSum.UpdateCheckSum(temp_byte);
                temp_byte = Convert.ToByte(period % 256);
                data_to_sent.Add(temp_byte);
                MyCheckSum.UpdateCheckSum(temp_byte);
            }
            // (6) Add RC width data
            {
                foreach (var val in pulse_packet)
                {
                    MyCheckSum.UpdateCheckSum(val);
                }
                data_to_sent.AddRange(pulse_packet);
            }
            // (7) Add 0xff as last data byte
            {
                data_to_sent.Add(0xff);
                MyCheckSum.UpdateCheckSum(0xff);
            }
            // (8) Finally add checksum at end of packet
            data_to_sent.Add(MyCheckSum.GetCheckSum());
            return data_to_sent;
        }

        // 小藍鼠專用的delay的內部資料與function
        // This list stores all running timers. If timer timeout, it is removed at event. When BlueRat stop connection, all timers under this bluerat are also removed.
        static private List<object> TimeOutTimerList = new List<object>();
        // This list stores only running timers belonged to this bluerat object.
        private List<object> MyOwnTimerList = new List<object>();

        private void Stop_MyOwn_HomeMade_Delay()
        {
            // Stop all timer created from own BlueRat Object (in list MyOwnTimerList)
            foreach (var timer in MyOwnTimerList)
            {
                if (TimeOutTimerList.Contains(timer))        // still timer of this bluerat object is running?
                {
                    TimeOutTimerList.Remove(timer);         // if yes, force it to expire
                    Application.DoEvents();
                }
            }
            MyOwnTimerList.Clear();                         // all timer expired, no need to keep record
        }

        static private void HomeMade_Delay_OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //HomeMade_TimeOutIndicator = true;
            TimeOutTimerList.Remove(source);
        }

        private void HomeMade_Delay(int delay_ms)
        {
            if (delay_ms <= 0) return;
            System.Timers.Timer aTimer = new System.Timers.Timer(delay_ms);
            aTimer.Elapsed += new ElapsedEventHandler(HomeMade_Delay_OnTimedEvent);
            //HomeMade_TimeOutIndicator = false;
            TimeOutTimerList.Add(aTimer);           // This list is to keep running timer until it reaches TimeOutEvent (as indicator of running timer)
            MyOwnTimerList.Add(aTimer);             // This list record all timer created in this bluerat object -- to be removed after timer expired
            aTimer.Enabled = true;
            while ((MyBlueRatSerial.Serial_PortConnection() == true) && (TimeOutTimerList.Contains(aTimer) == true)) { Application.DoEvents(); Thread.Sleep(1); }
            MyOwnTimerList.Remove(aTimer);          // timer expired, so remove it from the list recording timer created.
            aTimer.Stop();
            aTimer.Dispose();
        }
        // END - 小藍鼠專用的delay的內部資料與function

        enum ENUM_RETRY_RESULT
        {
            ENUM_OK_LAST_RETRY = 0,
            ENUM_OK_RETRY_02,
            ENUM_OK_RETRY_01,
            ENUM_OK_RETRY_00,
            ENUM_MAX_RETRY_PLUS_1,
            ENUM_ERROR_AT_READLINE = 0x100,
            ENUM_ERROR_AT_COMPARE,
            ENUM_ERROR_AT_SEND_COMMAND,
        };

        // 單純回應"HI"的指令,可用來試試看系統是否還有在接受指令 -- 目前不對外開放
        static private Boolean STATIC_Test_If_System_Can_Say_HI(BlueRatSerial bluerat_serial)
        {
            bool bRet = false;
            ENUM_RETRY_RESULT result_status;
            bluerat_serial.Start_ReadLine();
            if (bluerat_serial.BlueRatSendToSerial(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SAY_HI)).ToArray()) == false)
            {
                result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_SEND_COMMAND;
            }
            else
            {
                result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_READLINE;       // readline error will be cleared after line is read.
                uint MyRetryTimes = Convert.ToUInt32(ENUM_RETRY_RESULT.ENUM_MAX_RETRY_PLUS_1) - 1;
                do
                {
                    Application.DoEvents(); Thread.Sleep(4); Application.DoEvents();
                    if (bluerat_serial.ReadLine_Ready() == true)
                    {
                        String in_str = bluerat_serial.ReadLine_Result();
                        result_status = (ENUM_RETRY_RESULT)MyRetryTimes;                // Store retry_ok_times as result

                        if (String.Compare(in_str, _HI_STRING_VER_00) == 0)             // Currently only one version of HI string
                        {
                            bRet = true;
                        }
                        else
                        {
                            result_status = ENUM_RETRY_RESULT.ENUM_ERROR_AT_COMPARE;    // Store error_status result
                                                                                        //MyRetryTimes = 0;
                            break;
                            //Console.WriteLine("Not a HI");
                        }
                    }
                    else
                    {
                        // Console.WriteLine("No string at greeting");
                    }
                }
                while ((bRet == false) && (MyRetryTimes-- > 0));
            }

            // Debug purpose
            if (bRet == false)
            {
                Console.WriteLine("Check STATIC_Test_If_System_Can_Say_HI - " + result_status.ToString());
            }

            return bRet;
        }

        private Boolean Test_If_System_Can_Say_HI()
        {
            return STATIC_Test_If_System_Can_Say_HI(MyBlueRatSerial);
        }

        /*
        private Boolean Test_If_System_Can_Say_HI()
        {
            Boolean bRet = false;
            //Get_UART_Input = 1;
            MyBlueRatSerial.Start_ReadLine();
            MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SAY_HI)).ToArray());
            HomeMade_Delay(10);
            if (MyBlueRatSerial.ReadLine_Ready()==true)
            {
                String in_str = MyBlueRatSerial.ReadLine_Result();
                if (in_str.Contains(_CMD_SAY_HI_RETURN_HEADER_))
                {
                    bRet = true;
                }
                else
                {
                    Console.WriteLine("BlueRat no resonse to HI Command");
                }
            }
            return bRet;
        }
        */

        //
        // Try to use VID/PID to locate possible BlueRat com-port
        //
        static private List<string> Find_BluRat_USBSerial_Port()
        {
            List<string> return_com_port = new List<string> ();
            ManagementObjectSearcher search = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");
            ManagementObjectCollection collection = search.Get();
            var usbList = from u in collection.Cast<ManagementBaseObject>()
                          select new
                          {
                              id = u.GetPropertyValue("DeviceID"),
                              name = u.GetPropertyValue("Name"),
                              description = u.GetPropertyValue("Description"),
                              status = u.GetPropertyValue("Status"),
                              system = u.GetPropertyValue("SystemName"),
                              caption = u.GetPropertyValue("Caption"),
                              pnp = u.GetPropertyValue("PNPDeviceID"),
                          };

            foreach (var usbDevice in usbList)
            {
                string deviceId = (string)usbDevice.id;
                string deviceTp = (string)usbDevice.name;
                string deviecDescription = (string)usbDevice.description;

                string deviceStatus = (string)usbDevice.status;
                string deviceSystem = (string)usbDevice.system;
                string deviceCaption = (string)usbDevice.caption;
                string devicePnp = (string)usbDevice.pnp;

                if (deviecDescription != null)
                {
                    if (deviceId.IndexOf("&0&5", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        deviceId.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int FirstIndex = deviceTp.IndexOf("(");
                        string AutoBoxPortSubstring = deviceTp.Substring(FirstIndex + 1);
                        string AutoBoxPort = AutoBoxPortSubstring.Substring(0);

                        int AutoBoxPortLengh = AutoBoxPort.Length;
                        string AutoBoxPortFinal = AutoBoxPort.Remove(AutoBoxPortLengh - 1);

                        string BlueRatVid = "";
                        if (deviceId.IndexOf("VID_", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int vidIndex = deviceId.IndexOf("VID_");
                            string startingAtVid = deviceId.Substring(vidIndex + 4); // + 4 to remove "VID_"                    
                            BlueRatVid = startingAtVid.Substring(0, 4); // vid is four characters long
                            //Console.WriteLine(BlueRatVid);
                        }

                        string BlueRatPid = "";
                        if (deviceId.IndexOf("PID_", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int pidIndex = deviceId.IndexOf("PID_");
                            string startingAtPid = deviceId.Substring(pidIndex + 4); // + 4 to remove "PID_"                    
                            BlueRatPid = startingAtPid.Substring(0, 4); // pid is four characters long
                            //Console.WriteLine(BlueRatPid);
                        }

                        if (BlueRatVid == "067B" && BlueRatPid == "2303")
                        {
                            //AutoBox存在
                            if (AutoBoxPortSubstring.Substring(0, 3) == "COM")
                            {
                                return_com_port.Add(AutoBoxPortFinal);
                            }
                        }
                    }
                }
            }
            return return_com_port;
        }

        // For self testing purpose
        public void TEST_WalkThroughAllCMDwithData()
        {
            // Testing: send all CMD with input parameter
            for (byte cmd = Convert.ToByte(CMD_CODE_UPPER_LIMIT); cmd >= Convert.ToByte(CMD_CODE_LOWER_LIMIT); cmd--)
            //byte cmd = 0xdf;
            {
                MyBlueRatSerial.BlueRatSendToSerial(Prepare_Send_Input_CMD(cmd, 0x1010101U * cmd).ToArray());
                HomeMade_Delay(32);
                if (!MyBlueRatSerial.Serial_PortConnection())
                {
                    return;
                }
            }
        }

        // For self testing purpose
        public void TEST_GPIO_Output()
        {
            const int delay_time = 100;
            // Testing: send GPIO output with byte parameter -- Set output port value at once
            for (uint output_value = 0; output_value <= 0xff; output_value++)
            {
                Set_GPIO_Output(Convert.ToByte(output_value & 0xff));
                HomeMade_Delay(delay_time / 2);
                if (!MyBlueRatSerial.Serial_PortConnection())
                {
                    return;
                }
            }

            int run_time = 10;
            const UInt32 IO_value_mask = 0x0, reverse_IO_value_mask = 0x1;

            Set_GPIO_Output(Convert.ToByte((~reverse_IO_value_mask) & 0xff));
            HomeMade_Delay(delay_time);
            if (!MyBlueRatSerial.Serial_PortConnection())
            {
                return;
            }

            while (run_time-- > 0)
            {
                for (Byte output_bit = 0; output_bit < 7;)
                {
                    //UInt32 temp_parameter = (output_bit << 8) | reverse_IO_value_mask;
                    //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    Set_GPIO_Output_SinglePort(output_bit, Convert.ToByte(reverse_IO_value_mask));
                    HomeMade_Delay(16);
                    output_bit++;
                    //temp_parameter = (((output_bit) << 8) | IO_value_mask);
                    //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    Set_GPIO_Output_SinglePort(output_bit, Convert.ToByte(IO_value_mask));
                    HomeMade_Delay(delay_time);
                    if (!MyBlueRatSerial.Serial_PortConnection())
                    {
                        return;
                    }
                }
                for (Byte output_bit = 7; output_bit > 0;)
                {
                    //UInt32 temp_parameter = (output_bit << 8) | reverse_IO_value_mask;
                    //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    Set_GPIO_Output_SinglePort(output_bit, Convert.ToByte(reverse_IO_value_mask));
                    HomeMade_Delay(16);
                    output_bit--;
                    //temp_parameter = (((output_bit) << 8) | IO_value_mask);
                    //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_SINGLE_BIT), temp_parameter).ToArray());
                    Set_GPIO_Output_SinglePort(output_bit, Convert.ToByte(IO_value_mask));
                    HomeMade_Delay(delay_time);
                    if (!MyBlueRatSerial.Serial_PortConnection())
                    {
                        return;
                    }
                }
            }
        }
        
        // For self testing purpose
        public void TEST_GPIO_Input()
        {
            const int delay_time = 500;
            //byte GPIO_Read_Data = 0;

            // For reading an UART input, please make sure previous return data has been already received

            int run_time = 20;
            while (run_time-- > 0)
            {
                if (!MyBlueRatSerial.Serial_PortConnection())
                {
                    return;
                }

                UInt32 gpio_input_value;
                Get_GPIO_Input(out gpio_input_value);
                byte GPIO_Read_Data = Convert.ToByte(gpio_input_value & 0xff);
                Console.WriteLine("GPIO_IN:" + GPIO_Read_Data.ToString());
                GPIO_Read_Data ^= 0xff;
                //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), ~GPIO_Read_Data).ToArray());
                Set_GPIO_Output(GPIO_Read_Data);
                HomeMade_Delay(delay_time);
                //MyBlueRatSerial.BlueRatSendToSeria(Prepare_Send_Input_CMD(Convert.ToByte(ENUM_CMD_STATUS.ENUM_CMD_SET_GPIO_ALL_BIT), 0xff).ToArray());
                Set_GPIO_Output(0xff);
                HomeMade_Delay(delay_time);
            }
        }

        public void TEST_SENSOR_Input()
        {
            const int delay_time = 500;
            //byte GPIO_Read_Data = 0;

            // For reading an UART input, please make sure previous return data has been already received

            int run_time = 20;
            while (run_time-- > 0)
            {
                if (!MyBlueRatSerial.Serial_PortConnection())
                {
                    return;
                }

                UInt32 sensor_input_value;
                Get_Sensor_Input(out sensor_input_value);
                byte Sensor_Read_Data = Convert.ToByte(sensor_input_value & 0xff);
                Console.WriteLine("Sendor_IN:" + Sensor_Read_Data.ToString());
                HomeMade_Delay(delay_time);
            }
        }


        ///
        ///
    }
}