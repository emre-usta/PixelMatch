using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Kart Sesleri")]
    [SerializeField] private AudioClip[] sfxCardFlipVariants;

    [Header("Eşleşme Sesleri")]
    [SerializeField] private AudioClip[] sfxMatchVariants;
    [SerializeField] private AudioClip sfxCombo;

    [Header("Yanlış Eşleşme Sesleri")]
    [SerializeField] private AudioClip[] sfxMismatchVariants;

    [Header("Özel Sesler")]
    [SerializeField] private AudioClip sfxLevelComplete;
    [SerializeField] private AudioClip sfxTimeThief;
    [SerializeField] private AudioClip sfxFreeze;
    [SerializeField] private AudioClip sfxPowerUp;

    [Header("Arka Plan Müziği")]
    [SerializeField] private AudioClip bgmGame;
    [SerializeField] private AudioClip bgmMenu;

    [Header("Ses Seviyeleri")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.5f;

    private AudioSource sfxSource;
    private AudioSource bgmSource;

    // ─── COMBO SAYACI ─────────────────────────────────────────────
    private int comboCount = 0;
    private float comboResetTime = 3f;
    private float lastMatchTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetupAudioSources();

        bgmVolume = PlayerPrefs.GetFloat("settings_bgm", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("settings_sfx", 1f);
    }

    private void Update()
    {
        if (comboCount > 0 && Time.time - lastMatchTime > comboResetTime)
            comboCount = 0;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnsubscribeFromEvents();
        SubscribeToEvents();
        comboCount = 0;

        if (scene.name == "MainMenu")
            PlayBGM(bgmMenu);
        else if (scene.name == "Level1")
            PlayBGM(bgmGame);
    }

    private void SubscribeToEvents()
    {
        GameEvents.OnCardRevealed += HandleCardRevealed;
        GameEvents.OnPairMatched += HandlePairMatched;
        GameEvents.OnPairMismatch += HandlePairMismatch;
        GameEvents.OnGameStarted += HandleGameStarted;
        GameEvents.OnGameWon += HandleGameWon;
        GameEvents.OnEffectTriggered += HandleEffectTriggered;
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnCardRevealed -= HandleCardRevealed;
        GameEvents.OnPairMatched -= HandlePairMatched;
        GameEvents.OnPairMismatch -= HandlePairMismatch;
        GameEvents.OnGameStarted -= HandleGameStarted;
        GameEvents.OnGameWon -= HandleGameWon;
        GameEvents.OnEffectTriggered -= HandleEffectTriggered;
    }

    private void SetupAudioSources()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
    }

    // ─── EVENT HANDLERS ───────────────────────────────────────────

    private void HandleCardRevealed(CardController card)
    {
        if (sfxCardFlipVariants != null && sfxCardFlipVariants.Length > 0)
            PlaySFX(sfxCardFlipVariants[Random.Range(0, sfxCardFlipVariants.Length)]);
    }

    private void HandlePairMatched(CardController a, CardController b)
    {
        lastMatchTime = Time.time;
        comboCount++;

        if (comboCount >= 3)
        {
            PlaySFX(sfxCombo);

            if (comboCount == 3)
                ComboEffectManager.Instance?.TriggerComboFlash(comboCount);
            else
                ComboEffectManager.Instance?.TriggerComboText(comboCount);
        }
        else
        {
            if (sfxMatchVariants != null && sfxMatchVariants.Length > 0)
                PlaySFX(sfxMatchVariants[Random.Range(0, sfxMatchVariants.Length)]);
        }
    }

    private void HandlePairMismatch(CardController a, CardController b)
    {
        comboCount = 0;

        if (sfxMismatchVariants != null && sfxMismatchVariants.Length > 0)
            PlaySFX(sfxMismatchVariants[Random.Range(0, sfxMismatchVariants.Length)]);
    }

    private void HandleGameStarted()
    {
        PlayBGM(bgmGame);
    }

    private void HandleGameWon()
    {
        PlaySFX(sfxLevelComplete);
    }

    private void HandleEffectTriggered(CardEffectType effectType)
    {
        switch (effectType)
        {
            case CardEffectType.TimeThief:
                PlaySFX(sfxTimeThief);
                break;
        }
    }

    // ─── PUBLIC METOTLAR ──────────────────────────────────────────

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayFreezeSFX() => PlaySFX(sfxFreeze);
    public void PlayPowerUpSFX() => PlaySFX(sfxPowerUp);

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

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

    public void PlayMenuMusic() => PlayBGM(bgmMenu);
    public void PlayGameMusic() => PlayBGM(bgmGame);
}