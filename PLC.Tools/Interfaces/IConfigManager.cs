using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Interfaces
{
    /// <summary>
    /// 配置管理器接口
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        /// 加载所有PLC连接配置
        /// </summary>
        /// <returns>PLC连接配置列表</returns>
        Task<List<PlcConnectionConfig>> LoadPlcConfigsAsync();

        /// <summary>
        /// 保存PLC连接配置
        /// </summary>
        /// <param name="configs">PLC连接配置列表</param>
        /// <returns>是否成功</returns>
        Task<bool> SavePlcConfigsAsync(List<PlcConnectionConfig> configs);

        /// <summary>
        /// 加载指定PLC的标签
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>标签列表</returns>
        Task<List<PlcTag>> LoadTagsForPlcAsync(string plcIp);
        /// <summary>
        /// 加载指定PLC的标签
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>标签列表</returns>
        Task<List<PlcTag>> ParseJSONTags(string plcIp);

        /// <summary>
        /// 保存指定PLC的标签
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <param name="tags">标签列表</param>
        /// <returns>是否成功</returns>
        Task<bool> SaveTagsForPlcAsync(string plcIp, List<PlcTag> tags);
    }
}
