using MQTTnet.Protocol;

namespace UpdateClientService.API.Services.IoT
{
    public static class IotMqttExtensions
    {
        public static MqttQualityOfServiceLevel ToMqttQOS(this QualityOfServiceLevel qos)
        {
            if (qos == QualityOfServiceLevel.AtMostOnce)
                return MqttQualityOfServiceLevel.AtMostOnce;
            return MqttQualityOfServiceLevel.AtLeastOnce;
        }
    }
}
