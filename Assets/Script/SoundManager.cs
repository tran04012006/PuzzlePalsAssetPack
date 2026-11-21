using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    private AudioSource musicSource;
    private AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip musicBG;
    public AudioClip run;
    public AudioClip throwBlock;
    public AudioClip win;
    public AudioClip blockExplode;
    public AudioClip receive;

    private bool isRunning = false;
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        //su dung 2 audio source khac nhau
        AudioSource[] sources = GetComponents<AudioSource>();

        musicSource = sources[0];
        sfxSource = sources[1];
    }

    private void Start()
    {
        // Background Music
        musicSource.clip = musicBG;
        musicSource.loop = true;
        musicSource.Play();

    }
    

    public void PlayRun()
    {
        if (!isGameOver)
            sfxSource.PlayOneShot(run);
    }
    public void PlayReceive()
    {
        if (!isGameOver)
            sfxSource.PlayOneShot(receive);
    }
    public void PlayThrowBlock()
    {
        if (!isGameOver)
            sfxSource.PlayOneShot(throwBlock);
    }

    public void PlayBlockExplode()
    {
        if (!isGameOver)
            sfxSource.PlayOneShot(blockExplode);
    }

    public void PlayWin()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        musicSource.Stop();

        sfxSource.PlayOneShot(win);
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }
}