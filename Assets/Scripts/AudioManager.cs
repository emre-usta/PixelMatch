using UnityEngine;

/// <summary>
/// PixelMatch — Ses Yöneticisi
/// Tüm ses efektlerini ve müziği yönetir.
/// DontDestroyOnLoad ile sahneler arası yaşar.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─── INSPECTOR AYARLARI ───────────────────────────────────────

    [Header("Ses Efektleri")]
    [SerializeField] private AudioClip sfxMatch;        // sfx_match
    [SerializeField] private AudioClip sfxMismatch;     // sfx_mismatch
    [SerializeField] private AudioClip sfxCardFlip;     // sfx_cardflip

    [Header("Arka Plan Müziği")]
    [SerializeField] private AudioClip bgmGame;         // Oyun sahnesi müziği
    [SerializeField] private AudioClip bgmMenu;         // Ana menü müziği

    [Header("Ses Seviyeleri")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.5f;

    // ─── AUDIO SOURCES ────────────────────────────────────────────

    private AudioSource sfxSource;   // Efekt sesleri için
    private AudioSource bgmSource;   // Müzik için (loop)

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────

    private void Awake()
    {
        // Singleton + DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
    }

    private void OnEnable()
    {
        GameEvents.OnCardRevealed += HandleCardRevealed;
        GameEvents.OnPairMatched += HandlePairMatched;
        GameEvents.OnPairMismatch += HandlePairMismatch;
        GameEvents.OnGameStarted += HandleGameStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnCardRevealed -= HandleCardRevealed;
        GameEvents.OnPairMatched -= HandlePairMatched;
        GameEvents.OnPairMismatch -= HandlePairMismatch;
        GameEvents.OnGameStarted -= HandleGameStarted;
    }

    // ─── KURULUM ──────────────────────────────────────────────────

    private void SetupAudioSources()
    {
        // SFX source — kısa efekt sesleri
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;

        // BGM source — arka plan müziği
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
    }

    // ─── EVENT HANDLERS ───────────────────────────────────────────

    private void HandleCardRevealed(CardController card)
    {
        PlaySFX(sfxCardFlip);
    }

    private void HandlePairMatched(CardController a, CardController b)
    {
        PlaySFX(sfxMatch);
    }

    private void HandlePairMismatch(CardController a, CardController b)
    {
        PlaySFX(sfxMismatch);
    }

    private void HandleGameStarted()
    {
        PlayBGM(bgmGame);
    }

    // ─── PUBLIC METOTLAR ──────────────────────────────────────────

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }

    public void PlayMenuMusic()
    {
        PlayBGM(bgmMenu);
    }

    public void PlayGameMusic()
    {
        PlayBGM(bgmGame);
    }
}