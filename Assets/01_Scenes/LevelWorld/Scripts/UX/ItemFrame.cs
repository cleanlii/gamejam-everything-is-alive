using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ItemFrame : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private GameObject cellPrefab; // 预制体，代表一个单元格

    [SerializeField] private bool isVisible;

    private readonly Dictionary<Vector2Int, GameObject> frameCells = new();

    private Tween _fadeTween; // DoTween 动画缓存

    public void ShowFrame()
    {
        if (isVisible) return;

        isVisible = true; // 设置为可见状态
        _fadeTween?.Kill(); // 停止当前动画
        canvasGroup.alpha = 0;
        _fadeTween = canvasGroup.DOFade(1f, 0.3f); // 使用 DoTween 淡入
    }

    public void HideFrame()
    {
        if (!isVisible) return;

        _fadeTween?.Kill(); // 停止当前动画
        _fadeTween = canvasGroup.DOFade(0f, 0.1f);
        isVisible = false; // 设置为隐藏状态
    }

    public void HideInstant()
    {
        if (!isVisible) return;

        _fadeTween?.Kill(); // 停止当前动画
        canvasGroup.alpha = 0;
        isVisible = false; // 设置为隐藏状态
    }

    public void GenerateFrameGrid(Vector2Int[] shape, float cellSize)
    {
        if (shape == null || shape.Length == 0)
        {
            Debug.LogWarning("Shape data is empty!");
            return;
        }

        frameCells.Clear();

        // 计算 grid 宽高
        var width = shape.Max(v => v.x) + 1;
        var height = shape.Max(v => v.y) + 1;

        // 设置 GridLayoutGroup 行列
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = width;

        // 设置 Cell 大小
        gridLayoutGroup.cellSize = new Vector2(cellSize, cellSize);

        // 生成 Grid
        for (var y = height - 1; y >= 0; y--) // 逆向 Y 轴（上->下）
        {
            for (var x = 0; x < width; x++)
            {
                Vector2Int pos = new(x, y);
                var cell = Instantiate(cellPrefab, gridLayoutGroup.transform);
                cell.name = $"Cell_{x}_{y}";
                cell.GetComponent<BoxCollider2D>().size = new Vector2(cellSize, cellSize);

                // 记录 Cell
                frameCells[pos] = cell;
            }
        }

        // 让 GridLayoutGroup 排列完成后，缓存位置
        Canvas.ForceUpdateCanvases(); // 确保布局更新
        Dictionary<Vector2Int, Vector3> cellPositions = new();

        foreach (var kvp in frameCells)
        {
            cellPositions[kvp.Key] = kvp.Value.transform.localPosition; // 缓存最终位置
        }

        // 关闭 GridLayoutGroup
        gridLayoutGroup.enabled = false;

        // 还原每个 Cell 的位置
        foreach (var kvp in frameCells)
        {
            kvp.Value.transform.localPosition = cellPositions[kvp.Key];

            // 仅激活 shape 内的 Cell
            var isActive = shape.Contains(kvp.Key);
            kvp.Value.SetActive(isActive);
        }
    }
}