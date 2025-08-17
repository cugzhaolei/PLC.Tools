using HslCommunication;
using PLC.Tools.Interfaces;
using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HslCommunication.Profinet.Melsec;

using System;
using System.Text;


namespace PLC.Tools.Implementations
{
    /// <summary>
    /// 三菱PLC连接实现
    /// </summary>
    public class MitsubishiPlcConnection : IPlcConnection
    {
        private readonly HslCommunication.Profinet.Melsec.MelsecMcNet _plcClient;
        private bool _disposed = false;

        /// <summary>
        /// PLC连接配置
        /// </summary>
        public PlcConnectionConfig Config { get; }

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected {get;set; } = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">连接配置</param>
        public MitsubishiPlcConnection(PlcConnectionConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));

            // 初始化三菱MC协议客户端（适用于i-QR系列）
            _plcClient = new HslCommunication.Profinet.Melsec.MelsecMcNet
            {
                IpAddress = config.IpAddress,
                Port = config.Port,
                //Series = MitsubishiSeries.McIqR,  // 指定为i-QR系列
                TargetIOStation = (byte)config.TargetIOStation,
                NetworkNumber = (byte)config.NetworkStationNumber
            };
        }

        /// <summary>
        /// 连接PLC
        /// </summary>
        /// <returns></returns>
        public async Task<OperateResult> ConnectAsync()
        {
            if (IsConnected)
            {
                return OperateResult.CreateSuccessResult();
            }

            return await _plcClient.ConnectServerAsync();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (IsConnected)
            {
                _plcClient.ConnectClose();
            }
        }

        /// <summary>
        /// 读取标签数据
        /// </summary>
        /// <param name="tags">标签列表</param>
        /// <returns>数据字典</returns>
        public async Task<OperateResult<Dictionary<string, object>>> ReadTagsAsync(List<PlcTag> tags)
        {
            if (tags == null || !tags.Any())
            {
                return OperateResult.CreateFailedResult<Dictionary<string, object>>("没有标签数据",null);
            }

            if (!IsConnected)
            {
                var connectResult = await ConnectAsync();
                if (!connectResult.IsSuccess)
                {
                    return OperateResult.CreateFailedResult<Dictionary<string, object>>(connectResult.Message,null);
                }
            }

            try
            {
                var tagAddresses = tags.Select(t => t.Address).ToArray();
                var tagLengths = tags.Select(t => (ushort)t.DataLength).ToArray();

                // 使用HSL的批量读取方法
                var readResult = await _plcClient.ReadTagsAsync(tagAddresses, tagLengths);

                if (!readResult.IsSuccess)
                {
                    return OperateResult.CreateFailedResult<Dictionary<string, object>>(readResult.Message,null);
                }

                // 解析数据
                var resultData = new Dictionary<string, object>();
                int byteIndex = 0;

                for (int i = 0; i < tags.Count; i++)
                {
                    var tag = tags[i];
                    var dataLength = tag.DataLength;

                    if (byteIndex + dataLength > readResult.Content.Length)
                    {
                        resultData[tag.Name] = null;
                        continue;
                    }

                    // 提取当前标签的字节数据
                    byte[] tagData = new byte[dataLength];
                    Array.Copy(readResult.Content, byteIndex, tagData, 0, dataLength);
                    byteIndex += dataLength;

                    // 根据数据类型解析
                    resultData[tag.Name] = ParsePlcData(tag, tagData);
                }

                return OperateResult.CreateSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return OperateResult.CreateFailedResult<Dictionary<string, object>>(ex.Message,null);
            }
        }

        /// <summary>
        /// 解析PLC数据
        /// </summary>
        /// <param name="tag">标签信息</param>
        /// <param name="data">原始字节数据</param>
        /// <returns>解析后的数据</returns>
        private object ParsePlcData(PlcTag tag, byte[] data)
        {
            try
            {
                switch (tag.DataType?.ToLower())
                {
                    case "string":
                        Encoding encoding = GetEncoding(tag.StringEncode);
                        return encoding.GetString(data).TrimEnd('\0');

                    case "int16":
                    case "int":
                        return BitConverter.ToInt16(data, 0);

                    case "uint16":
                    case "ushort":
                        return BitConverter.ToUInt16(data, 0);

                    case "int32":
                        return BitConverter.ToInt32(data, 0);

                    case "uint32":
                    case "uint":
                        return BitConverter.ToUInt32(data, 0);

                    case "int64":
                        return BitConverter.ToInt64(data, 0);

                    case "uint64":
                        return BitConverter.ToUInt64(data, 0);

                    case "float":
                        return BitConverter.ToSingle(data, 0);

                    case "double":
                        return BitConverter.ToDouble(data, 0);

                    case "bool":
                        return data[0] != 0;

                    default:
                        return BitConverter.ToString(data);
                }
            }
            catch
            {
                return BitConverter.ToString(data);
            }
        }

        /// <summary>
        /// 获取编码方式
        /// </summary>
        /// <param name="encodeName">编码名称</param>
        /// <returns>编码实例</returns>
        private Encoding GetEncoding(string encodeName)
        {
            switch (encodeName?.ToLower())
            {
                case "utf8":
                    return Encoding.UTF8;
                case "gb2312":
                case "gbk":
                    return Encoding.GetEncoding("GBK");
                case "unicode":
                    return Encoding.Unicode;
                case "ascii":
                    return Encoding.ASCII;
                default:
                    return Encoding.UTF8;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否手动释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 释放托管资源
                Disconnect();
            }

            _disposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~MitsubishiPlcConnection()
        {
            Dispose(false);
        }
    }
}
