using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisoBath.MME
{
    public class Medidas2300
    {
        public double arcoInicial = 0; //0.87266462599716477; //=> -500.000, -50º
        public double arcoAvance = 0.0017453292519943296; //radianes por decima de grado
        public double seno1500 = 0.02617694830787315261061168555411;
        public double coseno1500 = 0.99965732497555728003676088836768;

        //public int numeroCapas = 600;
        //public int numeroSecciones = 60;

        public Dictionary<int, List<byte[]>> tramas;
        public Dictionary<int, int[]> secciones;

        public Medidas2300()
        {
            Vaciar();
        }

        public int AgregarDatos(byte[] cadena)
        {
            int indiceLectura = entero16(cadena, 10);
            if (!this.tramas.TryGetValue(indiceLectura, out List<byte[]> lista)) {
                lista = new List<byte[]>();
                this.tramas[indiceLectura] = lista;
            };
            lista.Add(cadena);
            return indiceLectura;
        }

        public void Vaciar()
        {
            tramas = new Dictionary<int, List<byte[]>>();
            secciones = new Dictionary<int, int[]>();
        }

        /// <summary>
        /// Metodo de pruebas que convierte todas las tramas de una lectura
        /// en un unico array de valores
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public bool CrearSeccion(int indice)
        {
            if (this.tramas.TryGetValue(indice, out List<byte[]> lista) && lista != null && lista.Count > 0)
            {
                int numeroPuntos = entero16(lista[0], 44); //numero total de puntos
                int[] valores = new int[numeroPuntos];

                lista.ForEach(cadena =>
                {
                    int numeroPuntosTrama = entero16(cadena, 46); //numero de puntos en el paquete
                    int primerPunto = entero16(cadena, 48); //indice primer punto
                    for (int i = 0; i < numeroPuntosTrama; i++)
                    {
                        valores[primerPunto + i] = entero16(cadena, 84 + (i * 4));
                    }
                });
                this.secciones[indice] = valores;
                return true;
            }
            return false;
        }
        public float[,] ConvertirSeccion(int indice)
        {
            if (this.secciones.TryGetValue(indice, out int[] valores) && valores != null)
            {
                float[,] coordenadas = new float[valores.Length, 5];
                for (int i = 0;i < valores.Length; i++)
                {
                    //comenzamos con el arco minimo por defecto
                    float v = valores[i];
                    float avance = (float)arcoAvance * i;
                    coordenadas[i, 0] = v;
                    //z
                    float z = 2020 + (float)(Math.Sin(arcoInicial - avance) * v);
                    coordenadas[i, 1] = z < 0 ? 0 : z;  
                    //xy
                    float xy = 1350 - (float)(Math.Cos(arcoInicial - avance) * v);
                    coordenadas[i, 2] = xy;
                    //x
                    //coordenadas[i, 3] = (float)this.seno1500 * coordenadas[i, 2];
                    //y
                    //coordenadas[i, 4] = (float)this.coseno1500 * coordenadas[i, 2];
                }
                return coordenadas;
            }
            return null;
        }

        public bool DetectarTacon(byte[] cadena)
        {
            //se valida la posicion 705-719 de la trama 3, equivalente a 105-119
            //Console.WriteLine(entero16(cadena, 46));
            if (entero16(cadena, 12) == 3 && entero16(cadena, 46) == 131)
            {
                float[,] valores = new float[5,3];
                float media = 0;
                //detectamos 15 puntos agrupados de 3 en 3
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int p = 105 + (i * 3) + j;
                        valores[i,0] += entero16(cadena, 84 + (p * 4));
                    }
                    float v = valores[i, 0] / 3;
                    valores[i, 0] = v;
                    float avance = (float)arcoAvance * (706 + i);
                    float z = 2470 + (float)(Math.Sin(arcoInicial - avance) * v);
                    //Console.WriteLine(avance);
                    valores[i, 1] = z < 35 ? 0 : z;
                    float xy = 1350 - (float)(Math.Cos(arcoInicial - avance) * v);
                    valores[i, 2] = xy;
                    media += valores[i, 1];
                }
                media = media / 5;
                if (media > 40)
                {
                    Console.WriteLine(
                         String.Format("{0} {1} {2} {3} {4} : {5}",
                         valores[0, 1],
                         valores[1, 1],
                         valores[2, 1],
                         valores[3, 1],
                         valores[4, 1],
                         media
                    ));

                    return true;
                }
            }
            return false;
        }

        public static int GetIndice(byte[] cadena)
        {
            return entero16(cadena, 10);
        }
        public static int GetPaquete(byte[] cadena)
        {
            //Console.WriteLine(entero16(cadena, 46));
            return entero16(cadena, 12);
        }
        public static int GetValor(byte[] cadena, int pos)
        {
            return entero16(cadena, 84 + (pos * 4));
        }
        public static int GetPuntos(byte[] cadena)
        {
            return entero16(cadena, 46);
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
    }
}
