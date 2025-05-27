using Grpc.Core;
using Analyze;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AnalysisService.Services
{
    public class AnalysisServiceImpl : Analyze.AnalysisService.AnalysisServiceBase
    {
        public override Task<AnalysisResult> Analyze(AnalyzeRequest request, ServerCallContext context)
        {
            var result = new AnalysisResult();
            string ficheiro = "dados.csv";

            if (!File.Exists(ficheiro))
                return Task.FromResult(result);

            var linhas = File.ReadAllLines(ficheiro)
                             .Skip(1) // Ignora o cabeçalho
                             .Select(l => l.Split(','))
                             .Where(p => p.Length >= 3 && p[1] == request.Sensor)
                             .Select(p => double.TryParse(p[2], out var val) ? val : (double?)null)
                             .Where(v => v.HasValue)
                             .Select(v => v.Value)
                             .ToList();

            if (linhas.Count > 0)
            {
                result.Count = linhas.Count;
                result.Average = linhas.Average();
            }

            return Task.FromResult(result);
        }
    }
}
