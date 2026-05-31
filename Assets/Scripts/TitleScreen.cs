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
    [System.Serializable]
    private sealed class CreditLinkButtonSettings
    {
        public string url;
        public Sprite normalSprite;
        public Sprite hoverSprite;
        public Sprite pressedSprite;
        public Vector2 size = new Vector2(260f, 82f);
        public Vector2 position;
    }

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

    [Header("Credit")]
    [SerializeField] private Button creditButton;
    [SerializeField] private Sprite creditWindowSprite;
    [SerializeField] private Vector2 creditWindowSize = new Vector2(1120f, 760f);
    [SerializeField] private Sprite closeButtonSprite;
    [SerializeField] private Sprite closeButtonHoverSprite;
    [SerializeField] private Sprite closeButtonPressedSprite;
    [SerializeField] private Vector2 closeButtonSize = new Vector2(160f, 72f);
    [SerializeField] private Vector2 closeButtonPosition = new Vector2(480f, 320f);
    [SerializeField] private CreditLinkButtonSettings[] creditLinkButtons =
    {
        new CreditLinkButtonSettings { position = new Vector2(-300f, -300f) },
        new CreditLinkButtonSettings { position = new Vector2(0f, -300f) },
        new CreditLinkButtonSettings { position = new Vector2(300f, -300f) }
    };

    [Header("Maru Mask")]
    [SerializeField] private Image maruMaskImage;
    [SerializeField] private bool showMaruMaskImage;

    [Header("Text")]
    [SerializeField] private string playButtonText = "PLAY";
    [SerializeField] private TMP_FontAsset playButtonFont;
    [SerializeField] private Color playButtonTextColor = new Color(0.05f, 0.02f, 0.85f);
    [SerializeField] private int playButtonFontSize = 52;

    private RectTransform creditPanelRoot;
    private Image creditInputBlocker;
    private Image creditWindowImage;
    private Button closeButton;
    private Button[] creditLinkButtonInstances;

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureEventSystem();
        EnsurePlayButton();
        ConfigurePlayButton();
        ConfigureCreditButton();
        EnsureCreditPanel();
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
        EnsureCreditPanel();
        EnsureMaruMask();
    }

    public void Play()
    {
        PlaySe(buttonClickSeName);
        SceneManager.LoadScene(inGameSceneName);
    }

    public void OpenCreditWindow()
    {
        PlaySe(buttonClickSeName);
        EnsureCreditPanel();
        if (creditPanelRoot != null)
        {
            creditPanelRoot.gameObject.SetActive(true);
        }
    }

    public void CloseCreditWindow()
    {
        PlaySe(buttonClickSeName);
        if (creditPanelRoot != null)
        {
            creditPanelRoot.gameObject.SetActive(false);
        }
    }

    private void OnValidate()
    {
        ConfigurePlayButton();
        ConfigureCreditPanel();
        ConfigureMaruMask();
    }

    private void EnsurePlayButton()
    {
        if (playButton != null)
        {
            return;
        }

        BuildUi();
    }

    private void BuildUi()
    {
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

    private void ConfigureCreditButton()
    {
        if (creditButton == null)
        {
            GameObject creditButtonObject = GameObject.Find("CreditButton");
            if (creditButtonObject != null)
            {
                creditButton = creditButtonObject.GetComponent<Button>();
            }
        }

        if (creditButton == null || !Application.isPlaying)
        {
            return;
        }

        creditButton.onClick.RemoveListener(OpenCreditWindow);
        creditButton.onClick.AddListener(OpenCreditWindow);
    }

    private void EnsureMaruMask()
    {
        if (maruMaskImage == null)
        {
            GameObject maruObject = GameObject.Find("MaruImage");
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

        ConfigureMaruMask();
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

        // Mask children are authored directly in the Unity hierarchy.
    }

    private void EnsureCreditPanel()
    {
        if (creditPanelRoot != null)
        {
            ConfigureCreditPanel();
            return;
        }

        Canvas canvas = GetOrCreateCanvas();
        GameObject rootObject = GameObject.Find("CreditPanel");
        if (rootObject == null)
        {
            rootObject = new GameObject("CreditPanel", typeof(RectTransform));
            rootObject.transform.SetParent(canvas.transform, false);
        }

        creditPanelRoot = rootObject.GetComponent<RectTransform>();
        creditPanelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        creditPanelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        creditPanelRoot.pivot = new Vector2(0.5f, 0.5f);
        creditPanelRoot.anchoredPosition = Vector2.zero;

        creditInputBlocker = GetOrCreateImage("CreditInputBlocker", creditPanelRoot);
        creditInputBlocker.color = Color.clear;
        creditInputBlocker.raycastTarget = true;
        Stretch(creditInputBlocker.rectTransform);

        creditWindowImage = GetOrCreateImage("CreditWindowImage", creditPanelRoot);
        SetupCenteredRect(creditWindowImage.rectTransform, creditWindowSize, Vector2.zero);

        closeButton = GetOrCreateImageButton("CreditCloseButton", creditPanelRoot);
        if (Application.isPlaying)
        {
            closeButton.onClick.RemoveListener(CloseCreditWindow);
            closeButton.onClick.AddListener(CloseCreditWindow);
        }

        int linkCount = Mathf.Min(3, creditLinkButtons != null ? creditLinkButtons.Length : 0);
        creditLinkButtonInstances = new Button[linkCount];
        for (int i = 0; i < linkCount; i++)
        {
            int index = i;
            Button linkButton = GetOrCreateImageButton("CreditLinkButton" + (i + 1), creditPanelRoot);
            if (Application.isPlaying)
            {
                linkButton.onClick.RemoveAllListeners();
                linkButton.onClick.AddListener(() => OpenCreditLink(index));
            }

            creditLinkButtonInstances[i] = linkButton;
        }

        ConfigureCreditPanel();
        creditPanelRoot.gameObject.SetActive(false);
    }

    private void ConfigureCreditPanel()
    {
        if (creditPanelRoot == null)
        {
            return;
        }

        creditPanelRoot.SetAsLastSibling();

        if (creditWindowImage != null)
        {
            creditWindowImage.sprite = creditWindowSprite;
            creditWindowImage.color = creditWindowSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.78f);
            creditWindowImage.type = creditWindowSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            creditWindowImage.rectTransform.sizeDelta = creditWindowSize;
            creditWindowImage.rectTransform.SetAsLastSibling();
        }

        ConfigureImageButton(closeButton, closeButtonSprite, closeButtonHoverSprite, closeButtonPressedSprite, closeButtonSize, closeButtonPosition);
        if (closeButton != null)
        {
            closeButton.transform.SetAsLastSibling();
        }

        if (creditLinkButtonInstances == null || creditLinkButtons == null)
        {
            return;
        }

        int count = Mathf.Min(creditLinkButtonInstances.Length, creditLinkButtons.Length);
        for (int i = 0; i < count; i++)
        {
            CreditLinkButtonSettings settings = creditLinkButtons[i];
            if (settings == null)
            {
                continue;
            }

            ConfigureImageButton(
                creditLinkButtonInstances[i],
                settings.normalSprite,
                settings.hoverSprite,
                settings.pressedSprite,
                settings.size,
                settings.position);
            creditLinkButtonInstances[i].transform.SetAsLastSibling();
        }
    }

    private void OpenCreditLink(int index)
    {
        PlaySe(buttonClickSeName);
        if (creditLinkButtons == null
            || index < 0
            || index >= creditLinkButtons.Length
            || string.IsNullOrWhiteSpace(creditLinkButtons[index].url))
        {
            return;
        }

        Application.OpenURL(creditLinkButtons[index].url);
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

    private static Image CreateImage(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<Image>();
    }

    private static Image GetOrCreateImage(string name, Transform parent)
    {
        Transform child = parent.Find(name);
        if (child != null && child.TryGetComponent(out Image existingImage))
        {
            return existingImage;
        }

        return CreateImage(name, parent);
    }

    private static Button CreateImageButton(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<Button>();
    }

    private static Button GetOrCreateImageButton(string name, Transform parent)
    {
        Transform child = parent.Find(name);
        if (child != null && child.TryGetComponent(out Button existingButton))
        {
            return existingButton;
        }

        return CreateImageButton(name, parent);
    }

    private static void ConfigureImageButton(
        Button button,
        Sprite normalSprite,
        Sprite hoverSprite,
        Sprite pressedSprite,
        Vector2 size,
        Vector2 position)
    {
        if (button == null)
        {
            return;
        }

        SetupCenteredRect(button.GetComponent<RectTransform>(), size, position);

        Image image = button.GetComponent<Image>();
        image.sprite = normalSprite;
        image.type = normalSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = normalSprite != null ? Color.white : new Color(0.18f, 0.9f, 1f, 0.92f);

        ApplyButtonSprites(button, normalSprite, hoverSprite, pressedSprite);
    }

    private static void SetupCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
