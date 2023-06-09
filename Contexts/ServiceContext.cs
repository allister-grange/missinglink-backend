using Microsoft.EntityFrameworkCore;
using missinglink.Models;

namespace missinglink.Contexts
{
  public class ServiceContext : DbContext
  {
    public DbSet<MetlinkService> Services { get; set; }
    public DbSet<ServiceStatistic> ServiceStatistics { get; set; }
    public ServiceContext(DbContextOptions<ServiceContext> options)
    : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      // Configure table names
      modelBuilder.Entity<MetlinkService>().ToTable("services");
      modelBuilder.Entity<ServiceStatistic>().ToTable("service_statistics");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.EnableSensitiveDataLogging();
    }

  }
}