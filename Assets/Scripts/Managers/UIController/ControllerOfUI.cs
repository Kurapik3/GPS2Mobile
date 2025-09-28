using UnityEngine;
using UnityEngine.UI;

public class ControllerOfUI : MonoBehaviour
{
    [SerializeField] private Slider musicSlider, sfxSlider, masterSlider;

    private void Start()
    {
        if (ManagerAudio.instance == null) return;

        if (masterSlider != null)
        {
            masterSlider.value = ManagerAudio.instance.GetMasterVolume();
            masterSlider.onValueChanged.AddListener(ManagerAudio.instance.SetMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.value = ManagerAudio.instance.GetMusicVolume();
            musicSlider.onValueChanged.AddListener(ManagerAudio.instance.SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = ManagerAudio.instance.GetSFXVolume();
            sfxSlider.onValueChanged.AddListener(ManagerAudio.instance.SetSFXVolume);
        }
    }
}
    
