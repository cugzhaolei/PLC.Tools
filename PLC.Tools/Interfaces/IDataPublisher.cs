using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Interfaces
{
    /// <summary>
    /// 数据发布器接口
    /// </summary>
    public interface IDataPublisher
    {
        /// <summary>
        /// 发布名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 启动发布器
        /// </summary>
        /// <returns>是否成功</returns>
        Task<bool> StartAsync();

        /// <summary>
        /// 停止发布器
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 发布PLC数据
        /// </summary>
        /// <param name="plcData">PLC数据</param>
        /// <returns>是否成功</returns>
        Task<bool> PublishDataAsync(PlcData plcData);
    }
}
