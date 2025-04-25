using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class HumanoidDataPlayer : MonoBehaviour
{
    [Header("Upload CSV file")]
    [SerializeField] private TextAsset csvData;
    [SerializeField] private bool loopPlayback = true;
    [SerializeField] private float playbackSpeed = 1.0f;

    private Animator animator;
    // Bone 이름(string) → Transform 매핑
    private Dictionary<string, Transform> boneMap;
    // Bone 이름 → CSV에서 해당 뼈의 "_RotX" 컬럼 인덱스
    private Dictionary<string, int> rotColumnIndex;

    private class FrameData
    {
        public float time;
        public Dictionary<string, Quaternion> rotations;
    }

    private List<FrameData> frames;
    private float playbackTimer = 0f;
    private int currentFrameIndex = 0;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogError("Humanoid Animator is required.");
            return;
        }

        // 모든 HumanBodyBones enum 값 순회하며 Transform 가져오기
        boneMap = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone == HumanBodyBones.LastBone) continue;
            Transform t = animator.GetBoneTransform(bone);
            if (t != null && !boneMap.ContainsKey(bone.ToString()))
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
        animator.enabled = false; // 내장 애니메이터 비활성화
    }

    private void ParseCSV()
    {
        var lines = csvData.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return;

        // 헤더 파싱: "BoneName_RotX" 컬럼 인덱스를 찾는다
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

        // 각 데이터 라인에서 시간과 뼈 회전값 읽기
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

            // 찾은 컬럼 인덱스로부터 X,Y,Z,W 순서로 Quaternion 생성
            foreach (var kv in rotColumnIndex)
            {
                int idx = kv.Value;
                if (idx + 3 < tokens.Length)
                {
                    float rx = float.Parse(tokens[idx],     CultureInfo.InvariantCulture);
                    float ry = float.Parse(tokens[idx + 1], CultureInfo.InvariantCulture);
                    float rz = float.Parse(tokens[idx + 2], CultureInfo.InvariantCulture);
                    float rw = float.Parse(tokens[idx + 3], CultureInfo.InvariantCulture);
                    frame.rotations[kv.Key] = new Quaternion(rx, ry, rz, rw);
                }
            }

            frames.Add(frame);
        }
    }

    void Update()
    {
        if (frames == null || frames.Count == 0) return;

        // 시간 흘러가게
        playbackTimer += Time.deltaTime * playbackSpeed;
        float endTime = frames[frames.Count - 1].time;
        if (playbackTimer > endTime)
        {
            playbackTimer = loopPlayback ? frames[0].time : endTime;
            currentFrameIndex = loopPlayback ? 0 : frames.Count - 1;
        }
        while (currentFrameIndex < frames.Count - 1 &&
               frames[currentFrameIndex + 1].time <= playbackTimer)
        {
            currentFrameIndex++;
        }

        ApplyFrame(frames[currentFrameIndex]);
    }

    private void ApplyFrame(FrameData frame)
    {
        foreach (var kv in frame.rotations)
        {
            if (boneMap.TryGetValue(kv.Key, out Transform t))
                t.localRotation = kv.Value;
        }
    }
}
