using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VisoBath.MME
{
    /// <summary>
    /// Clase para manipulacion de Lidar Pepper Fuchs R2300
    /// Autor: Rafael Sánchez Navarro 2021
    /// </summary>
    public class Lidar2300
    {
        private string ip;
        private string manejador;
        private UdpClient servidor;
        private Thread hiloServidor;

        public Medidas2300 medidas = new Medidas2300();
        public bool registrar = false;


        //http://192.168.10.9/cmd/set_parameter?pilot_laser=on&pilot_start_angle=-10000&pilot_stop_angle=10000

        //comandos
        private string plantilla_set_parameter = "http://{0}/cmd/set_parameter?{1}";
        private string plantilla_request_handler = "http://{0}/cmd/request_handle_udp?{1}";
        private string plantilla_start_scanoutput = "http://{0}/cmd/start_scanoutput?handle={1}";
        private string plantilla_stop_scanoutput = "http://{0}/cmd/stop_scanoutput?handle={1}";

        //parametros set_parameter
        private string plantilla_scan_frequency = "scan_frequency={0}"; //50 - 100
        private string plantilla_layer_enable = "layer_enable={0}"; //on,on,on,on
        private string plantilla_measure_start_angle = "measure_start_angle={0}"; //-500000
        private string plantilla_measure_stop_angle = "measure_stop_angle={0}"; //500000

        private string scan_frequency = "";
        private string layer_enable = "";
        private string measure_start_angle = "";
        private string measure_stop_angle = "";


        //parametros para request_handler
        private string plantilla_address = "address={0}";
        private string plantilla_port = "port={0}";
        private string plantilla_start_angle = "start_angle={0}"; //-500000
        private string plantilla_max_num_points_scan = "max_num_points_scan={0}"; //0
        private string plantilla_packet_type = "packet_type=C1";

        private string address = "";
        private string port = "";
        private string start_angle = "";
        private string max_num_points_scan = "";

        public Lidar2300(string ip)
        {
            //  this.oyentes = new List<Action<Respuesta>>();
            this.ip = ip;
        }

        public void Activar()
        {
            //activamos el servidor para la escucha de las tramas
            this.hiloServidor = new Thread(new ThreadStart(() => this.Escuchar(6001, null)));
            this.hiloServidor.Start();
            //preparamos el sensor
            this.SetParametros();
        }

        public void Escuchar(int puerto=6001, Action<object> funcionOK = null, Action<object> funcionKO = null)
        {
            try
            {
                byte[] data = new byte[5000];
                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, puerto);
                this.servidor = new UdpClient(ipep);
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    data = servidor.Receive(ref sender);
                    if (registrar)
                    {
                        medidas.AgregarDatos(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task SetParametros(Action<object> funcionOK = null, Action<object> funcionKO = null,
            string scan_frequency = "50", string layer_enable="off,on,off,off",
            string measure_start_angle = "-450000", string measure_stop_angle = "450000") //280000
        {
            Thread.Sleep(500);

            this.scan_frequency = scan_frequency;
            this.layer_enable = layer_enable;
            this.measure_start_angle = measure_start_angle;
            this.measure_stop_angle = measure_stop_angle;

            string cadenaRespuesta = null;
            RespuestaSensor respuesta;
            HttpClient clienteSensor = new HttpClient();

            //iniciamos los parametros
            string parametros = String.Format(plantilla_scan_frequency, scan_frequency);
            parametros += "&" + String.Format(plantilla_layer_enable, layer_enable);
            parametros += "&" + String.Format(plantilla_measure_start_angle, measure_start_angle);
            parametros += "&" + String.Format(plantilla_measure_stop_angle, measure_stop_angle);
            parametros += "&pilot_laser=on&pilot_start_angle=-450000&pilot_stop_angle=450000";
            string url = String.Format(plantilla_set_parameter, this.ip, parametros); //255 265
            Console.WriteLine(url);
            cadenaRespuesta = await clienteSensor.GetStringAsync(url);
            Console.WriteLine(cadenaRespuesta);
            respuesta = Validar(cadenaRespuesta);

            if (respuesta.error_code == 0 && funcionOK != null)
            {
                funcionOK(this);
            }
        }

        /// <summary>
        /// Iniciar conexion con Lidar 2000, obtiene un manejador
        /// Activa el watchdog
        /// </summary>
        public async Task Iniciar(string address = "169.254.244.2", string port = "6001",
            string start_angle = "-450000", string max_num_points_scan = "900",
            Action<object> funcionOK = null, Action<object> funcionKO = null) //730
        {
            //start_angle -500000
            this.address = address;
            this.port = port;
            this.start_angle = start_angle;
            this.max_num_points_scan = max_num_points_scan;

            string cadenaRespuesta = null;
            RespuestaSensor respuesta;
            HttpClient clienteSensor = new HttpClient();

            //iniciamos los parametros
            string parametros = String.Format(plantilla_address, address);
            parametros += "&" + String.Format(plantilla_port, port);
            parametros += "&" + String.Format(plantilla_start_angle, start_angle);
            parametros += "&" + String.Format(plantilla_max_num_points_scan, max_num_points_scan);
            parametros += "&" + plantilla_packet_type;
            string url = String.Format(this.plantilla_request_handler, this.ip, parametros);
            Console.WriteLine(url);
            cadenaRespuesta = await clienteSensor.GetStringAsync(url);
            Console.WriteLine(cadenaRespuesta);
            respuesta = Validar(cadenaRespuesta);
            this.manejador = respuesta.handle;

            //inicio del sensor
            url = String.Format(plantilla_start_scanoutput, this.ip, manejador);
            Console.WriteLine(url);
            cadenaRespuesta = await clienteSensor.GetStringAsync(url);
            Console.WriteLine(cadenaRespuesta);
            respuesta = Validar(cadenaRespuesta);
            this.registrar = false;
            this.medidas.Vaciar();
        }
        public void Registrar(bool r)
        {
            this.registrar = r;
            if (r == false)
            {
                Console.WriteLine(this.medidas.tramas.Count);
            }
        }
        public async Task Parar(Action<object> funcionOK = null, Action<object> funcionKO = null)
        {
            string cadenaRespuesta = null;
            RespuestaSensor respuesta;
            HttpClient clienteSensor = new HttpClient();

            string url = String.Format(plantilla_stop_scanoutput, this.ip, manejador);
            Console.WriteLine(url);
            cadenaRespuesta = await clienteSensor.GetStringAsync(url);
            respuesta = Validar(cadenaRespuesta);
            //Calcular();
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        public void Apagar()
        {
            try
            {
                this.servidor.Close();
                this.servidor.Dispose();
                this.hiloServidor.Abort();
                this.hiloServidor.Join();
            }
            catch (Exception e)
            {
                //this.gestor.EscribirError("ERROR (ApagarHilo): " + e.Message);
            }
        }


        private RespuestaSensor Validar(string respuesta)
        {
            RespuestaSensor r = JsonSerializer.Deserialize<RespuestaSensor>(respuesta);
            return r;
        }

        private class RespuestaSensor
        {
            public int port { get; set; }
            public string handle { get; set; }
            public int error_code { get; set; }
            public string error_text { get; set; }
        }
    }
}
