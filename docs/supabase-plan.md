# Genomförandeplan — Supabase + inloggning (e-post-kod) + GitHub Pages

Mål: publik **hemsida** (GitHub Pages) + fortsatt **Android-app**, båda mot **samma data** i
Supabase, med **e-post-kod (OTP)** som inloggning. iOS-användare når appen via hemsidan
(ingen Apple-avgift). Android sideloadas som nu.

## 0. Arkitektur i korthet

```
Fubkalkylator.Core        (ren logik + modeller + ISawJobStore)   ← oförändrad
Fubkalkylator.UI          (delade Razor-komponenter)              ← + login-UI, auth-gate
Fubkalkylator.Supabase    (NYTT delat projekt)                    ← Supabase-store + auth
Fubkalkylator.Web         (Blazor WASM → GitHub Pages)            ← DI + web-session
Fubkalkylator.App         (MAUI Android)                          ← DI + SecureStorage-session
```

Kärnidé: allt Supabase-beroende samlas i **ett nytt delat projekt** som både Web och App
refererar. `Core` förblir helt fri från Supabase. Loggboken byter bara `ISawJobStore`-
implementation; UI:t är redan delat, så login-vyn skrivs en gång.

Vad som flyttar till molnet i v1: **loggboken** (`SawJob`). Inställningar (sågspår, klämma,
sågordning) och orderlistan lämnas kvar i `localStorage` tills vidare — de kan flyttas senare
utan att röra det här.

---

## 1. Förberedelser (du gör — ~15 min)

1. Skapa ett gratis Supabase-projekt på https://supabase.com. Notera:
   - **Project URL** (`https://<ref>.supabase.co`)
   - **anon public key** (får ligga i klienten — den är publik by design)
   - ⚠️ **service_role**-nyckeln får ALDRIG hamna i appen/repot.
2. **Authentication → Providers → Email**: slå på. Sätt "Confirm email" enligt smak; OTP
   (kod) fungerar oavsett. Under **Email templates** kan "Magic Link"-mallen visa `{{ .Token }}`
   (koden) om du vill ha kod i mejlet.
3. **GitHub → repo → Settings → Pages → Source: GitHub Actions** (aktiveras i fas 5).

---

## 2. Supabase: tabell + RLS (SQL att klistra in i SQL Editor)

```sql
-- Loggboken. En rad per sågning, ägd av inloggad användare.
create table public.saw_jobs (
  id                     integer generated always as identity primary key,
  user_id                uuid not null default auth.uid(),
  saved_at               timestamptz not null default now(),

  -- teoretiskt (från beräkningen)
  stock_fub_inches       double precision not null,
  kerf_inches            double precision not null,
  target_thickness_inches double precision,
  target_width_inches    double precision,
  block_width_inches     double precision not null,
  block_height_inches    double precision not null,
  stock_length_inches    double precision,
  timber_volume_m3       double precision,
  estimated_value        double precision,
  yield_percent          integer,
  calculated_outcome     text not null default '',

  -- faktiskt (du fyller i)
  species                text not null default '',
  actual_outcome         text not null default '',
  note                   text not null default '',
  photo_data_url         text,               -- se not om storlek nedan
  drying                 integer not null default 0,
  drying_start           timestamptz,
  moisture_readings      jsonb not null default '[]'  -- List<MoistureReading> som JSON
);

-- Radnivå-säkerhet: varje konto ser BARA sina egna rader.
alter table public.saw_jobs enable row level security;

create policy "saw_jobs select own" on public.saw_jobs
  for select using (auth.uid() = user_id);
create policy "saw_jobs insert own" on public.saw_jobs
  for insert with check (auth.uid() = user_id);
create policy "saw_jobs update own" on public.saw_jobs
  for update using (auth.uid() = user_id) with check (auth.uid() = user_id);
create policy "saw_jobs delete own" on public.saw_jobs
  for delete using (auth.uid() = user_id);
```

Not:
- `moisture_readings` som **jsonb** slipper en join och matchar `List<MoistureReading>`.
- `photo_data_url` är base64-JPEG → kan bli stort per rad. OK för personligt bruk, men
  flytta gärna foton till **Supabase Storage** (URL i stället för base64) i en senare fas.
- `id` som `integer identity` matchar `SawJob.Id` (int). Unikt globalt, men RLS gör att du
  bara ser dina.

---

## 3. Nytt delat projekt: `Fubkalkylator.Supabase`

```
dotnet new classlib -n Fubkalkylator.Supabase -o src/Fubkalkylator.Supabase
dotnet add src/Fubkalkylator.Supabase package Supabase          # supabase-csharp (pinna versionen!)
dotnet add src/Fubkalkylator.Supabase reference src/Fubkalkylator.Core/Fubkalkylator.Core.csproj
# och lägg till projektet i .slnx + referera från Web och App
```

### 3a. Konfig (anon-key är publik)

```csharp
public sealed class SupabaseConfig
{
    public required string Url { get; init; }
    public required string AnonKey { get; init; }
}
```
Sätts vid DI-registrering (steg 5). Ingen hemlighet läcker.

### 3b. Auth-abstraktion (delad)

```csharp
// I Core eller UI (så UI-komponenter kan injicera den utan Supabase-beroende)
public interface IAuthService
{
    bool IsSignedIn { get; }
    string? Email { get; }
    event Action? Changed;

    Task SendCodeAsync(string email);                 // Supabase: SignInWithOtp
    Task<bool> VerifyCodeAsync(string email, string code); // Supabase: VerifyOTP → session
    Task SignOutAsync();
    Task InitializeAsync();                            // återställ sparad session vid start
}
```

`SupabaseAuthService` (i det nya projektet) implementerar den runt supabase-csharp:s
`Client.Auth.SignInWithOtp(...)` och `VerifyOTP(email, token, OtpType.Email)`. Vid lyckad
verifiering finns en session → RLS gör att alla efterföljande anrop scoping:as till kontot.

### 3c. Session-persistens (den enda plattforms-forkade delen)

supabase-csharp tar en session-hanterare. Två små implementationer:
- **Web:** spara/läsa sessions-JSON i `localStorage` via `IJSRuntime` (samma `appStore` du redan har).
- **Android:** `Microsoft.Maui.Storage.SecureStorage`.

Ett gränssnitt räcker:
```csharp
public interface ISessionStore   // set/get/clear en sträng
{
    Task<string?> LoadAsync();
    Task SaveAsync(string json);
    Task ClearAsync();
}
```

### 3d. Store mot Supabase

```csharp
// postgrest-modell (attribut från supabase-csharp)
[Table("saw_jobs")]
public sealed class SawJobRow : BaseModel
{
    [PrimaryKey("id", false)] public int Id { get; set; }
    [Column("saved_at")] public DateTime SavedAt { get; set; }
    // ... övriga kolumner ...
    [Column("moisture_readings")] public List<MoistureReading> MoistureReadings { get; set; } = new();
    // user_id sätts av DB:ns default auth.uid() — skicka INTE med den.
}

public sealed class SupabaseSawJobStore : ISawJobStore
{
    // GetAllAsync  → From<SawJobRow>().Order(saved_at desc).Get()  → mappa till SawJob
    // SaveAsync    → Id==0 ? Insert : Update  → returnera med Id
    // DeleteAsync  → From<SawJobRow>().Filter(id).Delete()
}
```
En liten `SawJob ↔ SawJobRow`-mappning (rak fält-för-fält). RLS sköter ägarskapet — inga
`user_id`-filter behövs i koden.

---

## 4. Inloggnings-UI (delad Razor)

- `Login.razor`: e-postfält → **"Skicka kod"** (`SendCodeAsync`) → kodfält → **"Logga in"**
  (`VerifyCodeAsync`). Enkel felhantering (fel kod, för många försök).
- `AuthGate.razor` (wrappar innehållet i `MainLayout`): visar `Login` när
  `!IsSignedIn`, annars appen. En "Logga ut"-knapp i menyn/footern.
- Loggboks-sidan laddar om sina poster när `IAuthService.Changed` triggas.

---

## 5. DI-registrering

**Web (`Program.cs`)** — byt InMemory mot Supabase:
```csharp
builder.Services.AddSingleton(new SupabaseConfig { Url = "...", AnonKey = "..." });
builder.Services.AddSingleton<ISessionStore, WebSessionStore>();     // localStorage
builder.Services.AddSingleton<IAuthService, SupabaseAuthService>();
builder.Services.AddSingleton<ISawJobStore, SupabaseSawJobStore>();
```

**Android (`MauiProgram.cs`)** — samma, men SecureStorage-session:
```csharp
builder.Services.AddSingleton(new SupabaseConfig { Url = "...", AnonKey = "..." });
builder.Services.AddSingleton<ISessionStore, SecureStorageSessionStore>();
builder.Services.AddSingleton<IAuthService, SupabaseAuthService>();
builder.Services.AddSingleton<ISawJobStore, SupabaseSawJobStore>();
```

Tips: behåll `InMemorySawJobStore`/`JsonFileSawJobStore` kvar i koden bakom en enkel
flagga tills Supabase är verifierat — inget bryts under tiden.

---

## 6. GitHub Pages-deploy (webben)

Tre Blazor-fallgropar måste hanteras (annars vit sida):

1. **`.nojekyll`** i publicerad `wwwroot` (annars strippas `_framework`).
2. **`<base href>`** = `/Fubkalkylator/` (Pages-undermapp) — eller `/` om eget domännamn.
3. **`404.html`** = kopia av `index.html` (SPA-fallback för `/order`, `/mal` …).

Workflow `.github/workflows/pages.yml` (skiss):
```yaml
on: { push: { branches: [main] } }
permissions: { contents: read, pages: write, id-token: write }
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet publish src/Fubkalkylator.Web -c Release -o dist
      - run: |                       # base href → undermapp (hoppa om eget domännamn)
          sed -i 's|<base href="/" />|<base href="/Fubkalkylator/" />|' dist/wwwroot/index.html
          cp dist/wwwroot/index.html dist/wwwroot/404.html
          touch dist/wwwroot/.nojekyll
      - uses: actions/upload-pages-artifact@v3
        with: { path: dist/wwwroot }
  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment: { name: github-pages }
    steps:
      - uses: actions/deploy-pages@v4
```
Aktivera sedan **Settings → Pages → Source: GitHub Actions**.

⚠️ Lägg till Pages-URL:en (och ev. eget domännamn) i Supabase **Authentication → URL
Configuration → Redirect URLs** (OTP-verifiering behöver godkänd origin).

---

## 7. Android

Med OTP behövs **inga deep links och ingen Google-konsol**. Det som skiljer mot webben:
- DI pekar på `SecureStorageSessionStore`.
- Bygg/signera APK som nu (`CLAUDE.md`-rutinen) och sideloada.
Resten (login-UI, store, mappning) är delat och redan gjort i fas 3–4.

---

## 8. Migrering av befintlig lokal loggbok (engång)

Vid första inloggningen på Android: om lokal JSON-loggbok finns, erbjud **"Importera X poster
till kontot"** → läs lokala store, `SaveAsync` var och en mot Supabase. Webben har ingen
beständig lokal data (InMemory) → inget att migrera där.

---

## 9. Offline (medvetet val för v1)

Supabase-only kräver **internet** för att läsa/skriva loggboken. Kalkylatorn i sig fungerar
ändå (ren klientlogik). Vill du ha offline-loggbok senare: lokal cache + synk mot Supabase —
ett separat, större steg. Rekommendation: kör online-krav i v1.

---

## 10. Ordning (faser)

1. **Supabase:** kör SQL:en (tabell + RLS), slå på e-post-auth.
2. **Delat projekt:** `Fubkalkylator.Supabase` — config, `IAuthService`+`SupabaseAuthService`,
   `ISessionStore`, `SupabaseSawJobStore`, mappning.
3. **UI:** `Login.razor` + `AuthGate` + "Logga ut".
4. **Webb:** DI mot Supabase, kör lokalt, verifiera (skicka kod → logga in → spara/läs loggbok).
5. **Pages:** workflow + `.nojekyll`/base href/404, aktivera Pages, verifiera publikt.
6. **Android:** DI + SecureStorage-session, bygg APK, verifiera sideload.
7. **(Valfritt)** importera lokal loggbok; flytta order/inställningar till molnet.

---

## 11. Konfig-checklista

- [ ] Supabase: Project URL + anon key noterade
- [ ] Email-auth på
- [ ] Tabell `saw_jobs` skapad
- [ ] **RLS på + fyra policyer** (utan detta är loggboken öppen för alla!)
- [ ] Redirect URLs innehåller Pages-URL (+ ev. eget domännamn)
- [ ] GitHub Pages: Source = GitHub Actions
- [ ] anon key i klienten — service_role INTE någonstans i repot

---

## 12. Risker / fallgropar

- **RLS av = läcka.** Enskilt viktigast. Verifiera att en annan användare inte ser dina rader.
- **`.nojekyll` saknas** → `_framework` strippas på Pages → vit sida.
- **base href** fel vid undermapp → tomma resurser. Eget domännamn (base `/`) undviker det och
  gör localStorage/URL stabil vid ev. värdbyte.
- **supabase-csharp API** skiljer mellan versioner → **pinna** paketversionen.
- **E-postleverans:** Supabase default-SMTP har låga gränser (fint för personligt bruk); egen
  SMTP om volymen växer.
- **photo_data_url** kan svälla rader → Supabase Storage senare.
- **Eget domännamn** rekommenderas om du vill kunna byta Pages→Netlify osmåärkbart för användaren.

---

## Vad jag (Claude) kan göra när du säger till

- Skapa `Fubkalkylator.Supabase`-projektet med `IAuthService`, `SupabaseAuthService`,
  `ISessionStore` (+ web/Android-impl), `SupabaseSawJobStore` och mappning.
- `Login.razor` + `AuthGate` + logga-ut.
- DI-inkoppling i Web och App bakom en flagga (inget bryts).
- Pages-workflowen med alla tre fixarna.

Det du måste göra själv: skapa Supabase-projektet, köra SQL:en, och ge mig **URL + anon key**.
