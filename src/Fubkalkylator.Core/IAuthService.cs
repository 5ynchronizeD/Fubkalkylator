namespace Fubkalkylator.Core;

/// <summary>
/// Inloggning (e-post-kod / OTP). Plattformen avgör implementationen; abstraktionen
/// hålls i Core så delade UI-komponenter kan använda den utan backend-beroende.
/// </summary>
public interface IAuthService
{
    /// <summary>Finns en giltig session?</summary>
    bool IsSignedIn { get; }

    /// <summary>Inloggad användares e-post, eller null.</summary>
    string? Email { get; }

    /// <summary>Trigga när inloggningsstatus ändras (UI kan uppdatera sig).</summary>
    event Action? Changed;

    /// <summary>Återställ ev. sparad session vid appstart.</summary>
    Task InitializeAsync();

    /// <summary>Skicka en inloggningskod till e-postadressen (OTP-kod, används på Android).</summary>
    Task SendCodeAsync(string email);

    /// <summary>Verifiera koden. Returnerar true vid lyckad inloggning.</summary>
    Task<bool> VerifyCodeAsync(string email, string code);

    /// <summary>Skicka en magisk inloggningslänk som pekar tillbaka till <paramref name="redirectUrl"/> (webben).</summary>
    Task SendMagicLinkAsync(string email, string redirectUrl);

    /// <summary>Logga in från en återvändande länk (token i URL:ens fragment). True om inloggad.</summary>
    Task<bool> TrySignInFromUrlAsync(string url);

    /// <summary>Logga ut och glöm sessionen.</summary>
    Task SignOutAsync();
}

/// <summary>
/// Beständig lagring av inloggningssessionen (en sträng). Plattformsberoende:
/// localStorage på webben, SecureStorage på Android.
/// </summary>
public interface ISessionStore
{
    Task<string?> LoadAsync();
    Task SaveAsync(string value);
    Task ClearAsync();
}
