using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public event Action<GameState, GameState> OnStateChanged; // (previous, next)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeState(GameState newState)
    {
        if (newState == CurrentState) return;

        GameState previous = CurrentState;
        CurrentState = newState;

        Time.timeScale = (newState == GameState.Pause) ? 0f : 1f;
        
        Debug.Log($"State: {previous} -> {newState}");
        OnStateChanged?.Invoke(previous, newState);
    }
}