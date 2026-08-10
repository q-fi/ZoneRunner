using System;

[Serializable]
public class PresetItem
{
    public string SourceInstanceId;
    public ItemData Data;
    public int StackCount;

    public PresetItem(ItemInstance source)
    {
        SourceInstanceId = source.InstanceId;
        Data = source.Data;
        StackCount = source.StackCount;
    }
}