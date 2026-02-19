using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Permissions;

namespace VisoBath
{
    class Servidor
    {
        private Socket servidorVolumetrico;
        private Thread hiloVolumetrico;
        private TcpClient clienteWeb;
        private Thread hiloWeb;

        private int puertoEscucha;
        private int puertoWeb;
        private Gestor gestor;
        private bool continuar { get; set; }

        // contructor
        public Servidor(Gestor gestor, int puertoPLCs = 5000, int puertoWeb = 5001)
        {
            this.gestor = gestor;
            this.puertoEscucha = puertoPLCs;
            this.puertoWeb = puertoWeb;
            this.continuar = true;
        }

        public bool Iniciar()
        {
            try
            {
                this.hiloVolumetrico = new Thread(new ThreadStart(IniciarServidorVolumetrico));
                this.hiloVolumetrico = new Thread(new ThreadStart(IniciarServidorVolumetrico));

                this.hiloVolumetrico.Start();
                this.hiloVolumetrico.Start();
            }
            catch (Exception e)
            {
                //this.gestor.EscribirError("Error (Iniciar): " + e.StackTrace);
                return false;
            }
            return true;
        }

        public void Apagar()
        {
            ApagarHilo();
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        private void ApagarHilo()
        {
            try
            {
                this.continuar = false;
                servidorVolumetrico.Close();
                servidorVolumetrico.Dispose();
                hiloVolumetrico.Abort();
                hiloVolumetrico.Join();
            }
            catch (Exception e)
            {
                //this.gestor.EscribirError("ERROR (ApagarHilo): " + e.Message);
            }
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        public void ReiniciarServidores()
        {
            servidorVolumetrico.Close();
        }

        private void IniciarServidorVolumetrico()
        {
            while (this.continuar)
            {
                try
                {
                    IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, this.puertoEscucha);
                    servidorVolumetrico = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    servidorVolumetrico.Bind(localEndPoint);
                    servidorVolumetrico.Listen(5);
                    
                    while (this.continuar)
                    {
                        string origen = "";
                        this.gestor.EncenderTestigo(true);
                        Socket conexion = servidorVolumetrico.Accept();
                        this.gestor.EncenderTestigo(false);
                        try
                        {
                            this.procesarConexion(conexion);
                        }
                        catch (Exception e)
                        {
                            //   this.gestor.EscribirError("Error (Crear Servidor Automatas, Bucle interno): " + origen + " - " + e.StackTrace);
                            this.gestor.EncenderTestigo(false);
                            Thread.Sleep(1000);
                        }
                        finally
                        {

                        }
                    }
                }
                catch (Exception e)
                {
                    this.gestor.Debug("Error (Crear Servidor Automatas, Bucle externo)" + e.StackTrace);
                }

                try
                {

                    this.gestor.Debug("Cerrando puerto del servidor de Automatas");
                    this.gestor.EncenderTestigo(false);
                    servidorVolumetrico.Close();
                    Thread.Sleep(2000);
                }
                catch (Exception e)
                {
                    this.gestor.Debug("Error (Cerrando puerto del servidor de Automatas): " + e.StackTrace);
                }
            }
        }

        /// <summary>
        /// Procesa las peticiones originarias de un automata (texto plano sin cabecera http)
        /// </summary>
        /// <returns></returns>
        private void procesarConexion(Socket conexion)
        {
            try
            {
                if (conexion == null)
                {
                    return;
                }
                string origen = ((IPEndPoint)conexion.RemoteEndPoint).Address.ToString();
                byte[] b = new Byte[20000];
                int length = 0;
                string trama = "";
                if (true)
                {
                    while ((length = conexion.Receive(b)) > 0)
                    {
                        string datos = Encoding.UTF8.GetString(b, 0, length);
                        trama += datos;
                    }
                }
                else
                {
                    length = conexion.Receive(b);
                    trama = Encoding.UTF8.GetString(b, 0, length);
                }
                conexion.Close();
                //Console.WriteLine(trama);
                NotificacionPLC n = JsonSerializer.Deserialize<NotificacionPLC>(trama);
                this.gestor.AccionRecibida(n);
            }
            catch (Exception e)
            {
                this.gestor.Debug("Error (procesarAutomata): " + e.StackTrace);
            }
        }
    }
    /*
     *
    

     * 
     * */

}
