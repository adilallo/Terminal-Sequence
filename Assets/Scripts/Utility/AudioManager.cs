using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [Header("Fade settings")]
    [SerializeField] private float fadeDuration = 2f;

    [Header("Runtime info (debug)")]
    [SerializeField] private AudioSource sourceA;
    [SerializeField] private AudioSource sourceB;

    private AudioSource _active;
    private AudioSource _incoming; 

    private readonly Queue<(List<AudioClip> playlist, bool fadeOut)> playQueue = new();
    private List<AudioClip> currentPlaylist = new();
    private int trackIndex;
    private bool fadeOutAtEnd;
    private Coroutine playlistRoutine;

    public event Action OnPlaylistFinished;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // create two AudioSources for overlap cross‑fades
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        sourceA.outputAudioMixerGroup = sourceB.outputAudioMixerGroup = musicMixerGroup;
        sourceA.playOnAwake = sourceB.playOnAwake = false;
        sourceA.loop = sourceB.loop = false;

        _active = sourceA;
        _incoming = sourceB;
    }

    void OnDisable()
    {
        if (playlistRoutine != null) StopCoroutine(playlistRoutine);
        OnPlaylistFinished = null; // clear invocation list
    }

    public void PlayPlaylist(List<AudioClip> playlist, bool fadeOut = false)
    {
        if (playlist == null || playlist.Count == 0) return;
        StartPlaylist(playlist, fadeOut);
    }

    private void StartPlaylist(List<AudioClip> playlist, bool fadeOut)
    {
        if (playlistRoutine != null) StopCoroutine(playlistRoutine);
        currentPlaylist = playlist;
        trackIndex = 0;
        fadeOutAtEnd = fadeOut;
        playlistRoutine = StartCoroutine(PlaylistLoop());
    }

    IEnumerator PlaylistLoop()
    {
        while (true)
        {
            if (currentPlaylist.Count == 0) yield break;
            AudioClip clip = currentPlaylist[trackIndex];
            yield return CrossFadeToClip(clip);

            yield return new WaitForSecondsRealtime(clip.length - fadeDuration); // start next fade slightly before end

            trackIndex++;
            if (trackIndex >= currentPlaylist.Count)
            {
                if (fadeOutAtEnd)
                {
                    yield return FadeOut(_active);
                    OnPlaylistFinished?.Invoke();
                    yield break;
                }
                trackIndex = 0;
            }
        }
    }

    IEnumerator CrossFadeToClip(AudioClip newClip)
    {
        // swap roles
        (_active, _incoming) = (_incoming, _active);

        _incoming.clip = newClip;
        _incoming.volume = 0f;
        _incoming.Play();

        float t = 0f;
        while (t < fadeDuration)
        {
            float step = Time.unscaledDeltaTime / fadeDuration;
            _incoming.volume = Mathf.MoveTowards(_incoming.volume, 1f, step);
            _active.volume = Mathf.MoveTowards(_active.volume, 0f, step);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        _incoming.volume = 1f;
        _active.Stop();
    }

    IEnumerator FadeOut(AudioSource src)
    {
        while (src.volume > 0f)
        {
            src.volume = Mathf.MoveTowards(src.volume, 0f, Time.unscaledDeltaTime / fadeDuration);
            yield return null;
        }
        src.Stop();
        src.volume = 1f;
    }

    // Public helpers
    public void Pause() { sourceA.Pause(); sourceB.Pause(); }
    public void Resume() { sourceA.UnPause(); sourceB.UnPause(); }
    public void Skip() { if (playlistRoutine != null) StopCoroutine(playlistRoutine); StartPlaylist(currentPlaylist, fadeOutAtEnd); }
}
