using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Fubkalkylator.Web;
using Fubkalkylator.UI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<SawSettings>();
builder.Services.AddSingleton<Fubkalkylator.Core.ISawJobStore, InMemorySawJobStore>();

await builder.Build().RunAsync();
