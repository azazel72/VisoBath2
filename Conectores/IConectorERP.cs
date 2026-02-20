using System.Threading.Tasks;

namespace VisoBath.Conectores
{
    interface IConectorERP
    {
        Task SolicitarAlbaran(Gestor g, string codigo);
        Task EnviarNotificacion(Gestor g, Albaran albaran);
    }
}
