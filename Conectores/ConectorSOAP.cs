using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using VisoBath.Conectores.WSVolumetricas;
using System.Threading;

namespace VisoBath.Conectores
{
    public class ConectorSOAP
    {
        private readonly WSVolumetricas.WSVolumetricasSoapClient _client;

        public ConectorSOAP()
        {
            string endpointUrl = Configuracion.Load().url_conexion;
            _client = new WSVolumetricasSoapClient(
                WSVolumetricas.WSVolumetricasSoapClient.EndpointConfiguration.WSVolumetricasSoap,
                endpointUrl
            );
            // timeouts si quieres:
            _client.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(30);
            _client.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(30);
        }

        public async Task<int> ConfimaOKAsync(CancellationToken ct)
            => await _client.ConfimaOKAsync();

        public async Task<string> ValidateUserAsync(string jsonIn, CancellationToken ct)
            => await _client.ValidateUserAsync(jsonIn);

        public async Task<string> EntradaDatosAsync(string jsonIn, CancellationToken ct)
            => await _client.entradaDatosAsync(jsonIn);

        public async Task<string> SalidaDatosAsync(string jsonIn, CancellationToken ct)
            => await _client.salidaDatosAsync(jsonIn);
    }
}