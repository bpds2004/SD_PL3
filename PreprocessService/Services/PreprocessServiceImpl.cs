using Grpc.Core;
using Preprocess;
using System.Threading.Tasks;

namespace PreprocessService.Services
{
    public class PreprocessServiceImpl : PreprocessService.PreprocessServiceBase
    {
        public override Task<PreprocessResponse> Preprocess(PreprocessRequest request, ServerCallContext context)
        {
            // Conversão do valor
            float valorConvertido = float.TryParse(request.valor, out float v) ? v : 0;

            // Conversão de timestamp para long uniforme
            long timestamp = long.TryParse(request.timestamp, out long t) ? t : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var response = new PreprocessResponse
            {
                Id = request.id,
                Sensor = request.sensor.ToLower(),
                Valor = valorConvertido,
                Timestamp = timestamp
            };

            return Task.FromResult(response);
        }
    }
}
