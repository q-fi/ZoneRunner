using System.Collections.Generic;

public class InventoryGrid
{
    public readonly int Width;
    public readonly int Height;

    private readonly ItemInstance[,] cells;
    private readonly Dictionary<ItemInstance, (int x, int y)> itemPositions = new();

    public InventoryGrid(int width, int height)
    {
        Width = width;
        Height = height;
        cells = new ItemInstance[width, height];
    }

    public bool TryAddItem(ItemInstance instance)
    {
        if (itemPositions.ContainsKey(instance))
            return false; // цей екземпляр вже в сітці

        var data = instance.Data;

        for (int y = 0; y <= Height - data.gridHeight; y++)
        {
            for (int x = 0; x <= Width - data.gridWidth; x++)
            {
                if (CanPlaceAt(data, x, y))
                {
                    PlaceAt(instance, x, y);
                    return true;
                }
            }
        }
        return false; // Немає вільного місця
    }

    private bool CanPlaceAt(ItemData data, int startX, int startY)
    {
        for (int x = startX; x < startX + data.gridWidth; x++)
        {
            for (int y = startY; y < startY + data.gridHeight; y++)
            {
                if (cells[x, y] != null)
                    return false;
            }
        }
        return true;
    }

    private void PlaceAt(ItemInstance instance, int startX, int startY)
    {
        var data = instance.Data;
        for (int x = startX; x < startX + data.gridWidth; x++)
        {
            for (int y = startY; y < startY + data.gridHeight; y++)
            {
                cells[x, y] = instance;
            }
        }
        itemPositions[instance] = (startX, startY);
    }

    public void RemoveItem(ItemInstance instance)
    {
        if (!itemPositions.TryGetValue(instance, out var pos))
            return;

        var data = instance.Data;
        for (int x = pos.x; x < pos.x + data.gridWidth; x++)
        {
            for (int y = pos.y; y < pos.y + data.gridHeight; y++)
            {
                cells[x, y] = null;
            }
        }
        itemPositions.Remove(instance);
    }

    public (int x, int y)? GetPosition(ItemInstance instance)
    {
        return itemPositions.TryGetValue(instance, out var pos) ? pos : null;
    }

    public IEnumerable<ItemInstance> GetAllItems() => itemPositions.Keys;

    /// <summary>Шукає існуючий стак того ж типу предмета, де є вільне місце (для стакання).</summary>
    public ItemInstance FindStackableInstance(ItemData data)
    {
        foreach (var instance in itemPositions.Keys)
        {
            if (instance.Data == data && instance.StackCount < data.maxStackSize)
                return instance;
        }
        return null;
    }

    /// <summary>Очищає всі позиції в сітці (використовується при сортуванні).</summary>
    public void Clear()
    {
        System.Array.Clear(cells, 0, cells.Length);
        itemPositions.Clear();
    }
}