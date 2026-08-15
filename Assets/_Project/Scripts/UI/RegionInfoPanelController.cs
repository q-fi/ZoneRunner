using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionInfoPanelController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private MapPanelController mapPanelController;

    [Header("Panel")]
    [SerializeField] private GameObject regionInfoPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleText;

    [Header("Region Texts")]
    [SerializeField] private TMP_Text regionNameText;
    [SerializeField] private TMP_Text shortDescriptionText;
    [SerializeField] private TMP_Text detailsText;

    private bool isExpanded;
    private bool isSubscribed;

    private void Awake()
    {
        if (mapPanelController == null)
        {
            mapPanelController =
                GetComponentInParent<MapPanelController>();
        }

        toggleButton.onClick.AddListener(Toggle);
        SetExpanded(false);
    }

    private void OnEnable()
    {
        Subscribe();

        SetExpanded(false);

        if (mapPanelController != null &&
            mapPanelController.CurrentRegion != null)
        {
            RefreshRegion(
                mapPanelController.CurrentRegion
            );
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(Toggle);

        Unsubscribe();
    }

    private void Subscribe()
    {
        if (isSubscribed ||
            mapPanelController == null)
        {
            return;
        }

        mapPanelController.OnRegionOpened += RefreshRegion;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed ||
            mapPanelController == null)
        {
            return;
        }

        mapPanelController.OnRegionOpened -= RefreshRegion;
        isSubscribed = false;
    }

    private void Toggle()
    {
        SetExpanded(!isExpanded);
    }

    private void SetExpanded(bool expanded)
    {
        isExpanded = expanded;

        if (regionInfoPanel != null)
            regionInfoPanel.SetActive(expanded);

        if (toggleText != null)
        {
            toggleText.text = expanded
                ? "REGION INFO ▲"
                : "REGION INFO ▼";
        }
    }

    private void RefreshRegion(RegionData region)
    {
        if (region == null)
            return;

        if (regionNameText != null)
            regionNameText.text = region.RegionName;

        if (shortDescriptionText != null)
        {
            shortDescriptionText.text =
                region.ShortDescription;
        }

        if (detailsText != null)
        {
            detailsText.text = BuildDetails(region);
        }
    }

    private string BuildDetails(RegionData region)
    {
        string legendaryArtifact =
            region.LegendaryArtifactPossible
                ? "Possible"
                : "Not detected";

        return
            $"Threat Level: {region.ThreatLevel}/10\n\n" +
            $"{region.FullDescription}\n\n" +
            $"Threats: {FormatList(region.PossibleThreats)}\n" +
            $"Events: {FormatList(region.PossibleEvents)}\n" +
            $"Phenomena: {FormatList(region.PossiblePhenomena)}\n" +
            $"Inhabitants: {FormatList(region.Inhabitants)}\n\n" +
            $"Loot: {FormatRange(region.LootAmountRange)}\n" +
            $"Stashes: {FormatRange(region.StashAmountRange)}\n" +
            $"Artifacts: {FormatRange(region.ArtifactAmountRange)}\n" +
            $"Legendary Artifact: {legendaryArtifact}";
    }

    private string FormatList(
        IReadOnlyList<string> values
    )
    {
        if (values == null || values.Count == 0)
            return "—";

        return string.Join(", ", values);
    }

    private string FormatRange(Vector2Int range)
    {
        if (range.x == range.y)
            return range.x.ToString();

        return $"{range.x}–{range.y}";
    }
}