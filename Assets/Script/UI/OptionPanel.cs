using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class OptionPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private float defaultSensitivity = 1f;
    [SerializeField] private float defaultBGMVolume = 0.5f;
    [SerializeField] private float defaultSFXVolume = 0.5f;

    private bool initialized = false;

    private CinemachineInputAxisController inputAxis; 

    public bool IsOpen => panel.activeSelf;

    private void Awake()
    {
        if (initialized) return;
        initialized = true;

        panel.SetActive(true);

        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
        sensitivitySlider.onValueChanged.RemoveAllListeners();

        sensitivitySlider.value = defaultSensitivity;
        bgmSlider.value = defaultBGMVolume;
        sfxSlider.value = defaultSFXVolume;

        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    private void Start()
    {
        inputAxis = FindAnyObjectByType<CinemachineInputAxisController>();
        AudioManager.Instance.SetBGMVolume(defaultBGMVolume);
        AudioManager.Instance.SetSFXVolume(defaultSFXVolume);
    }

    public void Open()
    {
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    public void OnSensitivityChanged(float value)
    {
        if (inputAxis == null) return;
        foreach (var controller in inputAxis.Controllers)
        {
            if (controller.Name.Contains("Y"))
                controller.Input.Gain = -value;
            else
                controller.Input.Gain = value;
        }
    }

    public void OnBGMVolumeChanged(float value)
    {
        AudioManager.Instance.SetBGMVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    public void OnMuteToggle(bool isMuted)
    {
        if (isMuted)
        {
            AudioManager.Instance.SetBGMVolume(0);
            AudioManager.Instance.SetSFXVolume(0);
        }
        else if (!isMuted)
        {
            AudioManager.Instance.SetBGMVolume(bgmSlider.value);
            AudioManager.Instance.SetSFXVolume(sfxSlider.value);
        }
    }
}