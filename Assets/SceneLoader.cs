using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private InputActionReference aButtonAction; // A 버튼 입력 액션
    [SerializeField] private UnityEvent onAPressed; // 인스펙터에서 함수 할당 가능

    void Start()
{
    XRSettings.useOcclusionMesh = false;
}
    void OnEnable()
    {
        if (aButtonAction != null && aButtonAction.action != null)
        {
            aButtonAction.action.performed += OnAButtonPressed;
            aButtonAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (aButtonAction != null && aButtonAction.action != null)
        {
            aButtonAction.action.performed -= OnAButtonPressed;
            aButtonAction.action.Disable();
        }
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        Debug.Log("A 버튼이 눌렸습니다!");
        LoadNextScene(); // A 버튼을 누르면 씬 이동 실행
    }

    public void LoadNextScene()
    {
        // 현재 씬 이름을 먼저 가져옴
        string currentScene = SceneManager.GetActiveScene().name;
        string nextScene = "";

        // 현재 씬에 따라 다음 씬 결정
        if (currentScene == "start")
            nextScene = "test";
        else if (currentScene == "test")
            nextScene = "concert";
        else if (currentScene == "concert")
            nextScene = "start";
        else
        {
            Debug.LogWarning($"현재 씬 '{currentScene}'은 정의되지 않았습니다.");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(nextScene))
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            Debug.LogWarning($"'{nextScene}' 씬을 찾을 수 없습니다. Build Settings에 추가했는지 확인하세요.");
        }
    }
}
