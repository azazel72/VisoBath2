using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisoBath.Conectores.WSVolumetricas;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace VisoBath.Conectores
{
    class ConectorSAP
    {
        public static ConectorSOAP _conectorSOAP = new ConectorSOAP();
        private const string BaseUrl = "http://192.78.70.230:8080/WSVolumetricas.asmx";
        private const string _credentials = "{\"userName\":\"mme\",\"password\":\"mme9874*\",\"domain\":\"JVISO\"}";
        private const string jsonAlbaranSAP = "{{\"tira\": \"{0}\", \"codigo\": \"{1}\"}}";

        public static async Task<String> ObtenerTira()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                try
                {
                    var resultado = await _conectorSOAP.ValidateUserAsync(_credentials, cts.Token).ConfigureAwait(false);
                    RecepcionToken r = JsonSerializer.Deserialize<RecepcionToken>(resultado);
                    return r.Token;
                }
                catch (OperationCanceledException ex)
                {
                    ErrorLogger.Add("Error (ObtenerTira SAP): operacion cancelada", ex);
                    return null;
                }
                catch (Exception ex)
                {
                    ErrorLogger.Add("Error (ObtenerTira SAP): " + ex.Message, ex);
                    return null;
                }
            }
        }

        public static async Task SolicitarAlbaran(Gestor g, string codigo)
        {
            try
            {
                String tira = await ObtenerTira();
                if (tira != null)
                {
                    string jsonBody = string.Format(jsonAlbaranSAP, tira, codigo);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        var resultado = await _conectorSOAP.SalidaDatosAsync(jsonBody, cts.Token).ConfigureAwait(false);

                        // Guardar JSON de SAP para inspeccionar
                        try
                        {
                            System.IO.File.WriteAllText("SAP_Albaran_Response.json", resultado);
                            ErrorLogger.Add("JSON de SAP guardado en: SAP_Albaran_Response.json");
                        }
                        catch { }

                        ResultadoAlbaran r = JsonSerializer.Deserialize<ResultadoAlbaran>(resultado);
                        g.Estado("Consulta realizada.");
                        if (r != null && r.result != null && r.result.Count > 0)
                        {
                            g.NuevoAlbaran(r.result[0]);
                        }
                        else
                        {
                            MessageBox.Show("No se encontro el albarán.", "No se completó la consulta", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    }
                }
                else
                {
                    g.Estado("El servidor rechazó la conexión.");
                    MessageBox.Show("Ocurrio un error durante la conexion al SAP.\n", "Error de conexion", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception e)
            {
                g.Estado("Error durante la consulta o el formato de la respuesta.");
                ErrorLogger.Add("Error (SolicitarAlbaran SAP): " + e.Message, e);
                MessageBox.Show("Ocurrio un error durante la consulta del albarán:\n" + e.Message, "Error de conexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            g.bloquearFormulario(false);
        }

        public static async Task EnviarNotificacion(Gestor g, Albaran albaran)
        {
            try
            {
                String tira = await ObtenerTira();
                if (tira != null)
                {
                    Notificacion notificacion = new Notificacion(tira, albaran);
                    notificacion.palets = notificacion.palets
                        .Select(p =>
                        {
                            var a = new Palet(p);
                            var dt = DateTime.ParseExact(a.hora, "dd/MM/yyyy HH:mm:ss", new CultureInfo("es-ES"));
                            var dto = new DateTimeOffset(dt, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time").GetUtcOffset(dt));
                            var isoConOffset = dto.ToString("o");
                            a.hora = isoConOffset;
                            return a;
                        })
                        .ToList();

                    string jsonBody = JsonSerializer.Serialize<Notificacion>(notificacion);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        string resultado = await _conectorSOAP.EntradaDatosAsync(jsonBody, cts.Token).ConfigureAwait(false);
                        EnvioNotificacion e = JsonSerializer.Deserialize<EnvioNotificacion>(resultado);
                        g.Debug(resultado);
                        ResultadoAlbaran r = JsonSerializer.Deserialize<ResultadoAlbaran>(resultado);
                        g.Estado("Notificacion realizada.");
                        if (r.errorMsg != null && r.errorMsg.message.Trim() != "")
                        {
                            g.Estado("Error de notificacion.");
                            MessageBox.Show("Ocurrio un error durante el envío de la notificación:\nCódigo de error: " + r.errorMsg.code.ToString() + "\nMensaje: " + r.errorMsg.message, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    g.Estado("El servidor rechazo la conexion.");
                    MessageBox.Show("Ocurrio un error durante la conexion al SAP.\n", "Error de conexion", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception e)
            {
                g.Estado("Error durante la consulta o el formato de la respuesta.");
                ErrorLogger.Add("Error (EnviarNotificacion SAP): " + e.Message, e);
                MessageBox.Show("Ocurrió un error durante el envío de la notificación:\n" + e.Message, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            g.bloquearFormulario(false);
        }
    }
}
