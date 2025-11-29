using Microsoft.EntityFrameworkCore;
using IotMonitoringApi.Models;

namespace IotMonitoringApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Measurement> Measurements { get; set; }
        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
    }
}
