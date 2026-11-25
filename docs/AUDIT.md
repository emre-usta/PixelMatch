# PixelMatch — Salt-Okunur Proje Denetimi

- **Tarih:** 2026-11-25
- **Dal:** `chore/code-audit` (çalışma ağacı temiz, `f10cf11`)
- **Sürüm:** `bundleVersion 1.10`, `versionCode 12`, Unity `2022.3.62f3`
- **Yöntem:** `Assets/Scripts` altındaki 38 C# dosyasının (6.717 satır) tamamı satır satır okundu. Sahne ve prefab YAML'ları ayrıştırıldı: persistent call'lar, eksik script'ler, isimler. MainMenu sahnesi ve tüm ScriptableObject'ler MCP `rql-query` ile sorgulandı. Gradle şablonları, EDM XML'leri, `ProjectSettings` ve `Packages/manifest.json` incelendi.
- **Kapsam dışı:** Hiçbir dosya değiştirilmedi. Sahne açılıp kapatılmadı, `script-execute` kullanılmadı.
- **Öncelikler:** **Kritik / Yüksek / Orta / Düşük**. Kesin olmayan bulgular **[Doğrulanmalı]** ile işaretli.

### Script envanteri (satır)

| Dosya | Satır | Dosya | Satır |
|---|---|---|---|
| LevelSelectManager | 627 | FreeModeManager | 320 |
| GameStateManager | 607 | AdManager | 315 |
| LocalizationManager | 455 | SettingsManager | 290 |
| WinPanelController | 454 | DailyChallengeManager | 288 |
| GridManager | 368 | AudioManager | 252 |
| PowerUpManager | 229 | AdRewardPopup | 196 |
| ComboEffectManager | 196 | FirebaseManager | 187 |
| CardController | 185 | GameEvents | 154 |
| EffectManager | 150 | TutorialManager | 137 |
| TimerController | 130 | InAppUpdateManager | 123 |
| InAppReviewManager | 113 | MoveController | 104 |
| MainMenuControl | 102 | LevelPopupManager | 91 |
| LevelProgressManager | 86 | PowerUpUI | 78 |
| PlayButtonJuicer | 73 | UIPopupController | 57 |
| BadgeManager | 52 | UIButtonPolish | 44 |
| Editor/CornerSpriteCreator | 41 | LevelConfig | 38 |
| Editor/CRTSpriteCreator | 33 | LocalizedText | 33 |
| FreeModeLimitConfig | 32 | MenuParallax | 31 |
| StarTwinkle | 26 | CategoryConfig | 20 |

### Hangi script hangi sahnede

| Sahne | Script'ler |
|---|---|
| **MainMenu** | AdManager, AudioManager, FirebaseManager, InAppReviewManager, InAppUpdateManager, LocalizationManager (hepsi DDOL, ayrı kök objelerde). `MenuManager` objesinde: MainMenuControl, DailyChallengeManager, SettingsManager, BadgeManager. Ayrıca MenuParallax, PlayButtonJuicer, StarTwinkle, UIPopupController, UIButtonPolish, LocalizedText |
| **LevelSelect** | LevelSelectManager, LevelPopupManager, LevelProgressManager (DDOL ama **yalnızca burada**), PowerUpManager, UIPopupController, AdRewardPopup (prefab) |
| **Level1** | GameStateManager, GridManager, TimerController, MoveController, TutorialManager, EffectManager, ComboEffectManager, PowerUpManager, PowerUpUI, WinPanelController, SettingsManager (+Panel_Settings prefab), AdRewardPopup (prefab) |
| **FreeMode** | FreeModeManager |

---

## 0. Üretim Verisinden Gelen Sorular

### a) `level_id` / `category_id` neden (not set)?

Event'ler `FirebaseManager.cs` içinden gönderiliyor. Parametreler gerçekten gönderiliyor ve adları GA4'te kayıtlı adlarla birebir aynı. Tipleri şöyle:

| Event | Gönderen | Parametre (tip) |
|---|---|---|
| `level_start` | `FirebaseManager.cs:57` ← `GameStateManager.cs:80-83` | `category_id` (long), `level_id` (long) |
| `level_complete` | `FirebaseManager.cs:66` ← `GameStateManager.cs:305` | `category_id` (long), `level_id` (long), `stars` (long) |
| `level_fail` | `FirebaseManager.cs:75` ← `GameStateManager.cs:372-375` | `category_id` (long), `level_id` (long) |
| `level_end` | `FirebaseManager.cs:87-112` ← `GameStateManager.cs:518-552` | `category_id` (long), `category_name` (string), `level_id` (long), `success` (**string** "true"/"false"), `game_mode` (string), `grid_size` (string "CxR"), `stars` (long), `time_left` (**double**), `moves_left` (long), `time_thief_hits` (long) |

Kodda parametre adı ya da eksik parametre hatası yok. Bu event'lerin **hepsinde** `level_id` ve `category_id` (not set) çıkıyor, `category_name` / `game_mode` gibi string parametreler ise çıkıyor. Ortak nokta, sayısal (`long`) gönderilmeleri. Olası sebepler, olasılık sırasıyla **[Doğrulanmalı]**:

1. Custom dimension'lar event'ler gönderilmeye başladıktan sonra tanımlandı. GA4 boyutları geriye dönük doldurmaz ve kayıttan önceki tüm satırlar (not set) görünür.
2. Boyut "User" kapsamında tanımlı, ya da parametre adında ince bir fark var (boşluk, büyük harf).
3. Sayısal parametreler raporda boyut olarak görünmüyor.

Kontrol yolu: DebugView'da tek bir `level_end` olayının ham parametrelerine bakın ve dimension tanımının kapsamını/oluşturulma tarihini kontrol edin. Önerilen kod düzeltmesi: `level_id` ve `category_id` değerlerini string olarak da gönderin (ör. `level_key = "c3_l7"`).

Ek tasarım sorunları:

- `level_id` 0 tabanlı ve her kategoride 0–9 arası tekrar ediyor. `category_id` olmadan tek başına anlamsız.
- `powerup_used` ve `ad_watched` içindeki `level_id`, DC/Free Mode'da bile son seçilen hikâye seviyesini taşıyor (`PowerUpManager.cs:70`, `AdManager.cs:167`).

### b) `tutorial_begin` ve `tutorial_step` neden hiç gelmiyor?

**`LogTutorialBegin` (`FirebaseManager.cs:164`) ve `LogTutorialStep` (`FirebaseManager.cs:175`) projede hiçbir yerden çağrılmıyor** (grep: yalnızca tanımları var). Tek çağrılan `LogTutorialComplete` (`TutorialManager.cs:132`).

Tutorial akışı adım adım:

1. Oyuncu ilk kez bir seviye açar. `TutorialManager` yalnızca **Level1** sahnesinde var.
2. `TutorialManager.Start` (`TutorialManager.cs:45-67`) çalışır. DC ya da Free Mode aktifse tutorial kapatılır. Değilse `tutorial_done==0` **ve** `SelectedLevel.levelID==0` şartı aranır. Kategori fark etmez: herhangi bir kategorinin 1. seviyesi tetikler.
3. `ShowTutorial` (`:71-79`) `Time.timeScale = 0` yapar ve paneli açar.
4. Ekranda 8 adım var (`tutorial_step1`…`tutorial_step8`). `Btn_Next` → `OnScreenTapped` (`:81-88`) ile ilerlenir. 8. adımda yalnızca "BAŞLA" (`btnAction`) görünür.
5. "BAŞLA" → `CompleteTutorial` (`:126-137`) çalışır: `tutorial_done=1` + `Save`, `tutorial_complete` event'i, `timeScale=1` ve `GameStateManager.OnGridReady()`.
6. Tutorial sadece bilgi ekranlarından oluşur, etkileşimli bir adım yok. "Bitti" anı oyun başlamadan önce, "BAŞLA" tuşuna basılan an.

Düzeltme: `ShowTutorial` içinde `LogTutorialBegin`, `UpdateUI` içinde `LogTutorialStep(currentStep+1, stepKeys[currentStep])` çağrısı ekleyin.

### c) `tutorial_complete` ile `level_start` neden aynı anda geliyor?

`CompleteTutorial` aynı karede önce `LogTutorialComplete`, ardından `OnGridReady()` çağırıyor. `OnGridReady` de hemen `level_start` gönderiyor (`TutorialManager.cs:132-136`, `GameStateManager.cs:77-83`). Arada 0 sn olmasının sebebi bu.

Ayrıca **çift `level_start`** oluşuyor:

- Tutorial sırasında `timeScale=0` olduğu için `GridManager.PeekAndStart` (`GridManager.cs:350-363`) `WaitForSeconds(1.5f)` noktasında donuyor.
- "BAŞLA"dan yaklaşık 1,8 sn sonra korutin devam ediyor ve `OnGridReady()`'i **ikinci kez** çağırıyor. Artık `tutorial_done==1` olduğu için guard geçiliyor.
- Sonuç: ikinci `GameStarted` (timer sıfırlanıp yeniden başlar, hamle sayacı sıfırlanır) ve ikinci `level_start`. **[Doğrulanmalı: tutorial kullanıcılarında level 0 için ~2 sn arayla iki `level_start` görülmeli.]**

### d) Daily Challenge'da `level_end` neden hep `success=false`?

DC kazanma yolunda (`GameStateManager.cs:133-142`) çağrı sırası şöyle:

1. `MarkPlayedToday`
2. **`DailyChallengeManager.SetInactive()`** (`:137`)
3. `GiveDailyChallengeReward` → `daily_challenge_complete` (`:180`)
4. `LogLevelEndEvent(success:true)` (`:142`)

`SetInactive` 4. adımdan önce çalıştığı için `LogLevelEndEvent` içindeki `IsDailyChallengeActive` kontrolü (`:526-538`) `false` dönüyor. DC galibiyetleri bu yüzden `game_mode="classic"` (DC `SelectedMode`'u Classic'e çekiyor, `DailyChallengeManager.cs:169`) ve hikâye seviyesinin grid'iyle loglanıyor. `game_mode="daily_challenge"` ile gelen tek `level_end`, `LoseGame` yolundan (`:377`) geliyor. Bu yüzden DC'de görünen `level_end` kayıtları hep `success=false`, `daily_challenge_complete` ise doğru gönderiliyor. **Öncelik: Yüksek.** Düzeltme: `LogLevelEndEvent`'i `SetInactive`'den önce çağırın ya da mod bilgisini önceden bir değişkende tutun.

### e) Classic modda `grid_size="0x0"` nereden geliyor?

(d)'deki hatanın yan etkisi. DC galibiyeti "classic" olarak loglanırken `columns/rows` değerleri `LevelSelectManager.SelectedLevel?.columns ?? 0` ifadesinden okunuyor (`GameStateManager.cs:532-538`). Oyuncu bu oturumda hiç hikâye seviyesi seçmediyse (Ana Menü → doğrudan DC) `SelectedLevel == null` olur ve sonuç `0x0`. Seçtiyse o seviyenin grid'i (yanlış değer) yazılır. `category_id` / `category_name` da aynı şekilde bayat ya da `"unknown"` gelir.

### f) Free Mode hiç `level_end` göndermiyor mu?

**Doğru.** Free Mode hiçbir seviye event'i göndermiyor:

- `LogLevelEndEvent` başında `if (FreeModeManager.IsFreeModeActive) return;` var (`GameStateManager.cs:520`).
- `level_start` (`:80`) ve `level_fail` (`:372`) Free Mode'u açıkça hariç tutuyor.
- `level_complete` yalnızca normal kazanma yolunda (`UnlockNextLevel`) gönderiliyor.

Free Mode'dan sadece `powerup_used` / `ad_watched` gelir (bayat `level_id` ile). Free Mode için ayrı bir `free_mode_start/end` event'i ya da `game_mode="free"` ile `level_end` eklenmeli.

### g) `level_end` ile `level_complete` / `level_fail` nerede çift gönderiliyor?

| Durum | `level_start` | `level_complete` | `level_fail` | `level_end` | DC event'leri |
|---|---|---|---|---|---|
| Hikâye – kazanma | `GSM:80` | `GSM:305` (UnlockNextLevel) | – | `GSM:161` success=true | – |
| Hikâye – kaybetme | `GSM:80` | – | `GSM:372` | `GSM:377` success=false | – |
| Hikâye – 2. şans sonrası tekrar kaybetme | – | – | **tekrar** `GSM:372` | **tekrar** `GSM:377` | – |
| Tutorial seviyesi | **2×** (bkz. c) | | | | |
| DC – kazanma | – | – | – | `GSM:142` **game_mode=classic** (hata) | `daily_challenge_complete` `GSM:180` |
| DC – kaybetme | – | – | – | `GSM:377` game_mode=daily_challenge, false | – |
| DC – başlangıç | – | – | – | – | `daily_challenge_start` `DCM:178` |
| Free Mode | – | – | – | – | – |

`GSM = GameStateManager.cs`, `DCM = DailyChallengeManager.cs`

- `level_complete` / `level_fail` eski event'ler (`FirebaseManager.cs:65` yorumu "Eski metodlar korunuyor"). `level_end` her şeyi kapsıyor. Eski ikisi kaldırılabilir ya da yalnızca funnel için tutulabilir.
- Second chance kullanıldıktan sonra yeniden kaybedilirse aynı oyunda **iki** `level_fail` + **iki** `level_end(false)` oluşur. Başarısızlık oranları bu yüzden şişiyor olabilir (bkz. j).
- Kazanma tarafında çift sayım yok, ama "son hamlede kazanma = kaybetme" hatası (Bulgu 1.1) gerçek galibiyetleri `level_fail` olarak yazıyor.

### h) Crashlytics: `AppUpdateHandler.OnApplicationPause` – `NoClassDefFoundError` (1.07–1.08)

**Düzeltme yerinde:**

- `Assets/Editor/PixelMatchDependencies.xml` → `androidx.activity:activity:1.8.2`
- EDM bunu `mainTemplate.gradle`'a işlemiş: `implementation 'androidx.activity:activity:1.8.2' // Assets/Editor/PixelMatchDependencies.xml:3`
- `InAppUpdateManager.cs:39-47` içinde `ActivityResultLauncher` sınıfı yoksa `AppUpdateManager` hiç oluşturulmuyor. `AppUpdateHandler` (paketin DDOL MonoBehaviour'u) yalnızca `new AppUpdateManager()` ile yaratıldığı için bu guard, sınıf eksikse çökme yolunu tamamen kapatıyor.
- Commit: `90e087a`, sürüm 1.09.
- Bağımlılıklar: `com.google.play.appupdate 1.8.5` → `app-update:2.1.0`, `core-common:2.0.4`. `AndroidMinifyRelease: 0` (R8 sınıf silmiyor).

**Yeterli mi?** Büyük ihtimalle evet, ama **[Doğrulanmalı]**: 1.09 ve 1.10 sürümlerinde aynı imza Crashlytics'te tamamen sıfırlandı mı? Kalan riskler:

- **Orta:** Guard yalnızca `ActivityResultLauncher` sınıfını kontrol ediyor. `NoClassDefFoundError` farklı bir sınıf için (ör. `ActivityResultRegistry`, Play Core iç sınıfı) gelmişse koruma işe yaramaz. Crashlytics'teki tam sınıf adı guard'daki adla karşılaştırılmalı.
- **Orta:** `WaitForDownloadComplete` (`InAppUpdateManager.cs:99-122`) indirmenin bittiğini `UpdateAvailability == UpdateNotAvailable` ile anlamaya çalışıyor. Flexible update'te indirme bitince `InstallStatus.Downloaded` olur, availability değişmez. Döngü oturum boyunca 3 sn'de bir `GetAppUpdateInfo` çağırıyor ve `CompleteUpdate` büyük olasılıkla hiç çağrılmıyor. Ayrıca `WaitForSeconds` ölçekli zaman kullandığı için `timeScale=0` anlarında donuyor. Düzeltme: `StartUpdate` operasyonunun `Status`/`InstallStatus` değerini izleyin ve `Downloaded` olunca kullanıcıya "Yeniden başlat" seçeneği sunun.
- **Düşük:** `InAppReviewManager` (`:1-113`) `#if UNITY_ANDROID` korumasına sahip değil ve review akışı her açılışta `Start`'ta isteniyor (gösterilmeyecek olsa bile). Review, kazanma panelindeyken `UnlockNextLevel` içinden tetikleniyor (`GameStateManager.cs:306`), yani tam reklamla aynı anda gelebilir.

### i) Reklam gösterim kuralları

| Format | Yer | Koşul | Kod |
|---|---|---|---|
| **Rewarded** (opsiyonel) | `powerup`: LevelSelect'teki power-up butonları ve oyun içinde envanter 0 iken power-up'a basma | Kullanıcı popup'ta "İzle" der | `LevelSelectManager.cs:578-586`, `PowerUpManager.cs:39-46`, `AdRewardPopup.cs:40-62` |
| | `second_chance` | Her kaybetmede popup çıkar (aynı sahne yüklemesinde bir kez). Kabul edilirse +30 sn ya da +10 hamle | `GameStateManager.cs:384-398`, `AdRewardPopup.cs:66-94` |
| | `unlock_level` | Kilitli **sıradaki** seviyeye tıklanınca | `LevelSelectManager.cs:450-465`, `AdRewardPopup.cs:98-117` |
| | `double_reward` | Kazanma panelinde "2x" butonu (power-up ödülü varsa) | `WinPanelController.cs:109-131` |
| **Interstitial** (zorunlu) | `level_transition` | **Yalnızca** kazanma panelinde "Sonraki"ye (`Btn_NextMission` → `NextOrRetryWin`) basınca. `ad_level_count` her basışta +1. Şartlar: `count % 3 == 0` **ve** oturumda son interstitial'dan bu yana 180 sn **ve** reklam yüklü | `AdManager.cs:187-213`, `GameStateManager.cs:581-598` |
| **Rewarded interstitial** (zorunlu tam ekran, ödüllü) | Etiketi her zaman `dc_start` | 1) **DC başlarken** (`DailyChallengeManager.cs:149-164`). 2) **Her kazanma sonrası "Sonraki"de** (`GameStateManager.cs:585`; yanlışlıkla yine `dc_start` etiketiyle). Şartlar: `ad_level_count % 3 == 0` (artırmadan önceki değer) **ve** `PlayerPrefs` içindeki son gösterimden 180 sn geçmiş **ve** reklam yüklü | `AdManager.cs:252-315` |

**İlk oturum ve ilk N seviye:** Koruma **yok**. `ad_level_count` 0'dan başlıyor ve `0 % 3 == 0`. Yeni bir oyuncu:

- **ilk seviyeyi** (tutorial seviyesi) kazanıp "Sonraki"ye bastığında **rewarded interstitial** görür;
- 3. galibiyette interstitial görür;
- **ilk kez DC'ye girdiğinde** de (sayaç 0 ise) **DC başlarken rewarded interstitial** görür.

Ayrıca `KEY_LAST_REWARDED_INT` değeri `Time.realtimeSinceStartup` ile (oturuma göreli) `PlayerPrefs`'e yazılıyor (`AdManager.cs:255-266`). Bir sonraki oturumda karşılaştırma anlamsızlaşıyor: önceki oturumun değeri büyükse rewarded interstitial uzun süre hiç çıkmaz, küçükse hemen çıkar.

**Rewarded interstitial, power-up'ı neden "ödül" olarak veriyor:** Ödül her durumda TimeFreeze ×1.

**Reklam analitiği hataları:**

- `AdRewardPopup.OnWatchClicked` → `AdManager.ShowRewarded(..)` çağrısı `adPlacement` vermiyor. Varsayılan `"powerup"` olduğu için her rewarded izlemede **iki** `ad_watched` gidiyor: biri `ad_placement="powerup"` (`AdManager.cs:164`), diğeri gerçek yerle (`AdRewardPopup.cs:176`). Sonuç: `powerup` şişiyor, toplam iki katına çıkıyor. **Yüksek.**
- `WinPanelController.OnDoubleRewardClicked` da placement vermiyor, `double_reward` yerine `powerup` olarak loglanıyor.
- Dokümante edilen `level_unlock` ile koddaki `unlock_level` uyuşmuyor (`FirebaseManager.cs:151` yorumu ile `AdRewardPopup.cs:100`).

---

### j) Zorluk verisi

Tüm 8 kategori (`Animals 0`, `Foods 1`, `Nature 2`, `Space 3`, `Legends 4`, `Professions 5`, `MusicInstruments 6`, `Vehicles 7`; her biri 18 kart sprite'ı, 10 seviye) **birebir aynı** seviye tablosunu kullanıyor (`rql`: 10 farklı satır × 8 adet). `levelID == dizi indeksi` ve tüm seviyelerde `useTimer=true, useMoveLimit=false`, ancak bu iki alan **kullanılmıyor**: mod popup'ta seçiliyor.

| levelID | Grid (sütun×satır) | Çift | Zorluk | Süre (Classic) | sn/çift | Hamle (Move) | hamle/çift | Zaman Hırsızı |
|---|---|---|---|---|---|---|---|---|
| 0 | 4×4 | 8 | Easy | 75 | 9,4 | 22 | 2,75 | – |
| 1 | 4×4 | 8 | Easy | 70 | 8,8 | 20 | 2,50 | – |
| 2 | 4×5 | 10 | Easy | 88 | 8,8 | 25 | 2,50 | – |
| 3 | 4×5 | 10 | Easy | 82 | 8,2 | 23 | 2,30 | – |
| 4 | 4×5 | 10 | Medium | 62 | 6,2 | 19 | **1,90** | – (Legends'ta sprite atanmış ama kullanılmıyor) |
| 5 | **5×4** | 10 | Medium | 60 | 6,0 | 18 | **1,80** | – |
| 6 | **5×4** | 10 | Medium | 57 | 5,7 | 17 | **1,70** | – |
| 7 | 5×6 | 15 | Hard | 92 | 6,1 | 37 | 2,47 | var (−5 sn / −3 hamle) |
| 8 | 6×5 | 15 | Hard | 95 | 6,3 | 38 | 2,53 | var |
| 9 | 6×6 | 18 | Hard | 108 | 6,0 | 43 | 2,39 | var |

**Free Mode ve DC limitleri** (`Assets/Data/FreeModeLimitConfig.asset`):

| Grid | Easy (sn / hamle) | Medium | Hard |
|---|---|---|---|
| 4×4 | 40 / 18 | 32 / 14 | 24 / 12 |
| 4×5 | 60 / 30 | 45 / 24 | 40 / 20 |
| 5×6 | 75 / 45 | 65 / 38 | 55 / 34 |
| 6×6 | 95 / 54 | **97** / 45 (Easy'den uzun) | 80 / 40 |
| **5×4** | **yok** → varsayılan 120 / 30 | yok | yok |

DC grid'leri `{4×4, 4×5, 5×4, 6×6}` arasından seçiliyor (`DailyChallengeManager.cs:71`). 5×4 günlerinde `GetLimit` tabloda kayıt bulamıyor ve **120 sn** veriyor (`FreeModeLimitConfig.cs:24-31`). Bu, en kolay DC olur. DC her zaman zamanlı (Classic) oynanıyor. Zorluk haftanın gününe göre belirleniyor: Pzt–Çar Easy, Per–Cmt Medium, Paz Hard.

**Diğer zorluk parametreleri:**

- Başlangıçta kartlar 1,5 sn gösteriliyor. Süre/hamle bu önizlemeden sonra başlıyor.
- **Combo** (3 sn içinde art arda eşleşmeler) bonusu: Classic'te +3/+5/+8/+10 sn, Move'da +2/+3/+4/+5 hamle (`AudioManager.cs:154-185`).
- **Second chance:** +30 sn / +10 hamle.
- **Zaman Hırsızı:** yalnızca Hard seviyelerde. Yanlış eşleşmede −5 sn ya da −3 hamle (`EffectManager.cs:26-55`).
- Move modunda **her deneme 1 hamle sayılıyor** (eşleşme dahil). İpucu power-up'ı ile yapılan eşleşme de 1 hamle harcıyor (`MoveController.cs:60-78`, `PowerUpManager.cs:195`).
- Yıldızlar: Classic'te kalan süre ≥%60 → 3★, ≥%30 → 2★. Move'da kalan hamle ≥%50 → 3★, ≥%20 → 2★.

**Move modu neden bu kadar zor (%61, 4×5'te %81, 5×4'te %94):**

1. Mükemmel hafızaya sahip bir oyuncu bile N çift için ortalama ~1,6·N deneme yapar. Gerçek oyuncular ≈2–2,5·N. 4. seviye (19 hamle = 1,9·N), 5. seviye (1,8·N) ve 6. seviye (1,7·N) neredeyse **kusursuz oyun** istiyor. 5×4'ün iki seviyesi en sıkı oranlara sahip, bu yüzden en yüksek başarısızlık orada.
2. **Kritik hata (1.1):** Son hamlede son çift eşleşirse `MoveController` aynı karede `LoseGame` çağırıyor, `WinGame` ise 0,8 sn sonra çalışıyor ve state artık `Lose` olduğu için reddediliyor. "Tam sınırda kazanma" kayıp olarak sayılıyor ve Move başarısızlık oranını yapay olarak artırıyor.
3. İkinci şanstan sonra tekrar kaybetme `level_fail` / `level_end`'i ikinci kez yazıyor (bkz. g).

**Classic 5×6 %88:** 15 çift için 92 sn (6,1 sn/çift), üstüne Zaman Hırsızı (−5 sn). Ayrıca 5 sütunlu grid'de kartlar küçük (aşağıya bakın).

**4×5 (%17) ile 5×4 (%41) farkı.** İki etken üst üste biniyor:

1. **Süre limiti (ana etken):** 4×5 seviyeleri 88/82/62 sn (ortalama 7,7 sn/çift, ikisi Easy). 5×4 seviyeleri 60/57 sn (5,85 sn/çift, ikisi de Medium). Aynı 20 kart için yaklaşık %25 daha az süre var ve 5×4 seviyeleri daha geç geliyor.
2. **Kart boyutu ve yerleşim:**
   - `Grid_Cards` tam genişlikte ve ekran yüksekliğinin ~%81'inde. Referans çözünürlük 1080×1920, match 0,5.
   - `AdjustGridLayout` (`GridManager.cs:220-240`) hücreyi `(kap − boşluk)/sütun` ve `/satır` olarak hesaplıyor. `preserveAspect=true` olduğu için kart, hücrenin dar kenarına sığıyor.
   - 4×5'te hücre ≈ **264×305 px**. 5×4'te ≈ **210×382 px**: kart genişliği ~%20 daha az (kare sprite için alan ~%36 daha küçük) ve dikey boşluk israf ediliyor.
   - 5×4, dikey telefonda yanlış yönlendirilmiş bir grid. 4×5 aynı kart sayısını daha büyük kartlarla gösteriyor.
   - **[Doğrulanmalı: sprite en-boy oranı ve SafeArea yüksekliği cihaza göre değişir.]**

Öneri: 5×4 seviyelerini 4×5'e çevirin ya da süreyi ≥75 sn yapın. Move limitlerini en az ≈2,3·N'ye çıkarın. Önce 1.1 hatasını düzeltip veriyi yeniden ölçün.

### k) Firebase Crashlytics SDK sürümü

- Firebase **Unity SDK 13.12.0** (`Assets/Firebase/Editor/FirebaseCrashlytics_version-13.12.0_manifest.txt`).
- Android tarafı: `firebase-crashlytics-ndk:20.0.6`, `firebase-crashlytics-unity:13.12.0`, `firebase-analytics:23.2.0`.
- 20.0.6 sürümü 18.2.4'ün çok üzerinde, yani ANR raporlaması için SDK gereksinimi **karşılanıyor**.

Not: `Crashlytics.ReportUncaughtExceptionsAsFatal = true` (`FirebaseManager.cs:31`). Yakalanmamış **tüm C# exception'ları "fatal crash" olarak** raporlanıyor. Böylece NRE'ler (bkz. 1.10) crash-free oranını düşürebilir.

---

## 1. Hatalar ve Riskli Durumlar

| # | Dosya:satır | Ne oluyor | Neden sorun | Öncelik | Önerilen düzeltme |
|---|---|---|---|---|---|
| 1.1 | `MoveController.cs:73-77`, `GridManager.cs:294-307` | Son hamlede son çift eşleşince `LoseGame` hemen çalışıyor. `WinDelayed` 0,8 sn sonra `WinGame` çağırıyor ama state `Lose` olduğu için reddediliyor. Classic'te aynısı, son eşleşmeden sonraki 0,4+0,8 sn içinde süre biterse oluyor (`TimerController.cs:114-118`) | Kazanılan oyun kaybedilmiş sayılıyor, `level_fail` yazılıyor | **Kritik** | Tüm çiftler eşleşince state'i hemen "kazanıldı" olarak işaretleyin (ya da timer/hamle kontrolünden önce `matched == total` kontrolü yapın). Win/Lose kararını tek bir yerde verin. |
| 1.2 | `GameStateManager.cs:401-421` | 1.1 durumunda oyuncu "İkinci şans"ı izlerse state `Playing` olur ama tahtada kart kalmamıştır ve `WinGame` bir daha çağrılmaz | Oyun kilitleniyor (boş tahta, süre akıyor ya da hamle bekleniyor). Oyuncu reklam izledikten sonra takılıyor | **Kritik** | 1.1'in düzeltmesi bunu da çözer. Ek olarak `ApplySecondChance` içinde `RemainingPairs==0` ise doğrudan `WinGame` çağırın. |
| 1.3 | `AdManager.cs:135-143`, `AdRewardPopup.cs:173-187` | Rewarded reklam ödül kazanılmadan kapatılırsa (`OnClosed`) ne `onSuccess` ne `onFail` çağrılıyor | `second_chance` için: state `Lose`, game over paneli hiç açılmıyor, oyuncu kilitleniyor (yalnızca Pause/Escape çalışmaz, çünkü state `Paused` değil) | **Kritik** | `OnClosed`'da ödül alınmadıysa `onFail` çağırın (rewarded interstitial'daki `rewardEarned` deseninin aynısı). |
| 1.4 | `TutorialManager.cs:126-137`, `GridManager.cs:350-363` | Tutorial sonrası `OnGridReady` iki kez çağrılıyor (bkz. 0-c) | Timer ve hamle sayacı ilk saniyelerde sıfırlanıyor, çift `level_start` | Yüksek | `OnGridReady`'yi idempotent yapın (`if (CurrentState != Initializing) return;`), ya da tutorial'ı grid önizlemesinden önce bitirin. |
| 1.5 | `GameStateManager.cs:64-70` + `TutorialManager.cs:56-63` | `tutorial_done==0` iken `levelID != 0` olan bir seviyeye girilirse tutorial gösterilmiyor ama `OnGridReady` erken `return` ediyor | Oyun hiç başlamaz (sonsuz `Initializing`). Bugün sadece reklamla açılan seviyeler istisna tutuluyor. Örneğin tutorial'ın ortasında uygulama kapatılıp ilerleme başka yoldan açılırsa tetiklenebilir | Orta [Doğrulanmalı] | Guard'ı `TutorialManager.IsActive`'e bağlayın. "tutorial_done==0" tek başına başlatmayı engellememeli. |
| 1.6 | `PowerUpManager.cs:76-97, 144-201` | İpucu, 2. kart açıkken (`isChecking`, 0,4 sn) kullanılabiliyor (`openCount > 2` kontrolü 2 açık karta izin veriyor) | `CheckMatch` uyuşmazlık sonrası kartları 0,7 sn'de `Hide` ederken İpucu aynı kartı 0,6 sn'de `SetMatched` yapıyor. Kart "Matched" sayılıp tekrar kapalı ve tıklanabilir hale geliyor | Yüksek | İpucu/X-Ray'i `isChecking` sırasında ve `!IsPlaying` iken devre dışı bırakın. |
| 1.7 | `PowerUpManager.cs:64-109` | Power-up'lar `GameStateManager.IsPlaying` kontrolü yapmıyor | Önizleme, pause, win/lose ya da tutorial sırasında kullanılabilir (UI engellemiyorsa), envanter boşa harcanır | Orta | Her `UseX` başına `IsPlaying` kontrolü ekleyin. |
| 1.8 | `PowerUpManager.cs:222-228` | TimeFreeze 5 sn sonra koşulsuz `ResumeTimer()` çağırıyor | Bu sırada oyun kazanılmış ya da kaybedilmişse timer korutini yeniden başlıyor (`timeScale=1` olunca ilk sonraki sahnede `TimeUp` tetiklenebilir). Ayrıca `ResumeGame` (`GameStateManager.cs:101`) dondurmayı erken bitiriyor | Orta | Dondurmayı `isFrozen` bayrağıyla yönetin. Resume yalnızca `IsPlaying` iken yapılsın. |
| 1.9 | `TimerController.cs:103-121` | Zaman Hırsızı süreyi 0'a indirirse ve hemen ardından ilk-kez popup'ı `timeScale=0` yaparsa (`EffectManager.cs:142`), döngü `continue` ile çıkıp `while (remainingTime > 0)` koşuluna takılıyor ve `LoseGame` hiç çağrılmıyor | Süre 00:00'da kalıyor, oyun bitmiyor | Orta | `AddTime` sonrası `remainingTime<=0` ise `TimeUp/LoseGame`'i doğrudan tetikleyin. Döngüyü `while(true)` + açık çıkış ile yazın. |
| 1.10 | `AudioManager.cs:41-46, 58-80` | Ana Menüye her dönüşte sahnedeki kopya `AudioManager` `Awake`'te `Destroy` ediliyor ama `OnEnable` yine çalışıp `sceneLoaded`'a abone oluyor. `OnSceneLoaded` → `PlayBGM` → `bgmSource` null (çünkü `SetupAudioSources` atlandı) | Muhtemel NullReferenceException. `ReportUncaughtExceptionsAsFatal=true` olduğu için Crashlytics'te "fatal" görünür | Yüksek [Doğrulanmalı: Crashlytics'te `AudioManager.OnSceneLoaded` NRE var mı?] | `OnEnable`/`OnDisable`'da `if (Instance != this) return;` ekleyin. DDOL yöneticileri tek bir bootstrap objesinde toplayın. |
| 1.11 | `GameStateManager.cs:53-56` | `OnDestroy` → `GameEvents.ClearAllEvents()` tüm abonelikleri siliyor (DDOL `AudioManager` dahil) | `AudioManager` bunu `sceneLoaded`'da yeniden abone olarak telafi ediyor. Sahne geçiş sırası değişirse (async load, additive) yeni sahnenin abonelikleri silinir. Duplicate `GameStateManager`'ın `Destroy`'u da her şeyi temizler | Orta | Her abone kendi `OnDisable`'ında çıkıyor zaten. `ClearAllEvents`'i kaldırın. |
| 1.12 | `GameStateManager.cs:116-131` + `Level1` `Btn_Retry` → `RestartGame` | Free Mode galibiyetinde `ResetFreeModeState()` çağrılıyor. Sonra "Tekrar" Level1'i Free Mode **kapalı** yeniden yüklüyor | Bayat `SelectedLevel` ile normal seviye açılıyor. `SelectedLevel==null` ise kazanınca `SaveStars` içinde `SelectedLevel.levelID` NRE (`GameStateManager.cs:256`) ve win paneli hiç açılmıyor | Yüksek | Free Mode durumunu galibiyette sıfırlamayın, yalnızca menüye çıkışta sıfırlayın. `SaveStars`'a null kontrolü ekleyin. |
| 1.13 | `GameStateManager.cs:379-391, 434-448` | DC'de kaybedince `hasAttempts` hesaplanıyor ama game over panelinde `Btn_Retry` **her zaman** gizleniyor. Ana Menüye dönüş `SetInactive` → günü "oynandı" olarak işaretliyor | "2 deneme" mantığı fiilen ölü. Buna karşın Pause → Restart (`RestartGame`, `:565-579`) deneme saymadan sınırsız yeniden başlatmaya izin veriyor | Orta | DC deneme kuralını netleştirin. Pause'daki Restart'ı DC'de deneme sayacına bağlayın ya da gizleyin. |
| 1.14 | `Level1.unity` → `Panel_Settings` prefabı `Btn_Close` | Prefabda `m_Target: NONE`, metot boş. MainMenu bunu override edip `CloseSettings`'e bağlamış, **Level1'de override yok** | Oyun içinde Ayarlar'ın kapat butonu çalışmıyor (yalnızca geri tuşu). `OpenSettings` oyunu duraklatmadığı için süre akmaya devam ediyor | Yüksek [Doğrulanmalı: editörde Level1 > Panel_Settings > Btn_Close] | Prefabın kendisinde `SettingsManager.CloseSettings`'i bağlayın. Ayarlar açılınca oyunu pause edin. |
| 1.15 | `FreeMode.unity` `Btn_Start` | Persistent call `GameStateManager.NextOrRetryWin`, hedef **yok** (Missing). Asıl işi koddaki listener yapıyor | Konsol uyarısı ve kafa karışıklığı | Düşük | Boş persistent call'ı silin. |
| 1.16 | `CardController.cs:46-51`, `GridManager.cs:244-257` | 3. kart: `CheckMatch` ikinci kartın aynı karesinde raycast'leri kapattığı için normal dokunuşla 3. kart açılamıyor. Ama `isChecking` sırasında `Reveal()` başka bir yoldan çağrılırsa kart `Open` durumunda **sonsuza kadar** açık kalır (event yutuluyor) | Bugün tetikleyen bir yol yok. Çoklu dokunuşta aynı karede iki kart için güvenli, ancak kırılgan | Düşük | `HandleCardRevealed` reddettiği kartı geri kapatsın ya da `OnCardClicked` `isChecking`'i kontrol etsin. |
| 1.17 | `GridManager.cs:167-181` | Zaman Hırsızı çifti `Random.Range(2, totalPairCount)` ile **sprite indeksi** olarak seçiliyor, ama kartlar 18 sprite'ın karıştırılmış bir alt kümesinden oluşuyor | 5×6 / 6×5'te (15/18 sprite) seçilen indeks grid'de olmayabilir, yaklaşık %17 olasılıkla o seviyede Zaman Hırsızı hiç çıkmaz. 6×6'da hep çıkar. 0 ve 1 indeksli sprite'lar hiç hırsız olamıyor | Orta | Çifti `selectedIDs` listesinden seçin. |
| 1.18 | `AdRewardPopup.cs:145-155`, `WinPanelController.cs:127` | Popup'lar `timeScale`'i doğrudan 0/1 yapıyor. Oyun `Paused` iken power-up popup'ı kapatılırsa `timeScale=1` olur ama state `Paused` kalır. 2x ödül sonrası `timeScale=0` zorlanıyor | Zaman ölçeği birden fazla yerden yönetiliyor ve tutarsız state'ler oluşuyor | Orta | `timeScale`'i yalnızca `GameStateManager` yönetsin, popup'lar ona istek göndersin. |
| 1.19 | Projede hiç `OnApplicationPause` / `OnApplicationFocus` yok | Arka plana atınca oyun pause olmuyor. Dönüşte süre devam ediyor (`WaitForSecondsRealtime` dönüşte en fazla 1 tik ilerler). Reklam açılırken `Time.timeScale` değişmiyor ama Unity zaten askıya alınıyor | Kullanıcı bildirim çekip döndüğünde oyun pause menüsü olmadan sürüyor | Orta | `OnApplicationPause(true)` ve `Playing` ise `PauseGame()`. Reklam gösterimi sırasında bunu bastırın. |
| 1.20 | Reklam callback'leri | Tüm AdMob callback'leri `MobileAdsEventExecutor.ExecuteInUpdate` ile ana iş parçacığına aktarılıyor (`ceb569f`). **Sorun yok.** İstisna: `Load` callback'leri (`AdManager.cs:115-119, 180-184, 245-249`) yalnızca alan atıyor, Unity API kullanmıyor (güvenli). UMP callback'i `Initialize` → `MobileAds.Initialize` çağırıyor (eklenti bunu destekliyor) | – | Bilgi | – |
| 1.21 | `AdManager.cs:61-79` | `Initialize()` UMP formu gösterilmeden **önce** çağrılıyor (`:70`) | Kullanıcı rıza vermeden reklam isteği yapılıyor. Google, rıza/`CanRequestAds` kontrolünden sonra başlatmayı öneriyor (GDPR) | Yüksek | Formdan sonra `ConsentInformation.CanRequestAds()` true ise başlatın. |
| 1.22 | `AdManager.cs:204-205, 215-237` | Interstitial `Show` öncesi event'lere abone olunuyor ama `ShowInterstitialIfReady` iki kez hızlıca çağrılırsa `onInterstitialClosed` üzerine yazılıyor | Çift tıklamada ilk callback kayboluyor ve sahne geçişi olmayabilir | Düşük | "Gösterimde" bayrağı ekleyin, `Btn_NextMission`'ı ilk tıklamada pasif yapın. |
| 1.23 | Çift tıklama genel | `Btn_NextMission`, `RestartGame`, `LevelPopupManager.StartLevel`, DC `Btn_Play` gibi butonlar tıklama sonrası kilitlenmiyor | Çift `LoadScene` ve çift reklam denemesi | Düşük | Sahne geçişi başlayınca `EventSystem`/buton etkileşimini kapatın. |
| 1.24 | `ComboEffectManager.cs:173-196`, `EffectManager.cs:60-83` | Animasyon bitmeden yeniden tetiklenirse `startPos` kayık konumdan alınıyor | Combo/hırsız yazısı kalıcı olarak yukarı kayar | Düşük | Başlangıç pozisyonunu `Awake`'te saklayın. |
| 1.25 | `LevelSelectManager.cs:12, 49-50, 598-609` | Static `Instance` hiç temizlenmiyor. Level1'de obje yok edildiği için `TotalCategoryCount == 0` | `UnlockNextLevel` bir sonraki kategoriyi hiç açmıyor (`GameStateManager.cs:317-319`). Şu an kategori kilidi hiç kullanılmadığı için etkisi yok, ama `IsLastLevelInCategory` de her zaman false | Düşük | Kategori sayısını ScriptableObject'ten okuyun. Ölü kategori kilidini ya silin ya da bağlayın. |
| 1.26 | `LevelSelectManager.cs:88` | `LoadCategory(SelectedCategoryID)`: ID indeks olarak kullanılıyor. `categories` dizi sırası ile ID'ler eşleşiyor mu? **[Doğrulanmalı]** | Sıra farklıysa yanlış kategori açılır | Düşük | ID'den indeksi bulun. |
| 1.27 | `LocalizationManager.cs:54-58` | Dil değişince LevelSelect `LoadCategory(0)`'a sıfırlanıyor | Kullanıcının bulunduğu kategori kayboluyor | Düşük | Mevcut indeksi koruyun. |
| 1.28 | `GameStateManager.cs:20-22` | Release'de log kapatma sadece `GameStateManager.Awake`'te (Level1) yapılıyor | MainMenu ve LevelSelect'te ilk Level1'e kadar `Debug.Log` açık kalıyor | Düşük | Bunu bir `RuntimeInitializeOnLoadMethod`'a taşıyın. |

---

## 2. Kayıt Sistemi (PlayerPrefs)

| Anahtar | Tip | Yazan | Okuyan | Not |
|---|---|---|---|---|
| `tutorial_done` | int | `TutorialManager.cs:129` | `TutorialManager.cs:55`, `GameStateManager.cs:66` | Sabit iki yerde ayrı ayrı yazılmış |
| `unlocked_level_{cat}_{lvl}` | int | `LevelProgressManager.cs:30` | `:25` | Level 0 her zaman açık |
| `unlocked_category_{cat}` | int | `LevelProgressManager.cs:44` | `:39` (**hiç çağrılmıyor**) | Ölü |
| `best_stars_{cat}_{lvl}` | int | `LevelProgressManager.cs:73` | `:78`, LevelSelect | Asıl ilerleme |
| `best_time_*`, `best_moves_*` | float/int | **Hiç yazılmıyor** | **Hiç okunmuyor** | Ölü. Yerine `record_*` kullanılıyor |
| `record_{cat}_{lvl}_{mode}` | float | `GameStateManager.cs:291` | `WinPanelController.cs:359`, `LevelSelectManager.cs:310` | mode=0 |
| `record_{cat}_{lvl}_{mode}_moves` | int | `GameStateManager.cs:284` | `WinPanelController.cs:352`, `LevelSelectManager.cs:311` | mode=1 |
| `ad_unlocked_{cat}_{lvl}` | int | `AdRewardPopup.cs:103` | `LevelSelectManager.cs:143,153,472,510,522`, `GameStateManager.cs:61` | Biçim 5 yerde elle yazılmış, `unlocked_level_` ile çakışıyor (reklam başarılıysa ikisi birden set ediliyor) |
| `total_stars` | int | `GameStateManager.cs:263`, `BadgeManager.cs:32` (çağrılmıyor) | `BadgeManager.cs:27`, `WinPanelController.cs:324` | `best_stars_*`'tan türetilebilir. İki kaynak senkron dışı kalabilir |
| `powerup_reveal` / `_hint` / `_freeze` | int | `PowerUpManager.cs:29,48`, `MainMenuControl.cs:47-49` | `PowerUpManager.cs:23`, `LevelSelectManager.cs:560-568` | `MainMenuControl` başlangıç paketi `MAX_COUNT=10` sınırını atlıyor. İsimler enum'la uyumsuz (`hint`=MindFreeze, `reveal`=XRay) |
| `starter_powerups_given` | int | `MainMenuControl.cs:50` | `:46` | |
| `seen_timethief` | int | `EffectManager.cs:51` | `:49` | |
| `dc_last_played` (string `yyyy-MM-dd`) | string | `DailyChallengeManager.cs:109,141` | `:95,137` | `:137`/`:141` sabit yerine literal kullanıyor |
| `dc_last_stars` | int | `:110,142` | `:103` | |
| `dc_attempts_{yyyy-MM-dd}` | int | `:126` | `:119` | **Her gün yeni bir anahtar** oluşuyor ve hiç silinmiyor. SharedPreferences sürekli büyüyor |
| `ad_level_count` | int | `AdManager.cs:192` | `:191,254` | |
| `last_rewarded_int_time` | float | `AdManager.cs:266` | `:255` | Oturuma göreli `realtimeSinceStartup` kalıcı olarak saklanıyor (**hata**, bkz. 0-i) |
| `review_levels_completed`, `review_shown` | int | `InAppReviewManager.cs:61,100` | `:58,60` | |
| `settings_bgm`, `settings_sfx` | float | `SettingsManager.cs:118,126` | `SettingsManager.cs:79-80`, `AudioManager.cs:48-49` | `AudioManager` mute bayraklarını okumuyor |
| `settings_bgm_muted`, `settings_sfx_muted` | int | `SettingsManager.cs:137,146` | `:81-82` | |
| `settings_language` | string | `LocalizationManager.cs:26,43` | `:17-19` | |

**Bulgular:**

- **S1 – Orta:** Anahtarlar 12 farklı dosyada string literal olarak dağınık. `"ad_unlocked_…"` 6, `"total_stars"` 4, `"powerup_*"` 3 dosyada geçiyor. Yazım hatası şu an yok, ama tek bir `SaveKeys` sınıfına toplanmalı.
- **S2 – Orta:** `LevelProgressManager.ResetAllProgress` (`:82`) `PlayerPrefs.DeleteAll()` çağırıyor ve dil, ses ayarları, review gibi her şeyi siliyor. Şu an çağrılmıyor, ama bağlanırsa tehlikeli.
- **S3 – Düşük:** `Save()` eksikliği yok. Aksine 31 ayrı `PlayerPrefs.Save()` var, bazıları ardışık (`SaveStars` içinde iki kez). Android'de her `Save` diske senkron yazıyor. Kazanma anında 5–6 kez çağrılıyor.
- **S4 – Orta (sürüm güncellemesi riski):** İlerleme `levelID` ve `categoryID` tam sayılarına bağlı. Kategori sırası/ID'si değişirse ya da bir kategoriye seviye eklenip sıra kayarsa ilerleme kayar. Şema sürümü (`save_version`) ve göç (migration) kodu yok. `record_*` ve `best_stars_*` aynı veriyi iki yerde tutuyor.
- **S5 – Düşük:** Bulut yedeği yok. Android Auto Backup'ın açık olup olmadığı **[Doğrulanmalı]**. Cihaz değişince tüm ilerleme kayboluyor.
- **S6 – Düşük:** `dc_*` tarihleri `DateTime.Now` ile yerel saatten alınıyor. Saati ileri alarak DC tekrar oynanabilir.

---

## 3. Ölü ve Tekrarlayan Kod

**Hiç çağrılmayan metod ve alanlar:**

| Öğe | Dosya:satır |
|---|---|
| `LogTutorialBegin`, `LogTutorialStep` | `FirebaseManager.cs:164,175` (bkz. 0-b) |
| `GameEvents.OnTimeUp`, `OnTimeChanged`, `OnMoveUsed`, `OnGameResumed`, `OnGridIntervention`, `OnPowerUpUsed` (yayınlanıyor ama **abonesi yok**), `RaiseGridIntervention`, `RaisePowerUpUsed` (hiç yayınlanmıyor), `GridInterventionType` enum'u | `GameEvents.cs` |
| `CardEffectType.Fog / Swapper / LockBreaker`, `CardState.Locked`, `CardController.SetLocked/Unlock` | `GameEvents.cs:131-138`, `CardController.cs:81-82` |
| `LevelProgressManager.SaveBestTime/GetBestTime/SaveBestMoves/GetBestMoves/IsCategoryUnlocked/ResetAllProgress` | `LevelProgressManager.cs:36-84` |
| `BadgeManager.AddStars` | `BadgeManager.cs:29` |
| `LevelSelectManager.IsLastLevelInCategory` | `:598` |
| `DailyChallengeManager.CanRetry` | `:114` |
| `ComboEffectManager.TriggerMatchFlash` | `:33` |
| `AudioManager.PlayPowerUpSFX/StopBGM/PlayMenuMusic/PlayGameMusic` (`sfxPowerUp` hiç çalmıyor) | `AudioManager.cs:226-252` |
| `WinPanelController.ClearPendingRewards` | `:57` |
| `MoveController.UseMoveLimit`, `TimerController.IsRunning` | |
| `LevelConfig.useTimer`, `useMoveLimit`, `levelIcon`, `levelName` (popup'ta kullanılıyor ✓), `TotalPairs` | `LevelConfig.cs` |
| `CategoryConfig.categoryIcon`, `categoryColor`, `categoryDescription`, `IsCompleted` | `CategoryConfig.cs` |
| `DailyChallengeManager.limitConfig` (atanıyor ama kullanılmıyor; GridManager kendi referansını kullanıyor) | `:33` |
| Tutorial için 3 dilde tanımlı ama kullanılmayan anahtarlar (`tutorial_title`, `tutorial_next`, `tutorial_tap_hint`, `freeze_banner`, `freeze_sub`, `mode_story_sub`, `btn_quit` vb.) | `LocalizationManager.cs` |

**Kullanılmayan `using`'ler (Düşük):** `GameEvents.cs: using UnityEngine` (yalnızca enum ve event), `LevelPopupManager.cs: using UnityEngine.UI`, `DailyChallengeManager.cs: System.Collections` (gerekli: `IEnumerator`) / `TMPro` (gerekli). Derleyici uyarılarıyla taranmalı.

**Kopyalanmış mantık:**

- **D1 – Orta:** Singleton `Awake` kalıbı 24 sınıfta aynen kopyalanmış. Bir kısmı DDOL, bir kısmı değil, hiçbiri `OnDestroy`'da `Instance`'ı temizlemiyor. Generic bir `Singleton<T>` ya da tek bir bootstrap kullanılmalı.
- **D2 – Orta:** `bool isMoveMode = LevelSelectManager.SelectedMode == GameMode.Move` ifadesi 11 yerde tekrarlanıyor (GSM, PowerUpManager, PowerUpUI, EffectManager×2, AdRewardPopup, AudioManager, WinPanel×2…). Aktif oturum bilgisi (mod, grid, limitler, kaynak: Story/DC/Free) tek bir `SessionContext` nesnesinde tutulmalı. 0-d ve 0-e hataları bu dağınıklıktan kaynaklanıyor.
- **D3 – Orta:** Zorluk → yerelleştirme anahtarı eşleşmesi 2 kez (`LevelSelectManager.cs:337-343`, `DailyChallengeManager.cs:203-209`), zorluk → "easy" string'i 1 kez kopyalanmış.
- **D4 – Düşük:** Yıldız hesabındaki eşikler (`GameStateManager.cs:236-247`) ile `WinPanelController.GetPerformanceLabel` (`:329-334`) ayrı ayrı sabit kodlanmış.
- **D5 – Düşük:** "Btn_Retry gizle" döngüsü 2 kez (`GameStateManager.cs:147-150, 443-446`). Game over panel güncellemeleri `transform.Find` yollarıyla 2 kez yapılıyor.
- **D6 – Düşük:** Nabız (pulse) animasyonu korutini 5 farklı yerde yeniden yazılmış (LevelSelect ×2, DC, WinPanel ×2, ComboEffect).
- **D7 – Düşük:** `HighlightCategoryButton` ile `HighlightButton` (`FreeModeManager.cs:235-285`) neredeyse aynı.

**Kullanılmayan asset ve script'ler:**

- Tüm `Assets/Scripts` MonoBehaviour'ları en az bir sahne ya da prefabda kullanılıyor (ScriptableObject'ler ve `GameEvents` hariç).
- `Assets/Art/Prefabs/Btn_Category_Template_Old.prefab`: hiçbir sahnede referansı bulunamadı **[Doğrulanmalı]**.
- `Assets/Scripts/Editor/CRTSpriteCreator.cs`, `CornerSpriteCreator.cs`: tek seferlik araçlar. Çıktıları `Assets/Art/Sprites/spr_*.png`'ye taşınmış, ama script'ler hâlâ olmayan `Assets/Sprites/` yoluna yazıyor.
- **`Assets/Art/Sprites/StagingCards`: 272 MB**, 169 dosya, 160'ı kategorilerde kullanılıyor. 9 dosya referanssız ("Ice Cream", "Black Hole" gibi boşluklu adlar; tarama betiği bunları ayrıştıramadı **[Doğrulanmalı]**). Tek dosya 8 MB'a kadar çıkıyor. Bkz. Performans P1.
- `Assets/Honeti` (52 MB): yalnızca birkaç UI prefab/sprite kullanılıyor. Kullanılmayan kısmı build'e girmez ama repoyu şişiriyor.

---

## 4. Performans

| # | Yer | Sorun | Öncelik | Öneri |
|---|---|---|---|---|
| P1 | Kart sprite'ları (`StagingCards/*.png.meta`) | `maxTextureSize: 2048`, platform override yok. Kartlar ekranda ~170–265 px. Her `CategoryConfig` 18 sprite'ı referans ediyor ve `LevelSelectManager`, `DailyChallengeManager`, `FreeModeManager` **8 kategorinin tamamını** sahne referansıyla tutuyor. Sonuç: MainMenu/LevelSelect açılırken 144 adet 2048'lik doku belleğe yükleniyor | **Yüksek** | Android için `maxTextureSize 512` (en fazla 1024) + ASTC. Kategorileri Addressables/Resources ile ihtiyaç anında yükleyin ya da kategori başına bir Sprite Atlas kullanın. Açılış süresi ve bellek doğrudan iyileşir. |
| P2 | `LevelSelectManager.CreateLevelCard` (`:232-498`) | Her kart için ~13 `transform.Find` + `GetComponent`, her kartta `new Color`, ve her kart için tüm seviyeleri tarayan `IsNextUnlockableLevel` (O(n²) PlayerPrefs okuma). Kategori değiştirince 10 kart `Destroy`/`Instantiate` ediliyor | Orta | Kart prefabına bir `LevelCardView` bileşeni ekleyip referansları serialize edin. Kilit hesaplamasını bir kez yapın. Kartları havuzlayın. |
| P3 | `LevelSelectManager.cs:187` | `string[] borderNames` her döngüde yeni dizi oluşturuyor | Düşük | `static readonly` yapın. |
| P4 | `DailyChallengeManager.Update` (`:54-57, 243-252`) | Her karede `DateTime.Now` + string interpolasyonu (`txtTimer.text = $"..."`), yani her karede GC tahsisi | Orta | Saniyede bir güncelleyin (korutin ya da önceki saniyeyle karşılaştırma). |
| P5 | `PlayButtonJuicer.Update`, `UIButtonPolish.Update` (22 örnek), `StarTwinkle.Update`, `MenuParallax.Update` | Her buton her karede `localScale` Lerp'liyor (dinlenme halindeyken bile). UI her karede yeniden çiziliyor (canvas rebuild) | Orta | Hedefe ulaşınca `enabled=false` yapın ya da yalnızca etkileşimde korutin çalıştırın. |
| P6 | `AudioManager.Update` | Önemsiz bir kontrol | Düşük | – |
| P7 | `LocalizationManager.SetLanguage` (`:47`) | `FindObjectsOfType<LocalizedText>(true)`. Yalnızca dil değişince çalışıyor | Düşük | Kabul edilebilir. İsterseniz `LocalizedText` kendini bir listeye kaydetsin. |
| P8 | `GameStateManager.cs:126,147,151,166,174,443` | `panelWin.GetComponentInChildren<WinPanelController>()` ödül başına tekrar çağrılıyor. `GetComponentsInChildren<Button>(true)` + isim karşılaştırması | Düşük | Serialize edilmiş referans kullanın. |
| P9 | `TimerController.UpdateTimerUI`, `MoveController.UpdateUI`, `PowerUpUI.UpdateUI` | Değişiklikte string tahsisi. Saniyede 1 kez, kabul edilebilir | Düşük | – |
| P10 | Kart animasyonları | `FlipAnimation`/`MatchAnimation` her kart için ayrı korutin. 36 kartlık önizlemede 36 korutin. Sorun değil | Bilgi | – |

**İsimden arama yapan tüm yerler** (`GameObject.Find` yok, `FindObjectOfType` yalnızca P7'de):

| Dosya:satır | Aranan yol |
|---|---|
| `GameStateManager.cs:457, 476, 478, 504` | `Panel_LoseContent/…/Text_TilesValue`, `TextPointsLabel`, `Text_PointsValue`, `Text_Reason` |
| `GameStateManager.cs:149, 445` | İsim karşılaştırması `"Btn_Retry"` |
| `EffectManager.cs:120, 131` | `Panel_Content/Panel_Title/Text_PauseTitle`, `Panel_Content/Text_Desc` |
| `MainMenuControl.cs:58-67, 72` | `Panel_Content/Upper_Panel/Text_ModeTitle` + 4 yol `Panel_Body/...` altında |
| `LevelSelectManager.cs:190, 260, 281, 296, 329, 362, 366, 369, 390, 393, 406, 409, 422` | `Border_*`, `Image`, `Panel_CardInfo/Text_LevelName`, `Text_LevelStat`, `Text_Difficulty`, `Img_LevelArt/...`, `Tag_New/Text_New`, `Tag_Ad/Text_Ad`, `Panel_Stars` |
| `FreeModeManager.cs:251, 277` | `Border_Bottom` |
| `SettingsManager.cs:214, 220` | `Image`, `Image (1)` |

**Boşluklu isimler** (YAML'da tırnaklı olarak doğrulandı):

| İsim | Konum | Kodla bağlantı |
|---|---|---|
| `'InAppReviewManager '` | MainMenu | Kodda isimle aranmıyor |
| `'Panel_TopBar '` | MainMenu | Kodda isimle aranmıyor |
| `' Panel_ModeSelect'` | MainMenu | Kodda isimle aranmıyor. `MainMenuControl` serialize referans kullanıyor ve alt yolları `Panel_Content/...` ile arıyor, bu yüzden üst objenin adı sorun değil |
| `'Btn_Restart '` | Level1 | Kodda isimle aranmıyor (`"Btn_Retry"` aranıyor, bu farklı bir obje) |

Bunların hiçbiri şu an bir aramayı kırmıyor. Ama ileride `Find(" Panel_ModeSelect")` gibi bir hata çıkmaması için temizlenmeli (**Düşük**).

**Açılıştan ilk oynanabilir ana kadar** (Ana Menü → Oyna → Hikâye → Seviye):

1. `BeforeSceneLoad`: `LocalizationManager` 143 string'lik sözlüğü doldurur. Ucuz.
2. MainMenu yüklemesi: `DailyChallengeManager.categories` ve buna bağlı **8 kategorinin tüm sprite'ları** belleğe girer (P1).
3. `Start` aşamasında: Firebase `CheckAndFixDependenciesAsync` (async), UMP `ConsentInformation.Update` → form (ağ) → `MobileAds.Initialize` → 3 reklam yükleme isteği, `InAppUpdate` → Play API ve `androidx` sınıf kontrolü, `InAppReview` → `RequestReviewFlow` (gösterilmeyecekse bile), `GiveStarterPowerUps`. Hepsi async, ana iş parçacığını bloklamıyor, ama ağ ve CPU rekabeti var.
4. LevelSelect: 10 kart `Instantiate` + P2.
5. Level1: `GenerateGridDelayed` bir kare bekliyor + **1,5 sn önizleme + 0,3 sn** (tasarım gereği). İlk seviyede buna 8 adımlık tutorial ekleniyor.

En büyük kazanç P1'den gelir (doku boyutu ve tümünü önceden yükleme). İkinci olarak Review akışının ilk açılışta istenmemesi ve reklam yüklemelerinin ilk etkileşimden sonraya kaydırılması değerlendirilebilir.

---

## 5. Mimari

**Singleton'lar ve yaşam döngüleri:**

| Sınıf | DDOL | Bulunduğu sahne | Not |
|---|---|---|---|
| AdManager, AudioManager, FirebaseManager, InAppUpdateManager, InAppReviewManager, LocalizationManager | ✓ | MainMenu (her biri ayrı kök obje) | Ana Menüye her dönüşte kopyaları oluşturulup `Awake`'te yok ediliyor (1.10 riski). Uygulama MainMenu dışı bir sahneden açılırsa (editör testi) hiçbiri yok. Hepsi `?.` ile korunuyor |
| **LevelProgressManager** | ✓ | **Yalnızca LevelSelect** | MainMenu → DC yolunda hiç oluşmuyor (DC'de kullanılmadığı için şimdilik sorun yok). Bir bootstrap'a taşınmalı |
| GameStateManager, GridManager, TimerController, MoveController, TutorialManager, EffectManager, ComboEffectManager, PowerUpUI, WinPanel | ✗ | Level1 | Static `Instance` yok edilen objeye işaret etmeye devam ediyor (Unity `==` null döndürdüğü için çoğu yerde zararsız) |
| PowerUpManager | ✗ | LevelSelect **ve** Level1 (iki ayrı örnek) | Durum `PlayerPrefs`'te olduğu için sorun yok |
| SettingsManager | ✗ | MainMenu (MenuManager) **ve** Level1 (GameManager) | |
| LevelSelectManager, LevelPopupManager, FreeModeManager, DailyChallengeManager, MainMenuControl, BadgeManager | ✗ | Kendi sahneleri | Static durumları (`SelectedLevel`, `IsDailyChallengeActive`, `IsFreeModeActive`…) sahneler arası taşıyıcı olarak kullanılıyor |

**GameState ve event sistemi gerçekte nasıl çalışıyor:**

- `GameStateManager.CurrentState`: `Initializing → Playing ⇄ Paused → Win | Lose → (second chance) Playing`. Tek gerçek kontrol `WinGame`/`LoseGame`'deki `if (CurrentState != Playing) return;` satırı.
- **Tek otorite yok:**
  - `LoseGame` 2 yerden çağrılıyor (Timer, MoveController), `WinGame` 1 yerden (GridManager).
  - `Time.timeScale` 8 farklı yerden değiştiriliyor: GSM, TutorialManager, AdRewardPopup, EffectManager, WinPanel, …
  - Second chance state'i doğrudan `Playing` yapıyor ve `OnGameResumed` yayınlamıyor.
  - "Hangi mod" bilgisi 3 static kaynakta dağınık (`LevelSelectManager.SelectedMode`, `DailyChallengeManager.IsDailyChallengeActive`, `FreeModeManager.IsFreeModeActive/SelectedMode`). Bunlar birbirini elle güncelliyor ve 0-d/0-e hataları buradan çıkıyor.
- `GameEvents`: 14 event tanımlı. Aktif olarak kullanılan 7 tanesi: CardRevealed, PairMatched, PairMismatch, GameStarted, GameWon, GameLost, GamePaused/EffectTriggered. Kalanların abonesi yok (bkz. 3). Event'ler UI'ı da doğrudan tetiklemiyor: çoğu sınıf birbirinin `Instance`'ını doğrudan çağırıyor (ör. `TimerController` → `GameStateManager.Instance.LoseGame()`). Yani event sistemi yarı yarıya kullanılıyor.

**Sorumlulukları karışmış sınıflar:**

- **GameStateManager (607 satır):** durum makinesi + yıldız hesabı + kayıt (stars, record, unlock) + ödül dağıtımı (rastgele power-up) + DC ödülü + analitik + game over UI'ını `transform.Find` ile doldurma + reklam akışı (`NextOrRetryWin`) + Escape tuşu. En az 4 sınıfa bölünmeli: `GameFlow`, `RewardService`, `ProgressService`, `ResultUI`.
- **AudioManager:** ses + **combo sistemi ve oyun ödülü** (`GiveComboReward` süre/hamle ekliyor). Oynanış kuralının ses yöneticisinde olması açıkça yanlış.
- **LevelSelectManager (627 satır):** kategori seçimi + seviye kartı görselleri (renkler sabit kodlu) + reklamla kilit açma kuralları + power-up satın alma + sahneler arası static seçim durumu.
- **PowerUpManager:** envanter (`PlayerPrefs`) + efekt uygulama + analitik + reklam popup'ı.
- **EffectManager:** efekt + tek seferlik tutorial popup'ı + `timeScale` yönetimi.

**Tween kütüphanesi:** **Yok** (DOTween, LeanTween ya da `DG.Tweening` referansı bulunamadı). Tüm animasyonlar elle yazılmış korutinlerle (`Mathf.Lerp` + `Time.deltaTime` / `unscaledDeltaTime`) ya da `Update` içinde Lerp ile yapılıyor (UIButtonPolish, PlayButtonJuicer). `UIPopupController` `AnimationCurve` kullanıyor. Oyun içi kart animasyonları **ölçekli** zaman kullanıyor (`timeScale=0` iken donuyor, tutorial önizlemesi bu yüzden duruyor), win ve combo UI'ı **ölçeksiz** zaman kullanıyor. Bu karışım tutarsız davranışların bir kısmının kaynağı.

---

## 6. Doküman Durum Haritası

| Madde | Durum | Dosya / Not |
|---|---|---|
| GameEvents | **Tamam** (yarı kullanımda) | `GameEvents.cs`. 6 event'in abonesi yok |
| GameStateManager (Initializing/Playing/Paused/Win/Lose) | **Tamam** | `GameStateManager.cs:601-608`. Tek otorite değil (bkz. 5) |
| CardController state'leri | **Kısmen** | `CardController.cs`: Closed/Open/Matched var, `Locked` tanımlı ama kullanılmıyor |
| GridManager dinamik grid | **Tamam** | `GridManager.cs:119-240` (sütun/satır ve hücre boyutu hesaplanıyor) |
| AudioManager | **Tamam** | `AudioManager.cs` (SFX varyantları, BGM, ses ayarı) |
| Input kilitleme | **Kısmen** | `GridManager.SetAllCardsInteractable` + `isChecking`. Power-up'lar ve state kontrolü eksik (1.6, 1.7) |
| TimerController | **Tamam** | `TimerController.cs` |
| MoveController | **Tamam** | `MoveController.cs` |
| Klasik mod | **Tamam** | Timer tabanlı |
| Hamle modu | **Tamam** | 1.1 hatasıyla birlikte |
| Kart çevirme animasyonu | **Tamam** | `CardController.FlipAnimation` |
| Eşleşme animasyonu | **Tamam** | `CardController.MatchAnimation` |
| Yanlış eşleşme animasyonu | **Tamam** | `CardController.ShakeAnimation` |
| LevelConfig | **Tamam** | `LevelConfig.cs`, 80 asset (`Assets/Data/Levels`) |
| Kategori sistemi | **Tamam** | `CategoryConfig.cs`, 8 asset |
| Level seçimi | **Tamam** | `LevelSelectManager.cs`, `LevelPopupManager.cs` |
| Mod seçimi | **Tamam** | Seviye popup'ında Classic/Move. Ana menüde Hikâye/Serbest (`MainMenuControl`) |
| İlerleme kaydı | **Tamam** | `LevelProgressManager.cs` + `GameStateManager` (bkz. 2) |
| Tutorial | **Kısmen** | `TutorialManager.cs`: 8 bilgi ekranı, etkileşimli değil. Begin/Step analitiği yok. Ayrıca `EffectManager` Zaman Hırsızı popup'ı |
| CardType enum (Normal/Fog/Swapper/TimeThief/LockBreaker) | **Kısmen** | `CardEffectType` enum'u var (`GameEvents.cs:131`). Yalnızca `TimeThief` uygulanmış |
| EffectManager | **Kısmen** | `EffectManager.cs`: yalnızca TimeThief |
| Zaman Hırsızı | **Tamam** | `GridManager.cs:164-210`, `EffectManager.cs` (Move'da hamle hırsızı). Seçim hatası var (1.17) |
| Kilit Kartı | **Yok** | Yalnızca `CardState.Locked` ve `SetLocked/Unlock` iskeleti var |
| Dönen Grid | **Yok** | Yalnızca `GridInterventionType.Rotate` enum'u |
| Sis Kartı | **Yok** | Yalnızca enum |
| Yer Değiştiren Kart | **Yok** | Yalnızca enum |
| Sıkışma efekti | **Yok** | Yalnızca `GridInterventionType.Shrink` enum'u |
| Stres Modu | **Yok** | – |
| Karanlık Modu | **Yok** | – |
| Mod unlock | **Yok** | Classic/Move ikisi de her zaman açık. Kategori kilidi kodda var ama kullanılmıyor (ölü) |
| PowerUpManager | **Tamam** | `PowerUpManager.cs` |
| X-Ray | **Tamam** | `PowerUpManager.XRayCoroutine` (2 sn) |
| Zihin Sabitleme (MindFreeze) | **Kısmen / farklı** | Enum adı `MindFreeze` ("5 sn kartlar kapanmaz"), ama gerçekte **İpucu** gibi çalışıyor: bir çifti otomatik eşleştiriyor (`HintCoroutine`) |
| Zaman Dondurma | **Tamam** | `TimeFreezeCoroutine` (5 sn; Move'da 3 bedava hamle) |
| Günlük ödül | **Kısmen** | Ayrı bir günlük giriş ödülü **yok**. Yalnızca DC tamamlama ödülü (`GameStateManager.GiveDailyChallengeReward`) ve tek seferlik başlangıç paketi (`MainMenuControl.GiveStarterPowerUps`) var |
| Combo sistemi | **Tamam** | `AudioManager.cs:125-185` (yanlış sınıfta), görseli `ComboEffectManager.cs` |
| Power-up envanteri | **Tamam** | `PlayerPrefs` `powerup_*`, en fazla 10 |
| Second chance (devam et) | **Tamam** | `AdRewardPopup.ShowForSecondChance` + `GameStateManager.ApplySecondChance` (1.2, 1.3 hatalarıyla) |
| Seviye kilidi açma | **Tamam** | Yıldızla ve reklamla (`LevelSelectManager`, `AdRewardPopup.ShowForLockedLevel`) |
| Haptic | **Yok** | `Handheld.Vibrate` ya da benzeri bir çağrı yok |
| Particle | **Yok** | Kodda `ParticleSystem` kullanımı yok. Efektler UI flash ve ölçek animasyonu |
| Arka plan müziği | **Tamam** | `AudioManager.PlayBGM` (menü ve oyun) |
| Analytics | **Kısmen** | `FirebaseManager.cs` (hatalar için bkz. bölüm 0) |
| AdMob | **Tamam** | `AdManager.cs` + UMP (rıza sırası hatalı: 1.21) |
| Daily Challenge | **Tamam** | `DailyChallengeManager.cs` (deneme mantığı ölü: 1.13) |
| Free Mode | **Tamam** | `FreeModeManager.cs`, `FreeModeLimitConfig` (analitik yok, 1.12 hatası) |
| Story Mode | **Kısmen** | Ayrı bir hikâye yok. "Story Mode" = 8 kategori × 10 seviyelik level select (`MainMenuControl.OnStoryModeClicked`) |
| Lokalizasyon | **Tamam** (2 dil) | `LocalizationManager.cs`: **Türkçe ve İngilizce**, her biri 143 anahtar, eksik ya da tekrar yok. Kodda kullanılan tüm anahtarlar tanımlı. Varsayılan dil sistem dili (Turkish → TR, diğerleri EN) |

---

## 7. Özet

### Bölüm 0 — tek cümlelik cevaplar

- **a)** `level_id` ve `category_id` her dört event'te de doğru adlarla ama **sayısal (`long`)** olarak gönderiliyor. Kod tarafında eksik yok. (not set) büyük olasılıkla dimension'ın sonradan tanımlanmasından ya da kapsam ve tip uyumsuzluğundan kaynaklanıyor **[Doğrulanmalı]**.
- **b)** `LogTutorialBegin` ve `LogTutorialStep` hiçbir yerden çağrılmıyor. Tutorial, Level1'de herhangi bir kategorinin 1. seviyesinde açılan 8 bilgi ekranından oluşuyor ve "BAŞLA"ya basıldığı an bitiyor.
- **c)** `CompleteTutorial` aynı karede `tutorial_complete` gönderip `OnGridReady`'yi (dolayısıyla `level_start`'ı) çağırıyor. Ayrıca ~1,8 sn sonra ikinci bir `level_start` daha geliyor.
- **d)** DC galibiyetinde `SetInactive()` `level_end`'den önce çağrıldığı için galibiyet "classic" olarak loglanıyor. `daily_challenge_complete` ayrı ve doğru gönderiliyor.
- **e)** "Classic 0x0" kayıtları, hiç hikâye seviyesi seçilmemiş oturumlardaki DC galibiyetleri (`SelectedLevel == null`).
- **f)** Doğru: Free Mode bilinçli olarak hiçbir seviye event'i göndermiyor.
- **g)** `level_start` `GSM:80`. Kazanmada `level_complete` `GSM:305` + `level_end` `GSM:161`, kaybetmede `level_fail` `GSM:372` + `level_end` `GSM:377`. İkinci şanstan sonra tekrar kaybetme ikisini de ikinci kez yazıyor.
- **h)** 1.09 düzeltmesi (`androidx.activity:1.8.2` + sınıf kontrolü) yerinde ve gradle'a işlenmiş. Büyük olasılıkla yeterli, ancak Crashlytics'teki eksik sınıf adı guard'daki adla karşılaştırılmalı ve flexible update tamamlama mantığı düzeltilmeli.
- **i)** Interstitial yalnızca galibiyet sonrası "Sonraki"de (her 3. basış, 180 sn arayla) çıkıyor. Rewarded'lar opsiyonel. Rewarded interstitial hem DC başında hem galibiyet sonrası çıkıyor, ve **ilk galibiyette ve ilk DC'de yeni oyuncuya reklam gösteriliyor** (ilk-N koruması yok).
- **j)** Tüm kategoriler aynı 10 seviyelik tabloyu kullanıyor. Move limitleri (özellikle 5×4'te 1,7–1,8·N) neredeyse kusursuz oyun istiyor ve "son hamlede kazanma = kayıp" hatası oranları şişiriyor. 4×5 ile 5×4 farkı önce süre limitinden (%25 daha az), sonra 5 sütunun kartları ~%20 daraltmasından kaynaklanıyor.
- **k)** Firebase Unity SDK 13.12.0 (Android Crashlytics NDK 20.0.6), yani ANR için gereken 18.2.4+ şartı karşılanıyor.

### En kritik 10 bulgu (öncelik sırasıyla)

1. **[Kritik] 1.1:** Son hamlede veya son saniyede tamamlanan oyun kaybedilmiş sayılıyor (`MoveController.cs:76`, `TimerController.cs:114`, `GridManager.cs:300-307`).
2. **[Kritik] 1.2:** 1.1'den sonra ikinci şans izlenirse oyun boş tahtada kilitleniyor (`GameStateManager.cs:406-421`).
3. **[Kritik] 1.3:** Rewarded reklam ödülsüz kapatılınca callback gelmiyor, second chance'te oyuncu kilitleniyor (`AdManager.cs:135-143`).
4. **[Yüksek] 0-d/e:** DC galibiyetleri yanlış mod ve grid ile loglanıyor (`GameStateManager.cs:137-142`), DC ve Classic analitiğini kirletiyor.
5. **[Yüksek] 0-i / AdManager:** Rewarded'larda çift `ad_watched` ve yanlış placement'lar (`AdManager.cs:122`, `AdRewardPopup.cs:173-176`, `GameStateManager.cs:585`), gelir analitiği güvenilmez.
6. **[Yüksek] 1.21:** AdMob, UMP rızası alınmadan başlatılıyor (`AdManager.cs:70`), GDPR uyumluluk riski.
7. **[Yüksek] 1.12:** Free Mode galibiyeti sonrası "Tekrar" bayat seviye açıyor ve `SaveStars` NRE ile kilitleniyor (`GameStateManager.cs:129, 256`).
8. **[Yüksek] 1.14:** Level1'de Ayarlar kapat butonu bağlı değil ve ayarlar açıkken süre akıyor **[Doğrulanmalı]**.
9. **[Yüksek] P1:** 144 adet 2048 px kart dokusu MainMenu'dan itibaren belleğe yükleniyor, açılış süresini ve bellek kullanımını ciddi şekilde etkiliyor.
10. **[Yüksek] 1.4 + 0-b/c:** Tutorial sonrası çift `OnGridReady` (timer sıfırlanıyor, çift `level_start`) ve tutorial begin/step analitiğinin eksikliği.

Hemen arkasından gelenler: 1.10 (AudioManager kopyası NRE, Crashlytics'te "fatal" olarak görünüyor **[Doğrulanmalı]**), 1.6 (İpucu yarışı), 0-i (`last_rewarded_int_time` oturumlar arası hatası ve ilk-N reklam koruması), InAppUpdate tamamlama mantığı.

### Önerilen temizlik sırası

1. **Oyun sonu otoritesi (1.1, 1.2, 1.9):** "Tüm çiftler eşleşti" kontrolünü timer/hamle kontrolünden önce yapın, Win/Lose'u tek bir `EndGame(result)` noktasına toplayın, `OnGridReady`'yi idempotent yapın (1.4). Diğer düzeltmeler bu tek otoriteye bağlı olduğu için en önce bu yapılmalı.
2. **Reklam callback'leri (1.3, 1.22):** Ödülsüz kapanışta `onFail` çağrılsın. Bu adım 1'den bağımsız, paralel yapılabilir.
3. **`SessionContext` (D2):** Story/DC/Free kaynağı, mod, grid ve limitler tek bir nesnede toplansın. 4 ve 5. adımlar buna dayanıyor.
4. **Analitik düzeltmeleri (0-a…g, reklam placement'ları):** `level_end`'i context'ten üretin, DC/Free modlarını doğru etiketleyin, `LogTutorialBegin/Step`'i ekleyin, `ad_watched`'ı tek yerden ve doğru placement'la gönderin, string `level_key` ekleyin. Yeni event şemasını bir sürüm notuyla birlikte yayınlayın, çünkü eski ve yeni veri karışacak.
5. **Free Mode ve DC akışı (1.12, 1.13):** Context üzerinden düzeltin.
6. **Reklam politikası (1.21, 0-i):** UMP → `CanRequestAds` → initialize sırası. İlk N seviye ya da ilk oturum için reklam muafiyeti. `last_rewarded_int_time`'ı gerçek saat (UTC) ile saklayın. RI placement'larını ayırın.
7. **Zorluk ayarı (0-j):** 1. adım düzeldikten **sonra** 1–2 hafta veri toplayıp 5×4 → 4×5 dönüşümü ve Move limitleri (≥2,3·N) değiştirilsin. Önce yapılırsa hata etkisi ile denge etkisi ayırt edilemez.
8. **Sahne ve UI bağlantıları (1.14, 1.15, boşluklu isimler, `Btn_Category_Template_Old`):** Küçük ve bağımsız işler.
9. **Performans (P1, P2, P4, P5):** Doku import ayarları ve kategori yükleme stratejisi. P1 asset değişikliği gerektirdiği için ayrı bir PR'da yapılmalı.
10. **Mimari temizlik (D1, `ClearAllEvents` 1.11, ölü kod bölüm 3, kayıt anahtarları S1/S4):** En son yapılmalı, çünkü önceki adımlar zaten bu sınıflara dokunacak. `ClearAllEvents`'in kaldırılması, tüm abonelerin `OnDisable`'da çıktığı doğrulandıktan sonra yapılmalı. Kayıt anahtarlarını tek sınıfa toplarken eski anahtar adlarını aynen koruyun, yoksa mevcut oyuncuların ilerlemesi kaybolur.
