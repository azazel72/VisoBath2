using System;

namespace VisoBath.MME
{
    // Mantiene juntos el sector solicitado y la orientación física del sensor.
    internal sealed class PerfilMaquina
    {
        public string Modo { get; private set; }
        public string IpLidar { get; private set; }
        public int AnguloInicial { get; private set; }
        public double CorreccionAngular { get; private set; }
        public float DistanciaEje { get; private set; }

        public double AnguloCalculo
        {
            get { return (AnguloInicial / 10000.0 + CorreccionAngular) * Math.PI / 180; }
        }

        public static PerfilMaquina Crear(string modo)
        {
            switch ((modo ?? "B").Trim().ToUpperInvariant())
            {
                case "A":
                    return new PerfilMaquina { Modo = "A", IpLidar = "192.78.70.118",
                        AnguloInicial = -765000, CorreccionAngular = 0, DistanciaEje = 1500 };
                case "B":
                    // Conserva exactamente la geometría utilizada por el proyecto actual.
                    return new PerfilMaquina { Modo = "B", IpLidar = "192.78.70.173",
                        AnguloInicial = 135000, CorreccionAngular = -90, DistanciaEje = 1520 };
                default:
                    throw new ArgumentException("configuracion_maquina debe ser A o B.");
            }
        }
    }
}
