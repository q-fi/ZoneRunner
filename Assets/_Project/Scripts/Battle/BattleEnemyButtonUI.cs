using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BattleEnemyButtonUI : MonoBehaviour
{
    [SerializeField] private BattleController battleController;
    [SerializeField] private BattleFormationSlot formationSlot;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text damagePreviewText;
    [SerializeField] private TMP_Text damageResultText;

    [Min(0f)]
    [SerializeField] private float damageResultDuration = 0.8f;

    private Button button;
    private Coroutine damageResultRoutine;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (battleController == null)
            battleController = GetComponentInParent<BattleController>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        button.onClick.RemoveListener(SelectEnemy);
        button.onClick.AddListener(SelectEnemy);

        if (battleController != null)
        {
            battleController.OnBattleStateChanged -= Refresh;
            battleController.OnBattleStateChanged += Refresh;
            battleController.OnEnemyDamaged -= HandleEnemyDamaged;
            battleController.OnEnemyDamaged += HandleEnemyDamaged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(SelectEnemy);

        if (battleController != null)
        {
            battleController.OnBattleStateChanged -= Refresh;
            battleController.OnEnemyDamaged -= HandleEnemyDamaged;
        }

        if (damageResultRoutine != null)
        {
            StopCoroutine(damageResultRoutine);
            damageResultRoutine = null;
        }

        if (damageResultText != null)
            damageResultText.gameObject.SetActive(false);
    }

    private void SelectEnemy()
    {
        int enemyIndex = FindEnemyIndex();

        if (enemyIndex >= 0)
            battleController.TryPlaySelectedCard(enemyIndex);
    }

    private void Refresh()
    {
        int enemyIndex = FindEnemyIndex();
        BattleEnemyState enemy = enemyIndex >= 0
            ? battleController.Enemies[enemyIndex]
            : null;

        if (label != null)
        {
            label.text = enemy != null
                ? $"{enemy.Data.displayName}\n" +
                  $"{enemy.CurrentHealth:0.#}/{enemy.MaxHealth:0.#} HP"
                : $"{formationSlot}\nEMPTY";
        }

        RefreshDamagePreview(enemy);

        if (button == null)
            return;

        button.interactable =
            enemy != null &&
            enemy.IsAlive &&
            battleController != null &&
            battleController.CurrentPhase == BattlePhase.PlayerTurn &&
            battleController.SelectedCard != null;
    }

    private void RefreshDamagePreview(BattleEnemyState enemy)
    {
        if (damagePreviewText == null)
            return;

        float minimumDamage = 0f;
        float maximumDamage = 0f;
        bool showPreview =
            battleController != null &&
            enemy != null &&
            enemy.IsAlive &&
            battleController.TryGetSelectedCardDamageRange(
                enemy,
                out minimumDamage,
                out maximumDamage
            );

        damagePreviewText.gameObject.SetActive(showPreview);

        if (!showPreview)
            return;

        damagePreviewText.text = Mathf.Approximately(
            minimumDamage,
            maximumDamage
        )
            ? $"<size=200%>~</size> {maximumDamage:0.#}"
            : $"<size=200%>~</size> " +
              $"{minimumDamage:0.#}-{maximumDamage:0.#}";
    }

    private void HandleEnemyDamaged(
        BattleEnemyState damagedEnemy,
        float damage
    )
    {
        if (damageResultText == null ||
            damagedEnemy == null ||
            damagedEnemy.FormationSlot != formationSlot)
        {
            return;
        }

        if (damageResultRoutine != null)
            StopCoroutine(damageResultRoutine);

        damageResultRoutine = StartCoroutine(
            ShowDamageResult(damage)
        );
    }

    private IEnumerator ShowDamageResult(float damage)
    {
        damageResultText.text = damage > 0f
            ? $"-{damage:0.#}"
            : "0";
        damageResultText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(
            damageResultDuration
        );

        damageResultText.gameObject.SetActive(false);
        damageResultRoutine = null;
    }

    private int FindEnemyIndex()
    {
        if (battleController == null)
            return -1;

        for (int i = 0; i < battleController.Enemies.Count; i++)
        {
            if (battleController.Enemies[i].FormationSlot == formationSlot)
                return i;
        }

        return -1;
    }
}
