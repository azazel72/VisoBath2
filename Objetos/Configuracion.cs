using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;

namespace VisoBath
{
    public class Configuracion
    {
        public int indice { get; set; }
        public string nombreImpresora { get; set; }
        public string conexion { get; set; }

        public Configuracion()
        {
            this.indice = 0;
            this.nombreImpresora = "";
            this.conexion = "SG";
        }

        public void Cargar(SQLiteDataReader datos)
        {
            if (datos != null)
            {
                while (datos.Read())
                {
                    this.indice = datos.GetInt32(0);
                    this.nombreImpresora = datos.GetString(1);
                    if (datos.FieldCount > 2 && !datos.IsDBNull(2))
                    {
                        this.conexion = datos.GetString(2);
                    }
                }
                datos.Close();
            }
        }

        public string[] GetValoresTabla()
        {
            string[] datos = {
                this.nombreImpresora
            };
            return datos;
        }

        public string[] GetCamposSQL()
        {
            string[] datos = {
                "indice",
                "nombreImpresora"
            };
            return datos;
        }

        public string[] GetValoresSQL()
        {
            string[] datos = {
                this.indice.ToString(),
                "'" + this.nombreImpresora + "'"
            };
            return datos;
        }

    }

}
