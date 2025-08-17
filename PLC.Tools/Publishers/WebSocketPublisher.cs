using Microsoft.AspNetCore.Http;
using PLC.Tools.Interfaces;
using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PLC.Tools.Publishers
{
    /// <summary>
    /// WebSocket数据发布器
    /// </summary>
    public class WebSocketPublisher : IDataPublisher
    {
        private readonly List<WebSocket> _connectedClients = new();
        private readonly object _lockObj = new();
        private bool _isRunning;

        /// <summary>
        /// 发布名称
        /// </summary>
        public string Name => "WebSocket Publisher";

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

            lock (_lockObj)
            {
                foreach (var client in _connectedClients)
                {
                    try
                    {
                        client.Dispose();
                    }
                    catch
                    {
                        // 忽略关闭时的错误
                    }
                }
                _connectedClients.Clear();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 发布PLC数据
        /// </summary>
        /// <param name="plcData">PLC数据</param>
        /// <returns>是否成功</returns>
        public async Task<bool> PublishDataAsync(PlcData plcData)
        {
            if (!_isRunning || _connectedClients.Count == 0)
            {
                return false;
            }

            try
            {
                var payload = JsonSerializer.Serialize(plcData);
                var buffer = System.Text.Encoding.UTF8.GetBytes(payload);
                var tasks = new List<Task>();

                lock (_lockObj)
                {
                    foreach (var client in _connectedClients.ToList())
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            tasks.Add(client.SendAsync(
                                new ArraySegment<byte>(buffer, 0, buffer.Length),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None));
                        }
                        else
                        {
                            _connectedClients.Remove(client);
                        }
                    }
                }

                await Task.WhenAll(tasks);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理WebSocket连接
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        public async Task HandleWebSocketConnectionAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            lock (_lockObj)
            {
                _connectedClients.Add(webSocket);
            }

            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;

            try
            {
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                } while (!result.CloseStatus.HasValue && _isRunning);

                await webSocket.CloseAsync(
                    result.CloseStatus.Value,
                    result.CloseStatusDescription,
                    CancellationToken.None);
            }
            catch
            {
                // 忽略连接错误
            }
            finally
            {
                lock (_lockObj)
                {
                    _connectedClients.Remove(webSocket);
                }
            }
        }
    }
}
