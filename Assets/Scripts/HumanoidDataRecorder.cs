using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

public class HumanoidDataRecorder : MonoBehaviour
{
    // 120fps
    [SerializeField] private int recordFrameRate = 120;

    [SerializeField] private bool isRecording = false;
    
    // auto start
    [SerializeField] private bool autoStartRecording = false;

    [SerializeField] private float recordedTime = 0f;
    [SerializeField] private int recordedFrameCount = 0;

    private StringBuilder csvBuilder;
    // file path
    private string relativeFolderPath = "Recordings";

    private Animator animator;
    private Dictionary<HumanBodyBones, Transform> boneDict = new Dictionary<HumanBodyBones, Transform>();

    private HumanBodyBones[] targetBones = new HumanBodyBones[]
    {
        HumanBodyBones.Hips,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.Neck,
        HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.LeftThumbProximal,
        HumanBodyBones.LeftThumbIntermediate,
        HumanBodyBones.LeftThumbDistal,
        HumanBodyBones.LeftIndexProximal,
        HumanBodyBones.LeftIndexIntermediate,
        HumanBodyBones.LeftIndexDistal,
        HumanBodyBones.LeftMiddleProximal,
        HumanBodyBones.LeftMiddleIntermediate,
        HumanBodyBones.LeftMiddleDistal,
        HumanBodyBones.LeftRingProximal,
        HumanBodyBones.LeftRingIntermediate,
        HumanBodyBones.LeftRingDistal,
        HumanBodyBones.LeftLittleProximal,
        HumanBodyBones.LeftLittleIntermediate,
        HumanBodyBones.LeftLittleDistal,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand,
        HumanBodyBones.RightThumbProximal,
        HumanBodyBones.RightThumbIntermediate,
        HumanBodyBones.RightThumbDistal,
        HumanBodyBones.RightIndexProximal,
        HumanBodyBones.RightIndexIntermediate,
        HumanBodyBones.RightIndexDistal,
        HumanBodyBones.RightMiddleProximal,
        HumanBodyBones.RightMiddleIntermediate,
        HumanBodyBones.RightMiddleDistal,
        HumanBodyBones.RightRingProximal,
        HumanBodyBones.RightRingIntermediate,
        HumanBodyBones.RightRingDistal,
        HumanBodyBones.RightLittleProximal,
        HumanBodyBones.RightLittleIntermediate,
        HumanBodyBones.RightLittleDistal,

        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.RightFoot
    };
    // 4 quaternion + 3 position
    private const int valuesPerBone = 7;
    // coroution
    private Coroutine recordingCoroutine;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogError("Humanoid Animator is required.");
            return;
        }

        foreach (HumanBodyBones bone in targetBones)
        {
            Transform t = animator.GetBoneTransform(bone);
            if (t == null)
            {
                Debug.LogWarning($"Animator could not find bone: {bone}");
                continue;
            }
            if (!boneDict.ContainsKey(bone))
                boneDict.Add(bone, t);
        }
        
        if(autoStartRecording)
        {
            StartRecording();
        }
    }

    // 
    public void StartRecording()
    {
        if (isRecording)
            return;

        isRecording = true;
        recordedTime = 0f;
        recordedFrameCount = 0;
        csvBuilder = new StringBuilder();

        // csv header
        List<string> header = new List<string> { "Frame", "Time" };
        foreach (HumanBodyBones bone in targetBones)
        {
            string boneName = bone.ToString();
            header.Add($"{boneName}_RotX");
            header.Add($"{boneName}_RotY");
            header.Add($"{boneName}_RotZ");
            header.Add($"{boneName}_RotW");
            header.Add($"{boneName}_PosX");
            header.Add($"{boneName}_PosY");
            header.Add($"{boneName}_PosZ");
        }
        csvBuilder.AppendLine(string.Join(",", header));

        recordingCoroutine = StartCoroutine(RecordRoutine());
        Debug.Log("Recording started");
    }

    private IEnumerator RecordRoutine()
    {
        // 
        float interval = 1f / recordFrameRate;

        while (isRecording)
        {
            recordedFrameCount++;
            recordedTime += interval;


            List<string> rowData = new List<string>();
            rowData.Add(recordedFrameCount.ToString());
            rowData.Add(recordedTime.ToString("F3", CultureInfo.InvariantCulture));

            foreach (HumanBodyBones bone in targetBones)
            {
                if (boneDict.TryGetValue(bone, out Transform t))
                {
                    Quaternion rot = t.localRotation;
                    Vector3 pos = t.localPosition;
                    rowData.Add(rot.x.ToString("F4", CultureInfo.InvariantCulture));
                    rowData.Add(rot.y.ToString("F4", CultureInfo.InvariantCulture));
                    rowData.Add(rot.z.ToString("F4", CultureInfo.InvariantCulture));
                    rowData.Add(rot.w.ToString("F4", CultureInfo.InvariantCulture));
                    rowData.Add(pos.x.ToString("F4", CultureInfo.InvariantCulture));
                    rowData.Add(pos.y.ToString("F4", CultureInfo.InvariantCulture));
                    rowData.Add(pos.z.ToString("F4", CultureInfo.InvariantCulture));
                }
                else
                {
                    for (int i = 0; i < valuesPerBone; i++)
                        rowData.Add("0");
                }
            }

            csvBuilder.AppendLine(string.Join(",", rowData));
            yield return new WaitForSeconds(interval);
        }
    }

// save csv
    public void StopRecording()
    {
        if (!isRecording)
            return;

        isRecording = false;
        if (recordingCoroutine != null)
            StopCoroutine(recordingCoroutine);

        string folderPath = Path.Combine(Application.dataPath, relativeFolderPath);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
        string filePath = Path.Combine(folderPath, fileName);

        try
        {
            File.WriteAllText(filePath, csvBuilder.ToString());
            Debug.Log($"Recording stopped. CSV saved: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving CSV: " + e.Message);
        }

        recordedTime = 0f;
        recordedFrameCount = 0;
        csvBuilder = null;
    }
}
