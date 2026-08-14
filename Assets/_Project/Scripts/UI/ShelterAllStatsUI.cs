using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShelterAllStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button allStatsButton;
    [SerializeField] private GameObject allStatsContent;
    [SerializeField] private DetailedStatRowUI rowPrefab;
    [SerializeField] private RectTransform scrollContent;

    private readonly List<DetailedStatRowUI> spawnedRows = new();
    private float baseScrollContentHeight;

    private void Awake()
    {
        allStatsButton.onClick.AddListener(ToggleAllStats);
    }

    private void Start()
    {
        baseScrollContentHeight = scrollContent.rect.height;
        allStatsContent.SetActive(false);

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= RefreshIfOpen;
    }

    private void ToggleAllStats()
    {
        bool shouldOpen = !allStatsContent.activeSelf;
        allStatsContent.SetActive(shouldOpen);

        if (shouldOpen)
            RebuildRows();
        else
            scrollContent.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                baseScrollContentHeight
            );
    }

    private void RefreshIfOpen()
    {
        if (allStatsContent.activeSelf)
            RebuildRows();
    }

    private void RebuildRows()
    {
        foreach (var row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        spawnedRows.Clear();

        foreach (PlayerStatType stat in Enum.GetValues(typeof(PlayerStatType)))
        {
            float baseValue = PlayerStats.Instance.GetBaseStat(stat);
            float finalValue = PlayerStats.Instance.GetFinalStat(stat);
            float modifierValue = finalValue - baseValue;

            DetailedStatRowUI row = Instantiate(
                rowPrefab,
                allStatsContent.transform
            );

            row.Setup(stat, finalValue, modifierValue);
            spawnedRows.Add(row);
        }

        Canvas.ForceUpdateCanvases();

        RectTransform allStatsRect =
            allStatsContent.GetComponent<RectTransform>();

        LayoutRebuilder.ForceRebuildLayoutImmediate(allStatsRect);

        scrollContent.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            baseScrollContentHeight + allStatsRect.rect.height
        );
    }
}