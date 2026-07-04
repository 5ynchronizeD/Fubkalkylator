# CLAUDE.md — Fubkalkylator

Sågverkskalkylator (postningsoptimering) portad från kalkylbladet "PostningsMax Beta".
Delad `Core` + `UI`, körs som Blazor WASM-webbapp och .NET MAUI Android-app.
Se `README.md` för projektstruktur och byggkommandon.

## Release-rutin vid nya versioner

När en ny version görs, bumpa `<Version>` i `Directory.Build.props`, commita och pusha till main.

1. **GitHub-release skapas automatiskt.** Workflowen `.github/workflows/release.yml`
   körs vid push till `main`: om versionen är ny (ingen tagg `vX.Y.Z` finns) körs
   testerna, APK:n byggs och en release med APK:n bifogad skapas. Bumpas inte
   versionen händer inget.
   > OBS: CI signerar med en egen (debug-)nyckel, så CI-APK:n och lokalt byggda
   > APK:er kan ha olika signaturer (går inte att uppgradera över varandra).

2. **Kopiera APK:n till Google Drive** (fortfarande manuellt/lokalt — CI når inte
   datorns Drive). Bygg Android-appen i Release och kopiera den signerade APK:n
   till `D:\Min enhet`:
   ```bash
   dotnet build src/Fubkalkylator.App/Fubkalkylator.App.csproj -f net10.0-android -c Release \
     -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr" \
     -p:AndroidSdkDirectory="%LOCALAPPDATA%\Android\Sdk" \
     -p:AcceptAndroidSDKLicenses=True
   ```
   Kopiera sedan
   `src/Fubkalkylator.App/bin/Release/net10.0-android/se.fubkalkylator.app-Signed.apk`
   till `D:\Min enhet\`. Själva projektet bor kvar på datorn — bara APK:n till Drive.

## Versionshantering

- En delad version bor i `Directory.Build.props` (`<Version>`). Bumpa där —
  då följer både UI:ts footer och Android-appens `ApplicationDisplayVersion` med.

## Roadmap (status)

- ✅ **Etapp 1 – Volym & värde:** `PostningEconomy` + `VolumeValueCard` (stocklängd, prislista → m³, board feet, utbyte %, kr). Sparas i loggboken.
- ✅ **Etapp 2 – Statistik/översikt:** `Statistik.razor` (volym, värde, snittutbyte, torkstatus, per trädslag).
- ✅ **Etapp 3 – Torkning:** `Shrinkage` + `ShrinkageCard` (krympmån) och `DryingForecast` + torkprognos i loggboken (målfukthalt → klar‑dag).
- ✅ **Etapp 4 – Småfixar:** `Bark` + `BarkCard`, sök/filter i loggboken, CSV‑export + JSON‑säkerhetskopia, foto per loggpost.
  - Auto‑sync till Google Drive levererades som **manuell JSON‑säkerhetskopia** (knapp i loggboken). Äkta auto‑sync från telefonen kräver Google Drive‑API/OAuth — separat, större jobb.
  - PDF‑export utelämnad (CSV täcker behovet); lägg till vid behov.
- ✅ **Etapp 5 – Avancerat:** `OrderPlanner` + `Order.razor` (orderlista → minsta stock/antal stockar) och `Taper` + `TaperCard` (avsmalning).

> Jämför‑två‑postningar valdes bort medvetet: med ett enda sågverk och fast sågspår finns inget att jämföra mot.

## Bra att veta

- All beräkning sker internt i tum; mm/cm visas parallellt. Sågspår är en variabel.
- Kör tester med `dotnet test` innan release.
