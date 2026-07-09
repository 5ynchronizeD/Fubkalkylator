namespace Fubkalkylator.Supabase;

/// <summary>
/// Supabase-anslutning. anon-nyckeln är publik by design och får ligga i klienten;
/// service_role-nyckeln får ALDRIG hamna här.
/// </summary>
public sealed class SupabaseConfig
{
    public required string Url { get; init; }
    public required string AnonKey { get; init; }

    /// <summary>True när riktiga värden är ifyllda (annars används lokal lagring).</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(AnonKey)
        && Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
