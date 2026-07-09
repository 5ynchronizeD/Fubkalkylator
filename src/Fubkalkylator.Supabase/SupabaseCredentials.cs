namespace Fubkalkylator.Supabase;

/// <summary>
/// Fyll i från ditt Supabase-projekt (Settings → API). Delas av webb och Android.
/// anon-nyckeln är PUBLIK by design och får ligga här. Lägg ALDRIG service_role-nyckeln här.
/// Är fälten tomma används lokal lagring och ingen inloggning (appen fungerar som förut).
/// </summary>
public static class SupabaseCredentials
{
    public const string Url = "";
    public const string AnonKey = "";

    public static SupabaseConfig ToConfig() => new() { Url = Url, AnonKey = AnonKey };
}
