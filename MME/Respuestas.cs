using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisoBath.MME
{
    public class Respuestas
    {
        public class Respuesta
        {
            public bool error { get; set; }
            public int id { get; set; }
            public object entidad { get; set; }
            public string mensaje { get; set; }
            public object objeto { get; set; }

            public Respuesta(bool valor = false)
            {
                this.error = valor;
            }

            public Respuesta Error(string mensaje)
            {
                this.error = true;
                this.mensaje = mensaje;
                return this;
            }

            public static Respuesta Crear(bool e = false, int i = 0, object en = null, string m = "", object o = null)
            {
                Respuesta r = new Respuesta();
                r.error = e;
                r.id = i;
                r.entidad = en;
                r.mensaje = m;
                r.objeto = o;
                return r;
            }
        }
        public class RespuestaHandle
        {
            public int port { get; set; }
            public string handle { get; set; }
            public int error_code { get; set; }
            public string error_text { get; set; }
        }
        public class RespuestaPLC
        {
            public int estado { get; set; }
            public int contador { get; set; }
            public long tiempo { get; set; }
        }
        public class Medidas
        {
            public Punto[] puntos { get; set; }

            public Medidas(byte[] datos, int puntosEnviados, double arcoAltura, double arcoSeccion, double offset)
            {
                if (puntosEnviados * 4 + 80 > datos.Length)
                {
                    return;
                }
                this.puntos = new Punto[puntosEnviados];
                int medida;
                for (int i = 0; i < puntosEnviados; i++)
                {
                    int Pos = 76 + (i * 4);
                    medida = datos[Pos + 3]; medida <<= 8;
                    medida += datos[Pos + 2]; medida <<= 8;
                    medida += datos[Pos + 1]; medida <<= 8;
                    medida += datos[Pos];
                    medida = (medida > 2550 || medida < 0) ? 2500 : medida;

                    //Console.WriteLine(medida);

                    this.puntos[i] = new Punto(medida, i * arcoAltura, arcoSeccion, offset);
                }
            }
        }

        public class Punto
        {
            public int valor;
            public float xy { get; set; }
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }

            public Punto(int v, double arcoAltura, double arcoSeccion, double offset)
            {
                //guardamos le valor
                this.valor = v;
                //guardamos la altura en el eje z
                this.z = 2000 - (float)(Math.Cos(arcoAltura+offset) * v);
                if (z < 35) z = 0;
                //guardamos la distancia al centro
                this.xy = 1500 - (float)(Math.Sin(arcoAltura+offset) * v);
                if (this.xy < 0) this.xy = 0;
                this.x = (float)(Math.Sin(arcoSeccion) * this.xy);
                this.y = (float)(Math.Cos(arcoSeccion) * this.xy);
                //if (x < 0) x = 0;
            }
        }

    }
}
