# Fubkalkylator

C#-portning av kalkylbladet **"PostningsMax Beta"** – ett sågverksverktyg för att
optimera hur en stock sågas upp till önskat sortiment (block + sido-/ändbrädor).

Samma kod körs som webbapp (Blazor WebAssembly) och som native Android-app
(.NET MAUI Blazor Hybrid) — all logik och allt UI delas.

## Projektstruktur

| Projekt | Beskrivning |
|---------|-------------|
| `src/Fubkalkylator.Core` | Ren beräkningsmotor (ingen UI). Postning, aptering, måldimension, utbyte, uppslag. Sågspår är en variabel. |
| `src/Fubkalkylator.UI` | Delat Razor Class Library: sidor, komponenter (stockände-SVG, sågspårskontroll), layout, CSS, tema. |
| `src/Fubkalkylator.Web` | Blazor WebAssembly-värd (PWA). Refererar UI + Core. |
| `src/Fubkalkylator.App` | .NET MAUI Blazor Hybrid — native Android-app. Refererar UI + Core. |
| `tests/Fubkalkylator.Core.Tests` | xUnit-tester som verifierar motorn mot kalkylbladet. |

## Domänmodell (allt internt i tum)

- **PostningsMax** – vid sågbänken: `fub` (diameter under bark) → blockbredd `B`,
  blockhöjd `H`, antal 1"/2"-brädor, samt förblock `FB`/`FH`.
- **ApteringsMax** – inför fällning: önskad blockbredd `B` → nödvändig toppdiameter.
- **Måldimension** – ange färdig dimension → minsta stock, eller mata in din stock →
  hur du kapar ut den. Valfria mått, "spelar ingen roll", utbyte.
- **Sågspår** – kedjesåg (1/4") eller bandsåg (~3 mm); genererar blocktabellen dynamiskt.

Referensfall som testas: `fub = 9,75"` → `B = 6"`, `H = 7,75"`, `FB = 8,5"`, `FH = 7,75"`.

## Köra webb + tester

```bash
dotnet test                                   # kör testerna
dotnet run --project src/Fubkalkylator.Web    # → http://localhost:5199
```

## Bygga Android-appen

Kräver MAUI Android-workload (installeras som administratör) samt Android SDK + JDK
(finns t.ex. med Android Studio):

```bash
dotnet workload install maui-android          # kör som administratör

dotnet build src/Fubkalkylator.App -f net10.0-android -c Release \
  -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr" \
  -p:AndroidSdkDirectory="%LOCALAPPDATA%\Android\Sdk" \
  -p:AcceptAndroidSDKLicenses=True
```

APK:n hamnar i `src/Fubkalkylator.App/bin/Release/net10.0-android/`
(`*-Signed.apk`). Överför den till telefonen och installera (tillåt "okända källor"),
eller installera via USB med `adb install <fil>.apk`.
