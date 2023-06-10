using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using missinglink.Models;

namespace missinglink.Metlink.Repository
{
  public interface IMetlinkServiceRepository
  {
    MetlinkService GetById(int id);
    void Add(MetlinkService service);
    void Update(MetlinkService service);
    void Delete(MetlinkService service);
    IEnumerable<MetlinkService> GetAll();
    IEnumerable<ServiceStatistic> GetServiceStatisticsByDate(DateTime startDate, DateTime endDate);
    Task AddStatisticAsync(ServiceStatistic statistic);
    Task AddServicesAsync(List<MetlinkService> services);
    void DeleteAllServices();
    Task<int> GetLatestBatchId();
    List<MetlinkService> GetByBatchId(int batchId);
  }
}
