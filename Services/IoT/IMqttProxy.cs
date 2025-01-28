using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Commands;

namespace UpdateClientService.API.Services.IoT
{
    public interface IMqttProxy
    {
        Task<bool> PublishAwsRuleActionAsync(
          string ruleTopicName,
          string payload,
          bool logMessage = true,
          QualityOfServiceLevel qualityOfServiceLevel = QualityOfServiceLevel.AtMostOnce);

        Task<bool> PublishIoTCommandAsync(string topicName, IoTCommandModel request);

        Task<bool> CheckConnectionAsync();

        Task Disconnect();
    }
}
