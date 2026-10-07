using System.Threading.Tasks;

namespace VisoBath.Conectores
{
    class ConectorFactory
    {
        public static Task SolicitarAlbaran(Gestor g, string codigo)
        {
            return ConectorSAP.SolicitarAlbaran(g, codigo);
        }

        public static Task EnviarNotificacion(Gestor g, Albaran albaran)
        {
            return ConectorSAP.EnviarNotificacion(g, albaran);
        }
    }
}
