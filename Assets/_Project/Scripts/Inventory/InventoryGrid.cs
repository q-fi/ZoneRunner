using System.Collections.Generic;

public class InventoryGrid
{
    public readonly int Width;
    public readonly int Height;

    private readonly ItemData[,] cells;
    private readonly Dictionary<ItemData, (int x, int y)> itemPositions = new();

    public InventoryGrid(int width, int height)
    {
        Width = width;
        Height = height;
        cells = new ItemData[width, height];
    }

    public bool TryAddItem(ItemData item)
    {
        for (int y = 0; y <= Height - item.gridHeight; y++)
        {
            for (int x = 0; x <= Width - item.gridWidth; x++)
            {
                if (CanPlaceAt(item, x, y))
                {
                    PlaceAt(item, x, y);
                    return true;
                }
            }
        }
        return false; // Немає вільного місця
    }

    private bool CanPlaceAt(ItemData item, int startX, int startY)
    {
        for (int x = startX; x < startX + item.gridWidth; x++)
        {
            for (int y = startY; y < startY + item.gridHeight; y++)
            {
                if (cells[x, y] != null)
                    return false;
            }
        }
        return true;
    }

    private void PlaceAt(ItemData item, int startX, int startY)
    {
        for (int x = startX; x < startX + item.gridWidth; x++)
        {
            for (int y = startY; y < startY + item.gridHeight; y++)
            {
                cells[x, y] = item;
            }
        }
        itemPositions[item] = (startX, startY);
    }

    public void RemoveItem(ItemData item)
    {
        if (!itemPositions.TryGetValue(item, out var pos))
            return;

        for (int x = pos.x; x < pos.x + item.gridWidth; x++)
        {
            for (int y = pos.y; y < pos.y + item.gridHeight; y++)
            {
                cells[x, y] = null;
            }
        }
        itemPositions.Remove(item);
    }

    public (int x, int y)? GetPosition(ItemData item)
    {
        return itemPositions.TryGetValue(item, out var pos) ? pos : null;
    }

    public IEnumerable<ItemData> GetAllItems() => itemPositions.Keys;
}