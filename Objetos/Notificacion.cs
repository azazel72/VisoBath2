using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;

namespace VisoBath
{
    public class Notificacion
    {
        public string tira { get; set; }
        public string albaran { get; set; }
        public int num_palets { get; set; }
        public string accion { get; set; }
        public List<Palet> palets { get; set; }

        public Notificacion(string tira, Albaran albaran, string accion = "I")
        {
            this.tira = tira;
            this.albaran = albaran.numeroAlbaran;
            this.num_palets = albaran.totalBultos;
            this.palets = albaran.ListadoPalets();
            this.accion = accion;
        }
    }

    public class NotificacionPLC
    {
        public string accion { get; set; }
        public int valor { get; set; }

        public NotificacionPLC()
        {
            this.accion = "";
            this.valor = 0;
        }
    }
}
