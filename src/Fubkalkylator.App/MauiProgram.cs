using Microsoft.Extensions.Logging;
using Fubkalkylator.Core;
using Fubkalkylator.UI;
using Fubkalkylator.Supabase;

namespace Fubkalkylator.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		// Delade såginställningar (samma som webb-appen).
		builder.Services.AddSingleton<SawSettings>();

		// Supabase om nycklar är ifyllda (SupabaseCredentials) — annars lokal JSON-fil + ingen inloggning.
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
			builder.Services.AddSingleton<ISawJobStore, JsonFileSawJobStore>();
		}

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
