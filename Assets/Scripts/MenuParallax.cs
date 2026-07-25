using UnityEngine;

public class MenuParallax : MonoBehaviour
{
    public RectTransform skyLayer;   // Gökyüzü katmaný
    public RectTransform starsLayer; // Yýldýzlar katmaný

    public float skySpeed = 10f;
    public float starsSpeed = 20f;

    void Update()
    {
        Vector3 inputOffset = Vector3.zero;

#if UNITY_EDITOR
        // Fare koordinatlarýný -1 ile 1 arasýna normalize et (Editor Testi)
        inputOffset.x = (Input.mousePosition.x / Screen.width) * 2f - 1f;
        inputOffset.y = (Input.mousePosition.y / Screen.height) * 2f - 1f;
#else
        // Mobilde jiroskop / ivmeölçer verisini oku
        inputOffset.x = Input.acceleration.x;
        inputOffset.y = Input.acceleration.y;
#endif

        // Katmanlarý farklý hýzlarda hafifçe kaydýrarak derinlik hissi yarat
        if (skyLayer != null)
            skyLayer.anchoredPosition = Vector2.Lerp(skyLayer.anchoredPosition, new Vector2(inputOffset.x * skySpeed, inputOffset.y * skySpeed), Time.deltaTime * 5f);

        if (starsLayer != null)
            starsLayer.anchoredPosition = Vector2.Lerp(starsLayer.anchoredPosition, new Vector2(inputOffset.x * starsSpeed, inputOffset.y * starsSpeed), Time.deltaTime * 5f);
    }
}