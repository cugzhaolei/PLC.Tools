using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Model
{
    /// <summary>
    /// PLC标签集合的根对象
    /// </summary>
    public class PlcTagRoot
    {
        /// <summary>
        /// OEE相关标签集合
        /// </summary>
        [JsonProperty("OEE")]
        public List<PlcTag> OeeTags { get; set; } = new List<PlcTag>();

        /// <summary>
        /// 不良计数相关标签集合
        /// </summary>
        [JsonProperty("NGCount")]
        public List<PlcTag> NgCountTags { get; set; } = new List<PlcTag>();

        /// <summary>
        /// 设备属性相关标签集合
        /// </summary>
        [JsonProperty("Property")]
        public List<PlcTag> PropertyTags { get; set; } = new List<PlcTag>();
    }

    /// <summary>
    /// PLC标签模型类
    /// </summary>
    public class PlcTag
    {
        /// <summary>
        /// 标签名称
        /// </summary>
        [JsonProperty("Name")]
        public string Name { get; set; }

        /// <summary>
        /// 标签地址
        /// </summary>
        [JsonProperty("Address")]
        public string Address { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonProperty("DataType")]
        public string DataType { get; set; }

        /// <summary>
        /// 数据长度
        /// </summary>
        [JsonProperty("DataLength")]
        public int DataLength { get; set; }

        /// <summary>
        /// 字符串编码方式
        /// </summary>
        [JsonProperty("StringEncode")]
        public string StringEncode { get; set; }
        /// <summary>
        /// 父目录名称
        /// </summary>
        [JsonProperty("ParentFolderName")]
        public string ParentFolderName { get; set; }
    }

    /// <summary>
    /// PLC标签JSON解析器
    /// </summary>
    public class PlcTagParser
    {
        /// <summary>
        /// 从文件解析PLC标签
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns>解析后的PLC标签集合</returns>
        public List<PlcTagRoot> ParseFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("PLC标签文件不存在", filePath);
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                return ParseFromString(jsonContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("解析PLC标签文件失败", ex);
            }
        }

        /// <summary>
        /// 从JSON字符串解析PLC标签
        /// </summary>
        /// <param name="jsonContent">JSON字符串内容</param>
        /// <returns>解析后的PLC标签集合</returns>
        public List<PlcTagRoot> ParseFromString(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
            {
                throw new ArgumentException("JSON内容不能为空", nameof(jsonContent));
            }

            try
            {
                // 解析JSON数组为标签根对象集合
                return JsonConvert.DeserializeObject<List<PlcTagRoot>>(jsonContent);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("JSON格式错误，解析失败", ex);
            }
        }

        /// <summary>
        /// 将PLC标签对象序列化为JSON字符串
        /// </summary>
        /// <param name="tagRoots">PLC标签根对象集合</param>
        /// <returns>JSON字符串</returns>
        public string SerializeToString(List<PlcTagRoot> tagRoots)
        {
            if (tagRoots == null)
            {
                throw new ArgumentNullException(nameof(tagRoots));
            }

            return JsonConvert.SerializeObject(tagRoots, Formatting.Indented);
        }

        /// <summary>
        /// 将PLC标签对象保存为JSON文件
        /// </summary>
        /// <param name="tagRoots">PLC标签根对象集合</param>
        /// <param name="filePath">保存路径</param>
        public void SaveToFile(List<PlcTagRoot> tagRoots, string filePath)
        {
            if (tagRoots == null)
            {
                throw new ArgumentNullException(nameof(tagRoots));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("文件路径不能为空", nameof(filePath));
            }

            try
            {
                string jsonContent = SerializeToString(tagRoots);
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("保存PLC标签文件失败", ex);
            }
        }
    }
}
