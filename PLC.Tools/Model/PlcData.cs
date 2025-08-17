using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Model
{
    /// <summary>
    /// PLC采集的数据
    /// </summary>
    public class PlcData
    {
        /// <summary>
        /// PLC IP地址
        /// </summary>
        public string PlcIp { get; set; }

        /// <summary>
        /// 采集时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 数据字典
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();

        /// <summary>
        /// 采集状态
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
