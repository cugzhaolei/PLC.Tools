using HslCommunication;
using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PLC.Tools
{
    public static class PLCConnect
    {

        private static HslCommunication.Profinet.Melsec.MelsecMcNet plc = new HslCommunication.Profinet.Melsec.MelsecMcNet();
        public static bool PLCStatus = false;
        private static bool HeartBeatStatus = false;
        private static System.Timers.Timer Timer1;

        private static string _HeartBeatAddress;

        public static bool MelsecPLCConnect(string IPAddress, int Port, ushort IOStation)
        {
            bool returnvalue = false;
            plc = new HslCommunication.Profinet.Melsec.MelsecMcNet();
            plc.NetworkNumber = 0;
            plc.NetworkStationNumber = 0;
            plc.TargetIOStation = IOStation;
            plc.EnableWriteBitToWordRegister = false;
            plc.ByteTransform.IsStringReverseByteWord = false;
            plc.CommunicationPipe = new HslCommunication.Core.Pipe.PipeTcpNet(IPAddress, Port)
            {
                ConnectTimeOut = 5000,    // 连接超时时间，单位毫秒
                ReceiveTimeOut = 10000,    // 接收设备数据反馈的超时时间
                SleepTime = 0,
                SocketKeepAliveTime = -1,
                IsPersistentConnection = true,
            };
            OperateResult connected = plc.ConnectServer();
            if (connected.IsSuccess)
            {
                returnvalue = true;
            }
            else
            {
                returnvalue = false;
            }
            return returnvalue;
        }

        private static void HeartBeatTime(object sender, EventArgs e)
        {
            if (PLCStatus)
            {
                if (HeartBeatStatus)
                {
                    short[] varOutput = new short[1];
                    varOutput[0] = 1;
                    plc.Write(_HeartBeatAddress, varOutput);
                    HeartBeatStatus = false;
                }
                else
                {
                    short[] varOutput = new short[1];
                    varOutput[0] = 0;
                    plc.Write(_HeartBeatAddress, varOutput);
                    HeartBeatStatus = true;
                }
            }
        }

        public static Dictionary<string, string> ReadPLCTags(List<MCData> mockTags)
        {

            //var mockTags = new List<MCData>();
            var data = new Dictionary<string, string>();
            var addressTags = new List<string>();
            var addressLength = new List<ushort>();
            if (PLCStatus)
            {
                if (HeartBeatStatus)
                {
                    // read plc tags address and return value
                    var result = plc.ReadTags([.. addressTags], [.. addressLength]);

                }
            }

            return data;
        }

        #region ref

        //public static class MelsecPLCMc_Positive
        //{
        //    private static HslCommunication.Profinet.Melsec.MelsecMcNet plc = new HslCommunication.Profinet.Melsec.MelsecMcNet();
        //    public static bool PLCStatus = false;
        //    private static bool HeartBeatStatus = false;
        //    private static System.Timers.Timer Timer1;

        //    private static string _CCD_PLC_ResultAddress;
        //    private static string _CCD_PLC_CompleteAddress;
        //    private static string _CCD_PLC_ResultTypeAddress;
        //    private static string _PLC_CCD_SNAddress;
        //    private static string _CCD_PLC_SNAddress;
        //    private static string _HeartBeatAddress;
        //    private static string _CCD_PLC_RunAddress;
        //    public static bool Init(string IPAddress, int Port, string CCD_PLC_ResultAddress, string CCD_PLC_CompleteAddress,
        //        string CCD_PLC_ResultTypeAddress, string PLC_CCD_SNAddress, string CCD_PLC_SNAddress, string HeartBeatAddress, string CCD_PLC_RunAddress)
        //    {
        //        bool returnvalue = false;
        //        _HeartBeatAddress = HeartBeatAddress;
        //        _CCD_PLC_ResultAddress = CCD_PLC_ResultAddress;
        //        _CCD_PLC_CompleteAddress = CCD_PLC_CompleteAddress;
        //        _CCD_PLC_ResultTypeAddress = CCD_PLC_ResultTypeAddress;
        //        _PLC_CCD_SNAddress = PLC_CCD_SNAddress;
        //        _CCD_PLC_SNAddress = CCD_PLC_SNAddress;
        //        _CCD_PLC_RunAddress = CCD_PLC_RunAddress;
        //        PLCStatus = MelsecPLCConnect(IPAddress, Port);
        //        if (PLCStatus)
        //        {
        //            //HeartBeat
        //            Timer1 = new System.Timers.Timer();
        //            Timer1.Elapsed += new ElapsedEventHandler(HeartBeatTime);
        //            Timer1.Interval = 1000;
        //            Timer1.AutoReset = true;
        //            Timer1.Start();
        //        }
        //        returnvalue = PLCStatus;
        //        return returnvalue;
        //    }

        //    public static bool MelsecPLCConnect(string IPAddress, int Port)
        //    {
        //        bool returnvalue = false;
        //        plc = new HslCommunication.Profinet.Melsec.MelsecMcNet();
        //        plc.NetworkNumber = 0;
        //        plc.NetworkStationNumber = 0;
        //        plc.TargetIOStation = 1023;
        //        plc.EnableWriteBitToWordRegister = false;
        //        plc.ByteTransform.IsStringReverseByteWord = false;
        //        plc.CommunicationPipe = new HslCommunication.Core.Pipe.PipeTcpNet(IPAddress, Port)
        //        {
        //            ConnectTimeOut = 5000,    // 连接超时时间，单位毫秒
        //            ReceiveTimeOut = 10000,    // 接收设备数据反馈的超时时间
        //            SleepTime = 0,
        //            SocketKeepAliveTime = -1,
        //            IsPersistentConnection = true,
        //        };
        //        OperateResult connected = plc.ConnectServer();
        //        if (connected.IsSuccess)
        //        {
        //            returnvalue = true;
        //        }
        //        else
        //        {
        //            returnvalue = false;
        //        }
        //        return returnvalue;
        //    }
        //    private static void HeartBeatTime(object sender, EventArgs e)
        //    {
        //        if (PLCStatus)
        //        {
        //            if (HeartBeatStatus)
        //            {
        //                short[] varOutput = new short[1];
        //                varOutput[0] = 1;
        //                plc.Write(_HeartBeatAddress, varOutput);
        //                HeartBeatStatus = false;
        //            }
        //            else
        //            {
        //                short[] varOutput = new short[1];
        //                varOutput[0] = 0;
        //                plc.Write(_HeartBeatAddress, varOutput);
        //                HeartBeatStatus = true;
        //            }
        //        }
        //    }
        //    public static ushort Read_PLC_SN()
        //    {
        //        ushort returnint = 0;
        //        string ReadAddress = _PLC_CCD_SNAddress;//ReadSN

        //        OperateResult<ushort> operateResult = plc.ReadUInt16(ReadAddress);
        //        if (operateResult.IsSuccess)
        //        {
        //            returnint = operateResult.Content;
        //        }
        //        else
        //        {
        //            returnint = 0;
        //        }
        //        return returnint;
        //    }
        //    public static bool Write_PLC_SN(ushort SN)
        //    {
        //        bool returnvalue = false;
        //        string WriteAddress = _CCD_PLC_SNAddress;//WriteSN

        //        OperateResult operateResult = plc.Write(WriteAddress, SN);
        //        if (operateResult.IsSuccess)
        //        {
        //            returnvalue = true;
        //        }
        //        else
        //        {
        //            returnvalue = false;
        //        }
        //        return returnvalue;
        //    }
        //    public static bool Write_PLC_Result(bool status)
        //    {
        //        bool returnvalue = false;
        //        ushort ResultSignal = 1;
        //        string WriteAddress = _CCD_PLC_ResultAddress;//WriteResult
        //        if (status)
        //        {
        //            ResultSignal = 2;//OK
        //        }
        //        else
        //        {
        //            ResultSignal = 1;//NG
        //        }
        //        OperateResult operateResult = plc.Write(WriteAddress, ResultSignal);
        //        if (operateResult.IsSuccess)
        //        {
        //            returnvalue = true;
        //        }
        //        else
        //        {
        //            returnvalue = false;
        //        }
        //        return returnvalue;
        //    }
        //    public static bool Write_PLC_ClearResult()
        //    {
        //        bool returnvalue = false;
        //        ushort ResultSignal = 0;
        //        string WriteAddress = _CCD_PLC_ResultAddress;//WriteResult
        //        OperateResult operateResult = plc.Write(WriteAddress, ResultSignal);
        //        if (operateResult.IsSuccess)
        //        {
        //            returnvalue = true;
        //        }
        //        else
        //        {
        //            returnvalue = false;
        //        }
        //        return returnvalue;
        //    }
        //    public static bool Write_PLC_Complete()
        //    {
        //        bool returnvalue = false;
        //        ushort ResultSignal = 1;
        //        string WriteAddress = _CCD_PLC_CompleteAddress;//WriteResult
        //        OperateResult operateResult = plc.Write(WriteAddress, ResultSignal);
        //        if (operateResult.IsSuccess)
        //        {
        //            returnvalue = true;
        //        }
        //        else
        //        {
        //            returnvalue = false;
        //        }
        //        return returnvalue;
        //    }
        //    public static bool Write_PLC_ResultType(int CheckType)
        //    {
        //        bool returnvalue = false;
        //        ushort CheckResult = 0;
        //        switch (CheckType)
        //        {
        //            case 0:
        //                CheckResult = 0; break;//OK信号
        //            case 1:
        //                CheckResult = 10; break;//划痕
        //            case 2:
        //                CheckResult = 20; break;//暗痕
        //            case 3:
        //                CheckResult = 30; break;//漏箔
        //            case 4:
        //                CheckResult = 40; break;//AT9漏箔
        //            case 5:
        //                CheckResult = 50; break;//暗斑
        //            case 6:
        //                CheckResult = 60; break;//亮斑
        //            case 7:
        //                CheckResult = 70; break;//其他NG
        //            case 8:
        //                CheckResult = 99; break;//
        //        }

        //        OperateResult operateResult = plc.Write(_CCD_PLC_ResultTypeAddress, CheckResult);
        //        if (operateResult.IsSuccess)
        //        {
        //            returnvalue = true;
        //        }
        //        else
        //        {
        //            returnvalue = false;
        //        }
        //        return returnvalue;
        //    }

        //    public static bool Write_PLC_Run(bool status)
        //    {
        //        bool returnvalue = false;
        //        ushort ResultSignal = 1;
        //        string WriteAddress = _CCD_PLC_RunAddress;//WriteResult
        //        if (status)
        //        {
        //            ResultSignal = 4;//Run
        //        }
        //        else
        //        {
        //            ResultSignal = 2; //Error
        //        }
        //        OperateResult operateResult = plc.Write(WriteAddress, ResultSignal);
        //        if (operateResult.IsSuccess)
        //        {
        //            returnvalue = true;
        //        }
        //        else
        //        {
        //            returnvalue = false;
        //        }
        //        return returnvalue;
        //    }
        //}
   
        #endregion
 }
}
