using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Analyze;  // ? namespace do stub gerado de analyze.proto

namespace AnalysisService.Services
{
    // Herdamos de Analyze.AnalysisService.AnalysisServiceBase
    public class AnalysisServiceImpl
        : Analyze.AnalysisService.AnalysisServiceBase
    {
        public override Task<AnalysisResult> Analyze(AnalyzeRequest request, ServerCallContext context)
        {
            var result = new AnalysisResult();
            var ficheiro = "dados.csv";

            if (!File.Exists(ficheiro))
                return Task.FromResult(result);

            var linhas = File.ReadAllLines(ficheiro)
                             .Skip(1) // ignora cabeçalho
                             .Select(l => l.Split(','))
                             .Where(p => p[1] == request.Sensor)
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
