using System;
using System.Collections.Generic;
using cfg.level;
using UnityEngine;

public class PropManager : MonoBehaviour
{
    private ItemLibrary itemLibrary;
    // public InventoryInitializer inventoryInitializer;
    public List<CellData> highlightedCells;
    public Dictionary<ItemTag, List<CellData>> taggedCells;
    public List<PropData> upgradedProps; // 升级后的prop，临时保存
    public Sprite originalBlock;
    public Sprite enabledCool;
    public Sprite disabledCool;
    public Sprite enabledNatural;
    public Sprite disabledNatural;
    public Sprite enabledFragile;
    public Sprite disabledFragile;
    private Vector2Int[] directions;

    public void Initialize()
    {
        itemLibrary = LevelStateController.Instance.GetService<ItemLibrary>();

        directions = new[]
        {
            new Vector2Int(-1, 0), // 左
            new Vector2Int(1, 0), // 右
            new Vector2Int(0, -1), // 下
            new Vector2Int(0, 1) // 上
        };

        originalBlock = itemLibrary.GetTagData(ItemTag.Ordinary).enabledIcon;
        // originalBlock = itemLibrary.GetTagData(ItemTag.Ordinary).disabledIcon;
        enabledCool = itemLibrary.GetTagData(ItemTag.CoolI).enabledIcon;
        disabledCool = itemLibrary.GetTagData(ItemTag.CoolI).disabledIcon;
        enabledNatural = itemLibrary.GetTagData(ItemTag.NaturalI).enabledIcon;
        disabledNatural = itemLibrary.GetTagData(ItemTag.NaturalI).disabledIcon;
        enabledFragile = itemLibrary.GetTagData(ItemTag.FragileI).enabledIcon;
        disabledFragile = itemLibrary.GetTagData(ItemTag.FragileI).disabledIcon;

        highlightedCells = new List<CellData>();
        upgradedProps = new List<PropData>();

        InitializeTaggedCells();
    }

    public void PrintHighlightCells()
    {
        if (highlightedCells.Count == 0)
            Debug.Log("高亮Cell坐标为空！");
        else
        {
            foreach (var cell in highlightedCells)
            {
                // 在这里对每个 CellData 进行处理
                Debug.Log($"高亮Cell坐标: {cell.gridPosition}");
            }
        }
    }

    public void PrintTaggedCells()
    {
        foreach (var kvp in taggedCells)
        {
            // kvp.Key 是 ItemTag
            var tag = kvp.Key;
            // kvp.Value 是 List<CellData>
            var cells = kvp.Value;

            foreach (var cell in cells)
            {
                // 在这里对每个 CellData 进行处理
                Debug.Log($"Tag: {tag}, Cell: {cell.gridPosition}");
            }
        }
    }

    public void InitializeTaggedCells()
    {
        // 初始化字典
        taggedCells = new Dictionary<ItemTag, List<CellData>>();

        // 对 ItemTag 枚举中的每个值进行迭代
        foreach (ItemTag tag in Enum.GetValues(typeof(ItemTag)))
        {
            // 为每个标签初始化一个新的 List<CellData>
            taggedCells[tag] = new List<CellData>();
        }
    }

    public void UpgradeProp(PropData prop1, PropData prop2)
    {
        if (prop1.value > prop2.value)
        {
            // 基于prop1创建一个新的propdata
        }
    }

    public bool CanUpgradeProp(PropData prop1, PropData prop2)
    {
        foreach (var offset1 in prop1.shape)
        {
            var cellX1 = prop1.position.x + offset1.x;
            var cellY1 = prop1.position.y + offset1.y;

            prop1.mapping.TryGetValue(offset1, out var tag1);
            foreach (var offset2 in prop2.shape)
            {
                var cellX2 = prop1.position.x + offset1.x;
                var cellY2 = prop1.position.y + offset1.y;

                prop1.mapping.TryGetValue(offset2, out var tag2);
                if (tag1 == tag2 && tag2 != ItemTag.Ordinary && tag2 != ItemTag.None)
                {
                    if ((cellX1 == cellX2 && Math.Abs(cellY1 - cellY2) == 1) ||
                        (cellY1 == cellY2 && Math.Abs(cellX1 - cellX2) == 1))
                    {
                        // 功能单元类型必须相同
                        // 功能单元必须相邻
                        // 功能单元等级必须相等
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public Vector2Int[] CalculateSingleCellBoundary(Vector2Int cell, Vector2Int[] shape)
    {
        if (shape == null || shape.Length == 0) return new Vector2Int[0];

        var shapeSet = new HashSet<Vector2Int>(shape);
        var boundaryOffsets = new List<Vector2Int>();

        foreach (var direction in directions)
        {
            var neighbor = cell + direction;
            if (!shapeSet.Contains(neighbor)) boundaryOffsets.Add(neighbor);
        }

        return boundaryOffsets.ToArray();
    }

    public Vector2Int[] CalculateValidBoundaryOffsets(Vector2Int[] currentShape, Vector2Int[] validCells)
    {
        if (currentShape == null || currentShape.Length == 0 || validCells == null || validCells.Length == 0) return new Vector2Int[0];

        var shapeSet = new HashSet<Vector2Int>(currentShape);
        var validSet = new HashSet<Vector2Int>(validCells);
        var boundaryOffsets = new List<Vector2Int>();

        foreach (var point in currentShape)
        {
            // 只考虑有效单元
            if (!validSet.Contains(point)) continue;

            foreach (var direction in directions)
            {
                var neighbor = point + direction;
                if (!shapeSet.Contains(neighbor) && !boundaryOffsets.Contains(neighbor)) boundaryOffsets.Add(neighbor);
            }
        }

        return boundaryOffsets.ToArray();
    }

    public Vector2Int[] CalculateBoundaryOffsets(Vector2Int[] currentShape)
    {
        if (currentShape == null || currentShape.Length == 0) return new Vector2Int[0];

        var shapeSet = new HashSet<Vector2Int>(currentShape);
        var boundaryOffsets = new List<Vector2Int>();

        foreach (var point in currentShape)
        {
            foreach (var direction in directions)
            {
                var neighbor = point + direction;
                if (!shapeSet.Contains(neighbor))
                    if (!boundaryOffsets.Contains(neighbor))
                        boundaryOffsets.Add(neighbor);
            }
        }

        return boundaryOffsets.ToArray();
    }

    // 根据gridPosition计算实际的外包围圈坐标
    public Vector2Int[] CalculateBoundaryByOffset(Vector2Int[] boundaryOffsets, Vector2Int gridPosition)
    {
        if (boundaryOffsets == null || boundaryOffsets.Length == 0) return new Vector2Int[0];

        var boundaryPositions = new List<Vector2Int>();

        foreach (var offset in boundaryOffsets)
        {
            boundaryPositions.Add(offset + gridPosition);
        }

        return boundaryPositions.ToArray();
    }

    public Vector2Int[] CalculateBoundaryByShape(Vector2Int[] currentShape, Vector2Int gridPosition)
    {
        if (currentShape == null || currentShape.Length == 0) return new Vector2Int[0];

        var shapeSet = new HashSet<Vector2Int>(currentShape);
        var boundaryPoints = new List<Vector2Int>();

        foreach (var point in currentShape)
        {
            foreach (var direction in directions)
            {
                var neighbor = point + direction;
                if (!shapeSet.Contains(neighbor))
                {
                    var boundaryPoint = neighbor + gridPosition;
                    if (!boundaryPoints.Contains(boundaryPoint)) boundaryPoints.Add(boundaryPoint);
                }
            }
        }

        return boundaryPoints.ToArray();
    }
}