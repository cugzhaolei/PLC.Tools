using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Interfaces
{
    /// <summary>
    /// PLC数据服务接口
    /// </summary>
    public interface IPlcDataService
    {
        /// <summary>
        /// 获取所有PLC配置
        /// </summary>
        /// <returns>PLC配置列表</returns>
        Task<List<PlcConnectionConfig>> GetAllPlcConfigsAsync();

        /// <summary>
        /// 添加或更新PLC配置
        /// </summary>
        /// <param name="config">PLC配置</param>
        /// <returns>是否成功</returns>
        Task<bool> AddOrUpdatePlcConfigAsync(PlcConnectionConfig config);

        /// <summary>
        /// 删除PLC配置
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>是否成功</returns>
        Task<bool> DeletePlcConfigAsync(string plcIp);

        /// <summary>
        /// 导入PLC标签
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <param name="tags">标签列表</param>
        /// <returns>是否成功</returns>
        Task<bool> ImportPlcTagsAsync(string plcIp, List<PlcTag> tags);

        /// <summary>
        /// 获取PLC标签
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>标签列表</returns>
        Task<List<PlcTag>> GetPlcTagsAsync(string plcIp);
        /// <summary>
        /// 获取PLC标签,OEE,NG,Property
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>标签列表</returns>
        Task<List<PlcTag>> GetPlcTagRoots(string plcIp);

        /// <summary>
        /// 读取PLC数据
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>PLC数据</returns>
        Task<PlcData> ReadPlcDataAsync(string plcIp);

        /// <summary>
        /// 读取所有PLC数据
        /// </summary>
        /// <returns>所有PLC数据</returns>
        Task<List<PlcData>> ReadAllPlcDataAsync();
    }
}
