using System;

namespace VisoBath.Conectores
{
    internal enum AccionMaquina { Ninguna, Iniciar, Peso, Capturar, Completar, Cancelar }

    internal interface IProtocoloMaquina
    {
        AccionMaquina Interpretar(NotificacionPLC mensaje);
    }

    // El PLC entrega las senales fisicas como inicio/peso/vuelta.
    // La segunda vuelta abre la captura y la tercera la cierra, como en Gestor original.
    internal class ProtocoloMaquinaB : IProtocoloMaquina
    {
        protected int vueltas;
        protected bool iniciado;
        protected bool capturando;

        public virtual AccionMaquina Interpretar(NotificacionPLC mensaje)
        {
            if (mensaje == null) return AccionMaquina.Ninguna;
            switch (mensaje.accion)
            {
                case "inicio":
                    vueltas = 0;
                    iniciado = true;
                    capturando = false;
                    return AccionMaquina.Iniciar;
                case "peso": return AccionMaquina.Peso;
                case "vuelta": return ProcesarVuelta(mensaje.valor);
                default: return AccionMaquina.Ninguna;
            }
        }

        protected virtual AccionMaquina ProcesarVuelta(int valor)
        {
            if (!iniciado) return AccionMaquina.Ninguna;
            if (capturando)
            {
                iniciado = capturando = false;
                return AccionMaquina.Completar;
            }
            if (++vueltas > 1)
            {
                capturando = true;
                return AccionMaquina.Capturar;
            }
            return AccionMaquina.Ninguna;
        }
    }

    internal sealed class ProtocoloMaquinaA : ProtocoloMaquinaB
    {
        public override AccionMaquina Interpretar(NotificacionPLC mensaje)
        {
            if (mensaje != null && string.Equals(mensaje.accion, "paro", StringComparison.Ordinal))
            {
                iniciado = capturando = false;
                vueltas = 0;
                return AccionMaquina.Cancelar;
            }
            return base.Interpretar(mensaje);
        }

        protected override AccionMaquina ProcesarVuelta(int valor)
        {
            if (!iniciado) return AccionMaquina.Ninguna;
            // Sin contador en el mensaje, conserva el protocolo antiguo de pulsos.
            if (valor == 0) return base.ProcesarVuelta(valor);
            if (valor < 0 || valor <= vueltas) return AccionMaquina.Ninguna;
            vueltas = valor;
            if (capturando)
            {
                iniciado = capturando = false;
                // Una vuelta perdida invalida una captura que ya no abarca 360 grados.
                return valor == 3 ? AccionMaquina.Completar : AccionMaquina.Cancelar;
            }
            if (valor == 2)
            {
                capturando = true;
                return AccionMaquina.Capturar;
            }
            if (valor > 2)
            {
                iniciado = false;
                return AccionMaquina.Cancelar;
            }
            return AccionMaquina.Ninguna;
        }
    }
}
