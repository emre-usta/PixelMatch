# PixelMatch: Claude Code kuralları

## Proje
- Unity 2022.3.62f3, Android (com.ReforgedGames.PixelMatch). Google Mobile Ads 11.2.0, Firebase (Analytics, Crashlytics).
- Denetim raporu: docs/AUDIT.md. Görev numaraları (ör. AUDIT 1.12) bu dosyaya referanstır.

## Kesin yasaklar
- Build, player compile veya geçici derleme klasörüyle (pm_playercompile, pm_compile_check vb.) derleme denemesi YAPMA. Unity'yi kilitliyor. Sadece editör derlemesi ve Console kontrolü yeterli.
- git commit / push / merge YAPMA. Commit'leri kullanıcı yapar.
- PlayerPrefs anahtar adlarını değiştirme, silme veya yeniden adlandırma (mevcut oyuncu kayıtları bozulur).
- Gerçek AdMob reklam ID'lerini değiştirme; test ID'leri yalnızca `#if UNITY_EDITOR || DEVELOPMENT_BUILD` altında kalır.
- Assets/Plugins/Android/FirebaseCrashlytics.androidlib/res/values/crashlytics_build_id.xml dosyasına dokunma.

## Çalışma şekli
- Kod yazmadan önce plan çıkar: hangi dosyalar, hangi değişiklik, neden, olası yan etkiler. Onay gelmeden uygulama.
- Sahne (.unity) veya prefab değişikliği gerekiyorsa önce sor.
- script-execute yalnızca kullanıcının açıkça gösterdiği tek satırlık çalışma zamanı testleri için.
- Kapsam dışına çıkma: analitik değişiklikleri Faz 3'e, reklam gösterim kuralları (sıklık, 180 sn, ilk oturum) Faz 4'e aittir. Görev dışı bir sorun görürsen düzeltme, sadece raporla.
- Değişiklikten sonra: değişen dosyaların listesi, kısa özet ve kullanıcının yapacağı test adımları.
