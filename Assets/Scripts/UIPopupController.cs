using System.Collections;
using UnityEngine;

public class UIPopupController : MonoBehaviour
{

    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Eðri 0'dan 1'e gitmelidir


    public float animationDuration = 0.25f;
    public AudioSource openSound;

    private Coroutine activeScaleCoroutine;

    public void OpenPopup()
    {
        if (activeScaleCoroutine != null) StopCoroutine(activeScaleCoroutine);

        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;

        activeScaleCoroutine = StartCoroutine(ScaleOverTime(Vector3.zero, Vector3.one, openCurve));

        if (openSound != null) openSound.Play();
    }

    public void ClosePopup()
    {
        if (activeScaleCoroutine != null) StopCoroutine(activeScaleCoroutine);

        // startScale = Vector3.one (1), endScale = Vector3.zero (0) yapýyoruz.
        // closeCurve eðrisi 0'dan 1'e doðru yükseldikçe, ölçek 1'den 0'a doðru küçülecektir.
        activeScaleCoroutine = StartCoroutine(ScaleOverTime(
            Vector3.one,
            Vector3.zero,
            closeCurve,
            () => gameObject.SetActive(false)
        ));
    }

    private IEnumerator ScaleOverTime(Vector3 startScale, Vector3 endScale, AnimationCurve curve, System.Action onComplete = null)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            // curve.Evaluate(t) deðeri 0 ile 1 arasýnda artarak interpolasyonu saðlar
            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, curve.Evaluate(t));
            yield return null;
        }

        transform.localScale = endScale;
        onComplete?.Invoke();
    }
}