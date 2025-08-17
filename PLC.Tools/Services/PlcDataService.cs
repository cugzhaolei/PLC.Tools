using PLC.Tools.Interfaces;
using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Services
{
    /// <summary>
    /// PLC数据服务
    /// </summary>
    public class PlcDataService : IPlcDataService
    {
        private readonly IConfigManager _configManager;
        private readonly IPlcConnectionFactory _plcConnectionFactory;
        private readonly IEnumerable<IDataPublisher> _dataPublishers;
        private readonly Dictionary<string, IPlcConnection> _plcConnections = new();
        private readonly object _lockObj = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configManager">配置管理器</param>
        /// <param name="plcConnectionFactory">PLC连接工厂</param>
        /// <param name="dataPublishers">数据发布器集合</param>
        public PlcDataService(
            IConfigManager configManager,
            IPlcConnectionFactory plcConnectionFactory,
            IEnumerable<IDataPublisher> dataPublishers)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _plcConnectionFactory = plcConnectionFactory ?? throw new ArgumentNullException(nameof(plcConnectionFactory));
            _dataPublishers = dataPublishers ?? throw new ArgumentNullException(nameof(dataPublishers));
        }

        /// <summary>
        /// 获取所有PLC配置
        /// </summary>
        /// <returns>PLC配置列表</returns>
        public async Task<List<PlcConnectionConfig>> GetAllPlcConfigsAsync()
        {
            return await _configManager.LoadPlcConfigsAsync();
        }

        /// <summary>
        /// 添加或更新PLC配置
        /// </summary>
        /// <param name="config">PLC配置</param>
        /// <returns>是否成功</returns>
        public async Task<bool> AddOrUpdatePlcConfigAsync(PlcConnectionConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.IpAddress))
            {
                return false;
            }

            var configs = await _configManager.LoadPlcConfigsAsync();
            var existingConfig = configs.FirstOrDefault(c => c.IpAddress == config.IpAddress);

            if (existingConfig != null)
            {
                configs.Remove(existingConfig);
            }

            // 确保ID存在
            if (string.IsNullOrEmpty(config.Id))
            {
                config.Id = Guid.NewGuid().ToString();
            }

            configs.Add(config);

            // 如果配置启用，创建或更新连接
            if (config.Enabled)
            {
                lock (_lockObj)
                {
                    if (_plcConnections.ContainsKey(config.IpAddress))
                    {
                        _plcConnections[config.IpAddress].Disconnect();
                        _plcConnections.Remove(config.IpAddress);
                    }

                    _plcConnections[config.IpAddress] = _plcConnectionFactory.CreateConnection(config);
                }
            }
            // 如果配置禁用，移除连接
            else if (_plcConnections.ContainsKey(config.IpAddress))
            {
                lock (_lockObj)
                {
                    _plcConnections[config.IpAddress].Disconnect();
                    _plcConnections.Remove(config.IpAddress);
                }
            }

            return await _configManager.SavePlcConfigsAsync(configs);
        }

        /// <summary>
        /// 删除PLC配置
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeletePlcConfigAsync(string plcIp)
        {
            if (string.IsNullOrEmpty(plcIp))
            {
                return false;
            }

            var configs = await _configManager.LoadPlcConfigsAsync();
            var configToRemove = configs.FirstOrDefault(c => c.IpAddress == plcIp);

            if (configToRemove == null)
            {
                return true;
            }

            configs.Remove(configToRemove);

            // 移除连接
            if (_plcConnections.ContainsKey(plcIp))
            {
                lock (_lockObj)
                {
                    _plcConnections[plcIp].Disconnect();
                    _plcConnections.Remove(plcIp);
                }
            }

            return await _configManager.SavePlcConfigsAsync(configs);
        }

        /// <summary>
        /// 导入PLC标签
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <param name="tags">标签列表</param>
        /// <returns>是否成功</returns>
        public async Task<bool> ImportPlcTagsAsync(string plcIp, List<PlcTag> tags)
        {
            if (string.IsNullOrEmpty(plcIp) || tags == null)
            {
                return false;
            }

            return await _configManager.SaveTagsForPlcAsync(plcIp, tags);
        }

        /// <summary>
        /// 获取PLC标签
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>标签列表</returns>
        public async Task<List<PlcTag>> GetPlcTagsAsync(string plcIp)
        {
            if (string.IsNullOrEmpty(plcIp))
            {
                return new List<PlcTag>();
            }

            return await _configManager.LoadTagsForPlcAsync(plcIp);
        }
        /// <summary>
        /// 获取PLC Root标签 包含 oee,ng,property信息
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>标签列表</returns>
        public async Task<List<PlcTag>> GetPlcTagRoots(string plcIp)
        {
            if (string.IsNullOrEmpty(plcIp))
            {
                return new List<PlcTag>();
            }
            return await _configManager.ParseJSONTags(plcIp);
        }   

        /// <summary>
        /// 读取PLC数据
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>PLC数据</returns>
        public async Task<PlcData> ReadPlcDataAsync(string plcIp)
        {
            var result = new PlcData { PlcIp = plcIp };

            if (string.IsNullOrEmpty(plcIp) || !_plcConnections.ContainsKey(plcIp))
            {
                result.Success = false;
                result.ErrorMessage = "PLC连接不存在";
                return result;
            }

            try
            {
                var connection = _plcConnections[plcIp];
                var tags = await GetPlcTagRoots(plcIp);

                if (tags.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "没有配置标签";
                    return result;
                }

                var readResult = await connection.ReadTagsAsync(tags);

                if (!readResult.IsSuccess)
                {
                    result.Success = false;
                    result.ErrorMessage = readResult.Message;
                    return result;
                }

                result.Success = true;
                result.Data = readResult.Content;

                // 发布数据
                await PublishDataAsync(result);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// 读取所有PLC数据
        /// </summary>
        /// <returns>所有PLC数据</returns>
        public async Task<List<PlcData>> ReadAllPlcDataAsync()
        {
            var results = new List<PlcData>();
            var tasks = _plcConnections.Keys.Select(ip => ReadPlcDataAsync(ip));

            results.AddRange(await Task.WhenAll(tasks));

            return results;
        }

        /// <summary>
        /// 发布数据到所有发布器
        /// </summary>
        /// <param name="plcData">PLC数据</param>
        private async Task PublishDataAsync(PlcData plcData)
        {
            foreach (var publisher in _dataPublishers)
            {
                try
                {
                    await publisher.PublishDataAsync(plcData);
                }
                catch
                {
                    // 单个发布器失败不影响其他发布器
                }
            }
        }
    }
}
