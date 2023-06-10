using System;
using System.Collections.Generic;
using missinglink.Contexts;
using missinglink.Models;
using System.Linq; // Add this namespace
using System.Threading.Tasks;

namespace missinglink.Metlink.Repository
{
  public class MetlinkServiceRepository : IMetlinkServiceRepository
  {
    private readonly ServiceContext _dbContext;

    public MetlinkServiceRepository(ServiceContext dbContext)
    {
      _dbContext = dbContext;
    }

    public MetlinkService GetById(int id)
    {
      return _dbContext.Services.Find(id);
    }

    public IEnumerable<MetlinkService> GetAll()
    {
      return _dbContext.Services;
    }

    public void Add(MetlinkService service)
    {
      _dbContext.Services.Add(service);
      _dbContext.SaveChanges();
    }

    public void Update(MetlinkService service)
    {
      _dbContext.Services.Update(service);
      _dbContext.SaveChanges();
    }

    public void Delete(MetlinkService service)
    {
      _dbContext.Services.Remove(service);
      _dbContext.SaveChanges();
    }

    public IEnumerable<ServiceStatistic> GetServiceStatisticsByDate(DateTime startDate, DateTime endDate)
    {
      return _dbContext.ServiceStatistics
          .Where(stat => stat.Timestamp >= startDate && stat.Timestamp <= endDate)
          .ToList();
    }

    public async Task AddStatisticAsync(ServiceStatistic statistic)
    {
      await _dbContext.ServiceStatistics.AddAsync(statistic);
      await _dbContext.SaveChangesAsync();
    }

    public async Task AddServicesAsync(List<MetlinkService> services)
    {
      await _dbContext.Services.AddRangeAsync(services);
      await _dbContext.SaveChangesAsync();
    }

    public void DeleteAllServices()
    {
      _dbContext.Services.RemoveRange(_dbContext.Services);
      _dbContext.SaveChanges();
    }

  }
}

