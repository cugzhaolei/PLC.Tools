using HslCommunication;
using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Interfaces
{
    /// <summary>
    /// PLC连接接口
    /// </summary>
    public interface IPlcConnection : IDisposable
    {
        /// <summary>
        /// PLC连接配置
        /// </summary>
        PlcConnectionConfig Config { get; }

        /// <summary>
        /// 是否连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接PLC
        /// </summary>
        /// <returns></returns>
        Task<OperateResult> ConnectAsync();

        /// <summary>
        /// 断开连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 读取标签数据
        /// </summary>
        /// <param name="tags">标签列表</param>
        /// <returns>数据字典</returns>
        Task<OperateResult<Dictionary<string, object>>> ReadTagsAsync(List<PlcTag> tags);
    }
}
