using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdateClickOnce.Serializer
{
    /// <summary>
    /// Provides methods for serializing and deserializing objects to and from JSON format.
    /// </summary>
    public class JsonSerializer
    {
        /// <summary>
        /// Serializes an object into JSON format.
        /// </summary>
        /// <param name="o">The object to be serialized.</param>
        /// <param name="formatted">If true, the resultant JSON will be formatted for easy human interpretation (whitespace and carriage returns added).</param>
        /// <returns>A string representing the object in JSON format.</returns>
        public static string SerializeObject(object o, bool formatted = false)
        {
            Newtonsoft.Json.Formatting format = formatted ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None;
            JsonSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None;
            JsonSettings.TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple;
            return Newtonsoft.Json.JsonConvert.SerializeObject(o, format, JsonSettings);
        }

        /// <summary>
        /// Serializes an object into JSON format, including type information.
        /// </summary>
        /// <param name="o">The object to be serialized.</param>
        /// <param name="formatted">If true, the resultant JSON will be formatted for easy human interpretation (whitespace and carriage returns added).</param>
        /// <returns>A string representing the object in JSON format.</returns>
        public static string SerializeObjectWithTypes(object o, bool formatted = false)
        {
            Newtonsoft.Json.Formatting format = formatted ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None;
            JsonSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
            JsonSettings.TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple;
            return Newtonsoft.Json.JsonConvert.SerializeObject(o, format, JsonSettings);
        }

        /// <summary>
        /// Converts a string in JSON format into the object that it represents.
        /// </summary>
        /// <typeparam name="T">The Type of the object.</typeparam>
        /// <param name="jsonData">A string in JSON format which represents the object.</param>
        /// <returns>An object of type <see langword="T"/> which is represented by the jsonData parameter.</returns>
        public static T? DeserializeObject<T>(string jsonData)
        {
            JsonSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonData, JsonSettings);
        }

        /// <summary>
        /// Converts a string in JSON format into the object that it represents.
        /// </summary>
        /// <param name="jsonData">A string in JSON format which represents the object.</param>
        /// <param name="type">The Type of the object.</param>
        /// <returns>An object of type <see langword="T"/> which is represented by the jsonData parameter.</returns>
        public static object? DeserializeObject(string jsonData, Type type)
        {
            JsonSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
            return Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData, type, JsonSettings);
        }

        private static Newtonsoft.Json.JsonSerializerSettings jsonSettings = new Newtonsoft.Json.JsonSerializerSettings();

        /// <summary>
        /// Settings that control how serialization and deserialization of objects will be done.
        /// </summary>
        public static Newtonsoft.Json.JsonSerializerSettings JsonSettings
        {
            get
            {
                jsonSettings ??= new Newtonsoft.Json.JsonSerializerSettings();
                return jsonSettings;
            }
            set
            {
                jsonSettings = value;
            }
        }
    }
}
