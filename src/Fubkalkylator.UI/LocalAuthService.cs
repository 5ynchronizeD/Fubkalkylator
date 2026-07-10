using Fubkalkylator.Core;

namespace Fubkalkylator.UI;

/// <summary>
/// "Ingen inloggning" — används när Supabase inte är konfigurerat. Appen beter sig som
/// förut (ingen inloggningsspärr): alltid inloggad, allt är no-op.
/// </summary>
public sealed class LocalAuthService : IAuthService
{
    public bool IsSignedIn => true;
    public string? Email => null;
    public event Action? Changed { add { } remove { } }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task SendCodeAsync(string email) => Task.CompletedTask;
    public Task<bool> VerifyCodeAsync(string email, string code) => Task.FromResult(true);
    public Task SendMagicLinkAsync(string email, string redirectUrl) => Task.CompletedTask;
    public Task<bool> TrySignInFromUrlAsync(string url) => Task.FromResult(false);
    public Task SignOutAsync() => Task.CompletedTask;
}
