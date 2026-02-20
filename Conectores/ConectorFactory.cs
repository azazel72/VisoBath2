using System.Threading.Tasks;

namespace VisoBath.Conectores
{
    class ConectorFactory
    {
        public static Task SolicitarAlbaran(Gestor g, string codigo, string tipoConector = "SG")
        {
            if (tipoConector == "SAP")
            {
                return ConectorSAP.SolicitarAlbaran(g, codigo);
            }
            else
            {
                return ConectorSG.SolicitarAlbaran(g, codigo);
            }
        }

        public static Task EnviarNotificacion(Gestor g, Albaran albaran, string tipoConector = "SG")
        {
            if (tipoConector == "SAP")
            {
                return ConectorSAP.EnviarNotificacion(g, albaran);
            }
            else
            {
                return ConectorSG.EnviarNotificacion(g, albaran);
            }
        }
    }
}
