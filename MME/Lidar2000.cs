using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static VisoBath.MME.Respuestas;

namespace VisoBath.MME
{
    /// <summary>
    /// Clase para manipulacion de Lidar Pepper Fuchs R2000
    /// Autor: Rafael Sánchez Navarro 2021
    /// </summary>
    class Lidar2000
    {
        private string ipSensor;
        private int puertoSensor;
        private string manejador;
        private bool registrar = false;
        public Medidas2000 medidas = new Medidas2000();

        //comandos
        private string plantilla_set_parameter = "http://{0}/cmd/set_parameter?{1}";
        private string plantilla_request_handler = "http://{0}/cmd/request_handle_tcp?{1}";
        private string plantilla_feed_watchdog = "http://{0}/cmd/feed_watchdog?handle={1}";
        private string plantilla_start_scanoutput = "http://{0}/cmd/start_scanoutput?handle={1}";
        private string plantilla_stop_scanoutput = "http://{0}/cmd/stop_scanoutput?handle={1}";

        //parametros set_parameter
        private string plantilla_scan_direction = "scan_direction={0}"; //cw ccw (default)
        private string plantilla_scan_frequency = "scan_frequency={0}";
        private string plantilla_samples_per_scan = "samples_per_scan={0}";

        //parametros para request_handler
        private string plantilla_packet_type = "packet_type={0}";
        private string plantilla_watchdogtimeout = "watchdog=on&watchdogtimeout={0}";
        private string plantilla_start_angle = "start_angle={0}";
        private string plantilla_max_num_points_scan = "max_num_points_scan={0}";
        private string plantilla_skip_scans = "skip_scans={0}";

        private string scan_direction = "ccw";
        private string scan_frequency = "";
        private string samples_per_scan = "";

        //parametros para request_handler
        private string packet_type = "";
        private string watchdogtimeout = "10000";
        private string start_angle = "";
        private string max_num_points_scan = "";
        private string skip_scans = "1";

        //variables del servidor
        private TcpClient clienteEscucha;
        private HttpClient clienteAlimentar;
        private Thread hiloEscucharSensor;
        private Thread hiloAlimentar;
        private string cadena_alimentar;
        private string cadena_start;
        private string cadena_stop;


        public Lidar2000(string ip)
        {
            this.ipSensor = ip;
        }

        public void Activar(Action<object> funcionOK = null, Action<object> funcionKO = null, bool primeraVez = true)
        {
            /*
            //activamos el servidor para la escucha de las tramas
            this.hiloServidor = new Thread(new ThreadStart(() => this.Escuchar(6001, null)));
            this.hiloServidor.Start();
            */
            //preparamos el sensor
            //this.SetParametros("ccw", "20", "720", "A", "-765000", "153", "1", "10000", funcionOK, funcionKO, primeraVez);
            this.SetParametros("ccw", "20", "720", "A", "135000", "153", "1", "10000", funcionOK, funcionKO, primeraVez);

        }

        public async Task SetParametros(string scan_direction, string scan_frequency,
            string samples_per_scan, string packet_type, string start_angle,
            string max_num_points_scan, string skip_scans, string watchdogtimeout,
            Action<object> funcionOK = null, Action<object> funcionKO = null, bool primeraVez = true)
        {
            this.scan_direction = scan_direction;
            this.scan_frequency = scan_frequency;
            this.samples_per_scan = samples_per_scan;
            this.packet_type = packet_type;
            this.start_angle = start_angle;
            this.max_num_points_scan = max_num_points_scan;
            this.skip_scans = skip_scans;
            this.watchdogtimeout = watchdogtimeout;

            try
            {
                string cadena_frecuencia = "http://" + this.ipSensor + "/cmd/set_parameter?scan_direction=ccw&scan_frequency=" + this.scan_frequency + "&samples_per_scan=" + this.samples_per_scan;
                string cadena_observable = "http://" + this.ipSensor + "/cmd/request_handle_tcp?packet_type=A&watchdog=on&watchdogtimeout=10000&start_angle=" + this.start_angle + "&max_num_points_scan=" + this.max_num_points_scan + "&skip_scans=1";
                this.cadena_start = "http://" + this.ipSensor + "/cmd/start_scanoutput?handle=";
                this.cadena_alimentar = "http://" + this.ipSensor + "/cmd/feed_watchdog?handle=";
                this.cadena_stop = "http://" + this.ipSensor + "/cmd/stop_scanoutput?handle=";
                Console.WriteLine(cadena_frecuencia);
                Console.WriteLine(cadena_observable);

                string cadenaRespuesta = null;
                RespuestaHandle respuesta;
                HttpClient clienteSensor = new HttpClient();

                //iniciamos los parametros
                cadenaRespuesta = await clienteSensor.GetStringAsync(cadena_frecuencia);
                Console.WriteLine(cadenaRespuesta);

                //configuramos el tipo de respuesta
                cadenaRespuesta = await clienteSensor.GetStringAsync(cadena_observable);
                Console.WriteLine(cadenaRespuesta);
                respuesta = JsonSerializer.Deserialize<RespuestaHandle>(cadenaRespuesta);

                //iniciamos el manejador
                this.manejador = respuesta.handle;
                this.puertoSensor = respuesta.port;
                this.cadena_start += this.manejador;
                this.cadena_alimentar += this.manejador;
                this.cadena_stop += this.manejador;

                Console.WriteLine(this.cadena_start);
                Console.WriteLine(this.cadena_alimentar);

                clienteSensor.Dispose();

                if (primeraVez)
                {
                    this.hiloEscucharSensor = new Thread(new ThreadStart(EscucharSensor));
                    this.hiloEscucharSensor.Start();
                    this.hiloAlimentar = new Thread(new ThreadStart(AlimentarSensor));
                    this.hiloAlimentar.Start();
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Excepcion (Conectar): " + ex.Message);
            }
        }
        private void EscucharSensor()
        {
            int puntos = int.Parse(this.max_num_points_scan);
            int paquete = (puntos * 4) + 176; //176? o 80
            try
            {
                if (this.clienteEscucha != null && this.clienteEscucha.Connected)
                {
                    this.clienteEscucha.Close();
                    this.clienteEscucha.Dispose();
                }
                clienteEscucha = new TcpClient(this.ipSensor, this.puertoSensor);
                while (true)
                {
                    byte[] data = new byte[paquete];
                    NetworkStream stream = clienteEscucha.GetStream();
                    Int32 bytes = stream.Read(data, 0, paquete);
                    //cabecera 76 bytes
                   // Console.WriteLine(String.Format("{0} : {1} : {2}", paquete, bytes, data.Length));

                    if (bytes >= (paquete-100))
                    {
                        //Console.WriteLine(">> " + System.Text.Encoding.UTF8.GetString(data));
                        /*for (int i = 0; i < 153; i++)
                        {
                            Console.WriteLine(Medidas2000.entero32(data, 76 + (i * 4)));
                        }
                        Console.WriteLine("----");*/
                        if (registrar)
                        {
                            medidas.AgregarDatos(data);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Fallo, paquete mas pequeño");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excepcion (Escuchar): {0} ", ex.Message);
            }
            finally
            {
                if (this.clienteEscucha != null && this.clienteEscucha.Connected)
                {
                    clienteEscucha.Close();
                    clienteEscucha.Dispose();
                }
            }
        }
        private void AlimentarSensor()
        {
            if (this.clienteAlimentar != null)
            {
                this.clienteAlimentar.Dispose();
            }
            this.clienteAlimentar = new HttpClient();
            try
            {
                while (true)
                {
                    Thread.Sleep(2000);
                    _ = clienteAlimentar.GetAsync(this.cadena_alimentar);
                   // Console.WriteLine(this.manejador);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excepcion (Alimentar): {0} ", ex.Message);
            }
            finally
            {
                if (this.clienteAlimentar != null)
                {
                    clienteAlimentar.Dispose();
                }
            }
        }
        public int Tramas()
        {
            return this.medidas.tramas.Count;
        }

        /// <summary>
        /// Iniciar conexion con Lidar 2000, obtiene un manejador
        /// </summary>
        public async Task Iniciar(Action<object> funcionOK = null, Action<object> funcionKO = null)
        {
            try
            {
                HttpClient clienteSensor = new HttpClient();
                string cadenaRespuesta = await clienteSensor.GetStringAsync(this.cadena_start);
                this.registrar = false;
                this.medidas.Vaciar();
                Console.WriteLine(cadenaRespuesta);
                clienteSensor.Dispose();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Excepcion (Iniciar): " + ex.Message);
            }
        }
        public async Task Parar(Action<object> funcionOK = null, Action<object> funcionKO = null)
        {
            try
            {
                HttpClient clienteSensor = new HttpClient();
                string cadenaRespuesta = await clienteSensor.GetStringAsync(this.cadena_stop);
                Console.WriteLine(cadenaRespuesta);
                clienteSensor.Dispose();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Excepcion (Parar): " + ex.Message);
            }

        }
        public void Registrar(bool r)
        {
            try
            {
                this.registrar = r;
                if (r == false)
                {
                    Console.WriteLine(this.medidas.tramas.Count);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Excepcion (Registrar): " + ex.Message);
            }
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        public void Apagar()
        {
            try
            {
                if (this.clienteEscucha != null)
                {
                    this.clienteEscucha.Close();
                    this.clienteEscucha.Dispose();
                }
                if (this.clienteAlimentar != null)
                {
                    this.clienteAlimentar.Dispose();
                }
                if (this.hiloEscucharSensor != null)
                {
                    this.hiloEscucharSensor.Abort();
                    this.hiloEscucharSensor.Join();
                }
                if (this.hiloAlimentar != null)
                {
                    this.hiloAlimentar.Abort();
                    this.hiloAlimentar.Join();
                }
            }
            catch (Exception e)
            {
                //this.gestor.EscribirError("ERROR (ApagarHilo): " + e.Message);
            }
        }

        private RespuestaHandle Validar(string respuesta)
        {
            RespuestaHandle r = JsonSerializer.Deserialize<RespuestaHandle>(respuesta);
            Console.WriteLine(respuesta);
            return r;
        }

        public float[] Calcular()
        {
            return this.medidas.Calcular();
        }

        public void Test()
        {
            //this.medidas.CalcularUna();
           // this.medidas.Calcular();
        }
    }
}
