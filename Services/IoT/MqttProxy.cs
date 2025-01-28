using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Certificate;
using UpdateClientService.API.Services.IoT.Commands;

namespace UpdateClientService.API.Services.IoT
{
    public class MqttProxy : IMqttProxy
    {
        private const int IoTCoreBrokerPort = 8883;
        private readonly ICertificateService _certService;
        private readonly ILogger<MqttProxy> _logger;
        private readonly IIotCommandDispatch _commandDispatch;
        private readonly IIoTStatisticsService _ioTStatisticsService;
        private readonly IStoreService _store;
        private readonly AppSettings _appSettings;
        private IMqttClient _iotClient;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private bool _isSubscribed;
        private bool _isRegistered;
        private int _inConnect;
        private IMqttClientOptions _clientOptions;
        private bool _isTimeSynced;

        public MqttProxy(
          ILogger<MqttProxy> logger,
          ICertificateService certService,
          IIotCommandDispatch commandDispatch,
          IStoreService store,
          IIoTStatisticsService ioTStatisticsService,
          IOptions<AppSettings> appSettings)
        {
            this._logger = logger;
            this._certService = certService;
            this._commandDispatch = commandDispatch;
            this._store = store;
            this._appSettings = appSettings.Value;
            this._ioTStatisticsService = ioTStatisticsService;
            this._iotClient = new MqttFactory().CreateMqttClient();
            MqttClientExtensions.UseApplicationMessageReceivedHandler(this._iotClient, (Func<MqttApplicationMessageReceivedEventArgs, Task>)(async e => await this.MessageReceivedHandler(e)));
            MqttClientExtensions.UseConnectedHandler(this._iotClient, (Func<MqttClientConnectedEventArgs, Task>)(async e => await this.ConnectedHandler(e)));
            MqttClientExtensions.UseDisconnectedHandler(this._iotClient, (Func<MqttClientDisconnectedEventArgs, Task>)(async e => await this.DisconnectedHandler(e)));
            this._logger.LogInfoWithSource("MqttProxy instantiated", ".ctor", "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
        }

        private async Task ConnectedHandler(MqttClientConnectedEventArgs e)
        {
            this._logger.LogInfoWithSource("Connected Result: " + e.ConnectResult.ToJson(), nameof(ConnectedHandler), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            if (e.ConnectResult.ResultCode != MqttClientConnectResultCode.Success)
                return;
            int num = await this._ioTStatisticsService.RecordConnectionSuccess() ? 1 : 0;
        }

        private async Task DisconnectedHandler(MqttClientDisconnectedEventArgs e)
        {
            if (e.Exception == null)
                this._logger.LogInfoWithSource(string.Format("Received DisconnectedHandler Event Reason: {0}", (object)e.Reason), nameof(DisconnectedHandler), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            else
                this._logger.LogErrorWithSource(e.Exception, string.Format("Received DisconnectedHandler Event Reason: {0}", (object)e.Reason), nameof(DisconnectedHandler), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            int num = await this._ioTStatisticsService.RecordDisconnection() ? 1 : 0;
        }

        private async Task MessageReceivedHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            byte[] payload = e?.ApplicationMessage?.Payload;
            if (payload == null)
            {
                this._logger.LogInfoWithSource(string.Format("Message received Topic = {0}, Payload Length = 0, QoS = {1}, Retain = {2}", (object)e.ApplicationMessage?.Topic, (object)e?.ApplicationMessage?.QualityOfServiceLevel, (object)e?.ApplicationMessage?.Retain), nameof(MessageReceivedHandler), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            }
            else
            {
                string str = Encoding.UTF8.GetString(payload);
                this._logger.LogInfoWithSource(string.Format("Message received Topic = {0}, Payload Length = {1}, QoS = {2}, Retain = {3}", (object)e?.ApplicationMessage?.Topic, (object)(str != null ? str.Length : 0), (object)e?.ApplicationMessage?.QualityOfServiceLevel, (object)e?.ApplicationMessage?.Retain), nameof(MessageReceivedHandler), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            }
            await Task.Run((async () => await this._commandDispatch.Execute(payload, e?.ApplicationMessage?.Topic)));
        }

        private IEnumerable<string> Topics
        {
            get
            {
                return new List<string>()
        {
          string.Format("redbox/kiosk/{0}/command", (object) this._store.KioskId),
          "redbox/kiosk/market/" + this._store.Market + "/command",
          "redbox/kiosk/all/command"
        };
            }
        }

        public async Task<bool> PublishIoTCommandAsync(string topicName, IoTCommandModel request)
        {
            return await this.PublishMessageAsync(topicName, request.ToJson(), ((request != null) ? request.LogRequest : null) ?? true, request.QualityOfServiceLevel);
        }

        public async Task<bool> PublishAwsRuleActionAsync(
      string ruleName,
      string payload,
      bool logMessage = true,
      QualityOfServiceLevel qualityOfServiceLevel = QualityOfServiceLevel.AtMostOnce)
        {
            return await this.PublishMessageAsync(ruleName, payload, logMessage, qualityOfServiceLevel);
        }

        private async Task<bool> PublishMessageAsync(
          string topicName,
          string message,
          bool logMessage = true,
          QualityOfServiceLevel qualityOfServiceLevel = QualityOfServiceLevel.AtLeastOnce)
        {
            bool flag = false;
            if (!this.IsMqttConnected)
            {
                this._logger.LogInfoWithSource("Not connected. Cannot publish to topic: " + topicName, nameof(PublishMessageAsync), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                return false;
            }
            string str1;
            if (!logMessage)
            {
                string str2 = message;
                str1 = string.Format("Message length: {0}", (object)(str2 != null ? str2.Length : 0));
            }
            else
                str1 = "Message : " + message;
            string str3 = str1;
            this._logger.LogInfoWithSource("Publishing message to topic: " + topicName + " ." + str3, nameof(PublishMessageAsync), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            try
            {
                MqttApplicationMessage applicationMessage = new MqttApplicationMessageBuilder().WithTopic(topicName).WithPayload(message).WithQualityOfServiceLevel(qualityOfServiceLevel.ToMqttQOS()).WithRetainFlag(false).Build();
                using (CancellationTokenSource cancellationToken = new CancellationTokenSource(5000))
                {
                    Stopwatch timer = Stopwatch.StartNew();
                    MqttClientPublishResult clientPublishResult = await this._iotClient.PublishAsync(applicationMessage, cancellationToken.Token);
                    timer.Stop();
                    if (clientPublishResult.ReasonCode == MqttClientPublishReasonCode.Success)
                    {
                        flag = true;
                        this._logger.LogInfoWithSource(string.Format("Publish successfully for Packet Identifier: {0}, Topic: {1}, QoS Level {2}, Duration: {3}", (object)clientPublishResult.PacketIdentifier, (object)topicName, (object)qualityOfServiceLevel, (object)timer.ElapsedMilliseconds), nameof(PublishMessageAsync), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                    }
                    else
                    {
                        this._logger.LogErrorWithSource(string.Format("Publish failed for ReasonCode: {0} - {1}, Topic: {2}, QoS Level {3}, Duration: {4}", (object)clientPublishResult.ReasonCode, (object)clientPublishResult.ReasonString, (object)topicName, (object)qualityOfServiceLevel, (object)timer.ElapsedMilliseconds), nameof(PublishMessageAsync), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                        flag = false;
                    }
                    timer = (Stopwatch)null;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while publishing message to topic " + topicName, nameof(PublishMessageAsync), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                flag = false;
            }
            return flag;
        }

        private async Task<bool> SubscribeTopics(IEnumerable<string> topics)
        {
            this._logger.LogInfoWithSource("Attempting to subscribe to topics", "SubscribeTopics", "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            this._isSubscribed = false;
            bool isSubscribed;
            if ((topics != null && topics.Count<string>() == 0) || !this.IsMqttConnected)
            {
                isSubscribed = false;
            }
            else
            {
                this._logger.LogInfoWithSource("calling _iotClient.Subscribe for topics: " + topics.ToJson(), "SubscribeTopics", "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                MqttClientSubscribeOptionsBuilder builder = new MqttClientSubscribeOptionsBuilder();
                topics.ToList<string>().ForEach(delegate (string t)
                {
                    builder.WithTopicFilter(t, MqttQualityOfServiceLevel.AtLeastOnce, false, false, MqttRetainHandling.SendAtSubscribe);
                });
                try
                {
                    using (CancellationTokenSource cancellationToken = new CancellationTokenSource(5000))
                    {
                        MqttClientSubscribeResult mqttClientSubscribeResult = await this._iotClient.SubscribeAsync(builder.Build(), cancellationToken.Token);
                        MqttClientSubscribeResult mqttClientSubscribeResult2 = mqttClientSubscribeResult;
                        mqttClientSubscribeResult2.Items.ForEach(delegate (MqttClientSubscribeResultItem item)
                        {
                            this._logger.LogInformation(string.Format("Subscribe Topic: {0} Result: {1}", item.TopicFilter.Topic, item.ResultCode), Array.Empty<object>());
                        });
                        this._isSubscribed = mqttClientSubscribeResult2.Items.All((MqttClientSubscribeResultItem item) => item.ResultCode <= MqttClientSubscribeResultCode.GrantedQoS2);
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception occurred", "SubscribeTopics", "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                }
                if (this._isSubscribed)
                {
                    this._logger.LogInfoWithSource("Successfully subscribed to Topics: " + topics.ToJson(), "SubscribeTopics", "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                }
                else
                {
                    this._logger.LogErrorWithSource("Failed to subscription to Topics: " + topics.ToJson(), "SubscribeTopics", "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                }
                isSubscribed = this._isSubscribed;
            }
            return isSubscribed;
        }

        public async Task<bool> CheckConnectionAsync()
        {
            bool flag;
            if (Interlocked.CompareExchange(ref this._inConnect, 1, 0) == 1)
            {
                this._logger.LogError("Already in CheckConnectionAsync, exiting", Array.Empty<object>());
                flag = false;
            }
            else
            {
                try
                {
                    bool flag2 = await this.Connect();
                    flag = flag2;
                }
                finally
                {
                    this._inConnect = 0;
                }
            }
            return flag;
        }

        private bool IsMqttConnected
        {
            get
            {
                IMqttClient iotClient = this._iotClient;
                return iotClient != null && iotClient.IsConnected;
            }
        }

        private async Task<bool> Connect()
        {
            try
            {
                if (!this.IsMqttConnected)
                    await this.ConnectToMqtt();
                if (this.IsMqttConnected && !this._isSubscribed)
                {
                    int num = await this.SubscribeTopics(this.Topics) ? 1 : 0;
                }
                if (this.IsMqttConnected && this._isSubscribed && !this._isRegistered)
                    Task.WaitAll(new Task[2]
                    {
            (Task) this.RegisterUCSVersion(),
            this.SyncTimestampAndTimezone()
                    });
                return this.IsMqttConnected && this._isSubscribed && this._isRegistered;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception occured while attempting to connect MqttClient.", nameof(Connect), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                return false;
            }
        }

        public async Task Disconnect()
        {
            try
            {
                IMqttClient iotClient = this._iotClient;
                await (iotClient != null ? MqttClientExtensions.DisconnectAsync(iotClient) : (Task)null);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception occured while attempting to disconnect from MqttClient, this can happen if the connection lost already.", nameof(Disconnect), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            }
            this.Clear();
        }

        private void Clear()
        {
            this._isSubscribed = false;
            this._isRegistered = false;
        }

        private async Task<bool> Initialize()
        {
            try
            {
                if (this._clientOptions != null)
                    return true;
                IotCert certificateAsync = await this._certService.GetCertificateAsync();
                if (certificateAsync == null)
                {
                    this._logger.LogErrorWithSource("No cert, aborting connection attempt", nameof(Initialize), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                    return false;
                }
                this._clientOptions = new MqttClientOptionsBuilder().WithTcpServer(this._appSettings?.IoTBrokerEndpoint, new int?(8883)).WithKeepAlivePeriod(new TimeSpan(0, 0, 0, 300)).WithTls(new MqttClientOptionsBuilderTlsParameters()
                {
                    Certificates = (IEnumerable<X509Certificate>)new List<X509Certificate>()
          {
            certificateAsync.RootCa,
            (X509Certificate) certificateAsync.DeviceCertPfx
          },
                    SslProtocol = SslProtocols.Tls12,
                    UseTls = true,
                    AllowUntrustedCertificates = false
                }).WithProtocolVersion(MqttProtocolVersion.V311).WithClientId(this._store.KioskId.ToString()).WithCommunicationTimeout(TimeSpan.FromSeconds(60.0)).WithCleanSession().Build();
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception during Initialization", nameof(Initialize), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                return false;
            }
        }

        private async Task ConnectToMqtt()
        {
            this._logger.LogInfoWithSource(string.Format("Attempting to connect KioskId: {0} to mqtt", (object)this._store.KioskId), nameof(ConnectToMqtt), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            int num1 = await this._ioTStatisticsService.RecordConnectionAttempt() ? 1 : 0;
            try
            {
                this.Clear();
                if (!await this.Initialize())
                {
                    this._logger.LogErrorWithSource("Initialization failed", nameof(ConnectToMqtt), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                    return;
                }
                using (CancellationTokenSource cancellationToken = new CancellationTokenSource(20000))
                    this._logger.LogInfoWithSource(string.Format("MqttClient ConnectAsync completed with KioskId: {0}, and ResultCode: {1}", (object)this._store.KioskId, (object)(await this._iotClient.ConnectAsync(this._clientOptions, cancellationToken.Token)).ResultCode), nameof(ConnectToMqtt), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception occurred in ConnectToMqtt", nameof(ConnectToMqtt), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
                int num2 = await this._ioTStatisticsService.RecordConnectionException(ex?.Message + ex?.InnerException?.Message) ? 1 : 0;
                int num3 = await this._certService.Validate() ? 1 : 0;
                this._clientOptions = (IMqttClientOptions)null;
            }
        }

        private async Task<bool> RegisterUCSVersion()
        {
            this._isRegistered = false;
            this._logger.LogInfoWithSource("Attempting to register ucs", nameof(RegisterUCSVersion), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            UCSRegisterMessage message = new UCSRegisterMessage()
            {
                KioskId = this._store.KioskId,
                Version = "2.0",
                AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            };
            this._isRegistered = await this.PublishMessageAsync("$aws/rules/kioskucsregister", message.ToJson());
            if (this._isRegistered)
                this._logger.LogInfoWithSource("Register UCS version " + message.Version + " succeeded", nameof(RegisterUCSVersion), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            else
                this._logger.LogErrorWithSource("Register UCS version " + message.Version + " failed", nameof(RegisterUCSVersion), "/sln/src/UpdateClientService.API/Services/IoT/MqttProxy.cs");
            return this._isRegistered;
        }

        private async Task SyncTimestampAndTimezone()
        {
            if (this._isTimeSynced)
                return;
            int num = await this.PublishIoTCommandAsync("$aws/rules/kioskrestcall", new IoTCommandModel()
            {
                RequestId = Guid.NewGuid().ToString(),
                Command = CommandEnum.SyncTimestampAndTimezone,
                MessageType = MessageTypeEnum.Request,
                Version = 1,
                SourceId = this._store?.KioskId.ToString(),
                LogRequest = new bool?(true),
                QualityOfServiceLevel = QualityOfServiceLevel.AtLeastOnce
            }) ? 1 : 0;
            this._isTimeSynced = true;
        }
    }
}
