using Microsoft.AspNetCore.Builder;
using Microsoft.Net.Http.Headers;
using Orleans;
using Orleans.Hosting;
using SBMonitor.API.Hubs;

var builder = Host.CreateDefaultBuilder(args);

builder.UseOrleans(builder =>
     {
         builder.ConfigureApplicationParts(manager =>
         {
             manager.AddApplicationPart(typeof(Program).Assembly).WithReferences();
         });

         builder.UseDashboard();
         builder.ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddConsole());

         builder.UseSignalR(b =>
             b.Configure((innerSiloBuilder, config) =>
             {
                 innerSiloBuilder.UseLocalhostClustering();
                 innerSiloBuilder.AddSimpleMessageStreamProvider("SMS");
                 innerSiloBuilder.AddAzureTableGrainStorageAsDefault(
                     configureOptions: options =>
                     {
                         options.TableName = "kiscica";
                         options.UseJson = true;
                         options.ConfigureTableServiceClient("DefaultEndpointsProtocol=https;AccountName=dcslhmsa;AccountKey=CBJBhy3sbNnJRN06sQ220SlznTbQt42heAOzo54559acjBLSJXAmNSi5nnrTS+YFNgWyFewN/MYV+AStXpMrqw==;EndpointSuffix=core.windows.net");
                     });
             })
             ).RegisterHub<MessageMonitor>();
     });

builder.ConfigureWebHostDefaults(webBuilder =>
{
    webBuilder.ConfigureServices(services =>
    {
        services.AddControllers();
    });
    webBuilder.Configure(app =>
    {
        app.UseCors(builder => builder.SetIsOriginAllowed(isOriginAllowed: _ => true).WithExposedHeaders(HeaderNames.ContentDisposition).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapHub<MessageMonitor>("/MessageMonitor");
        });
    });
});

builder.ConfigureServices(services =>
{
    services.Configure<ConsoleLifetimeOptions>(options =>
    {
        options.SuppressStatusMessages = true;
    });

    services.AddSignalR().AddOrleans();
});

await builder.RunConsoleAsync();
