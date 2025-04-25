// BoneSimilarityChecker.cs
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BoneSimilarityChecker : MonoBehaviour
{
    [Header("Paused-State Animator")]
    public Animator pausedAnimator;

    [Header("Reference CSVs")]
    [Tooltip("인스펙터에서 순서대로 할당할 CSV TextAsset 리스트")]
    public List<TextAsset> referenceCSVs;

    [Header("UI Text (optional)")]
    public TextMeshProUGUI similarityText;

    [Header("Real-Time Update")]
    public bool realTimeUpdate = true;

    /// <summary>마지막 계산된 평균 거리</summary>
    public float LastDistance { get; private set; }

    // 내부 참조 포즈 위치 목록 (각 Bone → world 위치)
    private List<Dictionary<HumanBodyBones, Vector3>> referencePoses;
    // 현재 비교 인덱스
    private int currentIndex = 0;

    // 비교용 본 목록
    private static readonly HumanBodyBones[] bonesToRecord =
    {
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand
    };

    /// <summary>레퍼런스 인덱스를 처음(0)으로 리셋합니다.</summary>
    public void ResetReferenceIndex()
    {
        currentIndex = 0;
    }

    void Awake()
    {
        // CSV 파싱: Bone,PosX,PosY,PosZ 형식
        referencePoses = new List<Dictionary<HumanBodyBones, Vector3>>();
        if (referenceCSVs != null)
        {
            foreach (var csv in referenceCSVs)
            {
                var lines = csv.text
                    .Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                var dict = new Dictionary<HumanBodyBones, Vector3>();
                for (int i = 1; i < lines.Length; i++)
                {
                    var tokens = lines[i].Split(',');
                    if (tokens.Length < 4) continue;

                    if (!System.Enum.TryParse(tokens[0], out HumanBodyBones bone))
                        continue;

                    if (float.TryParse(tokens[1], out float x) &&
                        float.TryParse(tokens[2], out float y) &&
                        float.TryParse(tokens[3], out float z))
                    {
                        dict[bone] = new Vector3(x, y, z);
                    }
                }
                referencePoses.Add(dict);
            }
        }
    }

    void Update()
    {
        // 게임 상태가 Idle 이면 어디서도 검사하지 않음
        if (GameManager.Instance.CurrentState != GameManager.GameState.Dance)
            return;

        if (realTimeUpdate)
        {
            CalculateAndUpdate();
        }
    }

    /// <summary>
    /// 현재 pausedAnimator의 월드 위치와
    /// 참조 포즈의 위치 간 거리를 비교해 평균 거리를 LastDistance에 저장합니다.
    /// </summary>
    public float CalculateAndUpdate()
    {
        // 다시 한번 안전장치로 Idle 상태면 이전 값 리턴
        if (GameManager.Instance.CurrentState == GameManager.GameState.Idle)
            return LastDistance;

        if (pausedAnimator == null || referencePoses == null || referencePoses.Count == 0)
        {
            LastDistance = 0f;
            UpdateText();
            return LastDistance;
        }

        var refPose = referencePoses[Mathf.Min(currentIndex, referencePoses.Count - 1)];

        float sum = 0f;
        int count = 0;
        foreach (var bone in bonesToRecord)
        {
            var t = pausedAnimator.GetBoneTransform(bone);
            if (t == null || !refPose.ContainsKey(bone))
                continue;

            sum += Vector3.Distance(t.position, refPose[bone]);
            count++;
        }

        LastDistance = (count > 0) ? (sum / count) : 0f;
        UpdateText();
        return LastDistance;
    }

    private void UpdateText()
    {
        if (similarityText != null)
            similarityText.text = $"AvgDist: {LastDistance:F3}";
    }

    /// <summary>
    /// 현재 레퍼런스 포즈 인덱스를 다음으로 이동합니다.
    /// </summary>
    public void NextReferencePose()
    {
        if (referencePoses != null && currentIndex < referencePoses.Count - 1)
            currentIndex++;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (referencePoses == null || referencePoses.Count == 0) return;
        if (pausedAnimator == null) return;

        int idx = Mathf.Clamp(currentIndex, 0, referencePoses.Count - 1);
        var refPose = referencePoses[idx];
        const float gizmoSize = 0.02f;

        // CSV 저장 위치(노란)
        Gizmos.color = Color.yellow;
        foreach (var kv in refPose)
            Gizmos.DrawSphere(kv.Value, gizmoSize);

        // 실제 본 위치(파란)
        Gizmos.color = Color.cyan;
        foreach (var bone in bonesToRecord)
        {
            var t = pausedAnimator.GetBoneTransform(bone);
            if (t != null)
                Gizmos.DrawSphere(t.position, gizmoSize);
        }
    }
#endif
}
