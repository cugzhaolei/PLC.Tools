using PLC.Tools.Interfaces;
using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Implementations
{
    /// <summary>
    /// 三菱PLC连接工厂
    /// </summary>
    public class MitsubishiPlcConnectionFactory : IPlcConnectionFactory
    {
        /// <summary>
        /// 创建PLC连接
        /// </summary>
        /// <param name="config">连接配置</param>
        /// <returns>PLC连接实例</returns>
        public IPlcConnection CreateConnection(PlcConnectionConfig config)
        {
            return new MitsubishiPlcConnection(config);
        }
    }
}
