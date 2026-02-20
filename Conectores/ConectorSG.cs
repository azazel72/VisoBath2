using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace VisoBath.Conectores
{
    class ConectorSG
    {
        /*
        campo "accion" que puede tomar 3 valores

        I / M / B

        Si me pasas una I es porque es la primera vez que se generan los datos de embalaje y por tanto yo generaré un nuevo registro.

        Si me pasas M, quieres que cambie los datos por los que me envías.

        Si me pasas B es porque quieres que elimine los datos.
        */
        private const string conexionTiraSG = "https://login.expertxrm.com/wsr/auth/validateuser";
        private const string jsonTiraSG = "{\"userName\": \"mme\", \"password\": \"mme9874*\", \"domain\": \"JVISO\"}";
        private const string conexionAlbaranSG = "https://erpws.expertxrm.com/wscomercial/paletizacion/salidaDatos";
        private const string jsonAlbaranSG = "{{\"tira\": \"{0}\", \"codigo\": \"{1}\"}}";
        private const string conexionNotificacionSG = "https://erpws.expertxrm.com/wscomercial/paletizacion/entradaDatos";


        private static async Task<HttpResponseMessage> Conectar(string cadenaConexion, string cadenaBody, Dictionary<string, string> cabeceras = null)
        {
            HttpClient conectorSG = new HttpClient();
            HttpContent contenido = new StringContent(cadenaBody, Encoding.UTF8, "application/json");
            //conectorSG.DefaultRequestHeaders.Add("x-ddol-security-key", "JVisopd-UZHopxENlUsMahivJIQqUrUaj");
            if (cabeceras != null)
            {
                foreach (KeyValuePair<string, string> par in cabeceras)
                {
                    conectorSG.DefaultRequestHeaders.Add(par.Key, par.Value);
                }
            }
            HttpResponseMessage r = await conectorSG.PostAsync(cadenaConexion, contenido);
            conectorSG.Dispose();
            return r;
        }

        public static async Task<String> ObtenerTira()
        {
            HttpResponseMessage respuesta = await Conectar(conexionTiraSG, jsonTiraSG);
            if (respuesta.IsSuccessStatusCode)
            {
                string resultado = await respuesta.Content.ReadAsStringAsync();
                Console.WriteLine(resultado);
                //ResultadoTira r = JsonSerializer.Deserialize<ResultadoTira>(resultado);
                String r = JsonSerializer.Deserialize<String>(resultado);
                return r;
            }
            else
            {
                return null;
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
                    string jsonBody = string.Format(jsonAlbaranSG, tira, codigo);
                    Dictionary<string, string> token = new Dictionary<string, string>() {
                        { "x-ddol-security-token", tira},
                    };
                    HttpResponseMessage respuesta = await Conectar(conexionAlbaranSG, jsonBody, token);
                    if (respuesta.IsSuccessStatusCode)
                    {
                        //string resultado = await respuesta.Content.ReadAsStringAsync();
                        var res = respuesta.Content.ReadAsByteArrayAsync().Result;
                        string resultado = System.Text.Encoding.UTF8.GetString(res);
                        g.Debug(resultado);
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
                    else
                    {
                        g.Estado("El servidor rechazó la conexión.");
                        MessageBox.Show("Ocurrió un error durante la peticion del albarán:\n" + respuesta.ReasonPhrase, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                else
                {
                    g.Estado("El servidor rechazó la conexión.");
                    MessageBox.Show("Ocurrió un error durante la conexion al SG.\n", "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
                    //obtenemos la informacion del albaran
                    string jsonBody = JsonSerializer.Serialize<Notificacion>(notificacion);
                    Dictionary<string, string> token = new Dictionary<string, string>() {
                        { "x-ddol-security-token", tira}
                    };
                    Console.WriteLine(jsonBody);
                    HttpResponseMessage respuesta = await Conectar(conexionNotificacionSG, jsonBody, token);
                    if (respuesta.IsSuccessStatusCode)
                    {
                        var res = respuesta.Content.ReadAsByteArrayAsync().Result;
                        string resultado = System.Text.Encoding.UTF8.GetString(res);
                        g.Debug(resultado);
                        ResultadoAlbaran r = JsonSerializer.Deserialize<ResultadoAlbaran>(resultado);
                        g.Estado("Notificación realizada.");
                        if (r.errorMsg != null && r.errorMsg.message.Trim() != "")
                        {
                            g.Estado("Error de notificación.");
                            MessageBox.Show("Ocurrió un error durante el envío de la notificación:\nCódigo de error: " + r.errorMsg.code.ToString() + "\nMensaje: " + r.errorMsg.message, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        g.Estado("El servidor rechazó la conexión.");
                        MessageBox.Show("Ocurrió un error durante el envío de la notificación:\n" + respuesta.ReasonPhrase, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                else
                {
                    g.Estado("El servidor rechazó la conexión.");
                    MessageBox.Show("Ocurrió un error durante la conexion al SG.\n", "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
