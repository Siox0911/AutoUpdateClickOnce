using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdateClickOnce.SettingsHelper
{
    /// <summary>
    /// Represents the settings export object.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn, ItemTypeNameHandling = TypeNameHandling.Auto)]
    internal class SettingsExport
    {
        /// <summary>
        /// Gets or sets the list of settings.
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        internal List<SettingObject>? Settings { get; set; }

        /// <summary>
        /// Gets or sets the settings key see Properties.Settings.Default.SettingsKey and <see cref="System.Configuration.ApplicationSettingsBase.SettingsKey"/>.
        /// For more information see <see href="https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/store-custom-information-config-file">
        /// Use Visual C# to store and retrieve custom information from an application configuration file</see>
        /// and <see href="https://learn.microsoft.com/en-us/visualstudio/deployment/clickonce-and-application-settings?view=vs-2022">ClickOnce and Application Settings</see>.
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        internal string? SettingsKey { get; set; }
    }
}
