using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Fubkalkylator.Core;
using Fubkalkylator.Web;
using Fubkalkylator.UI;
using Fubkalkylator.Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<SawSettings>();

// Supabase om nycklar är ifyllda (SupabaseCredentials) — annars lokal lagring + ingen inloggning.
var supa = SupabaseCredentials.ToConfig();
if (supa.IsConfigured)
{
    builder.Services.AddSingleton(new global::Supabase.Client(
        supa.Url, supa.AnonKey, new global::Supabase.SupabaseOptions()));
    builder.Services.AddSingleton<ISessionStore, JsSessionStore>();
    builder.Services.AddSingleton<IAuthService, SupabaseAuthService>();
    builder.Services.AddSingleton<ISawJobStore, SupabaseSawJobStore>();
}
else
{
    builder.Services.AddSingleton<IAuthService, LocalAuthService>();
    builder.Services.AddSingleton<ISawJobStore, InMemorySawJobStore>();
}

await builder.Build().RunAsync();
