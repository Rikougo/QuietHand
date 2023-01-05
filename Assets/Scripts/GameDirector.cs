using Terrain;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    public enum GameState
    {
        MENU    = 0,
        PLAYING = 1,
        PAUSE   = 2
    }

    public static GameDirector Instance { get; private set; }

    public static GameState CurrentState
    {
        set => Instance.m_currentState = value;
        get => Instance.m_currentState;
    }

    public static bool InPlayMode => CurrentState == GameState.PLAYING;

    private GameState m_currentState;

    private NoteManager m_noteManager;
    private HandController m_handController;
    private LastMapStatBehaviour m_lastMapStatBehaviour;

    private bool m_leftStart = false;
    private bool m_rightStart = false;

    public void Awake()
    {
        if (Instance is not null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        m_currentState = GameState.MENU;
    }

    public void Start()
    {
        m_noteManager = FindObjectOfType<NoteManager>();

        if (m_noteManager == null)
        {
            Debug.LogError("No NoteManager found in scene.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        m_noteManager.OnMapEnd += MapEnded;

        m_handController = FindObjectOfType<HandController>();
        
        if (m_noteManager == null)
        {
            Debug.LogError("No HandController found in scene.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        m_handController.OnNewLeftGesture += ProcessInput;
        m_handController.OnNewRightGesture += ProcessInput;

        m_lastMapStatBehaviour = FindObjectOfType<LastMapStatBehaviour>();
    }

    private void MapEnded(bool p_success)
    {
        Debug.LogFormat("Map ended : {0}", p_success);

        if (m_lastMapStatBehaviour != null)
        {
            m_lastMapStatBehaviour.UpdateTexts(m_noteManager.m_lastMapStats);
        }

        m_currentState = GameState.MENU;
    }

    private void ProcessInput(Gesture? p_newInput, Gesture? p_oldInput)
    {
        if (CurrentState == GameState.PLAYING)
        {
            m_noteManager.ProcessInput(p_newInput, p_oldInput);
        }
        else
        {
            if (!p_newInput.HasValue) return;
            
            if (p_newInput.Value.side == HandController.Side.LEFT)
            {
                if (p_newInput.Value.type == Gesture.Type.METAL) m_leftStart = true;
                else m_leftStart = false;
            }
            
            if (p_newInput.Value.side == HandController.Side.RIGHT)
            {
                if (p_newInput.Value.type == Gesture.Type.METAL) m_rightStart = true;
                else m_rightStart = false;
            }

            if (m_leftStart && m_rightStart)
            {
                StartSelectedMap();
            }
        }
    }

    private void StartSelectedMap()
    {
        m_currentState = GameState.PLAYING;
        m_noteManager.StartGame();
    }
}