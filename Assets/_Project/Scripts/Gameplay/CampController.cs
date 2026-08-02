using UnityEngine;

public class CampController : MonoBehaviour
{
    public void OnDepartureClicked()
    {
        GameManager.Instance.ChangeState(GameState.Travel);
    }

    public void OnBuildClicked()
    {
        Debug.Log("Будівництво: ще не реалізовано");
    }

    public void OnRecruitClicked()
    {
        Debug.Log("Найм напарників: ще не реалізовано");
    }

    public void OnTradeClicked()
    {
        Debug.Log("Торгівля: ще не реалізовано");
    }

    public void OnMuseumClicked()
    {
        Debug.Log("Музей: ще не реалізовано");
    }
}