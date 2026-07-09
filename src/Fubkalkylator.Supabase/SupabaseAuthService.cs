using System.Text.Json;
using Fubkalkylator.Core;
using Supabase.Gotrue;

namespace Fubkalkylator.Supabase;

/// <summary>
/// Inloggning via e-post-kod (OTP) mot Supabase. Sessionen (access/refresh-token) sparas
/// via <see cref="ISessionStore"/> — localStorage på webben, SecureStorage på Android.
/// </summary>
public sealed class SupabaseAuthService : IAuthService
{
    private readonly global::Supabase.Client _client;
    private readonly ISessionStore _sessions;

    public SupabaseAuthService(global::Supabase.Client client, ISessionStore sessions)
    {
        _client = client;
        _sessions = sessions;
    }

    public bool IsSignedIn => _client.Auth.CurrentSession is not null;
    public string? Email => _client.Auth.CurrentUser?.Email;
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        await _client.InitializeAsync();
        var saved = await _sessions.LoadAsync();
        if (!string.IsNullOrEmpty(saved))
        {
            try
            {
                var s = JsonSerializer.Deserialize<Saved>(saved);
                if (s is not null && !string.IsNullOrEmpty(s.Access) && !string.IsNullOrEmpty(s.Refresh))
                    await _client.Auth.SetSession(s.Access, s.Refresh);
            }
            catch { await _sessions.ClearAsync(); }
        }
        Changed?.Invoke();
    }

    public Task SendCodeAsync(string email)
        => _client.Auth.SignInWithOtp(new SignInWithPasswordlessEmailOptions(email));

    public async Task<bool> VerifyCodeAsync(string email, string code)
    {
        // Första inloggningen kan ge en "signup"-token, senare en vanlig "email"-token.
        // Prova de troliga typerna så det funkar oavsett Supabase-inställning.
        Session? session = null;
        foreach (var type in new[]
                 {
                     Constants.EmailOtpType.Email,
                     Constants.EmailOtpType.Signup,
                     Constants.EmailOtpType.MagicLink,
                 })
        {
            try
            {
                session = await _client.Auth.VerifyOTP(email, code, type);
                if (session is not null) break;
            }
            catch { /* fel typ — prova nästa */ }
        }
        if (session is null) return false;
        await PersistAsync();
        Changed?.Invoke();
        return true;
    }

    public async Task SignOutAsync()
    {
        await _client.Auth.SignOut();
        await _sessions.ClearAsync();
        Changed?.Invoke();
    }

    private async Task PersistAsync()
    {
        var s = _client.Auth.CurrentSession;
        if (s?.AccessToken is null || s.RefreshToken is null) return;
        await _sessions.SaveAsync(JsonSerializer.Serialize(new Saved
        {
            Access = s.AccessToken,
            Refresh = s.RefreshToken,
        }));
    }

    private sealed class Saved
    {
        public string Access { get; set; } = "";
        public string Refresh { get; set; } = "";
    }
}
