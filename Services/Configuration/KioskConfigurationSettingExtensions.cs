using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UpdateClientService.API.Services.IoT.Certificate.Security;

namespace UpdateClientService.API.Services.Configuration
{
    public static class KioskConfigurationSettingExtensions
    {
        public static T GetConfigValue<T>(
          this KioskConfigurationSettings kioskConfigurationSettings,
          string categoryName,
          string settingName,
          T defaultValue,
          ILogger logger,
          IEncryptionService encryptionService)
        {
            T configValue = defaultValue;
            try
            {
                KioskSettingValue kioskSettingValue = KioskConfigurationSettingExtensions.InnerGetKioskSettingValue(kioskConfigurationSettings, categoryName, settingName);
                if (kioskSettingValue != null)
                {
                    string result = kioskSettingValue.Value;
                    ConfigurationEncryptionType? encryptionType1 = kioskSettingValue.EncryptionType;
                    if (encryptionType1.HasValue)
                    {
                        IEncryptionService encryptionService1 = encryptionService;
                        encryptionType1 = kioskSettingValue.EncryptionType;
                        int encryptionType2 = (int)encryptionType1.Value;
                        string cipherText = result;
                        result = encryptionService1.Decrypt((ConfigurationEncryptionType)encryptionType2, cipherText).Result;
                    }
                    configValue = (T)Convert.ChangeType((object)result, typeof(T));
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError(ex, "Unable to GetConfigValue for category " + categoryName + " and setting " + settingName, Array.Empty<object>());
            }
            return configValue;
        }

        public static KioskSettingValue GetKioskSettingValue(
          this KioskConfigurationSettings kioskConfigurationSettings,
          string categoryName,
          string settingName)
        {
            return KioskConfigurationSettingExtensions.InnerGetKioskSettingValue(kioskConfigurationSettings, categoryName, settingName);
        }

        private static KioskSettingValue InnerGetKioskSettingValue(
          KioskConfigurationSettings kioskConfigurationSettings,
          string categoryName,
          string settingName)
        {
            KioskSettingValue kioskSettingValue = (KioskSettingValue)null;
            KioskSetting kioskSetting1;
            if (kioskConfigurationSettings == null)
            {
                kioskSetting1 = (KioskSetting)null;
            }
            else
            {
                List<KioskConfigurationCategory> categories = kioskConfigurationSettings.Categories;
                if (categories == null)
                {
                    kioskSetting1 = (KioskSetting)null;
                }
                else
                {
                    KioskConfigurationCategory configurationCategory = categories.FirstOrDefault<KioskConfigurationCategory>((Func<KioskConfigurationCategory, bool>)(x => x.Name.Equals(categoryName, StringComparison.CurrentCultureIgnoreCase)));
                    kioskSetting1 = configurationCategory != null ? configurationCategory.Settings.FirstOrDefault<KioskSetting>((Func<KioskSetting, bool>)(y => y.Name.Equals(settingName, StringComparison.CurrentCultureIgnoreCase))) : (KioskSetting)null;
                }
            }
            KioskSetting kioskSetting2 = kioskSetting1;
            if (kioskSetting2 != null)
                kioskSettingValue = kioskSetting2.SettingValues.Where<KioskSettingValue>((Func<KioskSettingValue, bool>)(x =>
                {
                    if (!(x.EffectiveDateTime <= DateTime.Now))
                        return false;
                    if (!x.ExpireDateTime.HasValue)
                        return true;
                    DateTime? expireDateTime = x.ExpireDateTime;
                    DateTime now = DateTime.Now;
                    return expireDateTime.HasValue && expireDateTime.GetValueOrDefault() > now;
                })).OrderByDescending<KioskSettingValue, long>((Func<KioskSettingValue, long>)(x => x.Rank)).ThenByDescending<KioskSettingValue, DateTime>((Func<KioskSettingValue, DateTime>)(x => x.EffectiveDateTime)).FirstOrDefault<KioskSettingValue>();
            return kioskSettingValue;
        }
    }
}
