using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

#pragma warning disable 0649
public sealed class KissTimingGame : MonoBehaviour
{
    private enum JudgeResult
    {
        Flying,
        Perfect,
        Good,
        Miss
    }

    private sealed class StepResult
    {
        public JudgeResult Judge;
        public float ErrorSeconds;
    }

    private enum UnityroomScoreWriteMode
    {
        HighScoreDesc,
        HighScoreAsc,
        Always
    }

    [System.Serializable]
    private sealed class KissSuccessSeEvent
    {
        [Min(0)] public int elementIndex;
        public string seName;
    }

    private const int StepCount = 3;
    private const long StepBaseScore = 100000;

    private readonly List<StepResult> results = new List<StepResult>();
    private readonly string[] stepNames = { "LOOK", "BREATH", "KISS" };
    private readonly string[] waitFaces = { ". .", "///", "..." };
    private readonly string[] cueFaces = { "< >", "<3 >", "<3 <3" };

    [Header("Timing")]
    [SerializeField] private Vector2[] cueTimingRanges =
    {
        new Vector2(0.45f, 0.95f),
        new Vector2(0.45f, 0.95f),
        new Vector2(0.45f, 0.95f)
    };
    [SerializeField] private float perfectWindow = 0.08f;
    [SerializeField] private float goodWindow = 0.25f;
    [SerializeField] private float missWindow = 0.45f;

    [Header("Production Sprites")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite[] characterLoopSprites;
    [SerializeField] private Sprite[] perfectCharacterLoopSprites;
    [SerializeField] private Sprite[] flyingCharacterLoopSprites;
    [SerializeField] private Sprite[] missCharacterLoopSprites;
    [SerializeField] private float characterLoopFps = 6f;
    [SerializeField] private bool syncCharacterLoopToBpm = true;
    [SerializeField] private float musicBpm = 200f;
    [SerializeField] private float beatsPerCharacterLoop = 2f;
    [SerializeField] private Sprite cueMarkSprite;
    [SerializeField] private Vector2 cueMarkSize = new Vector2(220f, 220f);
    [SerializeField] private Sprite speedLineSprite;
    [SerializeField] private Sprite perfectEffectSprite;
    [SerializeField] private Sprite kissCutSprite;
    [SerializeField] private Sprite[] kissSuccessSprites;
    [SerializeField] private float kissSuccessFps = 12f;
    [SerializeField] private KissSuccessSeEvent[] kissSuccessSeEvents;
    [SerializeField] private Sprite secondJudgeOverlaySprite;
    [SerializeField] private Vector2 secondJudgeOverlaySize = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 secondJudgeOverlayPosition;
    [SerializeField] private bool secondJudgeOverlayPreserveAspect;
    [SerializeField] private Sprite explosionSprite;

    [Header("unityroom Ranking")]
    [SerializeField] private bool submitScoreToUnityroom;
    [SerializeField] private int unityroomScoreboardNo = 1;
    [SerializeField] private UnityroomScoreWriteMode unityroomScoreWriteMode = UnityroomScoreWriteMode.HighScoreDesc;

    [Header("Tweet")]
    [SerializeField] private string tweetTextTemplate = "天数十四！";
    [SerializeField] private string tweetGameUrl;
    [SerializeField] private string tweetHashtags;
    [SerializeField] private Sprite postButtonSprite;
    [SerializeField] private Sprite postButtonHoverSprite;
    [SerializeField] private Sprite postButtonPressedSprite;
    [SerializeField] private Vector2 postButtonPosition = new Vector2(0f, 360f);
    [SerializeField] private Vector2 postButtonSize = new Vector2(320f, 86f);
    [SerializeField] private string postButtonSeName;

    [Header("Scene Flow")]
    [SerializeField] private string titleSceneName = "Title";

    [Header("Sound Effects")]
    [SerializeField] private string cueSeName;
    [SerializeField] private string perfectSeName;
    [SerializeField] private string goodSeName;
    [SerializeField] private string missSeName;
    [SerializeField] private string flyingSeName;
    [SerializeField] private string finalKissSuccessSeName;
    [SerializeField] private string titleButtonSeName;
    [SerializeField] private string retryButtonSeName;
    [SerializeField] private string resultScoreTickSeName;
    [SerializeField] private string resultScoreDoneSeName;

    [Header("UI Style")]
    [SerializeField] private TMP_FontAsset uiFont;
    [SerializeField] private TMP_FontAsset scoreFont;
    [SerializeField] private Color scoreTextColor = new Color(1f, 0.86f, 0.08f);
    [SerializeField] private Color scoreOutlineColor = new Color(0.05f, 0.02f, 0.85f);
    [SerializeField, Range(0f, 0.5f)] private float scoreOutlineWidth = 0.22f;
    [SerializeField] private string inputPromptText = "SPACE / CLICK";
    [SerializeField] private Sprite titleButtonSprite;
    [SerializeField] private Sprite titleButtonHoverSprite;
    [SerializeField] private Sprite titleButtonPressedSprite;
    [SerializeField] private Sprite retryButtonSprite;
    [SerializeField] private Sprite retryButtonHoverSprite;
    [SerializeField] private Sprite retryButtonPressedSprite;

    private Canvas canvas;
    private RectTransform stageRoot;
    private RectTransform kissRoot;
    private RectTransform effectRoot;
    private RectTransform uiRoot;
    private Image backgroundImage;
    private Image sharedCharacterImage;
    private Image leftCharacterImage;
    private Image rightCharacterImage;
    private Image secondJudgeOverlayImage;
    private TMP_Text titleText;
    private TMP_Text phaseText;
    private TMP_Text cueText;
    private TMP_Text judgeText;
    private TMP_Text scoreText;
    private TMP_Text breakdownText;
    private TMP_Text promptText;
    private TMP_Text leftFace;
    private TMP_Text rightFace;
    private RectTransform resultButtonRoot;
    private RectTransform postButtonRoot;
    private Button postButton;
    private Image cueMarkImage;
    private Image flashImage;
    private Image speedLineImage;
    private Image perfectEffectImage;
    private Image kissCutImage;
    private Image explosionImage;
    private RectTransform cameraRig;
    private Vector2 cameraRigBasePosition;
    private Vector3 cameraRigBaseScale;
    private bool running;
    private bool showingResult;
    private bool waitingForInput;
    private bool cueShown;
    private float cueShownAt;
    private int currentStep;
    private Coroutine shakeRoutine;
    private Coroutine scoreRoutine;
    private Coroutine floatingScoreRoutine;
    private Coroutine characterLoopRoutine;
    private Sprite[] activeCharacterLoopSprites;
    private long lastResultScore;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name == "Title")
        {
            return;
        }

        if (FindAnyObjectByType<KissTimingGame>() != null)
        {
            return;
        }

        var gameObject = new GameObject("Kiss Timing Game");
        gameObject.AddComponent<KissTimingGame>();
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        EnsureEventSystem();
        BuildUi();
        ShowTitle();
    }

    private void Update()
    {
        if (WasSubmitPressed())
        {
            if (IsPointerOverUi())
            {
                return;
            }

            if (!running && !showingResult)
            {
                StartCoroutine(PlayGame());
                return;
            }

            if (waitingForInput)
            {
                ResolveCurrentStep();
            }
        }
    }

    private IEnumerator PlayGame()
    {
        running = true;
        showingResult = false;
        RestartSceneBgm();
        results.Clear();
        ResetEffects();
        SetSecondJudgeOverlayVisible(false);
        SetResultButtonsVisible(false);

        titleText.text = "";
        scoreText.text = "";
        breakdownText.text = "";
        judgeText.text = "";
        promptText.text = "";
        StopResultEffects();
        phaseText.text = "READY";
        SetIdleFace();
        yield return new WaitForSeconds(0.35f);

        for (currentStep = 0; currentStep < StepCount; currentStep++)
        {
            yield return PlayStep(currentStep);
            yield return new WaitForSeconds(0.18f);
        }

        yield return PlayFinalSequence();
        ShowResult();
        running = false;
    }

    private IEnumerator PlayStep(int stepIndex)
    {
        cueShown = false;
        waitingForInput = true;
        cueText.text = "";
        SetCueMarkVisible(false);
        judgeText.text = "";
        phaseText.text = $"{stepNames[stepIndex]}  {stepIndex + 1}/{StepCount}";
        SetWaitingFace(stepIndex);

        float waitSeconds = GetCueWaitSeconds(stepIndex);
        float startAt = Time.time;
        while (Time.time - startAt < waitSeconds && waitingForInput)
        {
            PulseStage(1f + Mathf.Sin(Time.time * 13f) * 0.008f);
            yield return null;
        }

        if (!waitingForInput)
        {
            yield break;
        }

        cueShown = true;
        cueShownAt = Time.time;
        cueText.text = cueMarkSprite == null ? "!" : "";
        SetCueMarkVisible(true);
        SetCueFace(stepIndex);
        PlaySe(cueSeName);
        PlayCuePop(stepIndex);

        while (Time.time - cueShownAt < GetMissWindow() && waitingForInput)
        {
            PulseStage(1.01f + Mathf.Sin(Time.time * 18f) * 0.01f);
            yield return null;
        }

        if (waitingForInput)
        {
            RegisterJudge(JudgeResult.Miss, GetMissWindow());
        }
    }

    private float GetCueWaitSeconds(int stepIndex)
    {
        if (cueTimingRanges != null
            && stepIndex >= 0
            && stepIndex < cueTimingRanges.Length)
        {
            Vector2 range = cueTimingRanges[stepIndex];
            float min = Mathf.Max(0f, Mathf.Min(range.x, range.y));
            float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
            if (max > 0f)
            {
                return min == max ? min : Random.Range(min, max);
            }
        }

        return Random.Range(0.45f, 0.95f);
    }

    private void ResolveCurrentStep()
    {
        if (!cueShown)
        {
            RegisterJudge(JudgeResult.Flying, -1f);
            return;
        }

        float error = Time.time - cueShownAt;
        if (error <= GetPerfectWindow())
        {
            RegisterJudge(JudgeResult.Perfect, error);
        }
        else if (error <= GetGoodWindow())
        {
            RegisterJudge(JudgeResult.Good, error);
        }
        else
        {
            RegisterJudge(JudgeResult.Miss, error);
        }
    }

    private void RegisterJudge(JudgeResult judge, float errorSeconds)
    {
        waitingForInput = false;
        cueShown = false;
        cueText.text = "";
        SetCueMarkVisible(false);
        results.Add(new StepResult { Judge = judge, ErrorSeconds = errorSeconds });
        judgeText.text = GetJudgeLabel(judge);
        judgeText.color = GetJudgeColor(judge);
        if (judge == JudgeResult.Perfect)
        {
            SetCharacterLoop(perfectCharacterLoopSprites);
        }
        else if (judge == JudgeResult.Good)
        {
            SetCharacterLoop(characterLoopSprites);
        }
        else if (judge == JudgeResult.Flying)
        {
            SetCharacterLoop(flyingCharacterLoopSprites);
        }
        else if (judge == JudgeResult.Miss)
        {
            SetCharacterLoop(missCharacterLoopSprites);
        }

        PlayJudgeSe(judge);
        PlayImpact(judge, currentStep);

        if (currentStep == 1)
        {
            SetSecondJudgeOverlayVisible(true);
        }
        else if (currentStep == 2)
        {
            SetSecondJudgeOverlayVisible(false);
        }
    }

    private IEnumerator PlayFinalSequence()
    {
        JudgeResult finalJudge = results.Count > 0 ? results[results.Count - 1].Judge : JudgeResult.Miss;
        bool success = finalJudge == JudgeResult.Perfect || finalJudge == JudgeResult.Good;
        phaseText.text = success ? "KISS!!" : "OH NO...";
        promptText.text = "";
        cueText.text = "";
        SetCueMarkVisible(false);

        if (success)
        {
            PlaySe(finalKissSuccessSeName);
            yield return PlayKissSuccessAnimation();
            bool showsExplosion = explosionSprite != null;
            explosionImage.gameObject.SetActive(showsExplosion);
            explosionImage.color = GetExplosionColor(finalJudge);
            PlayImpact(finalJudge, 2);
            yield return new WaitForSeconds(0.28f);
            yield return FlashToWhite(finalJudge == JudgeResult.Perfect ? 0.95f : 0.5f);
        }
        else
        {
            SetMissFace();
            yield return new WaitForSeconds(0.45f);
        }

        if (!HasAnySprite(kissSuccessSprites))
        {
            kissCutImage.gameObject.SetActive(false);
        }
        explosionImage.gameObject.SetActive(false);
    }

    private IEnumerator PlayKissSuccessAnimation()
    {
        bool usesSuccessFrames = HasAnySprite(kissSuccessSprites);
        kissCutImage.gameObject.SetActive(true);
        kissCutImage.color = usesSuccessFrames ? Color.white : GetKissCutColor();
        kissCutImage.preserveAspect = false;
        Stretch(kissCutImage.rectTransform);

        if (usesSuccessFrames)
        {
            kissCutImage.sprite = null;
            float frameInterval = 1f / Mathf.Max(1f, kissSuccessFps);
            for (int i = 0; i < kissSuccessSprites.Length; i++)
            {
                if (kissSuccessSprites[i] == null)
                {
                    continue;
                }

                kissCutImage.sprite = kissSuccessSprites[i];
                kissCutImage.color = Color.white;
                PlayKissSuccessSeEvents(i);
                yield return new WaitForSeconds(frameInterval);
            }

            yield break;
        }

        yield return new WaitForSeconds(0.18f);
    }

    private void PlayKissSuccessSeEvents(int elementIndex)
    {
        if (kissSuccessSeEvents == null)
        {
            return;
        }

        for (int i = 0; i < kissSuccessSeEvents.Length; i++)
        {
            KissSuccessSeEvent seEvent = kissSuccessSeEvents[i];
            if (seEvent == null || seEvent.elementIndex != elementIndex)
            {
                continue;
            }

            PlaySe(seEvent.seName);
        }
    }

    private void ShowTitle()
    {
        running = false;
        showingResult = false;
        waitingForInput = false;
        SetResultButtonsVisible(false);
        phaseText.text = "";
        cueText.text = "";
        SetCueMarkVisible(false);
        judgeText.text = "";
        scoreText.text = "";
        breakdownText.text = "";
        titleText.text = "";
        promptText.text = inputPromptText;
        SetIdleFace();
    }

    private void ShowResult()
    {
        long score = CalculateScore();
        lastResultScore = score;
        showingResult = true;
        titleText.text = "";
        phaseText.text = "";
        judgeText.text = GetResultHeadline();
        ApplyBlueResultTextStyle(judgeText);
        scoreText.text = "0 PTS";
        breakdownText.text = BuildResultBreakdown();
        promptText.text = "";
        SetResultFace();
        SetResultButtonsVisible(true);
        PlaySe(resultScoreTickSeName);
        scoreRoutine = StartCoroutine(CountUpScore(score));
        floatingScoreRoutine = StartCoroutine(SpawnScoreTexts(score));
        SubmitUnityroomScore(score);
    }

    public void RetryGame()
    {
        if (running)
        {
            return;
        }

        StartCoroutine(PlayGame());
    }

    public void BackToTitle()
    {
        if (Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        ShowTitle();
    }

    private void OnTitleButtonClicked()
    {
        PlaySe(titleButtonSeName);
        BackToTitle();
    }

    private void OnRetryButtonClicked()
    {
        PlaySe(retryButtonSeName);
        RetryGame();
    }

    private void OnPostButtonClicked()
    {
        PlaySe(postButtonSeName);
        TweetResult(lastResultScore);
    }

    private void SetResultButtonsVisible(bool visible)
    {
        if (resultButtonRoot != null)
        {
            resultButtonRoot.gameObject.SetActive(visible);
        }

        if (postButtonRoot != null)
        {
            postButtonRoot.gameObject.SetActive(visible);
        }
    }

    private void RestartSceneBgm()
    {
        BGMChange bgmChange = FindAnyObjectByType<BGMChange>();
        if (bgmChange == null || SoundManager.Instance == null)
        {
            return;
        }

        FieldInfo bgmClipField = typeof(BGMChange).GetField("bgmClip", BindingFlags.Instance | BindingFlags.NonPublic);
        AudioClip bgmClip = bgmClipField != null ? bgmClipField.GetValue(bgmChange) as AudioClip : null;
        if (bgmClip == null)
        {
            return;
        }

        SoundManager.Instance.StopBGM();
        SoundManager.Instance.PlayBGM(bgmClip);
    }

    private void PlayJudgeSe(JudgeResult judge)
    {
        string seName = judge switch
        {
            JudgeResult.Perfect => perfectSeName,
            JudgeResult.Good => goodSeName,
            JudgeResult.Miss => missSeName,
            JudgeResult.Flying => flyingSeName,
            _ => string.Empty
        };

        PlaySe(seName);
    }

    private static void PlaySe(string seName)
    {
        if (string.IsNullOrWhiteSpace(seName) || SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlaySE(seName);
    }

    private void SetCueMarkVisible(bool visible)
    {
        if (cueMarkImage == null || cueMarkSprite == null)
        {
            return;
        }

        cueMarkImage.gameObject.SetActive(visible);
        if (visible)
        {
            cueMarkImage.transform.localScale = Vector3.one;
            cueMarkImage.rectTransform.sizeDelta = cueMarkSize;
        }
    }

    private void SetSecondJudgeOverlayVisible(bool visible)
    {
        if (secondJudgeOverlayImage == null)
        {
            return;
        }

        bool shouldShow = visible && secondJudgeOverlaySprite != null;
        secondJudgeOverlayImage.gameObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        secondJudgeOverlayImage.sprite = secondJudgeOverlaySprite;
        secondJudgeOverlayImage.color = Color.white;
        secondJudgeOverlayImage.preserveAspect = secondJudgeOverlayPreserveAspect;
        secondJudgeOverlayImage.rectTransform.sizeDelta = secondJudgeOverlaySize;
        secondJudgeOverlayImage.rectTransform.anchoredPosition = secondJudgeOverlayPosition;
    }

    private string BuildResultBreakdown()
    {
        int successChain = 0;
        List<string> lines = new List<string>();
        for (int i = 0; i < results.Count; i++)
        {
            StepResult result = results[i];
            long stepScore = 0;
            if (result.Judge == JudgeResult.Perfect || result.Judge == JudgeResult.Good)
            {
                successChain++;
                stepScore = ClampScore(CalculateStepScore(result, i, successChain));
            }
            else
            {
                successChain = 0;
            }

            string scorePart = stepScore > 0 ? " +" + stepScore.ToString("N0") : " +0";
            lines.Add((i + 1) + " " + stepNames[i] + "  " + GetJudgeLabel(result.Judge) + scorePart);
        }

        if (IsAllPerfect())
        {
            lines.Add("ALL PERFECT  x14");
        }

        return string.Join("\n", lines);
    }

    private long CalculateScore()
    {
        double score = 0;
        bool hasScoringHit = false;
        int successChain = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Judge == JudgeResult.Perfect || results[i].Judge == JudgeResult.Good)
            {
                hasScoringHit = true;
                successChain++;
                score += CalculateStepScore(results[i], i, successChain);
            }
            else
            {
                successChain = 0;
            }
        }

        if (results.Count == StepCount && IsAllPerfect())
        {
            score *= 14;
        }

        return hasScoringHit ? ClampScore(score) : 0;
    }

    private double CalculateStepScore(StepResult result, int stepIndex, int successChain)
    {
        float timingMultiplier = GetTimingMultiplier(result);
        float stepMultiplier = stepIndex switch
        {
            0 => 1f,
            1 => 3f,
            _ => 10f
        };
        float chainMultiplier = 1f + (successChain - 1) * 0.5f;
        float nearZeroBonus = Mathf.Max(0f, 1f - Mathf.Abs(result.ErrorSeconds) / GetGoodWindow()) * 9999f;

        return StepBaseScore * timingMultiplier * stepMultiplier * chainMultiplier + nearZeroBonus;
    }

    private float GetTimingMultiplier(StepResult result)
    {
        if (result.Judge == JudgeResult.Perfect)
        {
            float accuracy = 1f - Mathf.Clamp01(result.ErrorSeconds / GetPerfectWindow());
            return Mathf.Lerp(100f, 300f, accuracy);
        }

        if (result.Judge == JudgeResult.Good)
        {
            float goodRange = Mathf.Max(0.01f, GetGoodWindow() - GetPerfectWindow());
            float accuracy = 1f - Mathf.Clamp01((result.ErrorSeconds - GetPerfectWindow()) / goodRange);
            return Mathf.Lerp(10f, 80f, accuracy);
        }

        return 0f;
    }

    private float GetPerfectWindow()
    {
        return Mathf.Max(0.01f, perfectWindow);
    }

    private float GetGoodWindow()
    {
        return Mathf.Max(GetPerfectWindow() + 0.01f, goodWindow);
    }

    private float GetMissWindow()
    {
        return Mathf.Max(GetGoodWindow() + 0.01f, missWindow);
    }

    private static long ClampScore(double score)
    {
        if (score <= 0)
        {
            return 0;
        }

        if (score >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)score;
    }

    private void SubmitUnityroomScore(long score)
    {
        if (!submitScoreToUnityroom || score <= 0)
        {
            return;
        }

        Type clientType = FindType("unityroom.Api.UnityroomApiClient");
        Type writeModeType = FindType("unityroom.Api.ScoreboardWriteMode");
        if (clientType == null || writeModeType == null)
        {
            Debug.LogWarning("unityroom client library is not installed. Score was not submitted.");
            return;
        }

        PropertyInfo instanceProperty = clientType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        object client = instanceProperty != null ? instanceProperty.GetValue(null) : null;
        if (client == null)
        {
            Debug.LogWarning("UnityroomApiClient.Instance was not found. Put UnityroomApiClient prefab in the InGame scene.");
            return;
        }

        object writeMode = Enum.Parse(writeModeType, unityroomScoreWriteMode.ToString());
        MethodInfo sendScore = clientType.GetMethod("SendScore", new[] { typeof(int), typeof(float), writeModeType });
        if (sendScore == null)
        {
            Debug.LogWarning("UnityroomApiClient.SendScore was not found. Score was not submitted.");
            return;
        }

        sendScore.Invoke(client, new[] { unityroomScoreboardNo, (object)Mathf.Min(score, float.MaxValue), writeMode });
    }

    private void TweetResult(long score)
    {
        string tweetText = FormatTweetText(score);
        Type tweetType = FindType("naichilab.UnityRoomTweet");
        if (tweetType != null && TryCallUnityroomTweet(tweetType, tweetText))
        {
            return;
        }

        string url = BuildTweetFallbackUrl(tweetText);
        Application.OpenURL(url);
    }

    private string FormatTweetText(long score)
    {
        string text = string.IsNullOrEmpty(tweetTextTemplate) ? string.Empty : tweetTextTemplate;
        text = text
            .Replace("{score}", score.ToString())
            .Replace("{scoreN0}", score.ToString("N0"));
        string scoreLine = score.ToString("N0") + " PTS";
        return string.IsNullOrWhiteSpace(text) ? scoreLine : text + "\n" + scoreLine;
    }

    private bool TryCallUnityroomTweet(Type tweetType, string tweetText)
    {
        string gameId = ExtractUnityroomGameId(tweetGameUrl);
        string[] hashtags = GetNormalizedHashtags();
        string hashtag1 = hashtags.Length > 0 ? hashtags[0] : string.Empty;
        string hashtag2 = hashtags.Length > 1 ? hashtags[1] : string.Empty;

        if (string.IsNullOrWhiteSpace(gameId))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(hashtag1) && !string.IsNullOrEmpty(hashtag2)
            && TryInvokeTweet(tweetType, new object[] { gameId, tweetText, hashtag1, hashtag2 }))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(hashtag1)
            && TryInvokeTweet(tweetType, new object[] { gameId, tweetText, hashtag1 }))
        {
            return true;
        }

        return TryInvokeTweet(tweetType, new object[] { gameId, tweetText });
    }

    private string BuildTweetFallbackUrl(string tweetText)
    {
        List<string> query = new List<string>
        {
            "text=" + Uri.EscapeDataString(tweetText)
        };

        if (!string.IsNullOrWhiteSpace(tweetGameUrl))
        {
            query.Add("url=" + Uri.EscapeDataString(tweetGameUrl));
        }

        string hashtags = BuildHashtagQuery();
        if (!string.IsNullOrEmpty(hashtags))
        {
            query.Add("hashtags=" + Uri.EscapeDataString(hashtags));
        }

        return "https://twitter.com/intent/tweet?" + string.Join("&", query);
    }

    private string BuildHashtagQuery()
    {
        return string.Join(",", GetNormalizedHashtags());
    }

    private string[] GetNormalizedHashtags()
    {
        if (string.IsNullOrWhiteSpace(tweetHashtags))
        {
            return Array.Empty<string>();
        }

        string[] parts = tweetHashtags.Split(new[] { ',', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> hashtags = new List<string>();
        for (int i = 0; i < parts.Length; i++)
        {
            string hashtag = NormalizeHashtag(parts[i]);
            if (!string.IsNullOrEmpty(hashtag))
            {
                hashtags.Add(hashtag);
            }
        }

        return hashtags.ToArray();
    }

    private static string ExtractUnityroomGameId(string gameUrl)
    {
        if (string.IsNullOrWhiteSpace(gameUrl))
        {
            return string.Empty;
        }

        string trimmed = gameUrl.Trim().TrimEnd('/');
        int slashIndex = trimmed.LastIndexOf('/');
        return slashIndex >= 0 ? trimmed.Substring(slashIndex + 1) : trimmed;
    }

    private static string NormalizeHashtag(string hashtag)
    {
        return string.IsNullOrWhiteSpace(hashtag)
            ? string.Empty
            : hashtag.Trim().TrimStart('#');
    }

    private static bool TryInvokeTweet(Type tweetType, object[] args)
    {
        MethodInfo[] methods = tweetType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            if (method.Name != "Tweet")
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != args.Length)
            {
                continue;
            }

            bool canUse = true;
            for (int p = 0; p < parameters.Length; p++)
            {
                if (parameters[p].ParameterType != typeof(string))
                {
                    canUse = false;
                    break;
                }
            }

            if (!canUse)
            {
                continue;
            }

            method.Invoke(null, args);
            return true;
        }

        return false;
    }

    private static Type FindType(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type = assemblies[i].GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private bool IsAllPerfect()
    {
        if (results.Count < StepCount)
        {
            return false;
        }

        foreach (StepResult result in results)
        {
            if (result.Judge != JudgeResult.Perfect)
            {
                return false;
            }
        }

        return true;
    }

    private string GetResultHeadline()
    {
        JudgeResult finalJudge = GetFinalJudge();
        if (IsAllPerfect())
        {
            return "PERFECT KISS!!!";
        }

        if (finalJudge == JudgeResult.Perfect || finalJudge == JudgeResult.Good)
        {
            return GetJudgeLabel(finalJudge);
        }

        return GetJudgeLabel(finalJudge);
    }

    private JudgeResult GetBestFinalJudge()
    {
        if (IsAllPerfect())
        {
            return JudgeResult.Perfect;
        }

        return GetFinalJudge();
    }

    private JudgeResult GetFinalJudge()
    {
        return results.Count > 0 ? results[results.Count - 1].Judge : JudgeResult.Miss;
    }

    private void PlayCuePop(int stepIndex)
    {
        float scale = 1.1f + stepIndex * 0.12f;
        RectTransform target = cueMarkSprite != null && cueMarkImage != null
            ? cueMarkImage.rectTransform
            : cueText.rectTransform;
        target.localScale = Vector3.one * scale;
        StartCoroutine(ScaleBack(target, scale, 1f, 0.12f));
    }

    private void PlayImpact(JudgeResult judge, int stepIndex)
    {
        float judgePower = judge switch
        {
            JudgeResult.Perfect => 1f,
            JudgeResult.Good => 0.62f,
            JudgeResult.Flying => 0.28f,
            _ => 0.16f
        };

        float stepPower = stepIndex switch
        {
            0 => 0.6f,
            1 => 0.85f,
            _ => 1.3f
        };

        float power = judgePower * stepPower;
        StartCoroutine(ZoomPunch(power));
        StartCoroutine(Flash(power * 0.38f));
        if (judge == JudgeResult.Perfect || judge == JudgeResult.Good)
        {
            StartCoroutine(ShowSpeedLines(power));
        }

        if (judge == JudgeResult.Perfect && perfectEffectImage != null && perfectEffectSprite != null)
        {
            StartCoroutine(ShowPerfectEffect(power));
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(Shake(power));
    }

    private IEnumerator SpawnScoreTexts(long finalScore)
    {
        string[] perfectBursts =
        {
            "+10", "+10", "+100", "x10", "x100", "KISS BONUS", "LOVE BURST", "STAR BONUS", "x14"
        };
        string[] failedBursts = { "+0", "NO BONUS", "MISS...", "TOO EARLY" };
        string[] source = finalScore > 0 ? perfectBursts : failedBursts;

        float until = Time.time + 2f;
        while (Time.time < until)
        {
            SpawnFloatingText(source[Random.Range(0, source.Length)]);
            yield return new WaitForSeconds(finalScore > 0 ? 0.055f : 0.18f);
        }
    }

    private IEnumerator CountUpScore(long finalScore)
    {
        float duration = finalScore > 0 ? 1.25f : 0.25f;
        float startAt = Time.time;
        while (Time.time - startAt < duration)
        {
            float t = (Time.time - startAt) / duration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            long shownScore = (long)(finalScore * eased);
            scoreText.text = shownScore.ToString("N0") + " PTS";
            yield return null;
        }

        scoreText.text = finalScore.ToString("N0") + " PTS";
        scoreRoutine = null;
        PlaySe(resultScoreDoneSeName);
    }

    private void SpawnFloatingText(string value)
    {
        TMP_Text text = CreateText("FloatScore", uiRoot, value, 38, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(text, scoreFont);
        text.color = GetFloatingScoreColor();
        RectTransform rect = text.rectTransform;
        rect.sizeDelta = new Vector2(360f, 80f);
        rect.anchoredPosition = new Vector2(Random.Range(-430f, 430f), Random.Range(-210f, 210f));
        rect.localScale = Vector3.one * Random.Range(0.85f, 1.35f);
        StartCoroutine(FloatAndFade(text));
    }

    private IEnumerator FloatAndFade(TMP_Text text)
    {
        if (text == null)
        {
            yield break;
        }

        RectTransform rect = text.rectTransform;
        if (rect == null)
        {
            yield break;
        }

        Vector2 start = rect.anchoredPosition;
        Vector2 end = start + new Vector2(Random.Range(-30f, 30f), Random.Range(80f, 150f));
        Color color = text.color;
        float duration = 0.65f;
        float startAt = Time.time;
        while (Time.time - startAt < duration)
        {
            if (text == null || rect == null)
            {
                yield break;
            }

            float t = (Time.time - startAt) / duration;
            rect.anchoredPosition = Vector2.Lerp(start, end, t);
            rect.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.35f, Mathf.Sin(t * Mathf.PI));
            text.color = new Color(color.r, color.g, color.b, 1f - t);
            yield return null;
        }

        if (text != null)
        {
            Destroy(text.gameObject);
        }
    }

    private static Color GetFloatingScoreColor()
    {
        Color[] colors =
        {
            new Color(0.05f, 0.02f, 0.85f),
            new Color(0.12f, 0.62f, 1f),
            new Color(1f, 0.18f, 0.45f),
            new Color(1f, 0.88f, 0.12f)
        };
        return colors[Random.Range(0, colors.Length)];
    }

    private IEnumerator ZoomPunch(float power)
    {
        Vector3 start = cameraRigBaseScale;
        Vector3 peak = Vector3.one * (1f + 0.045f + power * 0.075f);
        float duration = 0.16f;
        float startAt = Time.time;
        while (Time.time - startAt < duration)
        {
            float t = (Time.time - startAt) / duration;
            float curve = Mathf.Sin(t * Mathf.PI);
            cameraRig.localScale = Vector3.Lerp(start, peak, curve);
            yield return null;
        }

        cameraRig.localScale = start;
    }

    private IEnumerator Shake(float power)
    {
        float duration = 0.12f + power * 0.09f;
        float amount = 5f + power * 20f;
        float startAt = Time.time;
        while (Time.time - startAt < duration)
        {
            cameraRig.anchoredPosition = cameraRigBasePosition + Random.insideUnitCircle * amount;
            yield return null;
        }

        cameraRig.anchoredPosition = cameraRigBasePosition;
        shakeRoutine = null;
    }

    private IEnumerator ShowSpeedLines(float power)
    {
        speedLineImage.gameObject.SetActive(true);
        speedLineImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.28f + power * 0.5f));
        speedLineImage.transform.localScale = Vector3.one * (1f + power * 0.08f);
        yield return new WaitForSeconds(0.13f + power * 0.08f);
        speedLineImage.gameObject.SetActive(false);
    }

    private IEnumerator ShowPerfectEffect(float power)
    {
        perfectEffectImage.gameObject.SetActive(true);
        perfectEffectImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.45f + power * 0.4f));
        perfectEffectImage.transform.localScale = Vector3.one * (1f + power * 0.12f);
        yield return new WaitForSeconds(0.18f + power * 0.1f);
        perfectEffectImage.gameObject.SetActive(false);
    }

    private IEnumerator Flash(float alpha)
    {
        flashImage.gameObject.SetActive(true);
        float startAt = Time.time;
        float duration = 0.16f;
        while (Time.time - startAt < duration)
        {
            float t = (Time.time - startAt) / duration;
            float a = Mathf.Sin(t * Mathf.PI) * alpha;
            flashImage.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }

        flashImage.color = Color.clear;
        flashImage.gameObject.SetActive(false);
    }

    private IEnumerator FlashToWhite(float alpha)
    {
        flashImage.gameObject.SetActive(true);
        float duration = 0.35f;
        float startAt = Time.time;
        while (Time.time - startAt < duration)
        {
            float t = (Time.time - startAt) / duration;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, alpha, t));
            yield return null;
        }

        yield return new WaitForSeconds(0.15f);
        startAt = Time.time;
        while (Time.time - startAt < duration)
        {
            float t = (Time.time - startAt) / duration;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(alpha, 0f, t));
            yield return null;
        }

        flashImage.color = Color.clear;
        flashImage.gameObject.SetActive(false);
    }

    private IEnumerator ScaleBack(RectTransform target, float from, float to, float duration)
    {
        float startAt = Time.time;
        while (Time.time - startAt < duration)
        {
            float t = (Time.time - startAt) / duration;
            target.localScale = Vector3.one * Mathf.Lerp(from, to, t);
            yield return null;
        }

        target.localScale = Vector3.one * to;
    }

    private void PulseStage(float scale)
    {
        cameraRig.localScale = Vector3.one * scale;
    }

    private void ResetEffects()
    {
        StopResultEffects();
        cameraRig.anchoredPosition = cameraRigBasePosition;
        cameraRig.localScale = cameraRigBaseScale;
        flashImage.gameObject.SetActive(false);
        speedLineImage.gameObject.SetActive(false);
        if (perfectEffectImage != null)
        {
            perfectEffectImage.gameObject.SetActive(false);
        }
        kissCutImage.gameObject.SetActive(false);
        explosionImage.gameObject.SetActive(false);
    }

    private void StopResultEffects()
    {
        if (scoreRoutine != null)
        {
            StopCoroutine(scoreRoutine);
            scoreRoutine = null;
        }

        if (floatingScoreRoutine != null)
        {
            StopCoroutine(floatingScoreRoutine);
            floatingScoreRoutine = null;
        }

        if (uiRoot == null)
        {
            return;
        }

        for (int i = uiRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = uiRoot.GetChild(i);
            if (child.name.StartsWith("FloatScore"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void SetIdleFace()
    {
        SetCharacterLoop(characterLoopSprites);
        SetFallbackFace(". .", ". .");
    }

    private void SetWaitingFace(int stepIndex)
    {
        SetFallbackFace(waitFaces[stepIndex], waitFaces[stepIndex]);
    }

    private void SetCueFace(int stepIndex)
    {
        SetFallbackFace(cueFaces[stepIndex], cueFaces[stepIndex]);
    }

    private void SetResultFace()
    {
        SetFallbackFace("<3 <3", "<3 <3");
    }

    private void SetMissFace()
    {
        SetFallbackFace("x x", "x x");
    }

    private void SetFallbackFace(string left, string right)
    {
        leftFace.text = left;
        rightFace.text = right;
        bool showFallback = !HasCharacterLoopSprites();
        leftFace.gameObject.SetActive(showFallback);
        rightFace.gameObject.SetActive(showFallback);
    }

    private bool HasCharacterLoopSprites()
    {
        return HasAnySprite(characterLoopSprites);
    }

    private void SetCharacterLoop(Sprite[] sprites)
    {
        if (!HasAnySprite(sprites))
        {
            return;
        }

        activeCharacterLoopSprites = sprites;
    }

    private static bool HasAnySprite(Sprite[] sprites)
    {
        if (sprites == null)
        {
            return false;
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator LoopCharacterSprites()
    {
        int frame = 0;
        while (true)
        {
            Sprite sprite = GetCharacterLoopSprite(frame);
            if (sprite != null)
            {
                sharedCharacterImage.sprite = sprite;
            }

            frame++;
            yield return new WaitForSeconds(GetCharacterLoopInterval());
        }
    }

    private float GetCharacterLoopInterval()
    {
        return 1f / Mathf.Max(1f, GetCharacterLoopFps());
    }

    private float GetCharacterLoopFps()
    {
        if (!syncCharacterLoopToBpm)
        {
            return characterLoopFps;
        }

        int frameCount = Mathf.Max(1, CountSprites(activeCharacterLoopSprites));
        float beats = Mathf.Max(0.25f, beatsPerCharacterLoop);
        return Mathf.Max(1f, musicBpm / 60f * frameCount / beats);
    }

    private static int CountSprites(Sprite[] sprites)
    {
        if (sprites == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private Sprite GetCharacterLoopSprite(int frame)
    {
        Sprite[] sprites = activeCharacterLoopSprites;
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        for (int offset = 0; offset < sprites.Length; offset++)
        {
            Sprite sprite = sprites[(frame + offset) % sprites.Length];
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private static string GetJudgeLabel(JudgeResult judge)
    {
        return judge switch
        {
            JudgeResult.Flying => "FLYING!",
            JudgeResult.Perfect => "PERFECT KISS!!",
            JudgeResult.Good => "GOOD KISS!",
            _ => "MISS..."
        };
    }

    private static Color GetJudgeColor(JudgeResult judge)
    {
        return judge switch
        {
            JudgeResult.Perfect => new Color(1f, 0.95f, 0.18f),
            JudgeResult.Good => new Color(0.55f, 1f, 0.95f),
            JudgeResult.Flying => new Color(1f, 0.3f, 0.4f),
            _ => new Color(0.6f, 0.65f, 0.72f)
        };
    }

    private Color GetExplosionColor(JudgeResult finalJudge)
    {
        if (explosionSprite != null)
        {
            return Color.white;
        }

        return finalJudge == JudgeResult.Perfect
            ? new Color(1f, 0.95f, 0.15f, 0.9f)
            : new Color(1f, 0.45f, 0.8f, 0.75f);
    }

    private Color GetKissCutColor()
    {
        return kissCutSprite != null || HasAnySprite(kissSuccessSprites)
            ? Color.white
            : new Color(1f, 0.35f, 0.65f, 0.8f);
    }

    private static Color GetUiBlue()
    {
        return new Color(0.05f, 0.02f, 0.85f);
    }

    private static void ApplyBlueResultTextStyle(TMP_Text text)
    {
        text.color = GetUiBlue();
        text.outlineColor = new Color(1f, 0.86f, 0.08f);
        text.outlineWidth = 0.14f;
    }

    private void ApplyScoreTextStyle(TMP_Text text)
    {
        text.color = scoreTextColor;
        text.outlineColor = scoreOutlineColor;
        text.outlineWidth = scoreOutlineWidth;
    }

    private static bool WasSubmitPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        Touchscreen touchscreen = Touchscreen.current;
        return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
               || (mouse != null && mouse.leftButton.wasPressedThisFrame)
               || (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame);
    }

    private static bool IsPointerOverUi()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return IsPointerOverSelectable(mouse.position.ReadValue());
        }

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            return IsPointerOverSelectable(touchscreen.primaryTouch.position.ReadValue());
        }

        return false;
    }

    private static bool IsPointerOverSelectable(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        var pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };
        var results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);
        for (int i = 0; i < results.Count; i++)
        {
            GameObject target = results[i].gameObject;
            if (target != null && target.GetComponentInParent<Selectable>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("KissGameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        cameraRig = CreateRect("CameraRig", canvas.transform);
        Stretch(cameraRig);
        cameraRigBasePosition = cameraRig.anchoredPosition;
        cameraRigBaseScale = cameraRig.localScale;

        stageRoot = CreateRect("Stage", cameraRig);
        Stretch(stageRoot);
        kissRoot = CreateRect("KissLayer", canvas.transform);
        Stretch(kissRoot);
        effectRoot = CreateRect("Effects", canvas.transform);
        Stretch(effectRoot);
        uiRoot = CreateRect("UiLayer", canvas.transform);
        Stretch(uiRoot);

        backgroundImage = CreateImage("Background", stageRoot, new Color(0.22f, 0.02f, 0.78f));
        Stretch(backgroundImage.rectTransform);
        ApplySprite(backgroundImage, backgroundSprite, false);

        sharedCharacterImage = CreateImage("SharedCharacterLoop", stageRoot, Color.white);
        Stretch(sharedCharacterImage.rectTransform);
        sharedCharacterImage.preserveAspect = false;
        sharedCharacterImage.gameObject.SetActive(HasCharacterLoopSprites());

        leftCharacterImage = CreateImage("LeftCharacter", stageRoot, new Color(1f, 0.58f, 0.62f, 0.96f));
        leftCharacterImage.rectTransform.anchorMin = new Vector2(0f, 0f);
        leftCharacterImage.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        leftCharacterImage.rectTransform.offsetMin = new Vector2(0f, 0f);
        leftCharacterImage.rectTransform.offsetMax = new Vector2(150f, 0f);

        rightCharacterImage = CreateImage("RightCharacter", stageRoot, new Color(1f, 0.62f, 0.54f, 0.96f));
        rightCharacterImage.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rightCharacterImage.rectTransform.anchorMax = new Vector2(1f, 1f);
        rightCharacterImage.rectTransform.offsetMin = new Vector2(-150f, 0f);
        rightCharacterImage.rectTransform.offsetMax = new Vector2(0f, 0f);

        bool usesSharedCharacterLoop = HasCharacterLoopSprites();
        leftCharacterImage.gameObject.SetActive(!usesSharedCharacterLoop);
        rightCharacterImage.gameObject.SetActive(!usesSharedCharacterLoop);
        if (usesSharedCharacterLoop)
        {
            activeCharacterLoopSprites = characterLoopSprites;
            characterLoopRoutine = StartCoroutine(LoopCharacterSprites());
        }

        secondJudgeOverlayImage = CreateImage("SecondJudgeOverlay", stageRoot, Color.white);
        secondJudgeOverlayImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        secondJudgeOverlayImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        secondJudgeOverlayImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        secondJudgeOverlayImage.sprite = secondJudgeOverlaySprite;
        secondJudgeOverlayImage.preserveAspect = secondJudgeOverlayPreserveAspect;
        secondJudgeOverlayImage.rectTransform.sizeDelta = secondJudgeOverlaySize;
        secondJudgeOverlayImage.rectTransform.anchoredPosition = secondJudgeOverlayPosition;
        secondJudgeOverlayImage.gameObject.SetActive(false);

        leftFace = CreateText("LeftFace", stageRoot, ". .", 120, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(leftFace, uiFont);
        leftFace.color = new Color(0.05f, 0.02f, 0.85f);
        leftFace.rectTransform.anchorMin = new Vector2(0.08f, 0.42f);
        leftFace.rectTransform.anchorMax = new Vector2(0.45f, 0.7f);
        leftFace.rectTransform.offsetMin = Vector2.zero;
        leftFace.rectTransform.offsetMax = Vector2.zero;

        rightFace = CreateText("RightFace", stageRoot, ". .", 120, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(rightFace, uiFont);
        rightFace.color = new Color(0.05f, 0.02f, 0.85f);
        rightFace.rectTransform.anchorMin = new Vector2(0.55f, 0.42f);
        rightFace.rectTransform.anchorMax = new Vector2(0.92f, 0.7f);
        rightFace.rectTransform.offsetMin = Vector2.zero;
        rightFace.rectTransform.offsetMax = Vector2.zero;

        titleText = CreateText("TitleText", uiRoot, "", 96, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(titleText, uiFont);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 245f);
        titleText.rectTransform.sizeDelta = new Vector2(1000f, 160f);

        phaseText = CreateText("PhaseText", uiRoot, "", 58, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(phaseText, uiFont);
        phaseText.rectTransform.anchoredPosition = new Vector2(0f, 370f);
        phaseText.rectTransform.sizeDelta = new Vector2(850f, 100f);

        cueText = CreateText("CueText", uiRoot, "", 210, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(cueText, uiFont);
        cueText.color = new Color(1f, 0.95f, 0.2f);
        cueText.rectTransform.anchoredPosition = new Vector2(0f, 80f);
        cueText.rectTransform.sizeDelta = new Vector2(260f, 260f);

        cueMarkImage = CreateImage("CueMarkImage", uiRoot, Color.white);
        cueMarkImage.rectTransform.anchoredPosition = new Vector2(0f, 105f);
        cueMarkImage.rectTransform.sizeDelta = cueMarkSize;
        ApplySprite(cueMarkImage, cueMarkSprite, true);
        cueMarkImage.gameObject.SetActive(false);

        judgeText = CreateText("JudgeText", uiRoot, "", 78, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(judgeText, uiFont);
        ApplyBlueResultTextStyle(judgeText);
        judgeText.rectTransform.anchoredPosition = new Vector2(0f, -295f);
        judgeText.rectTransform.sizeDelta = new Vector2(1100f, 120f);

        scoreText = CreateText("ScoreText", uiRoot, "", 86, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(scoreText, scoreFont != null ? scoreFont : uiFont);
        ApplyScoreTextStyle(scoreText);
        scoreText.rectTransform.anchoredPosition = new Vector2(0f, 35f);
        scoreText.rectTransform.sizeDelta = new Vector2(1300f, 140f);

        breakdownText = CreateText("BreakdownText", uiRoot, "", 32, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(breakdownText, uiFont);
        ApplyBlueResultTextStyle(breakdownText);
        breakdownText.rectTransform.anchoredPosition = new Vector2(0f, -112f);
        breakdownText.rectTransform.sizeDelta = new Vector2(1200f, 220f);

        promptText = CreateText("PromptText", uiRoot, "", 44, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(promptText, uiFont);
        promptText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        promptText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        promptText.rectTransform.anchoredPosition = new Vector2(0f, 82f);
        promptText.rectTransform.sizeDelta = new Vector2(650f, 90f);

        postButtonRoot = CreateRect("PostButtonRoot", uiRoot);
        postButtonRoot.anchorMin = new Vector2(0.5f, 0.5f);
        postButtonRoot.anchorMax = new Vector2(0.5f, 0.5f);
        postButtonRoot.anchoredPosition = postButtonPosition;
        postButtonRoot.sizeDelta = postButtonSize;
        postButton = CreateButton(
            "PostButton",
            postButtonRoot,
            "POST",
            Vector2.zero,
            postButtonSprite,
            postButtonHoverSprite,
            postButtonPressedSprite,
            uiFont);
        postButton.GetComponent<RectTransform>().sizeDelta = postButtonSize;
        postButton.onClick.AddListener(OnPostButtonClicked);

        resultButtonRoot = CreateRect("ResultButtons", uiRoot);
        resultButtonRoot.anchorMin = new Vector2(0.5f, 0f);
        resultButtonRoot.anchorMax = new Vector2(0.5f, 0f);
        resultButtonRoot.anchoredPosition = new Vector2(0f, 96f);
        resultButtonRoot.sizeDelta = new Vector2(760f, 96f);

        Button titleButton = CreateButton(
            "TitleButton",
            resultButtonRoot,
            "TITLE",
            new Vector2(-205f, 0f),
            titleButtonSprite,
            titleButtonHoverSprite,
            titleButtonPressedSprite,
            uiFont);
        titleButton.onClick.AddListener(OnTitleButtonClicked);
        Button retryButton = CreateButton(
            "RetryButton",
            resultButtonRoot,
            "RETRY",
            new Vector2(205f, 0f),
            retryButtonSprite,
            retryButtonHoverSprite,
            retryButtonPressedSprite,
            uiFont);
        retryButton.onClick.AddListener(OnRetryButtonClicked);
        SetResultButtonsVisible(false);

        speedLineImage = CreateSpeedLines();
        perfectEffectImage = CreateImage("PerfectEffect", effectRoot, Color.white);
        Stretch(perfectEffectImage.rectTransform);
        ApplySprite(perfectEffectImage, perfectEffectSprite, false);
        perfectEffectImage.gameObject.SetActive(false);

        flashImage = CreateImage("WhiteFlash", effectRoot, Color.clear);
        Stretch(flashImage.rectTransform);
        flashImage.gameObject.SetActive(false);

        kissCutImage = CreateImage("KissCut", kissRoot, new Color(1f, 0.35f, 0.65f, 0.8f));
        Stretch(kissCutImage.rectTransform);
        ApplySprite(kissCutImage, kissCutSprite, false);
        kissCutImage.gameObject.SetActive(false);

        explosionImage = CreateImage("Explosion", effectRoot, new Color(1f, 0.95f, 0.2f, 0.8f));
        explosionImage.rectTransform.sizeDelta = new Vector2(900f, 900f);
        ApplySprite(explosionImage, explosionSprite, true);
        explosionImage.gameObject.SetActive(false);

        stageRoot.SetSiblingIndex(0);
        kissRoot.SetSiblingIndex(1);
        effectRoot.SetSiblingIndex(2);
        uiRoot.SetAsLastSibling();
    }

    private Image CreateSpeedLines()
    {
        Image image = CreateImage("SpeedLines", effectRoot, Color.white);
        Stretch(image.rectTransform);
        image.sprite = speedLineSprite != null ? speedLineSprite : GenerateSpeedLineSprite();
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.gameObject.SetActive(false);
        return image;
    }

    private static void ApplySprite(Image image, Sprite sprite, bool preserveAspect)
    {
        if (sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
    }

    private Sprite GenerateSpeedLineSprite()
    {
        const int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = Color.clear;
        Color white = Color.white;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                Vector2 direction = p - center;
                float distance = direction.magnitude;
                float angle = Mathf.Atan2(direction.y, direction.x);
                float stripe = Mathf.Abs(Mathf.Sin(angle * 18f));
                float edge = Mathf.InverseLerp(90f, 260f, distance);
                bool isLine = stripe > 0.92f && edge > 0.1f;
                texture.SetPixel(x, y, isLine ? new Color(white.r, white.g, white.b, edge) : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 position,
        Sprite normalSprite,
        Sprite hoverSprite,
        Sprite pressedSprite,
        TMP_FontAsset font)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 86f);
        rect.anchoredPosition = position;

        Image image = gameObject.GetComponent<Image>();
        image.sprite = normalSprite;
        image.type = normalSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = normalSprite != null ? Color.white : new Color(0.18f, 0.9f, 1f, 0.92f);

        Button button = gameObject.GetComponent<Button>();
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
        else
        {
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.45f, 1f, 1f, 1f);
            colors.pressedColor = new Color(1f, 0.95f, 0.25f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }

        TMP_Text text = CreateText("Label", gameObject.transform, label, 42, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyFont(text, font);
        text.color = new Color(0.08f, 0.04f, 0.62f);
        Stretch(text.rectTransform);
        return button;
    }

    private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text != null && font != null)
        {
            text.font = font;
        }
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, int size, FontStyles style, TextAlignmentOptions anchor)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(600f, 120f);
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
    }

}
