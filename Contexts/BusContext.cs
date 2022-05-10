using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using missinglink.Models;

namespace missinglink.Contexts
{
  public class BusContext : DbContext
  {
    public DbSet<Bus> Buses { get; set; }
    public DbSet<BusStatistic> BusStatistic { get; set; }
    public BusContext(DbContextOptions<BusContext> options)
    : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.EnableSensitiveDataLogging();
    }

  }
}