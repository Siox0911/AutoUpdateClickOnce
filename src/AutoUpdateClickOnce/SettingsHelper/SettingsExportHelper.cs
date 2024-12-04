using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoUpdateClickOnce.SettingsHelper
{
    /// <summary>
    /// Hilft dabei die Einstellungen zu exportieren und importieren. Die Klasse kann nur mit <see cref="Properties.Settings.Default"/> verwendet werden.
    /// <para>
    /// Helps to export and import the settings. The class can only be used with <see cref="Properties.Settings.Default"/>.
    /// </para>
    /// </summary>
    public class SettingsExportHelper
    {
        #region private fields

        /// <summary>
        /// The settings to be exported. Properties.Settings.Default
        /// </summary>
        private readonly ApplicationSettingsBase applicationBaseSettings;

        /// <summary>
        /// The file name to which the settings should be exported.
        /// </summary>
        private readonly string filename;

        /// <summary>
        /// The settings export object.
        /// </summary>
        private SettingsExport? settingsExport;

        #endregion private fields

        #region Constructor

        /// <summary>
        /// Standard Konstruktor.
        /// <para>
        /// Standard constructor.
        /// </para>
        /// </summary>
        /// <param name="settings">Die Einstellungen, die exportiert werden sollen. Properties.Settings.Default
        /// <para>
        /// The settings to be exported. Properties.Settings.Default
        /// </para>
        /// </param>
        /// <param name="filename">Der Dateiname, in den die Einstellungen exportiert werden sollen.
        /// <para>
        /// The file name to which the settings should be exported.
        /// </para>
        /// </param>
        public SettingsExportHelper(ApplicationSettingsBase @settings, string filename)
        {
            if (string.IsNullOrEmpty(filename) || string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"\"{nameof(filename)}\" can't be null or empty.", nameof(filename));
            }

            applicationBaseSettings = @settings ?? throw new ArgumentNullException(nameof(settings));
            this.filename = filename;
            settingsExport = new SettingsExport
            {
                Settings = []
            };
        }

        #endregion Constructor

        #region public methods

        /// <summary>
        /// Serialisiert die Einstellungen. Erstellt von den aktuellen Einstellungen eine
        /// Liste. Diese Liste wird dann in ein Json string umgewandelt und in die Datei geschrieben, die im Konstruktor angegeben wurde.
        /// <para>
        /// Serializes the settings. Creates a list from the current settings.
        /// This list is then converted into a Json string and written to the file specified in the constructor.
        /// If you use SettingsKey, you may be interested in the following: <see href="https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/store-custom-information-config-file"/>.
        /// </para>
        /// </summary>
        /// <param name="formated">Gibt an, ob der generierte JSON-String formatiert werden soll.
        /// <para>
        /// If true, the resultant JSON will be formatted for easy human interpretation (whitespace and carriage returns added).
        /// </para>
        /// </param>
        /// <returns></returns>
        public void Export(bool formated = false)
        {
            if (settingsExport != null)
            {
                GenerateDataForExport();
                var json = Serializer.JsonSerializer.SerializeObjectWithTypes(settingsExport, formated);

                var fi = new FileInfo(filename);
                if (fi.Exists)
                {
                    fi.Delete();
                }
                using (var fStream = new FileStream(filename, FileMode.CreateNew))
                {
                    using (var sWriter = new StreamWriter(fStream))
                    {
                        sWriter.Write(json);
                    }
                }
            }
        }

        /// <summary>
        /// Deserialisiert die Json Daten zurück in die <seealso cref="SettingsExportHelper"/> Klasse.
        /// Setzt die aktuellen Daten in <see cref="Properties.Settings.Default"/>. Im Anschluss sollten die Einstellungen geladen werden. z.B. <code>LoadSettings()</code>
        /// <para>
        /// Deserializes the Json data back into the <seealso cref="SettingsExportHelper"/> class.
        /// Sets the current data in <see cref="Properties.Settings.Default"/>. Afterwards, the settings should be loaded. e.g. <code>LoadSettings()</code>
        /// </para>
        /// </summary>
        /// <returns></returns>
        public void Import()
        {
            var fName = filename;

            if (File.Exists(fName))
            {
                using (var fReader = new FileStream(fName, FileMode.Open, FileAccess.Read))
                {
                    using (var sReader = new StreamReader(fReader))
                    {
                        var json = sReader.ReadToEnd();
                        settingsExport = Serializer.JsonSerializer.DeserializeObject<SettingsExport>(json);
                        ImportDataFromExport();
                    }
                }
            }
        }

        #endregion public methods

        #region private methods

        /// <summary>
        /// Diese Funktion generiert die Daten für den Export. Dazu wird durch alle Properties von <seealso cref="Properties.Settings.Default"/> iteriert.
        /// Diese wurden im Konstruktor übergeben. Siehe <seealso cref="SettingsExportHelper(ApplicationSettingsBase, string)"/>"/>
        /// <para>
        /// This function generates the data for the export. To do this, it iterates through all properties of <seealso cref="Properties.Settings.Default"/>.
        /// This was passed in the constructor. See <seealso cref="SettingsExportHelper(ApplicationSettingsBase, string)"/>"/>
        /// </para>
        /// </summary>
        private void GenerateDataForExport()
        {
            settingsExport ??= new SettingsExport();
            //var settings = Properties.Settings.Default;
            var properties = applicationBaseSettings.Properties;

            //clear the settings
            settingsExport.Settings = [];

            //set the settings key for the export
            if (!string.IsNullOrEmpty(applicationBaseSettings.SettingsKey))
            {
                settingsExport.SettingsKey = applicationBaseSettings.SettingsKey;
            }

            foreach (SettingsProperty prop in properties)
            {
                settingsExport.Settings.Add(new SettingObject 
                { 
                    Name = prop.Name,
                    Type = prop.PropertyType,
                    Value = applicationBaseSettings[prop.Name] 
                });
            }
        }

        /// <summary>
        /// Deserialisiert die Json Daten zurück in die <seealso cref="SettingsExportHelper"/> Klasse.
        /// Setzt die aktuellen Daten in Properties.Settings
        /// <para>
        /// Deserializes the Json data back into the <seealso cref="SettingsExportHelper"/> class.
        /// Sets the current data in Properties.Settings
        /// </para>
        /// </summary>
        private void ImportDataFromExport()
        {
            if (settingsExport != null && settingsExport.Settings != null)
            {
                /*
                 * Relativ einfach. Nimm die Einstellung und Convertiere sie in den Typ.
                 */
                /*
                 * Relatively simple. Take the setting and convert it to the type.
                 */
                if (settingsExport.Settings.Count > 0)
                {
                    //set the settings key back to previous value
                    if (!string.IsNullOrEmpty(settingsExport.SettingsKey))
                    {
                        applicationBaseSettings.SettingsKey = settingsExport.SettingsKey;
                    }

                    foreach (var setting in settingsExport.Settings)
                    {
                        try
                        {
                            var obj = Convert.ChangeType(setting.Value, setting.Type ?? typeof(object));
                            applicationBaseSettings[setting.Name] = obj;
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
            }
        }

        #endregion private methods
    }
}