using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanceManager))]
public class DanceManagerEditor : Editor
{
    private DanceManager _manager;

    private void OnEnable()
    {
        _manager = (DanceManager)target;
        // 에디터 업데이트 콜백 등록
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        // 해제
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        // Inspector를 계속 갱신
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터
        DrawDefaultInspector();
        EditorGUILayout.Space();

        // Start / Pause 버튼
        if (!_manager.IsPlaying)
        {
            if (GUILayout.Button("Start"))
            {
                _manager.StartPlayback();
            }
        }
        else
        {
            if (GUILayout.Button("Pause"))
            {
                _manager.PausePlayback();
            }
        }

        EditorGUILayout.Space();
        // 현재 프레임 인덱스 표시
        EditorGUILayout.LabelField("Current Frame", _manager.CurrentFrameIndex.ToString());
    }
}
