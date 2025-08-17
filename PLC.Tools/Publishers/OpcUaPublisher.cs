using Opc.Ua;
using Opc.Ua.Server;
using PLC.Tools.Interfaces;
using PLC.Tools.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace PLC.Tools.Publishers
{
    /// <summary>
    /// OPC UA 数据发布器实现
    /// </summary>
    public class OpcUaPublisher : IDataPublisher, IDisposable
    {
        private readonly IPlcDataService _plcDataService;
        private readonly string _applicationName;
        private readonly ushort _serverPort;
        private StandardServer _server;
        //private PlcNodeManager _nodeManager;
        private bool _isRunning;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ApplicationConfiguration _config;

        public string Name => "OPC UA Publisher";

        public OpcUaPublisher(IPlcDataService plcDataService, string applicationName = "PlcDataCollector", ushort serverPort = 4840)
        {
            _plcDataService = plcDataService ?? throw new ArgumentNullException(nameof(plcDataService));
            _applicationName = applicationName;
            _serverPort = serverPort;
            _config = CreateApplicationConfiguration();
        }

        public async Task<bool> StartAsync()
        {
            if (_isRunning)
                return true;

            try
            {
                await _config.Validate(ApplicationType.Server);

                X509Certificate2 certificate = await LoadOrCreateCertificateAsync();
                _config.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;

                _server = new StandardServer();

                var serverInternal = _server as IServerInternal;


                //_server.AddNodeManager(new PlcNodeManagerFactory(_config));

                //_nodeManager = _server.NodeManagerFactories.OfType<PlcNodeManager>()!.FirstOrDefault();
                //if (_nodeManager == null)
                //{
                //    throw new Exception("无法创建PLC节点管理器");
                //}

                _server.Start(_config);
                _isRunning = true;

                Console.WriteLine($"OPC UA 服务器已启动 - 地址: {_config.ServerConfiguration.BaseAddresses.First()}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OPC UA 服务器启动失败: {ex.Message}");
                return false;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            _cts.Cancel();
            if (_server != null)
            {
                _server.Stop();
                _server.Dispose();
                _server = null;
            }

            //_nodeManager = null;
            _isRunning = false;
            Console.WriteLine("OPC UA 服务器已停止");
        }

        public Task<bool> PublishDataAsync(PlcData plcData)
        {
            if (!_isRunning )
                return Task.FromResult(false);

            try
            {
                //_nodeManager.UpdatePlcData(plcData);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OPC UA 数据发布失败: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private ApplicationConfiguration CreateApplicationConfiguration()
        {
            var config = new ApplicationConfiguration
            {
                ApplicationName = _applicationName,
                ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:{_applicationName}",
                ApplicationType = ApplicationType.Server,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault",
                        SubjectName = _applicationName
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\Trusted Peers"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\Rejected Certificates"
                    },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas
                {
                    MaxMessageSize = 4 * 1024 * 1024,
                    OperationTimeout = 15000
                },
                ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = new StringCollection
                    {
                        $"opc.tcp://{System.Net.Dns.GetHostName()}:{_serverPort}/{_applicationName}",
                        $"http://{System.Net.Dns.GetHostName()}:{_serverPort + 1}/{_applicationName}"
                    },
                    MinSubscriptionLifetime = 1000,
                    MaxSubscriptionLifetime = 300000
                }
            };

            config.CertificateValidator = new CertificateValidator();
            config.CertificateValidator.CertificateValidation += (s, e) =>
            {
                if (e.Error.StatusCode == Opc.Ua.StatusCodes.BadCertificateUntrusted)
                {
                    e.Accept = true;
                }
            };

            return config;
        }

        private async Task<X509Certificate2> LoadOrCreateCertificateAsync()
        {
            return await Task.Run(() =>
            {
                string storePath = _config.SecurityConfiguration.ApplicationCertificate.StorePath;
                string storeType = _config.SecurityConfiguration.ApplicationCertificate.StoreType;
                string subjectName = _config.SecurityConfiguration.ApplicationCertificate.SubjectName;

                string resolvedPath = Environment.ExpandEnvironmentVariables(storePath);
                X509Store store = null;

                try
                {
                    if (storeType.Equals("Directory", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Directory.Exists(resolvedPath))
                        {
                            Directory.CreateDirectory(resolvedPath);
                        }
                        store = new X509Store(resolvedPath);
                    }
                    else
                    {
                        store = new X509Store(storePath, StoreLocation.LocalMachine);
                    }

                    store.Open(OpenFlags.ReadOnly);

                    var certificates = store.Certificates.Find(
                        X509FindType.FindBySubjectName,
                        subjectName,
                        validOnly: false);

                    if (certificates.Count > 0)
                    {
                        return certificates[0];
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载现有证书失败: {ex.Message}");
                }
                finally
                {
                    store?.Close();
                }

                Console.WriteLine("创建新的服务器证书...");
                var newCertificate = CreateNewCertificate();

                try
                {
                    if (storeType.Equals("Directory", StringComparison.OrdinalIgnoreCase))
                    {
                        store = new X509Store(resolvedPath);
                    }
                    else
                    {
                        store = new X509Store(storePath, StoreLocation.LocalMachine);
                    }
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(newCertificate);
                    Console.WriteLine("新证书已保存到存储");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"保存证书失败: {ex.Message}");
                }
                finally
                {
                    store?.Close();
                }

                return newCertificate;
            });
        }

        private X509Certificate2 CreateNewCertificate()
        {
          return (X509Certificate2)CertificateFactory.CreateCertificate(
                applicationUri: _config.ApplicationUri,
                applicationName:"plc.melsec",
                subjectName: _config.SecurityConfiguration.ApplicationCertificate.SubjectName,
                domainNames: new string[] { _config.ApplicationName, System.Net.Dns.GetHostName() }
            );
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts.Dispose();
                if (_isRunning)
                {
                    StopAsync().Wait();
                }
            }
        }

        ~OpcUaPublisher()
        {
            Dispose(false);
        }
    }


    ///// <summary>
    ///// PLC节点管理器工厂（修正接口实现的返回类型）
    ///// </summary>
    //public class PlcNodeManagerFactory : INodeManagerFactory
    //{
    //    private readonly ApplicationConfiguration _configuration;
    //    private readonly StringCollection _namespacesUris;  // 修正：使用StringCollection类型

    //    public PlcNodeManagerFactory(ApplicationConfiguration configuration)
    //    {
    //        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    //        // 修正：初始化StringCollection并添加命名空间URI
    //        _namespacesUris = new StringCollection();
    //        _namespacesUris.Add("http://plc-data-collector/opcua"); // 与节点管理器中的命名空间一致
    //    }

    //    // 修正：实现接口要求的StringCollection返回类型
    //    public StringCollection NamespacesUris => _namespacesUris;

    //    /// <summary>
    //    /// 创建节点管理器实例
    //    /// </summary>
    //    //public INodeManager Create(IServerInternal server, ApplicationConfiguration configuration)
    //    //{
    //    //    return new PlcNodeManager(server, _configuration);
    //    //}
    //}

    ///// <summary>
    ///// PLC数据节点管理器
    ///// </summary>
    //public class PlcNodeManager : NodeManagerBase
    //{
    //    private readonly Dictionary<string, FolderState> _plcFolders = new Dictionary<string, FolderState>();
    //    private readonly object _lock = new object();
    //    private readonly ushort _namespaceIndex;
    //    private FolderState _rootFolder;

    //    public PlcNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    //        : base(server, configuration)
    //    {
    //        _namespaceIndex = AddNamespace("http://plc-data-collector/opcua");
    //    }

    //    public ushort NamespaceIndex => _namespaceIndex;

    //    protected override NodeStateCollection CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    //    {
    //        var addressSpace = new NodeStateCollection();

    //        _rootFolder = new FolderState(null)
    //        {
    //            NodeId = new NodeId("PlcRoot", NamespaceIndex),
    //            BrowseName = new QualifiedName("PlcRoot", NamespaceIndex),
    //            DisplayName = new LocalizedText("PLC Data Root"),
    //            Description = new LocalizedText("Root folder for PLC data"),
    //            WriteMask = AttributeWriteMask.None,
    //            UserWriteMask = AttributeWriteMask.None,
    //            EventNotifier = EventNotifier.None
    //        };

    //        addressSpace.Add(_rootFolder);

    //        _rootFolder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
    //        if (!externalReferences.ContainsKey(_rootFolder.NodeId))
    //        {
    //            externalReferences.Add(_rootFolder.NodeId, new List<IReference>());
    //        }
    //        externalReferences[_rootFolder.NodeId].Add(
    //            new Reference(ReferenceTypeIds.Organizes, false, ObjectIds.ObjectsFolder));

    //        return addressSpace;
    //    }

    //    public void UpdatePlcData(PlcData plcData)
    //    {
    //        if (plcData == null || !plcData.Success)
    //            return;

    //        lock (_lock)
    //        {
    //            var plcFolder = GetOrCreatePlcFolder(plcData.PlcIp);

    //            UpdateDataNode(plcFolder, "Timestamp", plcData.Timestamp.ToString("o"), DataTypeIds.String);

    //            foreach (var (tagName, value) in plcData.Data)
    //            {
    //                var dataType = GetDataType(value);
    //                UpdateDataNode(plcFolder, tagName, value?.ToString() ?? "", dataType);
    //            }

    //            plcFolder.ClearChangeMasks(this.SystemContext, false);
    //            //OnNodeManagerChanged(NodeManagerChangeType.DataChanged, plcFolder.NodeId);
    //        }
    //    }

    //    private FolderState GetOrCreatePlcFolder(string plcIp)
    //    {
    //        if (_plcFolders.TryGetValue(plcIp, out var folder))
    //            return folder;

    //        var safeIp = plcIp.Replace(".", "_");
    //        folder = new FolderState(null)
    //        {
    //            NodeId = new NodeId($"PLC_{safeIp}", NamespaceIndex),
    //            BrowseName = new QualifiedName($"PLC_{safeIp}", NamespaceIndex),
    //            DisplayName = new LocalizedText(plcIp),
    //            Description = new LocalizedText($"Data from PLC: {plcIp}"),
    //            EventNotifier = EventNotifiers.None
    //        };

    //        _rootFolder.AddChild(folder);
    //        //AddPredefinedNode(this.SystemContext, folder);

    //        _plcFolders[plcIp] = folder;
    //        return folder;
    //    }

    //    private void UpdateDataNode(FolderState parent, string nodeName, string value, NodeId dataType)
    //    {
    //        var nodeId = new NodeId($"{parent.BrowseName.Name}_{nodeName}", NamespaceIndex);

    //        var node = FindPredefinedNode(this.SystemContext, nodeId) as BaseDataVariableState;

    //        if (node == null)
    //        {
    //            node = new BaseDataVariableState(parent)
    //            {
    //                NodeId = nodeId,
    //                BrowseName = new QualifiedName(nodeName, NamespaceIndex),
    //                DisplayName = new LocalizedText(nodeName),
    //                DataType = dataType,
    //                ValueRank = ValueRanks.Scalar,
    //                AccessLevel = AccessLevels.CurrentRead,
    //                UserAccessLevel = AccessLevels.CurrentRead,
    //                Historizing = false
    //            };

    //            parent.AddChild(node);
    //            //AddPredefinedNode(this.SystemContext, node);
    //        }

    //        node.Value = value;
    //        node.Timestamp = DateTime.UtcNow;
    //        //node.ClearChangeMasks(this.SystemContext, false);
    //    }

    //    private NodeId GetDataType(object value)
    //    {
    //        if (value == null)
    //            return DataTypeIds.String;

    //        var type = value.GetType();

    //        if (type == typeof(string)) return DataTypeIds.String;
    //        if (type == typeof(bool)) return DataTypeIds.Boolean;
    //        if (type == typeof(sbyte)) return DataTypeIds.SByte;
    //        if (type == typeof(byte)) return DataTypeIds.Byte;
    //        if (type == typeof(short)) return DataTypeIds.Int16;
    //        if (type == typeof(ushort)) return DataTypeIds.UInt16;
    //        if (type == typeof(int)) return DataTypeIds.Int32;
    //        if (type == typeof(uint)) return DataTypeIds.UInt32;
    //        if (type == typeof(long)) return DataTypeIds.Int64;
    //        if (type == typeof(ulong)) return DataTypeIds.UInt64;
    //        if (type == typeof(float)) return DataTypeIds.Float;
    //        if (type == typeof(double)) return DataTypeIds.Double;
    //        if (type == typeof(DateTime)) return DataTypeIds.DateTime;

    //        return DataTypeIds.String;
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        if (disposing)
    //        {
    //            _plcFolders.Clear();
    //        }
    //        base.Dispose(disposing);
    //    }
    //}
}