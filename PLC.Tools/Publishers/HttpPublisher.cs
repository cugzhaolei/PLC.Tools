using Microsoft.AspNetCore.Mvc;
using PLC.Tools.Interfaces;
using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Publishers
{
    /// <summary>
    /// HTTP数据发布器（通过API提供数据）
    /// </summary>
    [ApiController]
    [Route("api/plc")]
    public class HttpPublisher : ControllerBase, IDataPublisher
    {
        private readonly IPlcDataService _plcDataService;
        private bool _isRunning;

        /// <summary>
        /// 发布名称
        /// </summary>
        public string Name => "HTTP Publisher";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="plcDataService">PLC数据服务</param>
        public HttpPublisher(IPlcDataService plcDataService)
        {
            _plcDataService = plcDataService ?? throw new ArgumentNullException(nameof(plcDataService));
        }

        /// <summary>
        /// 启动发布器
        /// </summary>
        /// <returns>是否成功</returns>
        public Task<bool> StartAsync()
        {
            _isRunning = true;
            return Task.FromResult(true);
        }

        /// <summary>
        /// 停止发布器
        /// </summary>
        public Task StopAsync()
        {
            _isRunning = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 发布PLC数据（HTTP发布器不需要主动发布，通过API查询）
        /// </summary>
        /// <param name="plcData">PLC数据</param>
        /// <returns>是否成功</returns>
        public Task<bool> PublishDataAsync(PlcData plcData)
        {
            // HTTP发布器不需要主动发布数据
            return Task.FromResult(true);
        }

        /// <summary>
        /// 获取所有PLC数据
        /// </summary>
        [HttpGet("data")]
        public async Task<ActionResult<List<PlcData>>> GetAllPlcData()
        {
            if (!_isRunning)
            {
                return StatusCode(503, "Service is not running");
            }

            var data = await _plcDataService.ReadAllPlcDataAsync();
            return Ok(data);
        }

        /// <summary>
        /// 获取指定PLC数据
        /// </summary>
        [HttpGet("data/{plcIp}")]
        public async Task<ActionResult<PlcData>> GetPlcData(string plcIp)
        {
            if (!_isRunning)
            {
                return StatusCode(503, "Service is not running");
            }

            var data = await _plcDataService.ReadPlcDataAsync(plcIp);
            return Ok(data);
        }

        /// <summary>
        /// 上传PLC标签
        /// </summary>
        [HttpPost("{plcIp}/tags")]
        public async Task<ActionResult> UploadTags(string plcIp, [FromBody] List<PlcTag> tags)
        {
            if (!_isRunning)
            {
                return StatusCode(503, "Service is not running");
            }

            if (tags == null || !tags.Any())
            {
                return BadRequest("Tags list is empty");
            }

            var result = await _plcDataService.ImportPlcTagsAsync(plcIp, tags);
            return result ? Ok() : StatusCode(500, "Failed to import tags");
        }

        /// <summary>
        /// 获取PLC标签
        /// </summary>
        [HttpGet("{plcIp}/tags")]
        public async Task<ActionResult<List<PlcTag>>> GetTags(string plcIp)
        {
            if (!_isRunning)
            {
                return StatusCode(503, "Service is not running");
            }

            var tags = await _plcDataService.GetPlcTagsAsync(plcIp);
            return Ok(tags);
        }

        /// <summary>
        /// 获取PLC标签,OEE,NG,Property
        /// </summary>
        [HttpGet("{plcIp}/root/tags")]
        public async Task<ActionResult<List<PlcTag>>> GetRootTags(string plcIp)
        {
            if (!_isRunning)
            {
                return StatusCode(503, "Service is not running");
            }

            var tags = await _plcDataService.GetPlcTagRoots(plcIp);
            return Ok(tags);
        }

        /// <summary>
        /// 添加或更新PLC配置
        /// </summary>
        [HttpPost("config")]
        public async Task<ActionResult> AddOrUpdatePlcConfig([FromBody] PlcConnectionConfig config)
        {
            if (!_isRunning)
            {
                return StatusCode(503, "Service is not running");
            }

            if (config == null || string.IsNullOrEmpty(config.IpAddress))
            {
                return BadRequest("Invalid PLC configuration");
            }

            var result = await _plcDataService.AddOrUpdatePlcConfigAsync(config);
            return result ? Ok() : StatusCode(500, "Failed to add or update PLC configuration");
        }

        /// <summary>
        /// 获取所有PLC配置
        /// </summary>
        [HttpGet("configs")]
        public async Task<ActionResult<List<PlcConnectionConfig>>> GetAllPlcConfigs()
        {
            if (!_isRunning)
            {
                return StatusCode(503, "Service is not running");
            }

            var configs = await _plcDataService.GetAllPlcConfigsAsync();
            return Ok(configs);
        }

        /// <summary>
        /// 删除PLC配置
        /// </summary>
        [HttpDelete("config/{plcIp}")]
        public async Task<ActionResult> DeletePlcConfig(string plcIp)
        {
            if (!_isRunning)
            {
                return StatusCode(503, "Service is not running");
            }

            var result = await _plcDataService.DeletePlcConfigAsync(plcIp);
            return result ? Ok() : StatusCode(500, "Failed to delete PLC configuration");
        }
    }
}
