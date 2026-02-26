using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisoBath.Conectores.WSVolumetricas
{
    internal class EnvioTira
    {
        public string tira { get; set; }

        public EnvioTira(RecepcionToken r)
        {
            this.tira = r?.Token ?? "";
        }
    }
}
