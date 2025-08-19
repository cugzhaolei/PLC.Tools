using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using PLC.Tools.Implementations;
using PLC.Tools.Interfaces;
using PLC.Tools.Publishers;
using PLC.Tools.Services;

// 确保使用正确的 WebApplication 创建方式
var builder = WebApplication.CreateBuilder(args);

// 添加控制器服务
builder.Services.AddControllers();

// 配置核心服务
builder.Services.AddSingleton<IConfigManager, JsonConfigManager>();
builder.Services.AddSingleton<IPlcConnectionFactory, MitsubishiPlcConnectionFactory>();
builder.Services.AddSingleton<IPlcDataService, PlcDataService>();

// 配置 WebSocket 发布器
builder.Services.AddSingleton<WebSocketPublisher>();

// 配置数据发布器
builder.Services.AddSingleton<IDataPublisher, HttpPublisher>();
builder.Services.AddSingleton<IDataPublisher>(sp =>new MqttPublisher("localhost", 1883));
//builder.Services.AddSingleton<IDataPublisher, OpcUaPublisher>();

// 构建应用程序
var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// 配置 WebSocket 支持
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

// 配置 WebSocket 端点
var webSocketPublisher = app.Services.GetRequiredService<WebSocketPublisher>();
app.Map("/ws", async context => await webSocketPublisher.HandleWebSocketConnectionAsync(context));

// 配置 API 控制器路由
app.MapControllers();

// 启动所有数据发布器
var publishers = app.Services.GetServices<IDataPublisher>();
foreach (var publisher in publishers)
{
    await publisher.StartAsync();
}

// 运行应用程序
app.Run();