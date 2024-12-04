using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdateClickOnce.SettingsHelper
{
    /// <summary>
    /// Hilfsklasse zum import und export von Einstellungen.
    /// <para>
    /// Helper class for importing and exporting settings.
    /// </para>
    /// </summary>
    [JsonObject(MemberSerialization.OptIn, ItemTypeNameHandling = TypeNameHandling.Auto)]
    internal class SettingObject
    {
        /// <summary>
        /// Der Name der Einstellung.
        /// <para>
        /// The name of the setting.
        /// </para>
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        internal string? Name { get; set; }

        /// <summary>
        /// Der Wert der Einstellung
        /// <para>
        /// The value of the setting
        /// </para>
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        internal object? Value { get; set; }

        /// <summary>
        /// Der Typ der Einstellung, des Objektes
        /// <para>
        /// The type of the setting, the object
        /// </para>
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        internal Type? Type { get; set; }
    }
}
