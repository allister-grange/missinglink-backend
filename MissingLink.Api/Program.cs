using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace missinglink
{
  public class Program
  {
    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
              webBuilder.UseStartup<Startup>();
              webBuilder.UseUrls("http://localhost:5002", "https://localhost:5001");
            });
  }
}
