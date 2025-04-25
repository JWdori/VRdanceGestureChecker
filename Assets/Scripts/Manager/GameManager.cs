using Bhaptics.SDK2;
using UnityEngine;
using UnityEngine.XR;

public class GameManager : MonoBehaviour
{
    public enum GameState { Idle, Dance }

    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    var go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    public GameState CurrentState { get; private set; } = GameState.Idle;
    public string HapticFeedback { get; private set; } = "0";

    private void Awake()
    {
        
    XRSettings.useOcclusionMesh = false;
        
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void SetState(GameState state)
    {
        CurrentState = state;
        if (state == GameState.Idle)
        {
            //HapticFeedback = "0";
            BhapticsLibrary.StopAll();

        }
    }
    //
    public void SetHapticFeedback(string feedbackName)
    {
        if (CurrentState == GameState.Dance)
        {
            HapticFeedback = feedbackName;
        }
    }
}