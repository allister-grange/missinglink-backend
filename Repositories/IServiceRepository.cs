using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using missinglink.Models;

namespace missinglink.Repository
{
  public interface IServiceRepository
  {
    Service GetById(int id);
    void Add(Service service);
    void Update(Service service);
    void Delete(Service service);
    IEnumerable<Service> GetAll();
    IEnumerable<ServiceStatistic> GetServiceStatisticsByDate(DateTime startDate, DateTime endDate);
    Task AddStatisticAsync(ServiceStatistic statistic);
    Task AddServicesAsync(List<Service> services);
    void DeleteAllServices();
    Task<int> GetLatestBatchId();
    List<Service> GetByBatchId(int batchId);
  }
}
