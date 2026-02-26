using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisoBath
{
    public class Configuracion
    {
        public string nombreImpresora { get; set; }
        public string url_conexion { get; set; }
        public string selector_conexion { get; set; }

        private const string ConfigFileName = "configuracion.json";
        private const string DefaultUrl = "http://192.78.70.230:8080/WSVolumetricas.asmx";
        private const string DefaultSelector = "SG";

        public Configuracion()
        {
            this.nombreImpresora = "";
            this.url_conexion = DefaultUrl;
            this.selector_conexion = DefaultSelector;
        }

        public static Configuracion Load()
        {
            try
            {
                string path = GetConfigPath();
                if (!File.Exists(path))
                {
                    return new Configuracion();
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new Configuracion();
                }

                PersistedConfig data = JsonSerializer.Deserialize<PersistedConfig>(json);
                if (data == null)
                {
                    return new Configuracion();
                }

                var configuracion = new Configuracion
                {
                    nombreImpresora = data.NombreImpresora ?? string.Empty,
                    url_conexion = string.IsNullOrWhiteSpace(data.UrlConexion) ? DefaultUrl : data.UrlConexion,
                    selector_conexion = ValidateSelector(data.SelectorConexion)
                };

                return configuracion;
            }
            catch
            {
                return new Configuracion();
            }
        }

        public void Save()
        {
            try
            {
                var data = new PersistedConfig
                {
                    NombreImpresora = this.nombreImpresora ?? string.Empty,
                    UrlConexion = string.IsNullOrWhiteSpace(this.url_conexion) ? DefaultUrl : this.url_conexion,
                    SelectorConexion = ValidateSelector(this.selector_conexion)
                };

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(GetConfigPath(), json);
            }
            catch
            {
                // Ignorado: no debemos bloquear la aplicación si no se puede guardar
            }
        }


        private static string GetConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        }

        private static string ValidateSelector(string value)
        {
            string conexionNormalizada = string.IsNullOrWhiteSpace(value)
                ? DefaultSelector
                : value.Trim().ToUpperInvariant();

            if (conexionNormalizada != "SG" && conexionNormalizada != "SAP")
            {
                conexionNormalizada = DefaultSelector;
            }

            return conexionNormalizada;
        }

        private class PersistedConfig
        {
            [JsonPropertyName("nombreImpresora")]
            public string NombreImpresora { get; set; }
            [JsonPropertyName("url_conexion")]
            public string UrlConexion { get; set; }
            [JsonPropertyName("selector_conexion")]
            public string SelectorConexion { get; set; }
        }
    }

}
