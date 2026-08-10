using System.Collections.Generic;

[System.Serializable]
public class EquipmentPreset
{
    public string PresetName;

    public bool IsDirty { get; set; }

    private readonly Dictionary<SlotCategory, PresetItem[]> slots = new()
    {
        { SlotCategory.Weapon,   new PresetItem[2] },
        { SlotCategory.Armor,    new PresetItem[1] },
        { SlotCategory.Detector, new PresetItem[1] },
        { SlotCategory.Artifact, new PresetItem[3] },
    };

    public EquipmentPreset(string presetName)
    {
        PresetName = presetName;
    }

    public PresetItem GetSlot(SlotCategory category, int index)
    {
        if (!slots.TryGetValue(category, out var array))
            return null;

        if (index < 0 || index >= array.Length)
            return null;

        return array[index];
    }

    public bool SetSlot(
        SlotCategory category,
        int index,
        PresetItem item)
    {
        if (!slots.TryGetValue(category, out var array))
            return false;

        if (index < 0 || index >= array.Length)
            return false;

        array[index] = item;
        return true;
    }

    public int SlotCount(SlotCategory category)
    {
        return slots.TryGetValue(category, out var array)
            ? array.Length
            : 0;
    }

    public bool Contains(ItemInstance instance)
    {
        if (instance == null)
            return false;

        return ContainsInstanceId(instance.InstanceId);
    }

    public bool ContainsInstanceId(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return false;

        foreach (var pair in slots)
        {
            foreach (var item in pair.Value)
            {
                if (item != null &&
                    item.SourceInstanceId == instanceId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static readonly SlotCategory[] SupportedCategories =
    {
        SlotCategory.Weapon,
        SlotCategory.Armor,
        SlotCategory.Detector,
        SlotCategory.Artifact
    };
}