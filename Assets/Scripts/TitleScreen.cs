using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#pragma warning disable 0649
public sealed class TitleScreen : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string inGameSceneName = "InGame";

    [Header("Button")]
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text playButtonLabel;
    [SerializeField] private Sprite playButtonSprite;
    [SerializeField] private Sprite playButtonHoverSprite;
    [SerializeField] private Sprite playButtonPressedSprite;
    [SerializeField] private Vector2 playButtonSize = new Vector2(420f, 120f);
    [SerializeField] private Vector2 playButtonPosition = new Vector2(0f, -300f);
    [SerializeField] private string buttonClickSeName;

    [Header("Text")]
    [SerializeField] private string playButtonText = "PLAY";
    [SerializeField] private TMP_FontAsset playButtonFont;
    [SerializeField] private Color playButtonTextColor = new Color(0.05f, 0.02f, 0.85f);
    [SerializeField] private int playButtonFontSize = 52;

    private void Awake()
    {
        EnsureEventSystem();
        if (playButton == null)
        {
            BuildUi();
        }

        ConfigurePlayButton();
    }

    public void Play()
    {
        PlaySe(buttonClickSeName);
        SceneManager.LoadScene(inGameSceneName);
    }

    private void OnValidate()
    {
        ConfigurePlayButton();
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        playButton = CreateButton("PlayButton", canvasObject.transform);
    }

    private void ConfigurePlayButton()
    {
        if (playButton == null)
        {
            return;
        }

        RectTransform rect = playButton.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = playButtonSize;
            rect.anchoredPosition = playButtonPosition;
        }

        Image image = playButton.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = playButtonSprite;
            image.type = playButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = playButtonSprite != null ? Color.white : new Color(0.18f, 0.9f, 1f, 0.92f);
        }

        ApplyButtonSprites(playButton, playButtonSprite, playButtonHoverSprite, playButtonPressedSprite);
        playButton.onClick.RemoveListener(Play);
        playButton.onClick.AddListener(Play);

        if (playButtonLabel == null)
        {
            playButtonLabel = playButton.GetComponentInChildren<TMP_Text>();
        }

        if (playButtonLabel != null)
        {
            playButtonLabel.text = playButtonText;
            playButtonLabel.font = playButtonFont != null ? playButtonFont : playButtonLabel.font;
            playButtonLabel.fontSize = playButtonFontSize;
            playButtonLabel.fontStyle = FontStyles.Bold;
            playButtonLabel.alignment = TextAlignmentOptions.Center;
            playButtonLabel.textWrappingMode = TextWrappingModes.NoWrap;
            playButtonLabel.overflowMode = TextOverflowModes.Overflow;
            playButtonLabel.color = playButtonTextColor;
            playButtonLabel.raycastTarget = false;
        }
    }

    private Button CreateButton(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.sizeDelta = playButtonSize;
        rect.anchoredPosition = playButtonPosition;

        Button button = gameObject.GetComponent<Button>();

        TMP_Text label = CreateLabel(gameObject.transform);
        label.text = playButtonText;
        playButtonLabel = label;
        return button;
    }

    private static void ApplyButtonSprites(Button button, Sprite normalSprite, Sprite hoverSprite, Sprite pressedSprite)
    {
        if (button == null)
        {
            return;
        }

        if (normalSprite != null || hoverSprite != null || pressedSprite != null)
        {
            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = hoverSprite != null ? hoverSprite : normalSprite;
            spriteState.selectedSprite = spriteState.highlightedSprite;
            spriteState.pressedSprite = pressedSprite != null ? pressedSprite : spriteState.highlightedSprite;
            button.spriteState = spriteState;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            button.colors = colors;
        }
    }

    private static void PlaySe(string seName)
    {
        if (string.IsNullOrWhiteSpace(seName) || SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlaySE(seName);
    }

    private TMP_Text CreateLabel(Transform parent)
    {
        var gameObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.font = playButtonFont != null ? playButtonFont : text.font;
        text.fontSize = playButtonFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = playButtonTextColor;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
