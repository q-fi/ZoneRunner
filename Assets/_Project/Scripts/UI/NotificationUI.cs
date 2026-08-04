using System.Collections;
using TMPro;
using UnityEngine;

public class NotificationUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float displayDuration = 2f;

    private Coroutine hideRoutine;

    private void Start()
    {
        root.SetActive(false);
        InventoryManager.Instance.OnInventoryMessage += Show;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryMessage -= Show;
    }

    private void Show(string message)
    {
        messageText.text = message;
        root.SetActive(true);

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        root.SetActive(false);
    }
}