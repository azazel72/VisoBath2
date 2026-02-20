using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text.Json.Serialization;

namespace VisoBath
{
    public class Albaranes
    {
        public Dictionary<string, Albaran> albaranes { get; set; }

        public Albaranes()
        {
            this.albaranes = new Dictionary<string, Albaran>();
        }

        public void Vaciar()
        {
            this.albaranes.Clear();
        }

        public void Agregar(Albaran a)
        {
            if (!this.albaranes.ContainsKey(a.numeroAlbaran))
            {
                this.albaranes.Add(a.numeroAlbaran, a);
            }
        }

        public void Agregar(SQLiteDataReader datos)
        {
            if (datos != null)
            {
                while (datos.Read())
                {
                    Albaran albaran = new Albaran(datos);
                    this.albaranes.Add(albaran.numeroAlbaran, albaran);
                }
                datos.Close();
            }
        }

        public void RepartirPalets(SQLiteDataReader datos)
        {
            if (datos != null)
            {
                while (datos.Read())
                {
                    Palet palet = new Palet(datos);
                    Albaran albaran = this.Buscar(palet.numeroAlbaran);
                    if (albaran != null)
                    {
                        albaran.AgregarPalet(palet);
                    }                    
                }
                datos.Close();
            }
        }

        public Albaran Buscar(string codigo)
        {
            this.albaranes.TryGetValue(codigo, out Albaran a);
            return a;
        }

        public IEnumerator GetEnumerator()
        {
            return this.albaranes.Values.GetEnumerator();
        }
    }

    public class Albaran
    {
        public string numeroAlbaran { get; set; }
        public int numeroPedido { get; set; }
        public string fechaAlbaran { get; set; }

        public string nombreEmpresa { get; set; }
        public string dirEmpresa { get; set; }
        public int cpEmpresa { get; set; }

        public string razonSocial { get; set; }
        public string direccion { get; set; }
        public string poblacion { get; set; }
        public string provincia { get; set; }
        public string nombreProvincia { get; set; }
        public string pais { get; set; }
        public string telefono { get; set; }
        public string email { get; set; }

        public int totalBultos { get; set; }
        public int bultoActual { get; set; }
        public string fechaIniciado { get; set; }
        public string fechaFinalizado { get; set; }
        public int estado { get; set; }
        private Dictionary<int, Palet> palets { get; set; }

        public Albaran()
        {
            this.palets = new Dictionary<int, Palet>();
            this.fechaIniciado = "";
            this.fechaFinalizado = "";
        }

        public Albaran(SQLiteDataReader datos)
        {
            this.numeroAlbaran = datos.GetString(0);
            this.numeroPedido = datos.GetInt32(1);
            this.fechaAlbaran = datos.GetString(2);
            this.nombreEmpresa = datos.GetString(3);
            this.dirEmpresa = datos.GetString(4);
            this.cpEmpresa = datos.GetInt32(5);
            this.razonSocial = datos.GetString(6);
            this.direccion = datos.GetString(7);
            this.poblacion = datos.GetString(8);
            this.provincia = datos.GetString(9);
            this.nombreProvincia = datos.GetString(10);
            this.pais = datos.GetString(11);
            this.telefono = datos.GetString(12);
            this.email = datos.GetString(13);

            this.totalBultos = datos.GetInt32(14);
            this.bultoActual = datos.GetInt32(15);
            this.fechaIniciado = datos.GetString(16);
            this.fechaFinalizado = datos.GetString(17);
            this.estado = datos.GetInt32(18);
            this.palets = new Dictionary<int, Palet>();
        }

        public void setProvincia(int value)
        {
            this.provincia = value.ToString();
        }

        public void FijarBultos(int totalBultos)
        {
            this.totalBultos = totalBultos;
            this.fechaIniciado = DateTime.Now.ToString();
        }

        public void AgregarPalet(Palet palet)
        {
            this.palets.Add(palet.numero, palet);
            //comprobamos si esta completo
            if (estado == 0)
            {
                if (this.bultoActual < palet.numero)
                {
                    this.bultoActual = palet.numero;
                }
                if (this.bultoActual == this.totalBultos)
                {
                    this.fechaFinalizado = DateTime.Now.ToString();
                    this.estado = 1;
                }
            }
        }

        public Palet ObtenerPalet(int num)
        {
            this.palets.TryGetValue(num, out Palet palet);
            return palet;
        }

        public List<Palet> ListadoPalets()
        {
            return this.palets.Values.ToList<Palet>();
        }

        public string[] GetValoresTabla()
        {
            string[] datos = {
                this.numeroAlbaran,
                this.bultoActual.ToString(),
                this.totalBultos.ToString(),
                this.estado.ToString()
            };
            return datos;
        }

        public string[] GetCamposSQL()
        {
            string[] datos = {
                "numeroAlbaran",
                "numeroPedido",
                "fechaAlbaran",
                "nombreEmpresa", 
                "dirEmpresa", 
                "cpEmpresa", 
                "razonSocial", 
                "direccion",
                "poblacion",
                "provincia",
                "nombreProvincia",
                "pais",
                "telefono",
                "email",
                "totalBultos",
                "fechaIniciado"
            };
            return datos;
        }

        public string[] GetValoresSQL()
        {
            string[] datos = {
                "'" + this.numeroAlbaran + "'",
                this.numeroPedido.ToString(),
                "'" + this.fechaAlbaran + "'",
                "'" + this.nombreEmpresa + "'",
                "'" + this.dirEmpresa + "'",
                this.cpEmpresa.ToString(),
                "'" + this.razonSocial + "'",
                "'" + this.direccion + "'",
                "'" + this.poblacion + "'",
                "'" + this.provincia + "'",
                "'" + this.nombreProvincia + "'",
                "'" + this.pais + "'",
                "'" + this.telefono + "'",
                "'" + this.email + "'",
                "'" + this.totalBultos + "'",
                "'" + this.fechaIniciado + "'"
            };
            return datos;
        }

        public IEnumerator GetEnumerator()
        {
            return this.palets.Values.GetEnumerator();
        }

    }

    public class Palet
    {
        public int numero { get; set; }
        public string hora { get; set; }
        public int peso { get; set; }
        public int volumen { get; set; }
        [JsonIgnore]
        public string numeroAlbaran { get; set; }

        public Palet()
        {
            this.hora = DateTime.Now.ToString();
        }

        public Palet(SQLiteDataReader datos)
        {
            this.numero = datos.GetInt32(0);
            this.hora = datos.GetString(1);
            this.peso = datos.GetInt32(2);
            this.volumen = datos.GetInt32(3);
            this.numeroAlbaran = datos.GetString(4);
        }

        public string[] GetValoresTabla()
        {
            string[] datos = {
                this.numero.ToString(),
                this.hora,
                this.peso.ToString(),
                this.volumen.ToString(),
            };
            return datos;
        }

        public string[] GetCamposSQL()
        {
            string[] datos = {
                "numero",
                "hora",
                "peso",
                "volumen",
                "numeroAlbaran",
            };
            return datos;
        }

        public string[] GetValoresSQL()
        {
            string[] datos = {
                this.numero.ToString(),
                "'" + this.hora + "'",
                this.peso.ToString(),
                this.volumen.ToString(),
                "'" + this.numeroAlbaran + "'"
            };
            return datos;
        }

    }

}
