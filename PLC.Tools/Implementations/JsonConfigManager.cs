using PLC.Tools.Interfaces;
using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PLC.Tools.Implementations
{
    /// <summary>
    /// JSON配置管理器
    /// </summary>
    public class JsonConfigManager : IConfigManager
    {
        private readonly string _configDirectory;
        private readonly string _plcConfigsFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configDirectory">配置目录</param>
        public JsonConfigManager(string configDirectory = "Configs")
        {
            _configDirectory = configDirectory;
            _plcConfigsFilePath = Path.Combine(_configDirectory, "plc_configs.json");

            // 确保目录存在
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }

        /// <summary>
        /// 加载所有PLC连接配置
        /// </summary>
        /// <returns>PLC连接配置列表</returns>
        public async Task<List<PlcConnectionConfig>> LoadPlcConfigsAsync()
        {
            if (!File.Exists(_plcConfigsFilePath))
            {
                return new List<PlcConnectionConfig>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_plcConfigsFilePath);
                return JsonSerializer.Deserialize<List<PlcConnectionConfig>>(json, _jsonOptions) ??
                       new List<PlcConnectionConfig>();
            }
            catch
            {
                return new List<PlcConnectionConfig>();
            }
        }

        /// <summary>
        /// 保存PLC连接配置
        /// </summary>
        /// <param name="configs">PLC连接配置列表</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SavePlcConfigsAsync(List<PlcConnectionConfig> configs)
        {
            try
            {
                var json = JsonSerializer.Serialize(configs, _jsonOptions);
                await File.WriteAllTextAsync(_plcConfigsFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 加载指定PLC的标签
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>标签列表</returns>
        public async Task<List<PlcTag>> LoadTagsForPlcAsync(string plcIp)
        {
            var tagsFilePath = GetTagsFilePath(plcIp);

            if (!File.Exists(tagsFilePath))
            {
                return new List<PlcTag>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(tagsFilePath);
                return JsonSerializer.Deserialize<List<PlcTag>>(json, _jsonOptions) ??
                       new List<PlcTag>();
            }
            catch
            {
                return new List<PlcTag>();
            }
        }

        public async Task<List<PlcTag>> ParseJSONTags(string plcIp)
        {
            // 创建解析器实例
            var parser = new PlcTagParser();
            var filePath = GetTagsFilePath(plcIp);

            var tagList = new List<PlcTag>();
            try
            {
                // 从文件解析
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"文件不存在: {filePath}");
                    return tagList;
                }
                string jsonContent = await File.ReadAllTextAsync(filePath);
                var plcTags = JsonSerializer.Deserialize<List<PlcTagRoot>>(jsonContent, _jsonOptions) ??
                       new List<PlcTagRoot>();

                // 访问解析后的数据
                foreach (var tagRoot in plcTags)
                {
                    Console.WriteLine("OEE标签:");
                    foreach (var tag in tagRoot.OeeTags)
                    {
                        Console.WriteLine($"- {tag.Name} ({tag.DataType}, 长度: {tag.DataLength})");
                        tag.ParentFolderName = "OEE";
                        tagList.Add(tag);   
                    }

                    Console.WriteLine("NG计数标签:");
                    foreach (var tag in tagRoot.NgCountTags)
                    {
                        Console.WriteLine($"- {tag.Name} ({tag.DataType})");
                        tag.ParentFolderName = "NGCount";
                        tagList.Add(tag);
                    }
                }

                // 序列化并保存
                string json = parser.SerializeToString(plcTags);
                //parser.SaveToFile(plcTags, "PLCtags_backup.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析失败: {ex.Message}");
                return tagList;
            }


            return tagList;
        }

        /// <summary>
        /// 保存指定PLC的标签
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <param name="tags">标签列表</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SaveTagsForPlcAsync(string plcIp, List<PlcTag> tags)
        {
            try
            {
                var tagsFilePath = GetTagsFilePath(plcIp);
                var json = JsonSerializer.Serialize(tags, _jsonOptions);
                await File.WriteAllTextAsync(tagsFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取标签文件路径
        /// </summary>
        /// <param name="plcIp">PLC IP地址</param>
        /// <returns>文件路径</returns>
        private string GetTagsFilePath(string plcIp)
        {
            // 替换IP中的点，避免文件系统问题
            var safeIp = plcIp.Replace(".", "_");
            return Path.Combine(_configDirectory, $"{safeIp}_tags.json");
        }
    }
}
