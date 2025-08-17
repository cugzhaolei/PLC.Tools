using HslCommunication.MQTT;
using MQTTnet;
using MQTTnet.Client;

using PLC.Tools.Interfaces;
using PLC.Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PLC.Tools.Publishers
{
    public class MqttPublisher : IDataPublisher
    {
        private readonly IPlcDataService _plcDataService;
        private readonly string _brokerAddress;
        private readonly int _brokerPort;
        private IMqttClient _mqttClient;
        private bool _isRunning;

        public string Name => "MQTT Publisher";

        public MqttPublisher(IPlcDataService plcDataService, string brokerAddress = "localhost", int brokerPort = 1883)
        {
            _plcDataService = plcDataService ?? throw new ArgumentNullException(nameof(plcDataService));
            _brokerAddress = brokerAddress;
            _brokerPort = brokerPort;
        }

        public async Task<bool> StartAsync()
        {
            if (_isRunning)
                return true;

            try
            {
                // 直接创建客户端（不依赖MqttFactory，兼容新旧版本）
                _mqttClient = new MqttFactory().CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_brokerAddress, _brokerPort)
                    .WithCleanSession()
                    .Build();

                var connectResult = await _mqttClient.ConnectAsync(options);
                _isRunning = connectResult.ResultCode == MqttClientConnectResultCode.Success;
                return _isRunning;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT连接失败: {ex.Message}");
                return false;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning || _mqttClient == null)
                return;

            await _mqttClient.DisconnectAsync();
            _isRunning = false;
        }

        public async Task<bool> PublishDataAsync(PlcData plcData)
        {
            if (!_isRunning || _mqttClient == null || !_mqttClient.IsConnected)
                return false;

            try
            {
                var topic = $"plc/data/{plcData.PlcIp.Replace(".", "_")}";
                var payload = JsonSerializer.Serialize(plcData);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.PublishAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT发布失败: {ex.Message}");
                return false;
            }
        }
    }
}
