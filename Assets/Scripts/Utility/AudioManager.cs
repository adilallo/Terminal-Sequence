using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float fadeDuration = 2f;
    private List<AudioClip> currentPlaylist = new List<AudioClip>();
    private int currentTrackIndex = 0;
    private Coroutine playlistCoroutine;
    private bool shouldFadeOutAtEnd = false;

    public event Action OnPlaylistFinished;

    public AudioSource CurrentAudioSource
    {
        get { return audioSource; }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDisable()
    {
        if (OnPlaylistFinished != null)
        {
            Delegate[] invocationList = OnPlaylistFinished.GetInvocationList();
            foreach (Delegate d in invocationList)
            {
                OnPlaylistFinished -= (Action)d;
            }
        }

        StopAllCoroutines();
    }

    public float GetCurrentTrackProgress()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            return audioSource.time / audioSource.clip.length;
        }
        return 0f;
    }
                    
    public void PlayPlaylist(List<AudioClip> playlist, bool fadeOutAtEnd = false)
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }

        currentPlaylist = playlist;
        currentTrackIndex = 0;
        shouldFadeOutAtEnd = fadeOutAtEnd;
        playlistCoroutine = StartCoroutine(PlayAudioTracks());
    }

    private IEnumerator PlayAudioTracks()
    {
        while (true)
        {
            AudioClip currentTrack = currentPlaylist[currentTrackIndex];

            yield return StartCoroutine(CrossfadeAudio(currentTrack));

            yield return new WaitForSeconds(currentTrack.length);

            currentTrackIndex++;

            if (currentTrackIndex >= currentPlaylist.Count)
            {
                if (shouldFadeOutAtEnd)
                {
                    yield return StartCoroutine(FadeOutLastTrack());
                    OnPlaylistFinished?.Invoke();
                    yield break;
                }
                else
                {
                    currentTrackIndex = 0;
                }
            }
        }
    }

    private IEnumerator CrossfadeAudio(AudioClip newClip)
    {
        if (audioSource == null)
            yield break;

        float startVolume = audioSource.volume;

        while (audioSource != null && audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        if (audioSource != null)
        {
            audioSource.clip = newClip;
            audioSource.Play();
            audioSource.volume = 0;

            while (audioSource != null && audioSource.volume < startVolume)
            {
                audioSource.volume += startVolume * Time.deltaTime / fadeDuration;
                yield return null;
            }

            if (audioSource != null)
                audioSource.volume = startVolume;
        }
    }

    private IEnumerator FadeOutLastTrack()
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = 1;
    }

    public void OnSceneChange()
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }
        StartCoroutine(FadeOutCurrentTrack());
    }

    public IEnumerator FadeOutCurrentTrack()
    {
        float startVolume = audioSource.volume;
        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume; // Reset the volume for the next track
    }
}
