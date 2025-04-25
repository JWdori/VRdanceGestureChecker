using UnityEngine;
using UnityEngine.InputSystem;
using Bhaptics.SDK2;
using System.Collections;

public class HapticManager : MonoBehaviour
{
    [SerializeField] private BoneSimilarityChecker boneSimilarityChecker;

    [Header("Feedback 반복 간격 (ms) — Rhythm 에만 적용")]
    [SerializeField] private float feedbackIntervalMs = 200f;

    private Coroutine _rhythmRoutine;
    private Coroutine _rhythmErrorRoutine;
    private Coroutine _rhythmGuidanceRoutine;

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        var kb = Keyboard.current;
        if (kb != null)
        {
            if      (kb.digit1Key.wasPressedThisFrame) Baseline();
            else if (kb.digit2Key.wasPressedThisFrame) Rhythm();
            else if (kb.digit3Key.wasPressedThisFrame) Error();
            else if (kb.digit4Key.wasPressedThisFrame) Guidance();
            else if (kb.digit5Key.wasPressedThisFrame) RhythmError();
            else if (kb.digit6Key.wasPressedThisFrame) RhythmGuidance();
        }
#endif

        if (GameManager.Instance.CurrentState == GameManager.GameState.Dance)
        {
            string hf = GameManager.Instance.HapticFeedback;

            // — Rhythm 단독 루프
            if (hf == nameof(Rhythm) && _rhythmRoutine == null)
                _rhythmRoutine = StartCoroutine(RhythmLoop());
            else if (hf != nameof(Rhythm) && _rhythmRoutine != null)
                StopRhythmRoutine();

            // — RhythmError (왼손만 루프)
            if (hf == nameof(RhythmError) && _rhythmErrorRoutine == null)
                _rhythmErrorRoutine = StartCoroutine(RhythmLoop()); // reuse same loop for left hand
            else if (hf != nameof(RhythmError) && _rhythmErrorRoutine != null)
                StopRhythmErrorRoutine();

            // — RhythmGuidance (왼손만 루프)
            if (hf == nameof(RhythmGuidance) && _rhythmGuidanceRoutine == null)
                _rhythmGuidanceRoutine = StartCoroutine(RhythmLoop());
            else if (hf != nameof(RhythmGuidance) && _rhythmGuidanceRoutine != null)
                StopRhythmGuidanceRoutine();

            // — Right-hand real-time error/guidance
            if (hf == nameof(RhythmError))
                ErrorRightOnly();
            if (hf == nameof(RhythmGuidance))
                GuidanceRightOnly();

            // — solo Error / Guidance 모드 (non-rhythm)
            if (hf == nameof(Error))
                Error();
            if (hf == nameof(Guidance))
                Guidance();
        }
        else
        {
            StopAllRhythmCoroutines();
        }
    }

    private void StopRhythmRoutine()
    {
        StopCoroutine(_rhythmRoutine);
        _rhythmRoutine = null;
    }
    private void StopRhythmErrorRoutine()
    {
        StopCoroutine(_rhythmErrorRoutine);
        _rhythmErrorRoutine = null;
    }
    private void StopRhythmGuidanceRoutine()
    {
        StopCoroutine(_rhythmGuidanceRoutine);
        _rhythmGuidanceRoutine = null;
    }
    private void StopAllRhythmCoroutines()
    {
        if (_rhythmRoutine        != null) StopRhythmRoutine();
        if (_rhythmErrorRoutine   != null) StopRhythmErrorRoutine();
        if (_rhythmGuidanceRoutine!= null) StopRhythmGuidanceRoutine();
    }

    private IEnumerator RhythmLoop()
    {
        float interval = feedbackIntervalMs / 1000f;
        while (true)
        {
            // 양손 리듬
            PlayMotorsBothHands(new int[] { 0, 0, 30 }, 100);
            yield return new WaitForSeconds(interval);
        }
    }

    public void Baseline()
    {
        GameManager.Instance.SetHapticFeedback(nameof(Baseline));
    }

    public void Rhythm()
    {
        GameManager.Instance.SetHapticFeedback(nameof(Rhythm));
        // 즉시 한 번, 그리고 루프가 돌면 반복
        PlayMotorsBothHands(new int[] { 0, 0, 30 }, 100);
    }

    public void Error()
    {
        GameManager.Instance.SetState(GameManager.GameState.Dance);
        GameManager.Instance.SetHapticFeedback(nameof(Error));
        int i = CalculateErrorIntensity();
        if (i > 0)
            PlayMotorsBothHands(new int[] { i, i, i }, 100);
    }

    public void Guidance()
    {
        GameManager.Instance.SetState(GameManager.GameState.Dance);
        GameManager.Instance.SetHapticFeedback(nameof(Guidance));
        int i = CalculateGuidanceIntensity();
        if (i > 0)
            PlayMotorsBothHands(new int[] { i, i, i }, 100);
    }

    public void RhythmError()
    {
        GameManager.Instance.SetState(GameManager.GameState.Dance);
        GameManager.Instance.SetHapticFeedback(nameof(RhythmError));
        // 즉시 왼손 리듬 + 오른손 에러
        PlayMotor(PositionType.ForearmL, new int[] { 0, 0, 30 }, 100);
        ErrorRightOnly();
    }

    public void RhythmGuidance()
    {
        GameManager.Instance.SetState(GameManager.GameState.Dance);
        GameManager.Instance.SetHapticFeedback(nameof(RhythmGuidance));
        // 즉시 왼손 리듬 + 오른손 가이던스
        PlayMotor(PositionType.ForearmL, new int[] { 0, 0, 30 }, 100);
        GuidanceRightOnly();
    }

    public void Exit()
    {
        GameManager.Instance.SetState(GameManager.GameState.Idle);
        GameManager.Instance.SetHapticFeedback("0");
        StopAllRhythmCoroutines();
    }

    // ───────── 헬퍼 ─────────

    private int CalculateErrorIntensity()
    {
        float d = boneSimilarityChecker.CalculateAndUpdate();
        if (d >= 0.3f)    return 40;
        if (d <= 0.07f)   return 0;
        float r = (d - 0.07f) / (0.3f - 0.07f);
        return Mathf.RoundToInt(Mathf.Pow(r, 2f) * 40f);
    }

    private int CalculateGuidanceIntensity()
    {
        float d = boneSimilarityChecker.CalculateAndUpdate();
        if (d <= 0.07f)   return 40;
        if (d >= 0.3f)    return 0;
        float r = (d - 0.07f) / (0.3f - 0.07f);
        return Mathf.RoundToInt(Mathf.Pow(1f - r, 2f) * 40f);
    }

    private void ErrorRightOnly()
    {
        int i = CalculateErrorIntensity();
        if (i > 0)
            PlayMotor(PositionType.ForearmR, new int[] { 0, i, 0 }, 100);
    }

    private void GuidanceRightOnly()
    {
        int i = CalculateGuidanceIntensity();
        if (i > 0)
            PlayMotor(PositionType.ForearmR, new int[] { 0, i, 0 }, 100);
    }

    private void PlayMotorsBothHands(int[] motors, int durationMs)
    {
        PlayMotor(PositionType.ForearmL, motors, durationMs);
        PlayMotor(PositionType.ForearmR, motors, durationMs);
    }

    private void PlayMotor(PositionType pos, int[] motors, int durationMs)
    {
        BhapticsLibrary.PlayMotors((int)pos, motors, durationMs);
    }
}
