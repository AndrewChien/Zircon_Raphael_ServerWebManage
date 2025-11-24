using WebManagement;

/// <summary>
/// lyo 2025-11-6 08:49:55
/// TabBlazor框架（使用方法：https://tabblazor.com/dashboard）
/// https://github.com/TabBlazor/TabBlazor
/// </summary>
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
            });
}

