# Hollow Park (korábban Nightmare Hollow) – Projekt-memória

Ez a fájl a projekt teljes "emlékezete" – minden új munkamenet ebből tudja, hol tartunk.

## Mi ez?
Halloween-témájú menekülős horrorvadászat Unity 6-ban (6000.6.7f1).
**A játékot és a kapufeliratot a concept-tábla alapján HOLLOW PARK-ra neveztük át** (2026-11-03).
Ellenfél: **JACKO – a Töklámpás Bohóc** (izzó szemű tökfejű, hosszú karú bohóc,
harlekin ruha: lila-narancs rombuszmintás kabát + öv csatokkal;
az eredeti karakterlap: „Minden látát... és mindig visszajövök.").
**Referencia concept-tábla**: kapu-ív HOLLOW PARK felirattal + tök a csúcson,
pénztárbódé (TICKETS), óriáskerék + körhinta, Security Office (monitorfal, sakkpadló),
cél-HUD (Find the flashlight → Find the fuse), "+100" pont-HUD.

## Játékmenet
- First-person menekülés egy éjjeli vidámparkban, zárt park
- **A játékos lámpa NÉLKÜL indul** – előbb megtalálja a zseblámpát a Security Office íróasztalán
- Cél: **3 zöld izzó biztosíték felvétele → kapu kinyílik → szökés a kapunál**
- Cél-HUD: ○/✓ lámpás, ○/✓ 3 biztosíték, ○ menekülés; pontok: +100 lámpa, +100/fuse, +250 szökés
- Jacko AI: lurkol → megáll és figyel (hipnotikus fejbillentés) → ólálkodik → üldöz → keres
- **Emlékszik a búvóhelyekre**: ha látta, hová bújtál, legközelebb onnan kezdi a keresést
- Bújó hordók (E), izzólámpa (F) – csak ha megvan! – fény messziről elárul, de közel visszatartja (mozgás 0.3x)
- Vereség: jumpscare (rángó fej, csattogó áll, nagyobb fej-skála) → R = újra
- Ijesztő-zónák (TriggerScare): körhinta ("magától forog... üresen") + Security Office ("a monitorok még izzanak")

## Fájl-térkép (Assets/)
| Fájl | Szerep |
|------|--------|
| `Scripts/JackoAI.cs` | Jacko test-építése kódból (harlekin DiamondTex kabát, öv, tökfej, fogak, állkapocs) + AI állapotgép + **harag-szint** + nevetés-hang |
| `Scripts/PlayerController.cs` | WASD/futás/guggolás, **lámpa-tulajdonlás (HasFlashlight)**, interakció-szken, rejtőzés, lépés-hangok |
| `Scripts/FlashlightPickup.cs` | **ÚJ: zseblámpa felvétel** (Security Office, asztalon) |
| `Scripts/TriggerScare.cs` | **ÚJ: egyszeri ijesztő-zóna** (üzenet + opcionális Jacko-nevetés) |
| `Scripts/GameManager.cs` | HUD + **cél-lista + pontszám**, szívverés/morgás/calliope-zene/rikanás (kódból!), fuse-randomizálás, legjobb idő, R-restart |
| `Scripts/EscapeGate.cs` | kapu – **animált nyitás** (csúszás 1,1 mp, majd eltűnik) |
| `Scripts/HideSpot.cs`, `FusePickup.cs` | bújóhely, biztosíték |
| `Scripts/NH.cs` | segédek: anyagok, procedurális textúrák (NoiseTex, StripeTex, **DiamondTex, CheckerTex**), mesh-epítő |
| `Scripts/Rotator.cs` | forgás (óriáskerék, körhinta) |
| `Editor/SceneBuilder.cs` | menü: **Hollow Park → Demo jelenet építése** – + kapu-ív, pénztárbódé, körhinta, Security Office (belső tér!), lámpa-pickup, ijesztő-zónák |

## Kulcs értékek (2026-11-03 állapot)
- chaseSpeed **4.4**, stalkSpeed 2.0, lurkSpeed 1.5
- **Harag-szint: maxRage 5, észleléskor +1 → sebesség ×(1 + 0.03·harag), szemfény erősödik; PlayerPrefs "NH_Harag" – megmarad restart közt**
- catchDist 1.6, loseSightTime 3.2, láv-lámpa fény: mozgás ×0.3
- **Csak lámpával: flashSightRange 32 vs 20 (lámpa nélkül); lámpa pickup: FlashlightPickup → HasFlashlight=true**
- Fuse: **6 stáció → minden kör 3 random** (GameManager.RandomizeFuses)
- PlayerPrefs: "NH_Harag", "NH_LegjobbIdo"
- Hangok: szívverés, morgás, calliope-keringő (A-moll), Jacko-nevetés, jumpscare-rikoltozás, lépések
- Jelenet-elemek: kapu-ív (arány: oszlopok ±4.6, gerenda 10.6×1.5, tök csúcson), pénztár (9.5,43), körhinta (20,36), Security Office (-30,32, forgatás 35°, ajtó +z oldalon), óriáskerék (-20,-26)

## localStorage: hogyan tovább (e következő iteráció ötletei)
- További belső terek a tábláról: **Puppet Workshop, Theater, Maze, Generator Room** (a Generator Room jól passzol a "fuse" témához!)
- Motorszoba-fészek: a fuse-ok egyike lehetne a Generator Room-ban (Jacko őrszemmel?)
- Mobilos shortcut? / folyamatos balanszolás játékos-visszajelzés alapján
- Harag-reset lehetőség (pl. győztes kör után, vagy M billentyű)
- Jacko beszéd-vonalak feliratként a jumpscare alatt
- Körhinta-ijesztés fejlesztése: a lovak néha mozognak maguktól is

## Technika jegyzetek
- Minden mesh primitívekből, anyag + textúra kódból – nincs külső asset
- Shader fallback: URP Lit → Standard → Legacy
- 3D szöveg: beépített LegacyRuntime.ttf + TextMesh (TMP helyett – nem kell Essentials import)
- **TextMesh olvasható oldala: transzform −Z! Kapunál/bódénál a gyerek 180°-ra forgatva a játékos felé**
- **C# csapda: `var x = NH.Mesh(...).name = "..."` → x STRING lesz (a .name értékadása)! Ha kell a GameObject, SOHA ne láncold a .name-et a var-val. E okozta az első fordítási hibát (CS1061).**
- Fordítás-ellenőrzés agentből: temp-be másolt projekt + `Unity.exe -batchmode -nographics -quit -projectPath ... -logFile`; az "error CS" sorok a logban
  - Csapda: a GUI-folyamat miatt a PowerShell nem vár rá – `Start-Process ... -PassThru` + `Wait-Process -Id $p.Id`
- **A concept-tábla Textures/Materials panelje beépítve (2026-11-03):** `NH.PumpkinFaceTex` (faragott arc, emisszív maszkkal – KapuTok + izzó tökök),
  `NH.PlankTex` (fa palánkok – bódék, pénztár, kerítés, ládák, körhinta), `NH.RustMetalTex` (kapu + óriáskerék), `NH.MarbleTex` (iroda falai),
  `NH.MoonTex` (Hold), `NH.MatEmis` (albedo + külön emisszív map). Segéd: `NH.InTri` – figyelem, `Vector2.Cross` NEM létezik, kézi kereseszorzás kell!
- Következő iteráció-jelöltek a tábláról: PUPPET WORKSHOP / THEATER / MAZE / GEN ROOM (ún. elképzeletlen zónák)
- 12+ szkript, SceneBuilder ~800 sor – a jelenet teljesen kódból épül
- **FONTOS: a jelenetet ÚJRA kell építeni: "Hollow Park → Demo jelenet építése" menü!**
- Unity zárolás gondjai: néha UnityLockfile marad bezáráskor – törölhető

## Elindítás felhasználóhoz
Unity Hub → Open → `C:\Users\ork20\Documents\NightmareHollow`
→ Assets/Scenes/Demo → Play.
Irányítás: WASD, egér, Shift futás, C guggolás, F lámpa, E interakció, R restart.

## Aktív beszélgetések
- Ez a feladat a "Default Project" workspace-ből vezérelt
- A játékterv a karakterlap-képről (JACKO A TÖKLÁMPÁS BOHÓC – Nightmare Hollow) indult
