using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleSceneButtonSetup
{
    public static void Build()
    {
        const string scenePath = "Assets/Scenes/Title.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        TitleScreen titleScreen = Object.FindFirstObjectByType<TitleScreen>();
        if (titleScreen == null)
        {
            Debug.LogError("[TitleSceneButtonSetup] TitleScreen not found.");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject buttonObject = GameObject.Find("PlayButton");
        if (buttonObject == null)
        {
            buttonObject = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform, false);
        }

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(420f, 120f);
        buttonRect.anchoredPosition = new Vector2(0f, -300f);

        Button button = buttonObject.GetComponent<Button>();
        Image image = buttonObject.GetComponent<Image>();

        GameObject labelObject = GameObject.Find("PlayButtonLabel");
        if (labelObject == null)
        {
            labelObject = new GameObject("PlayButtonLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
        }

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "PLAY";
        label.fontSize = 52;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        SerializedObject serializedTitle = new SerializedObject(titleScreen);
        serializedTitle.FindProperty("playButton").objectReferenceValue = button;
        serializedTitle.FindProperty("playButtonLabel").objectReferenceValue = label;
        serializedTitle.ApplyModifiedPropertiesWithoutUndo();

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        EditorUtility.SetDirty(titleScreen);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(label);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
