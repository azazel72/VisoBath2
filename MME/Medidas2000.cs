using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisoBath.MME
{
    class Medidas2000
    {
        public double arcoInicial;
        public double arcoAvance;
        public double arcoSeccion;

        public List<byte[]> tramas;
        public List<float[,]> coordenadas;

        public int numPuntos = 1;

        public Medidas2000(int puntos = 153)
        {
            //calculamos el desplazamiento del angulo de inicio en radianes
            this.arcoInicial = (Math.PI / 180) * -76.5; // 13.5; -76.5 grados
            this.arcoAvance = 0.017453292519943296 * 0.5; //medio grado en radianes
            this.arcoSeccion = 0;
            this.numPuntos = puntos;
            Vaciar();
        }

        public void AgregarDatos(byte[] cadena)
        {
            //Console.WriteLine("** " + System.Text.Encoding.UTF8.GetString(cadena));
            if (this.numPuntos * 4 + 80 > cadena.Length)
            {
                Console.WriteLine("Tamaño de paquete erroneo: " + cadena.Length.ToString());
                return;
            }

            tramas.Add(cadena);
        }

        public void Vaciar()
        {
            tramas = new List<byte[]>();
            coordenadas = new List<float[,]>();
        }

        public void Probar(byte[] cadena)
        {
            /*
            this.tramas.Clear();
            this.tramas.Add((cadena));
            this.CalcularCoordenadas();
            */
        }

        public float[] CalcularCoordenadas()
        {
            int numSecciones = this.tramas.Count;
            if (numSecciones == 0) return null;

            this.coordenadas.Clear();
            this.arcoSeccion = (Math.PI / 180) * (360.0 / numSecciones);
            float[,] seccion = new float[this.numPuntos, 5];

            for (int s = 0; s < numSecciones; s++)
            {
                //arco de la seccion en radianes
                double arcoS = arcoSeccion * s;

                for (int p = 0; p < this.numPuntos; p++)
                {
                    //arco del punto en radianes, calculado para medio grado, 720 puntos.
                    float arcoA = (float)arcoAvance * p;
                    //trama
                    byte[] trama = this.tramas[s];
                    int v = entero32(trama, 76 + (p*4));
                    if (v == -1)
                    {
                        seccion[s, 0] = 0;
                        seccion[s, 1] = 0;
                        seccion[s, 2] = 0;
                    }
                    else
                    {
                        //valor real
                        seccion[s, 0] = v;
                        //z - altura > se usa el Seno porque tenemos el angulo en negativo avanzando hacia 0º
                        float z = 2500 + (float)(Math.Sin(arcoInicial + arcoA) * v);
                        seccion[s, 1] = z < 50 ? 0 : z;
                        //xy > Se usa el coseno, que en este caso es positivo, por lo que se resta de la distancia al centro
                        float xy = 1350 - (float)(Math.Cos(arcoInicial + arcoA) * v);
                        seccion[s, 2] = xy;
                        //x
                        //seccion[s, 3] = (float)Math.Cos(arcoS) * xy;
                        //y
                        //seccion[s, 4] = (float)Math.Sin(arcoS) * xy;
                    }
                }
                this.coordenadas.Add(seccion);
            }

            for (int i = 0; i<153; i++)
            {
                Console.WriteLine(String.Format("{0}, {1}:{2}, {3}:{4}",
                    this.coordenadas[0][i, 0],
                    this.coordenadas[0][i, 1],
                    this.coordenadas[0][i, 2],
                    this.coordenadas[0][i, 3],
                    this.coordenadas[0][i, 4]
                    ));
            }
            //Console.WriteLine(this.tramas[0]);
            //luego se quedar a el mayor punto de cada seccion
            float[] maximos = new float[numSecciones];
            float maximo = 0;
            for (int s = 0; s < numSecciones; s++)
            {
                for (int p = 0; p < this.numPuntos; p++)
                {
                    if (this.coordenadas[s][p, 1] > maximo)
                    {
                        maximo = this.coordenadas[s][p, 1];
                    }
                }
                maximos[s] = maximo;
            }

            //luego se rotaran 90º para hallar el mayor x y el mayor y, con el menor area
            return null;

        }
        public static int entero32(byte[] cadena, int inicio)
        {
            int v = cadena[inicio + 3]; v <<= 8;
            v += cadena[inicio + 2]; v <<= 8;
            v += cadena[inicio + 1]; v <<= 8;
            v += cadena[inicio];
            return v;
        }

        public void CalcularUna()
        {
            int[] lista = { 2567, 2579, 2556, 2564, 2562, 2575, 2584, 2588, 2589, 2608, 2614, 2625, 2553, 2476, 2419, 2356, 2347, 2351, 2362, 2373, 2363, 2374, 2397, 2402, 2404, 2409, 2477, 2446, 2399, 2369, 2356, 2327, 2351, 2351, 2373, 2381, 2388, 2407, 2419, 2425, 2452, 2466, 2460, 2487, 2516, 2528, 2519, 2542, 3049, 3144, 3167, 3197, 3226, 3236, 3391, 3423, 3458, 3473, 3500, 3536, 3562, 3588, 3620, 3645, 3716, 3736, 3770, 3803, 3836, 3876, 3918, 3959, 3989, 4054, 4076, 4143, 4188, 4227, 4276, 4348, 4398, 4467, 4503, 4551, 4613, 4667, 4734, 4809, 4856, 4942, 5016, 5077, 5171, 5219, 5280, 5399, 5476, 5573, 5662, 5766, 5882, 5987, 6051, 6218, 6332, 6520, 6550, 6679, 6827, -1, -1, 7333, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            float max_altura = 0;
            float max_distancia = 0;
            
            for (int p = 0; p < lista.Length; p++)
            {
                float z = 0;
                float xy = 0;
                if (lista[p] > 0)
                {

                    //arco del punto en radianes, calculado para medio grado, 720 puntos.
                    float arcoA = (float)arcoAvance * p;
                    z = 2500 + (float)(Math.Sin(arcoInicial + arcoA) * lista[p]);
                    z = z < 50 ? 0 : z;
                    xy = 1350 - (float)(Math.Cos(arcoInicial + arcoA) * lista[p]);
                    xy = xy < 0 ? 0 : xy;
                    Console.WriteLine(String.Format("{0}: {1} - {2}", lista[p], z, xy));

                    //solo si la profundidad no supera el eje ...
                    if (xy > 0)
                    {
                        if (max_altura < z)
                        {
                            max_altura = z;
                        }
                    }

                    //solo cuando exista algo de altura tendre en cuenta la distancia al eje
                    if (z > 0)
                    {
                        if (max_distancia < xy)
                        {
                            max_distancia = xy;
                        }
                    }

                    //La altura sera la mayor comun de todas las secciones

                    //la distancia se almacena en un array, para rotarlas y hallar el menor area.



                    //x
                    //seccion[s, 3] = (float)Math.Cos(arcoS) * xy;
                    //y
                    //seccion[s, 4] = (float)Math.Sin(arcoS) * xy;
                }
            }

            Console.WriteLine(String.Format("Altura: {0}, Profundidad: {1}", max_altura, max_distancia));

        }

        public float[] Calcular()
        {
            //int numSecciones = 77;
            int numSecciones = this.tramas.Count;
            if (numSecciones == 0) return null;
            //int[] lista = { 2567, 2579, 2556, 2564, 2562, 2575, 2584, 2588, 2589, 2608, 2614, 2625, 2553, 2476, 2419, 2356, 2347, 2351, 2362, 2373, 2363, 2374, 2397, 2402, 2404, 2409, 2477, 2446, 2399, 2369, 2356, 2327, 2351, 2351, 2373, 2381, 2388, 2407, 2419, 2425, 2452, 2466, 2460, 2487, 2516, 2528, 2519, 2542, 3049, 3144, 3167, 3197, 3226, 3236, 3391, 3423, 3458, 3473, 3500, 3536, 3562, 3588, 3620, 3645, 3716, 3736, 3770, 3803, 3836, 3876, 3918, 3959, 3989, 4054, 4076, 4143, 4188, 4227, 4276, 4348, 4398, 4467, 4503, 4551, 4613, 4667, 4734, 4809, 4856, 4942, 5016, 5077, 5171, 5219, 5280, 5399, 5476, 5573, 5662, 5766, 5882, 5987, 6051, 6218, 6332, 6520, 6550, 6679, 6827, -1, -1, 7333, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

            this.arcoSeccion = (Math.PI / 180) * (360.0 / numSecciones);

            float max_altura = 0;
            float[] seccion = new float[numSecciones];

            for (int s = 0; s < numSecciones; s++)
            {
                float max_distancia = 0;
                //trama
                byte[] trama = this.tramas[s];

                for (int p = 0; p < this.numPuntos; p++)
                {
                    //arco del punto en radianes, calculado para medio grado, 720 puntos.
                    float arcoA = (float)arcoAvance * p;
                    int v = entero32(trama, 76 + (p * 4));
                    //int v = lista[p];

                    if (v > 0)
                    {
                        //z - altura > se usa el Seno porque tenemos el angulo en negativo avanzando hacia 0º
                        float z = 2470 + (float)(Math.Sin(arcoInicial + arcoA) * v);
                        z = z < 50 ? 0 : z;
                        //xy > Se usa el coseno, que en este caso es positivo, por lo que se resta de la distancia al centro
                        float xy = 1520 - (float)(Math.Cos(arcoInicial + arcoA) * v);
                        xy = xy < 0 ? 0 : xy;

                        //solo si la profundidad no supera el eje ...
                        if (xy > 0 && max_altura < z)
                        {
                            max_altura = z;
                        }

                        //solo cuando exista algo de altura tendre en cuenta la distancia al eje
                        if (z > 0 && max_distancia < xy)
                        {
                            max_distancia = xy;
                        }
                    }
                }
                seccion[s] = max_distancia;
            }

            float minArea = 4000000;
            float[] medidas = null;

            //rotamos las secciones 90º, hayando las distancias mayores x e y, que den la menor area posible.
            for (int r = 0; r < Math.Ceiling(numSecciones / 4.0); r++)
            {
                float minX = 0;
                float maxX = 0;
                float minY = 0;
                float maxY = 0;

                for (int s = 0; s < numSecciones; s++)
                {
                    //arco de la seccion en radianes
                    double arcoS = arcoSeccion * (s + r);

                    float x = (float)Math.Cos(arcoS) * seccion[s];
                    float y = (float)Math.Sin(arcoS) * seccion[s];

                    if (minX > x) minX = x;
                    if (minY > y) minY = y;
                    if (maxX < x) maxX = x;
                    if (maxY < y) maxY = y;
                }

                float area = (maxX - minX) * (maxY - minY);
                //Console.WriteLine(String.Format("{0} - {1} {2} {3} {4}", area, maxX, minX, maxY, minY));

                if (minArea > area)
                {
                    minArea = area;
                    medidas = new float[] { minX, maxX, minY, maxY };
                }
            }

            if (medidas != null)
            {
                return new float[] { max_altura, minArea, medidas[0], medidas[1], medidas[2], medidas[3] };
                //Console.WriteLine(String.Format("{0} - {1} : {2} {3} {4} {5}", max_altura, minArea, medidas[0], medidas[1], medidas[2], medidas[3]));
            }
            else
            {
                return null;
            }
        }

    }
}
