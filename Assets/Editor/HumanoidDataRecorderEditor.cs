using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HumanoidDataRecorder))]
public class HumanoidDataRecorderEditor : Editor
{
    SerializedProperty isRecordingProp;
    SerializedProperty recordedTimeProp;
    SerializedProperty recordedFrameCountProp;
    
    void OnEnable()
    {
        isRecordingProp = serializedObject.FindProperty("isRecording");
        recordedTimeProp = serializedObject.FindProperty("recordedTime");
        recordedFrameCountProp = serializedObject.FindProperty("recordedFrameCount");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Recording Status", isRecordingProp.boolValue ? "Recording" : "Not Recording");
        EditorGUILayout.LabelField("Elapsed Time (sec)", recordedTimeProp.floatValue.ToString("F3"));
        EditorGUILayout.LabelField("Frame Count", recordedFrameCountProp.intValue.ToString());
        EditorGUILayout.Space();

        if (!isRecordingProp.boolValue)
        {
            if (GUILayout.Button("Start Recording"))
            {
                ((HumanoidDataRecorder)target).StartRecording();
            }
        }
        else
        {
            if (GUILayout.Button("Stop Recording"))
            {
                ((HumanoidDataRecorder)target).StopRecording();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
