using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Interfaces
{
    /// <summary>
    /// PLC连接工厂接口
    /// </summary>
    public interface IPlcConnectionFactory
    {
        /// <summary>
        /// 创建PLC连接
        /// </summary>
        /// <param name="config">连接配置</param>
        /// <returns>PLC连接实例</returns>
        IPlcConnection CreateConnection(PlcConnectionConfig config);
    }
}
