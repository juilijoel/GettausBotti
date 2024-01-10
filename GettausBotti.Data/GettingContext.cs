namespace GettausBotti.Data
{
    using GettausBotti.Data.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    public class GettingContext : DbContext
    {
        public GettingContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public DbSet<GetAttempt> GetAttempts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Configuration["connectionString"]);
        }
    }
}