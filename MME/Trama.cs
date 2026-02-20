using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisoBath.MME
{
    public class Trama
    {
        private double arcoInicial = -0.87266462599716477; // -500 000
        private double arcoAvance = 0.0017453292519943296;
        private static Int64 bias = 621355968000000000;

        public int magic;
        public int packet_type;
        public int packet_size;
        public int header_size;
        public int scan_number;
        public int packet_number;
        public int layer_index;
        public int layer_inclination; // 1/10000
        public DateTime timestamp;
        public long reserved;
        public int status_flags;
        public int scan_frequency;
        public int num_points_scan;
        public int num_points_packet;
        public int first_index;
        public int first_angle;
        public int angular_increment; // 1/10000
        public long reserver2; //32+32+64+64=24 bytes
        public long header_padding; //1 byte, hasta 3 para que la cabecera sea multiplo de 32 bits

        public Punto[] puntos { get; set; } // 20bits distancia, 8bits amplitud

        public Trama(byte[] cadena)
        {
            if (cadena.Length < 84) return;
            //primeros 84 bytes:

            //magic A25C, 41564

            //type C1

            this.packet_size = entero32(cadena, 4);
            if (cadena.Length != this.packet_size)
            {
                Console.WriteLine("Error en el tamaño del paquete");
                return;
            }

            this.header_size = entero16(cadena, 8);

            this.scan_number = entero16(cadena, 10);
            //Console.WriteLine("Nº scan: " + this.scan_number.ToString());

            this.packet_number = entero16(cadena, 12);
            //Console.WriteLine("y paquete: " + this.packet_number.ToString());

            this.layer_index = entero16(cadena, 14);
            this.layer_inclination = entero32(cadena, 16);

            //this.timestamp = fecha(cadena, 20); //20 + 8

            //reserved; 28 + 8
            //status_flags; 36 + 4

            this.scan_frequency = entero32(cadena, 40);
            this.num_points_scan = entero16(cadena, 44);
            this.num_points_packet = entero16(cadena, 46);
            this.puntos = new Punto[this.num_points_packet];

            this.first_index = entero16(cadena, 48);
            this.first_angle = entero32(cadena, 50); //grados * 10 000

            this.angular_increment = entero32(cadena, 54); //incremento por cada punto * 10 000

            //reserver2; 58 + 24
            //header_padding; 80 + 1 byte, hasta 3 para que la cabecera sea multiplo de 32 bits

            arcoInicial = Math.PI / 1800000 * this.first_angle; // -500 000

            Console.WriteLine(String.Format("{0}:{1} - {2} {3}", this.scan_number, this.packet_number, this.first_angle, arcoInicial));


            for (int i = 0; i < this.num_points_packet; i++)
            {
                this.puntos[i] = new Punto(entero16(cadena, 84 + (i * 4)), arcoInicial, i * arcoAvance, 0);
            }
        }

        public static int GetIndice(byte[] cadena)
        {
            return entero16(cadena, 10);
        }
        public static int GetValor(byte[] cadena, int pos)
        {
            return entero16(cadena, 84 + (pos * 4));
        }

        private static int entero16(byte[] cadena, int inicio)
        {
            int v = cadena[inicio + 1]; v <<= 8;
            v += cadena[inicio];
            return v;
        }
        private static int entero32(byte[] cadena, int inicio)
        {
            int v = cadena[inicio + 3]; v <<= 8;
            v += cadena[inicio + 2]; v <<= 8;
            v += cadena[inicio + 1]; v <<= 8;
            v += cadena[inicio];
            return v;
        }
        private static Int64 entero64(byte[] cadena, int inicio)
        {
            Int64 v = cadena[inicio + 7]; v <<= 8;
            v += cadena[inicio + 6]; v <<= 8;
            v += cadena[inicio + 5]; v <<= 8;
            v += cadena[inicio + 4]; v <<= 8;
            v += cadena[inicio + 3]; v <<= 8;
            v += cadena[inicio + 2]; v <<= 8;
            v += cadena[inicio + 1]; v <<= 8;
            v += cadena[inicio];
            return v;
        }
        public static DateTime fecha(byte[] cadena, int inicio)
        {
            try
            {
                return new DateTime((entero64(cadena, inicio) / 100) + bias);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }
        private static byte[] caracteres(byte[] cadena, int inicio, int longitud)
        {
            byte[] c = new byte[longitud];
            Array.Copy(cadena, inicio, c, 0, longitud);
            return c;
        }
    }

    public class Punto
    {
        public int valor;
        public float xy { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public Punto(int v, double arcoInicial, double arcoAvance, double arcoSeccion)
        {
            this.valor = v;
            this.z = (float)(Math.Sin(arcoInicial - arcoAvance) * v);
            this.xy = (float)(Math.Cos(arcoInicial - arcoAvance) * v);
            //Console.WriteLine(String.Format("{0}: {1} {2}", v, z, xy));
            /*
            //guardamos el valor
            this.valor = v;
            //guardamos la altura en el eje z
            this.z = 2000 - (float)(Math.Cos(arcoAltura + offset) * v);
            if (z < 35) z = 0;
            //guardamos la distancia al centro
            this.xy = 1500 - (float)(Math.Sin(arcoAltura + offset) * v);
            if (this.xy < 0) this.xy = 0;
            this.x = (float)(Math.Sin(arcoSeccion) * this.xy);
            this.y = (float)(Math.Cos(arcoSeccion) * this.xy);
            //if (x < 0) x = 0;
            */
        }
    }
}
