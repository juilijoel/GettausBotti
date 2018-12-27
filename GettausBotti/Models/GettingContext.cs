using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GettausBotti.Models
{
    public class GettingContext : DbContext
    {
        public DbSet<GetAttempt> GetAttempts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //Set config file
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            optionsBuilder.UseSqlServer(configuration["connectionString"]);
        }
    }

    public class GetAttempt
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public long ChatId { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
