using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace VisoBath.Conectores.WSVolumetricas
{
    public class EnvioNotificacion
    {
        public string tira { get; set; }
        public string albaran { get; set; }
        public int num_palets { get; set; }
        public string accion { get; set; }
        public List<Palet> palets { get; set; }
    }

    public class Palet
    {
        public int numero { get; set; }
        public string hora { get; private set; }
        public void SetHora(string nuevaHora)
        {

            var s = "19/10/2025 16:36:07";
            var dt = DateTime.ParseExact(s, "dd/MM/yyyy HH:mm:ss", new CultureInfo("es-ES"));

            // Si quieres mantener la zona de Madrid:
            var dto = new DateTimeOffset(dt, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time").GetUtcOffset(dt));

            // En ISO 8601 con offset:
            var isoConOffset = dto.ToString("o");              // "2025-10-19T16:36:07+02:00"

            // En ISO 8601 sin offset (UTC):
            var isoUtc = dto.ToUniversalTime().ToString("o");  // "2025-10-19T14:36:07Z"

            hora = isoConOffset;
        }

        public int peso { get; set; }
        public int volumen { get; set; }

    }

}
