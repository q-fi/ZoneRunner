using System.Collections.Generic;

public class BackpackPresetCollection
{
    public List<BackpackPreset> Presets { get; } = new();

    public BackpackPreset CurrentPreset { get; private set; }

    public BackpackPresetCollection(int gridWidth, int gridHeight)
    {
        for (int i = 0; i < 5; i++)
        {
            Presets.Add(
                new BackpackPreset(
                    $"Preset {i + 1}",
                     gridWidth,
                     gridHeight));
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