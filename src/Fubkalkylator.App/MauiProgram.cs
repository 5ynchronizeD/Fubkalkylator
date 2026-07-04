using Microsoft.Extensions.Logging;
using Fubkalkylator.UI;

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

		// Loggbok — beständig JSON-fil i appens datamapp.
		builder.Services.AddSingleton<Fubkalkylator.Core.ISawJobStore, JsonFileSawJobStore>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
