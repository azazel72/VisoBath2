using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisoBath
{
    public class DatosVolumetrico
    {
        public List<int[]> lecturas { get; set; }
        public int[] alturas { get; set; }
        public int vuelta_subida { get; set; }
        public int diente_subida { get; set; }
        public int vuelta_fin { get; set; }
        public int diente_fin { get; set; }
        public int vueltas { get; set; }
        public int dientes { get; set; }
        public int peso { get; set; }
        public double volumen { get; set; }
        public int alturaMaxima { get; set; }
        public double alturaNormalizada { get; set; }
        private List<List<double>> puntos { get; set; }
        private List<double> alturasPuntos { get; set; }
        private double arco { get; set; }
        private double seno { get; set; }
        private double constanteAltura { get; set; }
        private double constanteDistancia { get; set; }

        private int numDientes = 86;

        public string resumen { get; set; }

        public DatosVolumetrico()
        {
            this.lecturas = new List<int[]>();
            this.puntos = new List<List<double>>();
            this.alturasPuntos = new List<double>();
            this.arco = 2 * Math.PI / this.numDientes;
            this.seno = Math.Sin(arco);
            //maxima altura: 2662mm.
            //minima altura: 705mm.
            //radio del disco = 820, pero el 0 del sensor esta establecido en 850mm del centro.

            this.constanteAltura = (2662 - 705) / 1023.0;
            this.constanteDistancia = 850 / 1023.0;

            this.resumen = "";
        }

        /// <summary>
        /// Normalizamos todas las capas a 86 valores, incluyendo el primero del siguiente al final, y si tiene 85, tomando el ultimo o el primero del anterior.
        /// Añadimos la capa 0 a ras del suelo, como copia de la primera.
        /// </summary>
        public void Normalizar()
        {
            //deben  de existir mas de una vuelta
            if (this.vueltas == 0)
            {
                return;
            }
            if (this.vuelta_fin < this.vuelta_subida)
            {
                this.vuelta_fin = this.vuelta_subida;
            }
            //for (int v = this.vuelta_subida; v < (this.vuelta_fin+1); v++)
            for (int v = 1; v < (this.vuelta_fin + 1); v++)
            {
                    this.puntos.Add(NormalizarPuntos(v));
                this.alturasPuntos.Add(NormalizarAltura(this.alturas[v]));
            }
            //agregamos una capa mas a la maxima altura
            this.alturaNormalizada = NormalizarAltura(this.alturaMaxima);
            List<double> capa = new List<double>(this.puntos.Last().ToArray());
            this.puntos.Add(capa);
            if (this.alturaNormalizada < 710)
            {
                //altura minima que detecta el sensor de fin de palet en mm 710mm
                this.alturasPuntos.Add(710);
                this.alturaNormalizada = 710;
            }
            else
            {
                this.alturasPuntos.Add(this.alturaNormalizada);
            }            
        }
        private List<double> NormalizarPuntos(int indice)
        {
            int[] lectura = this.lecturas[indice];
            List<double> capa = new List<double>();
            //pasamos los primeros 86 valores
            for (int i = 0; i < numDientes; i++)
            {
                capa.Add(NormalizarDistancia(lectura[i]));
            }
            //si la ultima lectura es cero, la eliminamos y colocamos la anterior lectura al principio
            if (lectura[numDientes - 1] == 0)
            {
                capa.RemoveAt(numDientes - 1);
                if (this.puntos.Count > 0)
                {
                    capa.Insert(0, this.puntos.Last()[numDientes - 1]);
                }
                else
                {
                    if (indice < (this.vueltas - 1))
                    {
                        capa.Prepend(NormalizarDistancia(this.lecturas[indice + 1][0]));
                    }
                    else
                    {
                        capa.Prepend(capa[0]);
                    }
                        
                }
            }
            //agregamos la siguiente lectura a la ultima posicion para tener 87 puntos con 86 lecturas.
            if (indice < (this.vueltas - 1))
            {
                capa.Add(NormalizarDistancia(this.lecturas[indice + 1][0]));
            }
            else
            {
                capa.Add(capa[0]);
            }
            return capa;
        }

        /// <summary>
        /// Calculamos el volumen por exceso
        /// </summary>
        /// <returns></returns>
        public void CalcularExceso()
        {
            this.volumen = 0;
            List<double> puntosArea = new List<double>();
            double punto;
            //no se necesita el ultimo punto repetido
            for (int i = 0; i < numDientes; i++)
            {
                punto = 0.0;
                for (int j = 0; j < this.puntos.Count; j++)
                {
                    if (this.puntos[j][i] > punto)
                    {
                        punto = this.puntos[j][i];
                    }
                }
                puntosArea.Add(punto);
            }
            //ya tenemos los puntos maximos
            //rotamos 90º (18 puntos), para comprobar cual da menor area. Area = [ area, x, y ]
            double[] areaMenor = { double.PositiveInfinity, 0.0, 0.0 };
            for (int i = 0; i < (numDientes / 4); i++)
            {
                double[] area = CalcularAreaExceso(puntosArea, i);
                //Console.WriteLine(area[0] + "-" + area[1] + "-" + area[2]);
                if (areaMenor[0] > area[0])
                {
                    areaMenor = area;
                }
            }

            //Parche lectura para Visobath.
            double factorAjuste1 = .78;
            double factorAjuste2 = .72;
            if (areaMenor[1] > areaMenor[2])
            {
                areaMenor[1] *= factorAjuste1;
                areaMenor[2] *= factorAjuste2;
            } else
            {
                areaMenor[1] *= factorAjuste2;
                areaMenor[2] *= factorAjuste1;
            }
            areaMenor[0] = areaMenor[1] * areaMenor[2];
            //

            this.volumen = areaMenor[0] * this.alturaNormalizada;
            //this.resumen = "Peso: " + this.peso.ToString() + ", Volumen: " + (int)(this.volumen / 1000000) + ", Lado 1: " + (int)areaMenor[1] + ", Lado 2: " + (int)areaMenor[2] + ", Altura: " + (int)this.alturaNormalizada;
            this.resumen = "Peso: " + this.peso.ToString() + ", Volumen: " + (int)(this.volumen / 1000000) + ", Lado 1: " + (int)areaMenor[1] + ", Lado 2: " + (int)areaMenor[2] + ", Altura: " + (int)this.alturaNormalizada;
        }


        private double[] CalcularAreaExceso(List<double> puntosArea, int desplazamiento)
        {
            List<double> xs = new List<double>();
            List<double> ys = new List<double>();

            for (int i = 0; i < this.numDientes; i++)
            {
                xs.Add(getX(i + desplazamiento, puntosArea[i]));
                ys.Add(getY(i + desplazamiento, puntosArea[i]));
            }

            //el ancho sera la distancia entre el menor x y el mayor x
            double minimoX = xs.Min();
            double maximoX = xs.Max();
            //la profundidad sera la distancia entre el menor y y el mayor y
            double minimoY = ys.Min();
            double maximoY = ys.Max();

            double ancho = maximoX - minimoX;
            double profundo = maximoY - minimoY;

            //Console.WriteLine("Ancho X: " + ancho.ToString() + ", Y: " + profundo); ;

            double[] area = { ancho * profundo, ancho, profundo };
            return area;
        }
        private double getX(int numero_diente, double distancia)
        {
            return Math.Cos(this.arco * numero_diente) * distancia;
        }
        private double getY(int numero_diente, double distancia)
        {
            return Math.Sin(this.arco * numero_diente) * distancia;
        }

        /// <summary>
        /// Calculamos el volumen real por rodajas
        /// </summary>
        /// <returns></returns>
        public double Calcular()
        {
            this.volumen = 0;
            for (int s = 0; s < this.puntos.Count; s++)
            {
                double alturaPrevia = 0;
                if (s > 0)
                {
                    alturaPrevia = this.alturasPuntos[s - 1];
                }
                double altura = this.alturasPuntos[s] - alturaPrevia;
                List<double> seccion = this.puntos[s];
                for (int i = 0; i < this.numDientes; i++)
                {
                    double h1 = seccion[i];
                    double h2 = seccion[i + 1];
                    double area = h1 * h2 * this.seno / 2;
                    //Console.WriteLine(s.ToString() + i.ToString() + "   H1:" + h1.ToString()+ " H2:" + h2.ToString() + ", Area: " + area.ToString() + ", altura: " + altura.ToString() + ". Altura real: " + this.alturasPuntos[s].ToString());
                    double v = area * altura;
                    this.volumen += v;
                }
            }
            Console.WriteLine(volumen);
            return this.volumen;
        }

        private double NormalizarDistancia(int distancia)
        {
            int nuevoValor = (int) (distancia * this.constanteDistancia);
            return 850 - nuevoValor;
        }

        private double NormalizarAltura(int altura)
        {
            //705 sensor hasta el suelo 705 - 110 altura plataforma = 595
            int nuevoValor = (int)(altura * this.constanteAltura);
            return nuevoValor + 595;
        }
    }
}
