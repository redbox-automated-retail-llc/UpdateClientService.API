using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UpdateClientService.API.Services.IoT.Certificate.Security;

namespace UpdateClientService.API.Services.Configuration
{
    public abstract class BaseConfiguration
    {
        protected readonly ILogger<BaseConfiguration> _logger;
        protected readonly IEncryptionService _encryptionService;
        protected KioskConfigurationSettings _kioskConfigurationSettings;
        private Dictionary<(string categoryName, string settingName), object> _defaultValues;
        protected IOptionsMonitor<KioskConfigurationSettings> _optionsMonitorKioskConfigurationSettings;

        public BaseConfiguration(
          ILogger<BaseConfiguration> logger,
          IOptions<KioskConfigurationSettings> optionsKioskConfigurationSettings,
          IEncryptionService encryptionService)
        {
            this._logger = logger;
            this._encryptionService = encryptionService;
            this._kioskConfigurationSettings = optionsKioskConfigurationSettings.Value;
            this.PopulateConfigurationSettingValues();
        }

        public BaseConfiguration(
          ILogger<BaseConfiguration> logger,
          IOptionsMonitor<KioskConfigurationSettings> optionsMonitorKioskConfigurationSettings,
          IEncryptionService encryptionService)
        {
            this._logger = logger;
            this._encryptionService = encryptionService;
            this._optionsMonitorKioskConfigurationSettings = optionsMonitorKioskConfigurationSettings;
            this._optionsMonitorKioskConfigurationSettings.OnChange(new Action<KioskConfigurationSettings, string>(this.OnChangeKioskConfigurationSettings));
            this._kioskConfigurationSettings = optionsMonitorKioskConfigurationSettings.CurrentValue;
            this.PopulateConfigurationSettingValues();
        }

        private void OnChangeKioskConfigurationSettings(
          KioskConfigurationSettings kioskConfigurationSettings,
          string data)
        {
            this._kioskConfigurationSettings = kioskConfigurationSettings;
            this.PopulateConfigurationSettingValues();
        }

        public long ConfigurationVersion
        {
            get
            {
                KioskConfigurationSettings configurationSettings = this._kioskConfigurationSettings;
                return configurationSettings == null ? 0L : configurationSettings.ConfigurationVersion;
            }
        }

        public void Log(ConfigLogParameters loggingParameters = null)
        {
            string str = string.Format("KioskConfiguration Settings for configuration version: {0}", (object)this._kioskConfigurationSettings.ConfigurationVersion);
            if (this._kioskConfigurationSettings.ConfigurationVersion == 0L)
                this._logger.LogWarning(str + "  KioskConfiguration is not current.", Array.Empty<object>());
            else
                this._logger.LogInformation(str, Array.Empty<object>());
            foreach (PropertyInfo property in this.GetType().GetProperties())
            {
                if (property.PropertyType.IsSubclassOf(typeof(BaseCategorySetting)))
                    this.LogCategorySettingValues(this.GetCategoryClassInstance(property), loggingParameters);
            }
        }

        private bool isSettingIncluded(
          object categoryClassInstance,
          PropertyInfo settingProperty,
          ConfigLogParameters loggingParameters)
        {
            string categoryName = this.GetCategoryName(categoryClassInstance);
            string name1 = categoryClassInstance.GetType().Name;
            string name2 = ((MemberInfo)settingProperty).Name;
            string settingName = this.GetSettingName(settingProperty);
            bool flag1 = loggingParameters?.IncludeCategories == null || !loggingParameters.IncludeCategories.Any<string>();
            int num1;
            if (loggingParameters?.IncludeCategories != null)
                num1 = loggingParameters.IncludeCategories.Intersect<string>((IEnumerable<string>)new string[2]
                {
          categoryName,
          name1
                }).Any<string>() ? 1 : 0;
            else
                num1 = 0;
            bool flag2 = num1 != 0;
            int num2;
            if (loggingParameters?.ExcludeCategories != null)
                num2 = loggingParameters.ExcludeCategories.Intersect<string>((IEnumerable<string>)new string[2]
                {
          categoryName,
          name1
                }).Any<string>() ? 1 : 0;
            else
                num2 = 0;
            bool flag3 = num2 != 0;
            int num3;
            if (loggingParameters?.IncludeSettings != null)
                num3 = loggingParameters.IncludeSettings.Intersect<(string, string)>((IEnumerable<(string, string)>)new (string, string)[4]
                {
          (categoryName, settingName),
          (categoryName, name2),
          (name1, settingName),
          (name1, name2)
                }).Any<(string, string)>() ? 1 : 0;
            else
                num3 = 0;
            bool flag4 = num3 != 0;
            int num4;
            if (loggingParameters?.ExcludeSettings != null)
                num4 = loggingParameters.ExcludeSettings.Intersect<(string, string)>((IEnumerable<(string, string)>)new (string, string)[4]
                {
          (categoryName, settingName),
          (categoryName, name2),
          (name1, settingName),
          (name1, name2)
                }).Any<(string, string)>() ? 1 : 0;
            else
                num4 = 0;
            bool flag5 = num4 != 0;
            if (flag4)
                return true;
            return ((!flag1 ? 0 : (!flag3 ? 1 : 0)) | (flag2 ? 1 : 0)) != 0 && !flag5;
        }

        private void LogCategorySettingValues(
          object categoryClassInstance,
          ConfigLogParameters loggingParameters = null)
        {
            if (categoryClassInstance == null)
                return;
            string categoryName = this.GetCategoryName(categoryClassInstance);
            foreach (PropertyInfo property in categoryClassInstance.GetType().GetProperties())
            {
                if (property.CanWrite && ((MemberInfo)property).Name != "ParentKioskConfiguration" && this.isSettingIncluded(categoryClassInstance, property, loggingParameters))
                {
                    string settingName = this.GetSettingName(property);
                    object settingValue = (object)null;
                    bool flag1 = false;
                    KioskSettingValue kioskSettingValue = this._kioskConfigurationSettings.GetKioskSettingValue(categoryName, settingName);
                    if (kioskSettingValue != null)
                    {
                        try
                        {
                            string cipherText = kioskSettingValue.Value;
                            bool flag2 = true;
                            if (kioskSettingValue.EncryptionType.HasValue)
                            {
                                if (this.GetMaskLogValueAttribute(property) != null)
                                {
                                    cipherText = this._encryptionService.Decrypt(kioskSettingValue.EncryptionType.Value, cipherText).Result;
                                }
                                else
                                {
                                    flag2 = false;
                                    cipherText = "[Encrypted]";
                                }
                            }
                            settingValue = flag2 ? Convert.ChangeType((object)cipherText, property.PropertyType) : (object)cipherText;
                        }
                        catch (Exception ex)
                        {
                            flag1 = true;
                            this._logger.LogErrorWithSource(ex, "Unable to get " + categoryName + " property " + settingName, nameof(LogCategorySettingValues), "/sln/src/UpdateClientService.API/Services/Configuration/BaseConfiguration.cs");
                        }
                    }
                    else
                        flag1 = true;
                    if (flag1)
                    {
                        try
                        {
                            object obj;
                            if (this._defaultValues.TryGetValue((categoryName, settingName), out obj))
                                settingValue = obj;
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogErrorWithSource(ex, "Unable to get " + categoryName + " property " + settingName + " with default value.", nameof(LogCategorySettingValues), "/sln/src/UpdateClientService.API/Services/Configuration/BaseConfiguration.cs");
                        }
                    }
                    object obj1 = (object)this.MaskSettingValue(property, settingValue);
                    string str = flag1 ? " [Default value]" : (string)null;
                    this._logger.LogInformation(string.Format("KioskConfiguration.{0}.{1} = {2}{3}", (object)categoryName, (object)settingName, obj1, (object)str), Array.Empty<object>());
                }
            }
        }

        private string MaskSettingValue(PropertyInfo settingPropertyInfo, object settingValue)
        {
            string str = string.Format("{0}", settingValue);
            MaskLogValue logValueAttribute = this.GetMaskLogValueAttribute(settingPropertyInfo);
            if (logValueAttribute != null && !string.IsNullOrEmpty(str))
            {
                int count = Math.Max(0, str.Length - logValueAttribute.VisibleChars);
                int startIndex = Math.Max(0, str.Length - logValueAttribute.VisibleChars);
                str = new string('X', count) + str.Substring(startIndex);
            }
            return str;
        }

        private MaskLogValue GetMaskLogValueAttribute(PropertyInfo propertyInfo)
        {
            return (MaskLogValue)Attribute.GetCustomAttribute((MemberInfo)propertyInfo, typeof(MaskLogValue));
        }

        private void PopulateConfigurationSettingValues()
        {
            try
            {
                if (this._optionsMonitorKioskConfigurationSettings != null)
                    this._kioskConfigurationSettings = this._optionsMonitorKioskConfigurationSettings.CurrentValue;
                bool populateDefaultValues = this._defaultValues == null;
                if (populateDefaultValues)
                    this._defaultValues = new Dictionary<(string, string), object>();
                foreach (PropertyInfo property in this.GetType().GetProperties())
                {
                    if (property.PropertyType.IsSubclassOf(typeof(BaseCategorySetting)))
                        this.PopulateCategorySettingValues(this.GetCategoryClassInstance(property), populateDefaultValues);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while populating configuration setting values.", nameof(PopulateConfigurationSettingValues), "/sln/src/UpdateClientService.API/Services/Configuration/BaseConfiguration.cs");
            }
        }

        private void PopulateCategorySettingValues(
          object categoryClassInstance,
          bool populateDefaultValues)
        {
            if (categoryClassInstance == null)
                return;
            string categoryName = this.GetCategoryName(categoryClassInstance);
            foreach (PropertyInfo property in categoryClassInstance.GetType().GetProperties())
            {
                if (property.CanWrite)
                {
                    string settingName = this.GetSettingName(property);
                    if (populateDefaultValues)
                        this._defaultValues[(categoryName, settingName)] = property.GetValue(categoryClassInstance, (object[])null);
                    bool flag = false;
                    KioskSettingValue kioskSettingValue = this._kioskConfigurationSettings.GetKioskSettingValue(categoryName, settingName);
                    if (kioskSettingValue != null)
                    {
                        try
                        {
                            string result = kioskSettingValue.Value;
                            ConfigurationEncryptionType? encryptionType1 = kioskSettingValue.EncryptionType;
                            if (encryptionType1.HasValue)
                            {
                                IEncryptionService encryptionService = this._encryptionService;
                                encryptionType1 = kioskSettingValue.EncryptionType;
                                int encryptionType2 = (int)encryptionType1.Value;
                                string cipherText = result;
                                result = encryptionService.Decrypt((ConfigurationEncryptionType)encryptionType2, cipherText).Result;
                            }
                            object obj = Convert.ChangeType((object)result, property.PropertyType);
                            property.SetValue(categoryClassInstance, obj, (object[])null);
                        }
                        catch (Exception ex)
                        {
                            flag = true;
                            this._logger.LogErrorWithSource(ex, "Unable to set " + categoryName + " property " + settingName, nameof(PopulateCategorySettingValues), "/sln/src/UpdateClientService.API/Services/Configuration/BaseConfiguration.cs");
                        }
                    }
                    else
                        flag = true;
                    if (flag)
                    {
                        try
                        {
                            object obj;
                            if (this._defaultValues.TryGetValue((categoryName, settingName), out obj))
                                property.SetValue(categoryClassInstance, obj, (object[])null);
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogErrorWithSource(ex, "Unable to set " + categoryName + " property " + settingName + " with default value.", nameof(PopulateCategorySettingValues), "/sln/src/UpdateClientService.API/Services/Configuration/BaseConfiguration.cs");
                        }
                    }
                }
            }
        }

        private string GetSettingName(PropertyInfo settingPropertyInfo)
        {
            string name = ((MemberInfo)settingPropertyInfo).Name;
            SettingAttribute customAttribute = (SettingAttribute)Attribute.GetCustomAttribute((MemberInfo)settingPropertyInfo, typeof(SettingAttribute));
            if (customAttribute != null)
                name = customAttribute.Name;
            return name;
        }

        private string GetCategoryName(object categoryClassInstance)
        {
            string name = categoryClassInstance.GetType().Name;
            CategoryAttribute customAttribute = (CategoryAttribute)Attribute.GetCustomAttribute((MemberInfo)categoryClassInstance.GetType(), typeof(CategoryAttribute));
            if (!string.IsNullOrEmpty(customAttribute?.Name))
                name = customAttribute?.Name;
            return name;
        }

        private object GetCategoryClassInstance(PropertyInfo categoryClassPropertyInfo)
        {
            object instance = categoryClassPropertyInfo?.GetValue((object)this, (object[])null);
            if (instance == null)
            {
                try
                {
                    instance = Activator.CreateInstance(categoryClassPropertyInfo.PropertyType);
                    if (instance != null)
                        categoryClassPropertyInfo.SetValue((object)this, instance);
                    else
                        this._logger.LogErrorWithSource("Unable to instantiate configuration category class " + categoryClassPropertyInfo?.PropertyType?.Name, nameof(GetCategoryClassInstance), "/sln/src/UpdateClientService.API/Services/Configuration/BaseConfiguration.cs");
                    this.SetParentKioskConfiguration(instance);
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Unable to instantiate configuration category class " + categoryClassPropertyInfo?.PropertyType?.Name, nameof(GetCategoryClassInstance), "/sln/src/UpdateClientService.API/Services/Configuration/BaseConfiguration.cs");
                }
            }
            return instance;
        }

        private void SetParentKioskConfiguration(object categoryClassInstance)
        {
            if (categoryClassInstance == null)
                return;

            PropertyInfo property = categoryClassInstance.GetType().GetProperty("ParentKioskConfiguration");
            if (property == null || property.GetValue(categoryClassInstance) != null)
                return;

            property.SetValue(categoryClassInstance, this);
        }
    }
}
