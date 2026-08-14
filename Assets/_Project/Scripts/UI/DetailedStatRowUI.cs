using TMPro;
using UnityEngine;

public class DetailedStatRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statNameText;
    [SerializeField] private TMP_Text finalValueText;
    [SerializeField] private TMP_Text modifierInfoText;

    public void Setup(
        PlayerStatType stat,
        float finalValue,
        float totalModifier)
    {
        statNameText.text = stat.ToString();
        finalValueText.text = FormatValue(finalValue);

        modifierInfoText.text = totalModifier == 0f
            ? "—"
            : totalModifier.ToString("+0.##;-0.##;0");
    }

    private string FormatValue(float value)
    {
        return value % 1f == 0f
            ? value.ToString("0")
            : value.ToString("0.##");
    }
}