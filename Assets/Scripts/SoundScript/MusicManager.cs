using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField]
    private MusicLibrary musicLibrary;

    [SerializeField]
    private AudioSource musicSource;

    [Header("Boss Music")]
    [SerializeField]
    private string phaseTwoTrackName = "PhraseTwo";

    private BossHealth bossHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        BindToBoss();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (bossHealth != null)
        {
            bossHealth.OnEnraged -= PlayPhaseTwoMusic;
        }
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        BindToBoss();
    }

    private void BindToBoss()
    {
        if (bossHealth != null)
        {
            bossHealth.OnEnraged -= PlayPhaseTwoMusic;
            bossHealth = null;
        }

        bossHealth = FindAnyObjectByType<BossHealth>();

        if (bossHealth != null)
        {
            bossHealth.OnEnraged += PlayPhaseTwoMusic;
        }
    }

    private void PlayPhaseTwoMusic()
    {
        Debug.Log(
            $"Boss enraged. Switching music to '{phaseTwoTrackName}'.",
            this);

        PlayMusic(phaseTwoTrackName);
    }

    public void PlayMusic(
        string trackName,
        float fadeDuration = 0.5f)
    {
        if (musicLibrary == null || musicSource == null)
        {
            Debug.LogWarning(
                "MusicManager is missing its MusicLibrary or AudioSource.",
                this);

            return;
        }

        AudioClip nextTrack = musicLibrary.GetClipFromName(trackName);

        if (nextTrack == null)
        {
            Debug.LogWarning(
                $"MusicManager: No track found for '{trackName}'.",
                this);

            return;
        }

        Debug.Log(
            $"MusicManager: Playing track '{trackName}'.",
            this);

        StartCoroutine(
            AnimateMusicCrossfade(
                nextTrack,
                fadeDuration));
    }

    private IEnumerator AnimateMusicCrossfade(
        AudioClip nextTrack,
        float fadeDuration)
    {
        float percent = 0f;

        while (percent < 1f)
        {
            percent += Time.deltaTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(1f, 0f, percent);
            yield return null;
        }

        musicSource.clip = nextTrack;
        musicSource.Play();

        percent = 0f;

        while (percent < 1f)
        {
            percent += Time.deltaTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(0f, 1f, percent);
            yield return null;
        }

        musicSource.volume = 1f;
    }
}