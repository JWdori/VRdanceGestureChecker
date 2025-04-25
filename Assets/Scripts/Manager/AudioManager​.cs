// AudioManager.cs

using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton AudioManager that handles background music with fade in/out loops
/// and plays one-shot sound effects on demand.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM Settings")]
    [SerializeField] private AudioClip backgroundMusic;
    [Tooltip("Volume of the background music (0 to 1).")]
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Tooltip("Time in seconds for fade in/out transitions.")]
    [SerializeField] private float fadeDuration = 1f;
    [Tooltip("Delay between loops after fade out.")]
    [SerializeField] private float delayBetweenLoops = 0.5f;

    [Header("SFX Settings")]
    [Tooltip("Global volume for sound effects (0 to 1).")]
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Success SFX")]
    [Tooltip("Sound for a good success.")]
    [SerializeField] private AudioClip goodSoundClip;
    [Tooltip("Sound for an excellent success.")]
    [SerializeField] private AudioClip excellentSoundClip;
    [SerializeField] private AudioClip fail;


    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    private void Awake()
    {
        // Singleton 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // BGM / SFX용 AudioSource 생성
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = false;
        _bgmSource.volume = 0f;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.loop = false;
        _sfxSource.volume = sfxVolume;
    }

    private void Start()
    {
        if (backgroundMusic != null)
            StartCoroutine(PlayBGM());
        else
            Debug.LogWarning("AudioManager: No backgroundMusic assigned.");
    }

    /// <summary>
    /// BGM을 페이드 인/아웃 + 딜레이를 두고 루프 재생
    /// </summary>
    private IEnumerator PlayBGM()
    {
        while (true)
        {
            _bgmSource.clip = backgroundMusic;

            // Fade in
            yield return StartCoroutine(FadeAudio(_bgmSource, 0f, bgmVolume, fadeDuration));
            _bgmSource.Play();

            // 트랙 끝 직전까지 대기
            yield return new WaitForSeconds(backgroundMusic.length - fadeDuration);

            // Fade out
            yield return StartCoroutine(FadeAudio(_bgmSource, bgmVolume, 0f, fadeDuration));
            _bgmSource.Stop();

            // 루프 전 딜레이
            yield return new WaitForSeconds(delayBetweenLoops);
        }
    }

    /// <summary>
    /// 주어진 AudioSource 볼륨을 start→end로 duration 동안 페이드
    /// </summary>
    private IEnumerator FadeAudio(AudioSource source, float start, float end, float duration)
    {
        float elapsed = 0f;
        source.volume = start;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        source.volume = end;
    }

    /// <summary>
    /// 지정된 AudioClip을 사운드 이펙트로 한 번 재생
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: PlaySound called with null clip.");
            return;
        }

        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>
    /// Play the 'Good' success sound.
    /// </summary>
    public void PlayGoodSound()
    {
        PlaySound(goodSoundClip);
    }

    /// <summary>
    /// Play the 'Excellent' success sound.
    /// </summary>
    public void PlayExcellentSound()
    {
        PlaySound(excellentSoundClip);
    }
        public void FailSound()
    {
        PlaySound(fail);
    }


    /// <summary>
    /// 런타임에 BGM 볼륨 조정
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        _bgmSource.volume = bgmVolume;
    }

    /// <summary>
    /// 런타임에 SFX 볼륨 조정
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        _sfxSource.volume = sfxVolume;
    }
}
