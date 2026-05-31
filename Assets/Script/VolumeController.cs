using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// プレイヤーがBGM・SEの音量をスライダーで調整するUIコントローラー。
/// スライダー値が0のときはAudioMixer上でミュートします。
/// AudioMixerを直接操作します。
/// </summary>
public class VolumeController : MonoBehaviour
{
    // -------------------------------------------------------
    // インスペクター設定
    // -------------------------------------------------------

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("BGM")]
    [SerializeField] private Slider bgmSlider;

    [Header("SE")]
    [SerializeField] private Slider seSlider;
    [SerializeField] private string seSliderPreviewSeName;

    // -------------------------------------------------------
    // ライフサイクル
    // -------------------------------------------------------

    private void Start()
    {
        SetupSlider(bgmSlider, OnBGMSliderChanged, ApplyBGMVolume);
        SetupSlider(seSlider, OnSESliderChanged, ApplySEVolume);
    }

    private static void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> onChanged, System.Action<float> applyVolume)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.RemoveListener(onChanged);
        slider.onValueChanged.AddListener(onChanged);
        applyVolume(Mathf.Clamp01(slider.value));
    }

    // -------------------------------------------------------
    // スライダー
    // -------------------------------------------------------

    /// <summary>
    /// BGMスライダーの値が変わったときに呼ばれます。
    /// </summary>
    private void OnBGMSliderChanged(float value)
    {
        ApplyBGMVolume(value);
    }

    /// <summary>
    /// SEスライダーの値が変わったときに呼ばれます。
    /// </summary>
    private void OnSESliderChanged(float value)
    {
        ApplySEVolume(value);
        PlaySEPreview(value);
    }

    private void PlaySEPreview(float volume)
    {
        if (volume <= 0f
            || string.IsNullOrWhiteSpace(seSliderPreviewSeName)
            || SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlaySE(seSliderPreviewSeName);
    }

    // -------------------------------------------------------
    // AudioMixer操作
    // -------------------------------------------------------

    /// <summary>
    /// BGMの音量をAudioMixerに反映します。
    /// </summary>
    /// <param name="volume">音量（0〜1）</param>
    private void ApplyBGMVolume(float volume)
    {
        float dB = volume <= 0.0001f ? -80f : Mathf.Clamp(Mathf.Log10(volume) * 20f, -80f, 0f);
        audioMixer.SetFloat("BGMVolume", dB);
    }

    /// <summary>
    /// SEの音量をAudioMixerに反映します。
    /// </summary>
    /// <param name="volume">音量（0〜1）</param>
    private void ApplySEVolume(float volume)
    {
        float dB = volume <= 0.0001f ? -80f : Mathf.Clamp(Mathf.Log10(volume) * 20f, -80f, 0f);
        audioMixer.SetFloat("SEVolume", dB);
    }
}
