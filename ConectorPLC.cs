using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VisoBath
{
    class ConectorPLC
    {
        static private readonly object bloqueoEnviarPlc = new object();

        //Thread hilo = new Thread(() => Enviar(slitter.ip, slitter.puerto, mensaje + "\n\r"));

        static private void Enviar(string ip, int puerto, string mensaje)
        {
            lock (bloqueoEnviarPlc)
            {
                try
                {
                    Console.WriteLine("a PLC: " + ip + " - " + mensaje);
                    //IPAddress ip = new IPAddress(Convert.ToByte(IPEnvioTxt.Text));
                    IPAddress IP = IPAddress.Parse(ip);
                    TcpClient client = new TcpClient();
                    client.Connect(IP, puerto);
                    Stream conexion = client.GetStream();

                    ASCIIEncoding msg = new ASCIIEncoding();
                    byte[] ba = msg.GetBytes(mensaje);

                    conexion.Write(ba, 0, ba.Length);
                    //Nuevo
                    conexion.Flush();
                    conexion.Close();
                    //
                    client.Close();
                }
                catch (Exception e)
                {
                    //ConectorSQL.InsertarError("Error (Enviar a plc): " + mensaje + " - " + e.StackTrace);
                    //Console.WriteLine("Error: " + e.StackTrace);
                }
            }
        }

    }
}
