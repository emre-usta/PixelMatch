using UnityEngine;
using UnityEngine.EventSystems;

public class PlayButtonJuicer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Idle - Nefes Alma Ayarlari")]
    public bool enableBreathe = true;
    public float breatheSpeed = 2.0f;       // Nefes alma hizi
    public float breatheAmplitude = 0.03f;  // Nefes alma büyüklük orani (0.03 = %3 esneme)

    [Header("Click - Esneme Ayarlari")]
    public float pressScale = 0.90f;        // Basildiginda küçülme orani (%10 küçülme)
    public float releaseScale = 1.08f;      // Birakildiginda firlama/yaylanma orani (%8 büyüme)
    public float lerpSpeed = 15f;           // Yaylanma hizi/sertligi


    public AudioSource clickSound;          // Buton tiklama ses efekti

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isPressed = false;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        if (enableBreathe && !isPressed)
        {
            // Sinüs dalgasi kullanarak bosta durma (breathe) hesaplamasi:
            // Scale = OriginalScale * (1 + Sin(Time * Speed) * Amplitude)
            float breatheEffect = Mathf.Sin(Time.time * breatheSpeed) * breatheAmplitude;
            Vector3 breatheScale = originalScale * (1f + breatheEffect);

            // Mevcut scale degerini yumusatýlmýþ olarak nefes alma boyutuna tasi
            transform.localScale = Vector3.Lerp(transform.localScale, breatheScale, Time.deltaTime * lerpSpeed);
        }
        else
        {
            // Basildiysa veya birakildiysa hedef esneme boyutuna dogru hizla lerp yap
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * lerpSpeed);
        }
    }

    // Oyuncu parmagini butona bastigi an (Pointer Down)
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetScale = originalScale * pressScale; // Butonu içe dogru bük/basitlestir

        if (clickSound != null) clickSound.Play(); // Ses efekti gecikmesiz aninda oynatilir
    }

    // Oyuncu parmagini butondan çektigi an (Pointer Up)
    public void OnPointerUp(PointerEventData eventData)
    {
        // Butonu önce normalden biraz daha büyük boyuta firlatip (spring), ardindan normale döndürüyoruz
        targetScale = originalScale * releaseScale;

        // C# gecikmeli baslatma (Coroutine) ile yayý sönümlendirip orijinal boyuta döndürüyoruz
        StartCoroutine(SettleBackToNormal());
    }

    private System.Collections.IEnumerator SettleBackToNormal()
    {
        yield return new WaitForSeconds(0.08f); // 80 milisaniye firlama boyutunda kal
        targetScale = originalScale;
        yield return new WaitForSeconds(0.12f); // Normale dönerken bekle
        isPressed = false; // Nefes alma animasyonunu tekrar aktif et
    }
}