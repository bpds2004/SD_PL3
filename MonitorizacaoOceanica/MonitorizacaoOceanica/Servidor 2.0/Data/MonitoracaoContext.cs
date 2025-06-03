using Microsoft.EntityFrameworkCore;
using Servidor20.Models;

namespace Servidor20.Data
{
    public class MonitoracaoContext : DbContext
    {
        // Construtor com options — usado no servidor para passar a connection string
        public MonitoracaoContext(DbContextOptions<MonitoracaoContext> options)
            : base(options)
        {
        }

        // Construtor sem parâmetros — evita erros se for usado diretamente com "new"
        public MonitoracaoContext()
        {
        }

        // Opcional: apenas se precisares de fallback (não usado com options explícitas)
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Server=localhost,1433;" +
                    "Database=MonitorizacaoOceanica;" +
                    "Trusted_Connection=True;" +
                    "Encrypt=False;" +
                    "TrustServerCertificate=True;");
            }
        }


        public DbSet<Registo> Registos { get; set; } = null!;
    }
}
