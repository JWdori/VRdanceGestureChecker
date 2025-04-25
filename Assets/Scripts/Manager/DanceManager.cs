using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// CSV 기반 댄스 애니메이션 재생 및 특정 프레임에서 본 유사도 검사
/// </summary>
public class DanceManager : MonoBehaviour
{
    [Header("Upload CSV file")]
    [SerializeField] private TextAsset csvData;
    [SerializeField] private float playbackSpeed = 1.0f;

    [Header("Similarity Checker")]
    public BoneSimilarityChecker boneSimilarityChecker;

    private Animator animator;
    private Dictionary<string, Transform> boneMap;
    private Dictionary<string, int> rotColumnIndex;

    private class FrameData
    {
        public float time;
        public Dictionary<string, Quaternion> rotations;
    }
    private List<FrameData> frames;
    private float playbackTimer = 0f;
    private int currentFrameIndex = 0;
    private bool _isPlaying = false;
    private bool _hasCompleted = false;
    public bool IsPlaying => _isPlaying;
    public int CurrentFrameIndex => currentFrameIndex;

    // 멈춰야 할 프레임 번호들
    private readonly HashSet<int> _pauseFrames = new HashSet<int> { 139, 234, 328, 458, 592, 680, 760, 890 };
    // 이미 처리한 프레임 기록
    private HashSet<int> _triggeredFrames = new HashSet<int>();

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogError("Humanoid Animator is required.");
            return;
        }

        // 본 매핑
        boneMap = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone == HumanBodyBones.LastBone) continue;
            var t = animator.GetBoneTransform(bone);
            if (t != null)
                boneMap[bone.ToString()] = t;
        }

        if (csvData == null)
        {
            Debug.LogError("CSV file is not assigned.");
            return;
        }
        ParseCSV();
        if (frames == null || frames.Count == 0)
        {
            Debug.LogError("No frame data loaded.");
            return;
        }

        playbackTimer = frames[0].time;
        animator.enabled = false;
    }

    private void ParseCSV()
    {
        var lines = csvData.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return;

        var header = lines[0].Split(',');
        rotColumnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++)
        {
            var col = header[i].Trim();
            if (col.EndsWith("_RotX", StringComparison.OrdinalIgnoreCase))
            {
                var boneName = col.Substring(0, col.Length - 5);
                if (boneMap.ContainsKey(boneName))
                    rotColumnIndex[boneName] = i;
            }
        }

        frames = new List<FrameData>();
        for (int r = 1; r < lines.Length; r++)
        {
            var tokens = lines[r].Split(',');
            if (tokens.Length < 2) continue;
            if (!float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float time))
                continue;

            var frame = new FrameData
            {
                time = time,
                rotations = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase)
            };
            foreach (var kv in rotColumnIndex)
            {
                int idx = kv.Value;
                if (idx + 3 < tokens.Length)
                {
                    frame.rotations[kv.Key] = new Quaternion(
                        float.Parse(tokens[idx],     CultureInfo.InvariantCulture),
                        float.Parse(tokens[idx + 1], CultureInfo.InvariantCulture),
                        float.Parse(tokens[idx + 2], CultureInfo.InvariantCulture),
                        float.Parse(tokens[idx + 3], CultureInfo.InvariantCulture)
                    );
                }
            }
            frames.Add(frame);
        }
    }

    void Update()
    {
        if (!_isPlaying || frames == null || frames.Count == 0 || _hasCompleted) return;

        playbackTimer += Time.deltaTime * playbackSpeed;
        // playbackTimer 업데이트 아래
        while (currentFrameIndex < frames.Count - 1 
                && frames[currentFrameIndex + 1].time <= playbackTimer)
        {
            int nextIdx = currentFrameIndex + 1;
            // 아직 멈춰야 할 pause‐frame인데, trigger가 안 된 상태라면
            if (_pauseFrames.Contains(nextIdx) && !_triggeredFrames.Contains(nextIdx))
            {
                // pause‐frame에서 멈추고 Update 루프 탈출
                currentFrameIndex = nextIdx;
                break;
            }
            // 아니면 안전하게 다음 프레임으로
            currentFrameIndex = nextIdx;
        }

        ApplyFrame(frames[currentFrameIndex]);

        // pause-frame에 도달했으면 처리
        if (_pauseFrames.Contains(currentFrameIndex) && !_triggeredFrames.Contains(currentFrameIndex))
        {
            _triggeredFrames.Add(currentFrameIndex);
            StartCoroutine(HandlePauseAtFrame());
            return;
        }

        // 마지막 프레임 시, 리셋 후 일시정지
        if (currentFrameIndex == frames.Count - 1)
        {
            ResetPlayback();
            PausePlayback();
        }
    }

    private void ApplyFrame(FrameData frame)
    {
        foreach (var kv in frame.rotations)
        {
            if (boneMap.TryGetValue(kv.Key, out Transform t))
                t.localRotation = kv.Value;
        }
    }

    private IEnumerator HandlePauseAtFrame()
    {
        if (_hasCompleted) yield break;    // 완료 후엔 아예 빠져나감
        _isPlaying = false;
        float similarity = 0f;
        float timer = 0f;
        const float TIMEOUT = 20f;
        bool passed = false;

        // 40초 안에 similarity ≤ 0.06 이 되면 passed=true
        while (timer < TIMEOUT)
        {
            if (_hasCompleted)
                yield break;
            similarity = boneSimilarityChecker.CalculateAndUpdate();
            if (similarity <= 0.06f)
            {
                passed = true;
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // passed == true 일 때만 사운드 재생 (타임아웃 시 소리 없이 건너뜀)
        if (passed)
        {
            if (Random.value < 0.3f)
                AudioManager.Instance.PlayExcellentSound();
            else
                AudioManager.Instance.PlayGoodSound();
        }else{
                    AudioManager.Instance.FailSound();

        }
        GameManager.Instance.SetState(GameManager.GameState.Idle);

        yield return new WaitForSeconds(2f);

        boneSimilarityChecker.NextReferencePose();
        GameManager.Instance.SetState(GameManager.GameState.Dance);
        _isPlaying = true;

    }

    /// <summary>재생을 시작합니다.</summary>
    public void StartPlayback()
    {
        _hasCompleted = false;
        if (_isPlaying) return;
        _isPlaying = true;
        GameManager.Instance.SetState(GameManager.GameState.Dance);
        // 이미 처리된 프레임 기록 및 레퍼런스 인덱스 초기화
        //_triggeredFrames.Clear();
        //boneSimilarityChecker.ResetReferenceIndex();
    }

    /// <summary>일시정지합니다.</summary>
    public void PausePlayback()
    {
        if (!_isPlaying) return;
        _isPlaying = false;
        GameManager.Instance.SetState(GameManager.GameState.Idle);
    }

    /// <summary>Stop과 동일하게 일시정지 처리합니다.</summary>
    public void StopPlayback() => PausePlayback();

    /// <summary>전체를 처음 프레임으로 되돌립니다.</summary>
    private void ResetPlayback()
    {
        playbackTimer = frames[0].time;
        currentFrameIndex = 0;
        ApplyFrame(frames[0]);
        _triggeredFrames.Clear();
        boneSimilarityChecker.ResetReferenceIndex();
        _hasCompleted = true; 
    }
}
