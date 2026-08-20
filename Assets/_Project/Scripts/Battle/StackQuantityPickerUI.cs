using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StackQuantityPickerUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Slider amountSlider;

    [Header("Actions")]
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button halfButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action<int> confirmAction;
    private int maximumAmount;
    private int currentAmount;
    private bool initialized;

    public bool IsOpen =>
        panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        EnsureInitialized();

        if (panelRoot != gameObject)
            panelRoot.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        if (panelRoot == null)
            panelRoot = gameObject;

        if (minusButton != null)
            minusButton.onClick.AddListener(Decrease);

        if (plusButton != null)
            plusButton.onClick.AddListener(Increase);

        if (halfButton != null)
            halfButton.onClick.AddListener(SelectHalf);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        if (amountSlider != null)
            amountSlider.onValueChanged.AddListener(OnSliderChanged);

        initialized = true;
    }

    private void OnDestroy()
    {
        if (minusButton != null)
            minusButton.onClick.RemoveListener(Decrease);

        if (plusButton != null)
            plusButton.onClick.RemoveListener(Increase);

        if (halfButton != null)
            halfButton.onClick.RemoveListener(SelectHalf);

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Cancel);

        if (amountSlider != null)
            amountSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnDisable()
    {
        confirmAction = null;
    }

    public void Open(
        string itemLabel,
        int maxAmount,
        int initialAmount,
        Action<int> onConfirmed
    )
    {
        EnsureInitialized();

        if (maxAmount <= 0 || onConfirmed == null)
        {
            Close();
            return;
        }

        maximumAmount = maxAmount;
        currentAmount = Mathf.Clamp(initialAmount, 1, maximumAmount);
        confirmAction = onConfirmed;

        if (titleText != null)
        {
            string itemName = string.IsNullOrWhiteSpace(itemLabel)
                ? "Item"
                : itemLabel;
            titleText.text =
                $"SPLIT {itemName} (x{maximumAmount})";
        }

        if (amountSlider != null)
        {
            amountSlider.wholeNumbers = true;
            amountSlider.minValue = 1f;
            amountSlider.maxValue = maximumAmount;
            amountSlider.SetValueWithoutNotify(currentAmount);
        }

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        confirmAction = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Decrease()
    {
        SetAmount(currentAmount - 1);
    }

    private void Increase()
    {
        SetAmount(currentAmount + 1);
    }

    private void SelectHalf()
    {
        SetAmount(Mathf.CeilToInt(maximumAmount * 0.5f));
    }

    private void OnSliderChanged(float value)
    {
        SetAmount(Mathf.RoundToInt(value), false);
    }

    private void SetAmount(int amount, bool updateSlider = true)
    {
        currentAmount = Mathf.Clamp(amount, 1, maximumAmount);

        if (updateSlider && amountSlider != null)
            amountSlider.SetValueWithoutNotify(currentAmount);

        Refresh();
    }

    private void Confirm()
    {
        Action<int> callback = confirmAction;
        int confirmedAmount = currentAmount;
        Close();
        callback?.Invoke(confirmedAmount);
    }

    private void Cancel()
    {
        Close();
    }

    private void Refresh()
    {
        if (amountText != null)
        {
            amountText.text =
                $"Selected: x{currentAmount}\n" +
                $"Remaining in stack: " +
                $"x{maximumAmount - currentAmount}";
        }

        if (minusButton != null)
            minusButton.interactable = currentAmount > 1;

        if (plusButton != null)
            plusButton.interactable =
                currentAmount < maximumAmount;

        if (halfButton != null)
            halfButton.interactable = maximumAmount > 1;

        if (confirmButton != null)
            confirmButton.interactable =
                maximumAmount > 1 && currentAmount < maximumAmount;
    }
}
