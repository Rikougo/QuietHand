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
    
    public static GameState CurrentState => Instance.m_currentState;

    private GameState m_currentState;

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
}