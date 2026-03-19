using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WinPanelController : MonoBehaviour
{
    [Header("Yýldýzlar")]
    [SerializeField] private Image star1;
    [SerializeField] private Image star2;
    [SerializeField] private Image star3;

    [SerializeField] private Color starActiveColor = new Color(0.98f, 0.78f, 0.46f);
    [SerializeField] private Color starInactiveColor = new Color(0.16f, 0.13f, 0.06f);

    public void ShowResult(int stars)
    {
        if (star1) star1.color = starInactiveColor;
        if (star2) star2.color = starInactiveColor;
        if (star3) star3.color = starInactiveColor;
        StartCoroutine(AnimateStars(stars));
    }

    private IEnumerator AnimateStars(int stars)
    {
        yield return new WaitForSecondsRealtime(0.4f);

        if (stars >= 1 && star1)
        {
            star1.color = starActiveColor;
            StartCoroutine(ScalePop(star1.transform));
            yield return new WaitForSecondsRealtime(0.4f);
        }
        if (stars >= 2 && star2)
        {
            star2.color = starActiveColor;
            StartCoroutine(ScalePop(star2.transform));
            yield return new WaitForSecondsRealtime(0.4f);
        }
        if (stars >= 3 && star3)
        {
            star3.color = starActiveColor;
            StartCoroutine(ScalePop(star3.transform));
        }
    }

    private IEnumerator ScalePop(Transform t)
    {
        float duration = 0.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1f, 1.4f, elapsed / duration);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, elapsed / duration);
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}