using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    private const int StepCount = 3;
    private const float PerfectWindow = 0.08f;
    private const float GoodWindow = 0.25f;
    private const float MissWindow = 0.45f;
    private const long StepBaseScore = 100000;

    private readonly List<StepResult> results = new List<StepResult>();
    private readonly string[] stepNames = { "LOOK", "BREATH", "KISS" };
    private readonly string[] waitFaces = { ". .", "///", "..." };
    private readonly string[] cueFaces = { "< >", "<3 >", "<3 <3" };

    private Canvas canvas;
    private RectTransform stageRoot;
    private RectTransform effectRoot;
    private TMP_Text titleText;
    private TMP_Text phaseText;
    private TMP_Text cueText;
    private TMP_Text judgeText;
    private TMP_Text scoreText;
    private TMP_Text breakdownText;
    private TMP_Text promptText;
    private TMP_Text leftFace;
    private TMP_Text rightFace;
    private Image flashImage;
    private Image speedLineImage;
    private Image kissCutImage;
    private Image explosionImage;
    private RectTransform cameraRig;
    private Vector2 cameraRigBasePosition;
    private Vector3 cameraRigBaseScale;
    private bool running;
    private bool waitingForInput;
    private bool cueShown;
    private float cueShownAt;
    private int currentStep;
    private Coroutine shakeRoutine;
    private Coroutine scoreRoutine;
    private Coroutine floatingScoreRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
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
        BuildUi();
        ShowTitle();
    }

    private void Update()
    {
        if (WasSubmitPressed())
        {
            if (!running)
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
        results.Clear();
        ResetEffects();

        titleText.text = "";
        scoreText.text = "";
        breakdownText.text = "";
        judgeText.text = "";
        promptText.text = "";
        StopResultEffects();
        phaseText.text = "READY";
        SetFace(". .", ". .");
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
        judgeText.text = "";
        phaseText.text = $"{stepNames[stepIndex]}  {stepIndex + 1}/{StepCount}";
        SetFace(waitFaces[stepIndex], waitFaces[stepIndex]);

        float waitSeconds = Random.Range(0.45f, 0.95f);
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
        cueText.text = "!";
        SetFace(cueFaces[stepIndex], cueFaces[stepIndex]);
        PlayCuePop(stepIndex);

        while (Time.time - cueShownAt < MissWindow && waitingForInput)
        {
            PulseStage(1.01f + Mathf.Sin(Time.time * 18f) * 0.01f);
            yield return null;
        }

        if (waitingForInput)
        {
            RegisterJudge(JudgeResult.Miss, MissWindow);
        }
    }

    private void ResolveCurrentStep()
    {
        if (!cueShown)
        {
            RegisterJudge(JudgeResult.Flying, -1f);
            return;
        }

        float error = Time.time - cueShownAt;
        if (error <= PerfectWindow)
        {
            RegisterJudge(JudgeResult.Perfect, error);
        }
        else if (error <= GoodWindow)
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
        results.Add(new StepResult { Judge = judge, ErrorSeconds = errorSeconds });
        judgeText.text = GetJudgeLabel(judge);
        judgeText.color = GetJudgeColor(judge);
        PlayImpact(judge, currentStep);
    }

    private IEnumerator PlayFinalSequence()
    {
        JudgeResult finalJudge = results.Count > 0 ? results[results.Count - 1].Judge : JudgeResult.Miss;
        bool success = finalJudge == JudgeResult.Perfect || finalJudge == JudgeResult.Good;
        phaseText.text = success ? "KISS!!" : "OH NO...";
        promptText.text = "";
        cueText.text = "";

        if (success)
        {
            kissCutImage.gameObject.SetActive(true);
            kissCutImage.color = new Color(1f, 0.35f, 0.65f, 0.8f);
            yield return new WaitForSeconds(0.18f);
            explosionImage.gameObject.SetActive(true);
            explosionImage.color = finalJudge == JudgeResult.Perfect
                ? new Color(1f, 0.95f, 0.15f, 0.9f)
                : new Color(1f, 0.45f, 0.8f, 0.75f);
            PlayImpact(finalJudge, 2);
            yield return new WaitForSeconds(0.28f);
            yield return FlashToWhite(finalJudge == JudgeResult.Perfect ? 0.95f : 0.5f);
        }
        else
        {
            SetFace("x x", "x x");
            yield return new WaitForSeconds(0.45f);
        }

        kissCutImage.gameObject.SetActive(false);
        explosionImage.gameObject.SetActive(false);
    }

    private void ShowTitle()
    {
        running = false;
        waitingForInput = false;
        phaseText.text = "";
        cueText.text = "";
        judgeText.text = "";
        scoreText.text = "";
        breakdownText.text = "";
        titleText.text = "KISS TIMING";
        promptText.text = "SPACE / CLICK";
        SetFace(". .", ". .");
    }

    private void ShowResult()
    {
        long score = CalculateScore();
        titleText.text = "";
        phaseText.text = "RESULT";
        judgeText.text = GetResultHeadline();
        judgeText.color = GetJudgeColor(GetBestFinalJudge());
        scoreText.text = "0 PTS";
        breakdownText.text = BuildResultBreakdown();
        promptText.text = "SPACE / CLICK RETRY";
        SetFace("<3 <3", "<3 <3");
        scoreRoutine = StartCoroutine(CountUpScore(score));
        floatingScoreRoutine = StartCoroutine(SpawnScoreTexts(score));
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

    private static double CalculateStepScore(StepResult result, int stepIndex, int successChain)
    {
        float timingMultiplier = GetTimingMultiplier(result);
        float stepMultiplier = stepIndex switch
        {
            0 => 1f,
            1 => 3f,
            _ => 10f
        };
        float chainMultiplier = 1f + (successChain - 1) * 0.5f;
        float nearZeroBonus = Mathf.Max(0f, 1f - Mathf.Abs(result.ErrorSeconds) / GoodWindow) * 9999f;

        return StepBaseScore * timingMultiplier * stepMultiplier * chainMultiplier + nearZeroBonus;
    }

    private static float GetTimingMultiplier(StepResult result)
    {
        if (result.Judge == JudgeResult.Perfect)
        {
            float accuracy = 1f - Mathf.Clamp01(result.ErrorSeconds / PerfectWindow);
            return Mathf.Lerp(100f, 300f, accuracy);
        }

        if (result.Judge == JudgeResult.Good)
        {
            float goodRange = GoodWindow - PerfectWindow;
            float accuracy = 1f - Mathf.Clamp01((result.ErrorSeconds - PerfectWindow) / goodRange);
            return Mathf.Lerp(10f, 80f, accuracy);
        }

        return 0f;
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
        if (IsAllPerfect())
        {
            return "PERFECT KISS!!!";
        }

        foreach (StepResult result in results)
        {
            if (result.Judge == JudgeResult.Flying || result.Judge == JudgeResult.Miss)
            {
                return GetJudgeLabel(result.Judge);
            }
        }

        return "GOOD KISS!";
    }

    private JudgeResult GetBestFinalJudge()
    {
        if (IsAllPerfect())
        {
            return JudgeResult.Perfect;
        }

        foreach (StepResult result in results)
        {
            if (result.Judge == JudgeResult.Flying || result.Judge == JudgeResult.Miss)
            {
                return result.Judge;
            }
        }

        return JudgeResult.Good;
    }

    private void PlayCuePop(int stepIndex)
    {
        float scale = 1.1f + stepIndex * 0.12f;
        cueText.transform.localScale = Vector3.one * scale;
        StartCoroutine(ScaleBack(cueText.rectTransform, scale, 1f, 0.12f));
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
    }

    private void SpawnFloatingText(string value)
    {
        TMP_Text text = CreateText("FloatScore", effectRoot, value, 38, FontStyles.Bold, TextAlignmentOptions.Center);
        text.color = Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(0.55f, 1f), Random.Range(0.9f, 1f));
        RectTransform rect = text.rectTransform;
        rect.sizeDelta = new Vector2(360f, 80f);
        rect.anchoredPosition = new Vector2(Random.Range(-430f, 430f), Random.Range(-210f, 210f));
        rect.localScale = Vector3.one * Random.Range(0.85f, 1.35f);
        StartCoroutine(FloatAndFade(text));
    }

    private IEnumerator FloatAndFade(TMP_Text text)
    {
        RectTransform rect = text.rectTransform;
        Vector2 start = rect.anchoredPosition;
        Vector2 end = start + new Vector2(Random.Range(-30f, 30f), Random.Range(80f, 150f));
        Color color = text.color;
        float duration = 0.65f;
        float startAt = Time.time;
        while (Time.time - startAt < duration)
        {
            float t = (Time.time - startAt) / duration;
            rect.anchoredPosition = Vector2.Lerp(start, end, t);
            rect.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.35f, Mathf.Sin(t * Mathf.PI));
            text.color = new Color(color.r, color.g, color.b, 1f - t);
            yield return null;
        }

        Destroy(text.gameObject);
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

        for (int i = effectRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = effectRoot.GetChild(i);
            if (child.name.StartsWith("FloatScore"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void SetFace(string left, string right)
    {
        leftFace.text = left;
        rightFace.text = right;
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

    private static bool WasSubmitPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
               || (mouse != null && mouse.leftButton.wasPressedThisFrame);
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
        effectRoot = CreateRect("Effects", canvas.transform);
        Stretch(effectRoot);

        Image background = CreateImage("Background", stageRoot, new Color(0.22f, 0.02f, 0.78f));
        Stretch(background.rectTransform);

        Image leftPanel = CreateImage("LeftCharacter", stageRoot, new Color(1f, 0.58f, 0.62f, 0.96f));
        leftPanel.rectTransform.anchorMin = new Vector2(0f, 0f);
        leftPanel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        leftPanel.rectTransform.offsetMin = new Vector2(0f, 0f);
        leftPanel.rectTransform.offsetMax = new Vector2(150f, 0f);

        Image rightPanel = CreateImage("RightCharacter", stageRoot, new Color(1f, 0.62f, 0.54f, 0.96f));
        rightPanel.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rightPanel.rectTransform.anchorMax = new Vector2(1f, 1f);
        rightPanel.rectTransform.offsetMin = new Vector2(-150f, 0f);
        rightPanel.rectTransform.offsetMax = new Vector2(0f, 0f);

        TMP_Text kissMark = CreateText("CenterKissMark", stageRoot, "KISS", 160, FontStyles.Bold, TextAlignmentOptions.Center);
        kissMark.color = new Color(0.12f, 0.02f, 0.85f);
        kissMark.rectTransform.anchoredPosition = new Vector2(0f, -65f);
        kissMark.rectTransform.sizeDelta = new Vector2(260f, 260f);

        leftFace = CreateText("LeftFace", stageRoot, ". .", 120, FontStyles.Bold, TextAlignmentOptions.Center);
        leftFace.color = new Color(0.05f, 0.02f, 0.85f);
        leftFace.rectTransform.anchorMin = new Vector2(0.08f, 0.42f);
        leftFace.rectTransform.anchorMax = new Vector2(0.45f, 0.7f);
        leftFace.rectTransform.offsetMin = Vector2.zero;
        leftFace.rectTransform.offsetMax = Vector2.zero;

        rightFace = CreateText("RightFace", stageRoot, ". .", 120, FontStyles.Bold, TextAlignmentOptions.Center);
        rightFace.color = new Color(0.05f, 0.02f, 0.85f);
        rightFace.rectTransform.anchorMin = new Vector2(0.55f, 0.42f);
        rightFace.rectTransform.anchorMax = new Vector2(0.92f, 0.7f);
        rightFace.rectTransform.offsetMin = Vector2.zero;
        rightFace.rectTransform.offsetMax = Vector2.zero;

        titleText = CreateText("TitleText", effectRoot, "", 96, FontStyles.Bold, TextAlignmentOptions.Center);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 245f);
        titleText.rectTransform.sizeDelta = new Vector2(1000f, 160f);

        phaseText = CreateText("PhaseText", effectRoot, "", 58, FontStyles.Bold, TextAlignmentOptions.Center);
        phaseText.rectTransform.anchoredPosition = new Vector2(0f, 370f);
        phaseText.rectTransform.sizeDelta = new Vector2(850f, 100f);

        cueText = CreateText("CueText", effectRoot, "", 210, FontStyles.Bold, TextAlignmentOptions.Center);
        cueText.color = new Color(1f, 0.95f, 0.2f);
        cueText.rectTransform.anchoredPosition = new Vector2(0f, 80f);
        cueText.rectTransform.sizeDelta = new Vector2(260f, 260f);

        judgeText = CreateText("JudgeText", effectRoot, "", 78, FontStyles.Bold, TextAlignmentOptions.Center);
        judgeText.rectTransform.anchoredPosition = new Vector2(0f, -295f);
        judgeText.rectTransform.sizeDelta = new Vector2(1100f, 120f);

        scoreText = CreateText("ScoreText", effectRoot, "", 86, FontStyles.Bold, TextAlignmentOptions.Center);
        scoreText.color = new Color(1f, 1f, 1f);
        scoreText.rectTransform.anchoredPosition = new Vector2(0f, 35f);
        scoreText.rectTransform.sizeDelta = new Vector2(1300f, 140f);

        breakdownText = CreateText("BreakdownText", effectRoot, "", 32, FontStyles.Bold, TextAlignmentOptions.Center);
        breakdownText.color = new Color(1f, 0.92f, 0.98f);
        breakdownText.rectTransform.anchoredPosition = new Vector2(0f, -112f);
        breakdownText.rectTransform.sizeDelta = new Vector2(1200f, 220f);

        promptText = CreateText("PromptText", effectRoot, "", 44, FontStyles.Bold, TextAlignmentOptions.Center);
        promptText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        promptText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        promptText.rectTransform.anchoredPosition = new Vector2(0f, 82f);
        promptText.rectTransform.sizeDelta = new Vector2(650f, 90f);

        speedLineImage = CreateSpeedLines();
        flashImage = CreateImage("WhiteFlash", effectRoot, Color.clear);
        Stretch(flashImage.rectTransform);
        flashImage.gameObject.SetActive(false);

        kissCutImage = CreateImage("KissCut", effectRoot, new Color(1f, 0.35f, 0.65f, 0.8f));
        kissCutImage.rectTransform.anchorMin = new Vector2(0.05f, 0.06f);
        kissCutImage.rectTransform.anchorMax = new Vector2(0.95f, 0.94f);
        kissCutImage.rectTransform.offsetMin = Vector2.zero;
        kissCutImage.rectTransform.offsetMax = Vector2.zero;
        kissCutImage.gameObject.SetActive(false);

        explosionImage = CreateImage("Explosion", effectRoot, new Color(1f, 0.95f, 0.2f, 0.8f));
        explosionImage.rectTransform.sizeDelta = new Vector2(900f, 900f);
        explosionImage.gameObject.SetActive(false);
    }

    private Image CreateSpeedLines()
    {
        Image image = CreateImage("SpeedLines", effectRoot, Color.white);
        Stretch(image.rectTransform);
        image.sprite = GenerateSpeedLineSprite();
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.gameObject.SetActive(false);
        return image;
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

}
