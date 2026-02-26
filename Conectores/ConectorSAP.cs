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
        /*
        campo "accion" que puede tomar 3 valores

        I / M / B

        Si me pasas una I es porque es la primera vez que se generan los datos de embalaje y por tanto yo generaré un nuevo registro.

        Si me pasas M, quieres que cambie los datos por los que me envías.

        Si me pasas B es porque quieres que elimine los datos.
        */
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
                    //entrada esperada {"userName": "mme", "password": "mme9874*", "domain": "JVISO"}
                    var resultado = await _conectorSOAP.ValidateUserAsync(_credentials, cts.Token).ConfigureAwait(false);
                    RecepcionToken r = JsonSerializer.Deserialize<RecepcionToken>(resultado);
                    return r.Token;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }
        }

        public static async Task SolicitarAlbaran(Gestor g, string codigo)
        {
            try
            {
                //obtenemos la tira
                String tira = await ObtenerTira();
                if (tira != null)
                {
                    //obtenemos la informacion del albaran
                    string jsonBody = string.Format(jsonAlbaranSAP, tira, codigo);

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        //entrada esperada {"tira": "{0}", "codigo": "{1}"}
                        var resultado = await _conectorSOAP.SalidaDatosAsync(jsonBody, cts.Token).ConfigureAwait(false);
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
                    MessageBox.Show("Ocurrió un error durante la conexion al SAP.\n", "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception e)
            {
                g.Estado("Error durante la consulta o el formato de la respuesta.");
                MessageBox.Show("Ocurrió un error durante la consulta del albarán:\n" + e.Message, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            g.bloquearFormulario(false);
        }

        public static async Task EnviarNotificacion(Gestor g, Albaran albaran)
        {
            try
            {
                //obtenemos la tira
                String tira = await ObtenerTira();
                if (tira != null)
                {
                    Notificacion notificacion = new Notificacion(tira, albaran);

                    notificacion.palets = notificacion.palets
                        .Select(p => { 
                            var a = new Palet(p);
                            var dt = DateTime.ParseExact(a.hora, "dd/MM/yyyy HH:mm:ss", new CultureInfo("es-ES"));
                            // Si quieres mantener la zona de Madrid:
                            var dto = new DateTimeOffset(dt, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time").GetUtcOffset(dt));
                            // En ISO 8601 con offset:
                            var isoConOffset = dto.ToString("o");              // "2025-10-19T16:36:07+02:00"
                                                                               // En ISO 8601 sin offset (UTC):
                                                                               //var isoUtc = dto.ToUniversalTime().ToString("o");  // "2025-10-19T14:36:07Z"
                            a.hora = isoConOffset;
                            return a;
                        })
                        .ToList();

                    //obtenemos la informacion del albaran
                    string jsonBody = JsonSerializer.Serialize<Notificacion>(notificacion);

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        string resultado = await _conectorSOAP.EntradaDatosAsync(jsonBody, cts.Token).ConfigureAwait(false);
                        EnvioNotificacion e = JsonSerializer.Deserialize<EnvioNotificacion>(resultado);
                        //string resultado = System.Text.Encoding.UTF8.GetString(res);
                        g.Debug(resultado);
                        ResultadoAlbaran r = JsonSerializer.Deserialize<ResultadoAlbaran>(resultado);
                        g.Estado("Notificación realizada.");
                        if (r.errorMsg != null && r.errorMsg.message.Trim() != "")
                        {
                            g.Estado("Error de notificación.");
                            MessageBox.Show("Ocurrió un error durante el envío de la notificación:\nCódigo de error: " + r.errorMsg.code.ToString() + "\nMensaje: " + r.errorMsg.message, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    g.Estado("El servidor rechazó la conexión.");
                    MessageBox.Show("Ocurrió un error durante la conexion al SAP.\n", "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception e)
            {
                g.Estado("Error durante la consulta o el formato de la respuesta.");
                MessageBox.Show("Ocurrió un error durante el envío de la notificación:\n" + e.Message, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            g.bloquearFormulario(false);
        }

    }
}
