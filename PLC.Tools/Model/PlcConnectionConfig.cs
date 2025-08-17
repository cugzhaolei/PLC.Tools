using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Model
{
    /// <summary>
    /// PLC连接配置
    /// </summary>
    public class PlcConnectionConfig
    {
        /// <summary>
        /// PLC唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// IP地址
        /// </summary>
        public required string IpAddress { get; set; }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 1026;

        /// <summary>
        /// 目标IO站号
        /// </summary>
        public int TargetIOStation { get; set; }

        /// <summary>
        /// 网络站号
        /// </summary>
        public int NetworkStationNumber { get; set; }

        /// <summary>
        /// 连接名称
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
