using UnityEngine;

public class VoiceControl : MonoBehaviour
{
    private static GameObject instance;
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        if (instance == null)
        {
            instance = gameObject;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
