using UnityEngine;

[System.Serializable]
public class BackpackPreset
{
    public string PresetName;

    public string Description = "";

    public bool IsDirty { get; set; }

    public InventoryGrid Grid { get; }

    public BackpackPreset(string presetName, int width, int height)
    {
        PresetName = presetName;
        Grid = new InventoryGrid(width, height);
    }
}