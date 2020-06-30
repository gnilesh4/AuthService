using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AuthService
{
    public class Program
    {
        public static void Main()
        {
            Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>()).Build().Run();
        }
    }
}
