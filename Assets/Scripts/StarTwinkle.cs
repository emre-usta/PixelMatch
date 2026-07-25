using UnityEngine;
using UnityEngine.UI;

public class StarTwinkle : MonoBehaviour
{
    private Image starImage;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1.0f;
    public float speed = 1.5f;

    void Start()
    {
        starImage = GetComponent<Image>();
    }

    void Update()
    {
        if (starImage != null)
        {
            // Sinüs dalgasý kullanarak opaklýðý (Alpha) yumuþak bir þekilde dalgalandýr
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * speed) + 1f) / 2f);
            Color tempColor = starImage.color;
            tempColor.a = alpha;
            starImage.color = tempColor;
        }
    }
}