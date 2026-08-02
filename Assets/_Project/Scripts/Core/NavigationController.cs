using UnityEngine;

public class NavigationController : MonoBehaviour
{
    public void GoToCamp() => GameManager.Instance.ChangeState(GameState.Camp);
    public void GoToMap() => GameManager.Instance.ChangeState(GameState.Travel);
    public void GoToInventory() => GameManager.Instance.ChangeState(GameState.Inventory);
    public void GoToLeaderboard() => GameManager.Instance.ChangeState(GameState.Leaderboard);
    public void GoToPause() => GameManager.Instance.ChangeState(GameState.Pause);
}