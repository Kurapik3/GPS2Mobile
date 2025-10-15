using System;
using UnityEngine;
using UnityEngine.Audio;

public class ManagerAudio : MonoBehaviour
{
    public static ManagerAudio instance;

    [SerializeField] private Sound[] musics, sfxs;
    [SerializeField] private AudioSource musicSource, sfxSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyVolumes();
    }

    private void Start()
    {
        PlayMusic("BGM");
    }

    public void PlayMusic(string name)
    {
        Sound s = Array.Find(musics, x => x.name == name);
        if (s == null)
        {
            Debug.LogWarning($"Music '{name}' not found!");
            return;
        }

        musicSource.clip = s.clip;
        musicSource.loop = true;
        ApplyVolumes();
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxs, x => x.name == name);
        if (s == null)
        {
            Debug.LogWarning($"SFX '{name}' not found!");
            return;
        }

        float volumeScale = GetSFXVolume() * GetMasterVolume();
        sfxSource.PlayOneShot(s.clip, volumeScale);
    }

    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat("MasterVolume", Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("SFXVolume", Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public float GetMasterVolume() => PlayerPrefs.GetFloat("MasterVolume", 1f);
    public float GetMusicVolume() => PlayerPrefs.GetFloat("MusicVolume", 1f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat("SFXVolume", 1f);

    private void ApplyVolumes()
    {
        float master = GetMasterVolume();
        float music = GetMusicVolume();
        float sfx = GetSFXVolume();

        if (musicSource != null)
            musicSource.volume = music * master;

        if (sfxSource != null)
            sfxSource.volume = sfx * master; 
    }
}
