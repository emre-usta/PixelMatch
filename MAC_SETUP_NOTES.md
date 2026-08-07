# Mac Editor Kurulum Notları

## Firebase macOS Editor Bundle (gerekli, git'e dahil değil)

Bu proje Mac'te ilk kez klonlandığında, Unity Editor'de Play Mode'a girince
Firebase şu hatayı verir:

    DllNotFoundException: FirebaseCppApp-13_12_0

Sebep: Assets/Firebase/Plugins/x86_64/FirebaseCppApp-13_12_0.bundle dosyası
büyük boyutu (106 MB, GitHub'ın 100 MB dosya limitini aşıyor) nedeniyle
bilinçli olarak .gitignore'da tutuluyor ve repoya commit edilmiyor. Bu dosya
SADECE macOS Editor'de Play Mode testi için gerekli - gerçek iOS build'ini
hiç etkilemez, Android build'ini de etkilemez.

### Çözüm (yeni bir Mac'te bir kereliğine yapılır)

1. https://github.com/firebase/firebase-unity-sdk/releases adresinden
   v13.12.0 surumunun FirebaseAnalytics.unitypackage dosyasini indirin.
2. Dosyayi cikartin: tar -xzf FirebaseAnalytics.unitypackage -C analytics_extracted
3. Dogru klasoru bulun: grep -rl "FirebaseCppApp-13_12_0.bundle" analytics_extracted/*/pathname
4. Bulunan klasordeki asset dosyasini kopyalayin:
   cp analytics_extracted/<GUID>/asset Assets/Firebase/Plugins/x86_64/FirebaseCppApp-13_12_0.bundle
5. Unity Editor'e donun, otomatik import edilmesini bekleyin.
6. Bu dosyayi asla git add etmeyin, .gitignore zaten disliyor.
