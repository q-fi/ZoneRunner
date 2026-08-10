using System.Collections.Generic;

public class EquipmentPresetCollection
{
    public List<EquipmentPreset> Presets { get; } = new();

    public EquipmentPreset CurrentPreset { get; private set; }

    public EquipmentPresetCollection()
    {
        for (int i = 0; i < 5; i++)
        {
            Presets.Add(
                new EquipmentPreset($"Preset {i + 1}")
            );
        }

        CurrentPreset = Presets[0];
    }

    public void SelectPreset(int index)
    {
        if (index < 0 || index >= Presets.Count)
            return;

        CurrentPreset = Presets[index];
    }

    public void RenamePreset(int index, string newName)
    {
        if (index < 0 || index >= Presets.Count)
            return;

        Presets[index].PresetName = newName;
    }
}