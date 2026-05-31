using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#pragma warning disable 0649
[ExecuteAlways]
public sealed class TitleScreen : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string inGameSceneName = "InGame";

    [Header("Play Button")]
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text playButtonLabel;
    [SerializeField] private Sprite playButtonSprite;
    [SerializeField] private Sprite playButtonHoverSprite;
    [SerializeField] private Sprite playButtonPressedSprite;
    [SerializeField] private string buttonClickSeName;

    [Header("Credit Panel")]
    [SerializeField] private Button creditButton;
    [SerializeField] private GameObject creditPanel;
    [SerializeField] private Button creditCloseButton;
    [SerializeField] private Button creditLinkButton1;
    [SerializeField] private string creditLinkUrl1;
    [SerializeField] private Button creditLinkButton2;
    [SerializeField] private string creditLinkUrl2;
    [SerializeField] private Button creditLinkButton3;
    [SerializeField] private string creditLinkUrl3;

    [Header("Maru Mask")]
    [SerializeField] private Image maruMaskImage;
    [SerializeField] private bool showMaruMaskImage;

    [Header("Text")]
    [SerializeField] private string playButtonText = "PLAY";
    [SerializeField] private TMP_FontAsset playButtonFont;
    [SerializeField] private Color playButtonTextColor = new Color(0.05f, 0.02f, 0.85f);
    [SerializeField] private int playButtonFontSize = 52;

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureEventSystem();
        EnsurePlayButton();
        ConfigurePlayButton();
        ResolveCreditReferences();
        ConfigureCreditCallbacks();
        SetCreditPanelVisible(false);
        EnsureMaruMask();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            return;
        }

        EnsureEventSystem();
        EnsurePlayButton();
        ConfigurePlayButton();
        ResolveCreditReferences();
        EnsureMaruMask();
    }

    private void OnValidate()
    {
        ConfigurePlayButton();
        ConfigureMaruMask();
    }

    public void Play()
    {
        PlaySe(buttonClickSeName);
        SceneManager.LoadScene(inGameSceneName);
    }

    public void OpenCreditWindow()
    {
        PlaySe(buttonClickSeName);
        ResolveCreditReferences();
        SetCreditPanelVisible(true);
    }

    public void CloseCreditWindow()
    {
        PlaySe(buttonClickSeName);
        SetCreditPanelVisible(false);
    }

    private void EnsurePlayButton()
    {
        if (playButton != null)
        {
            return;
        }

        Canvas canvas = GetOrCreateCanvas();
        playButton = CreateButton("PlayButton", canvas.transform);
    }

    private Canvas GetOrCreateCanvas()
    {
        Canvas canvas = playButton != null ? playButton.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            return canvas;
        }

        var canvasObject = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private void ConfigurePlayButton()
    {
        if (playButton == null)
        {
            return;
        }

        Image image = playButton.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = playButtonSprite;
            image.type = playButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = playButtonSprite != null ? Color.white : new Color(0.18f, 0.9f, 1f, 0.92f);
        }

        ApplyButtonSprites(playButton, playButtonSprite, playButtonHoverSprite, playButtonPressedSprite);
        if (Application.isPlaying)
        {
            playButton.onClick.RemoveListener(Play);
            playButton.onClick.AddListener(Play);
        }

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

    private void ResolveCreditReferences()
    {
        creditButton = creditButton != null ? creditButton : FindSceneComponent<Button>("CreditButton");
        creditPanel = creditPanel != null ? creditPanel : FindSceneGameObject("CreditPanel");
        creditCloseButton = creditCloseButton != null ? creditCloseButton : FindSceneComponent<Button>("CreditCloseButton");
        creditLinkButton1 = creditLinkButton1 != null ? creditLinkButton1 : FindSceneComponent<Button>("CreditLinkButton1");
        creditLinkButton2 = creditLinkButton2 != null ? creditLinkButton2 : FindSceneComponent<Button>("CreditLinkButton2");
        creditLinkButton3 = creditLinkButton3 != null ? creditLinkButton3 : FindSceneComponent<Button>("CreditLinkButton3");
    }

    private void ConfigureCreditCallbacks()
    {
        if (creditButton != null)
        {
            creditButton.onClick.RemoveListener(OpenCreditWindow);
            creditButton.onClick.AddListener(OpenCreditWindow);
        }

        if (creditCloseButton != null)
        {
            creditCloseButton.onClick.RemoveListener(CloseCreditWindow);
            creditCloseButton.onClick.AddListener(CloseCreditWindow);
        }

        ConfigureCreditLinkButton(creditLinkButton1, creditLinkUrl1);
        ConfigureCreditLinkButton(creditLinkButton2, creditLinkUrl2);
        ConfigureCreditLinkButton(creditLinkButton3, creditLinkUrl3);
    }

    private void ConfigureCreditLinkButton(Button button, string url)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OpenCreditLink(url));
    }

    private void OpenCreditLink(string url)
    {
        PlaySe(buttonClickSeName);
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        Application.OpenURL(url);
    }

    private void SetCreditPanelVisible(bool visible)
    {
        if (creditPanel != null)
        {
            creditPanel.SetActive(visible);
        }
    }

    private void EnsureMaruMask()
    {
        if (maruMaskImage == null)
        {
            GameObject maruObject = FindSceneGameObject("MaruImage");
            if (maruObject != null)
            {
                maruMaskImage = maruObject.GetComponent<Image>();
            }
        }

        if (maruMaskImage == null)
        {
            return;
        }

        Mask mask = maruMaskImage.GetComponent<Mask>();
        if (mask == null)
        {
            mask = maruMaskImage.gameObject.AddComponent<Mask>();
        }

        mask.showMaskGraphic = showMaruMaskImage;
    }

    private void ConfigureMaruMask()
    {
        if (maruMaskImage == null)
        {
            return;
        }

        Mask mask = maruMaskImage.GetComponent<Mask>();
        if (mask != null)
        {
            mask.showMaskGraphic = showMaruMaskImage;
        }
    }

    private Button CreateButton(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420f, 120f);
        rect.anchoredPosition = new Vector2(0f, -300f);

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

    private static GameObject FindSceneGameObject(string objectName)
    {
        Transform transform = FindSceneComponent<Transform>(objectName);
        return transform != null ? transform.gameObject : null;
    }

    private static T FindSceneComponent<T>(string objectName) where T : Component
    {
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        foreach (T component in components)
        {
            if (component == null
                || component.gameObject == null
                || component.gameObject.name != objectName
                || !component.gameObject.scene.IsValid())
            {
                continue;
            }

            return component;
        }

        return null;
    }

    private static void PlaySe(string seName)
    {
        if (string.IsNullOrWhiteSpace(seName) || SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlaySE(seName);
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
