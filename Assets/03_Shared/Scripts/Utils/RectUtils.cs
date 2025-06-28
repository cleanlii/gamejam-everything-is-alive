using System.Collections.Generic;
using UnityEngine;

public static class RectUtils
{
    /// <summary>
    ///     复制一个Rect的属性参数至另一个Rect
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public static void SetRectTransform(RectTransform source, RectTransform target)
    {
        source.anchoredPosition = target.anchoredPosition;
        // source.sizeDelta = target.sizeDelta;
        source.anchorMin = target.anchorMin;
        source.anchorMax = target.anchorMax;
        source.pivot = target.pivot;
    }

    /// <summary>
    ///     将世界坐标转换为UI坐标
    /// </summary>
    /// <param name="parentCanvas"></param>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public static Vector2 WorldToUISpace(Canvas parentCanvas, Vector3 worldPosition)
    {
        // 将世界坐标转换为屏幕坐标
        var screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        // 将屏幕坐标转换为 Canvas/UI 坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.GetComponent<RectTransform>(), screenPos, parentCanvas.worldCamera,
            out var localPoint);

        return localPoint;
    }

    /// <summary>
    ///     获取四角坐标
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <returns></returns>
    public static Vector3[] GetWorldCorners(RectTransform rectTransform)
    {
        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners); // 获取四个角的世界坐标
        return corners;
    }

    /// <summary>
    ///     获取物理中心位置。该方法适用于任意锚点和pivot设置。
    /// </summary>
    /// <param name="rectTransform">目标UI元素的RectTransform</param>
    /// <returns>UI元素的物理中心点坐标</returns>
    public static Vector3 GetCenterPosition(RectTransform rectTransform, Canvas canvas)
    {
        // 检查是否为有效的RectTransform
        if (rectTransform == null || canvas == null)
        {
            Debug.LogError("RectTransform 或 Canvas 不能为空");
            return Vector3.zero;
        }

        // 获取UI元素相对于其pivot的偏移量
        var pivotOffset = new Vector2((0.5f - rectTransform.pivot.x) * rectTransform.rect.width,
            (0.5f - rectTransform.pivot.y) * rectTransform.rect.height);

        // 获取UI元素相对于canvas的屏幕坐标
        var screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rectTransform.position);

        // 考虑旋转，将偏移量转换为旋转后的坐标系
        var rotation = rectTransform.localRotation;
        var rotatedOffset = rotation * new Vector3(pivotOffset.x, pivotOffset.y, 0);

        // 计算中心点在屏幕坐标系中的位置
        var screenCenter = screenPoint + new Vector2(rotatedOffset.x, rotatedOffset.y);

        // 将屏幕坐标转换为世界坐标
        Vector3 worldCenter;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, screenCenter, canvas.worldCamera, out worldCenter);

        return worldCenter;
    }

    /// <summary>
    ///     在最小旋转矩形内绕左上角旋转一次
    /// </summary>
    /// <param name="shape">左上角坐标系内的一组形状坐标</param>
    /// <returns>旋转后的形状坐标</returns>
    public static Vector2Int[] RotateShape(Vector2Int[] shape)
    {
        var rotated = new List<Vector2Int>();

        // 找到左上角的坐标
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        foreach (var cell in shape)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.y < minY) minY = cell.y;
        }

        // 计算相对坐标，使左上角保持不变
        foreach (var cell in shape)
        {
            var relativeX = cell.x - minX;
            var relativeY = cell.y - minY;
            rotated.Add(new Vector2Int(relativeY, -relativeX));
        }

        // 找到新的左上角偏移量
        var newMinX = int.MaxValue;
        var newMinY = int.MaxValue;
        foreach (var cell in rotated)
        {
            if (cell.x < newMinX) newMinX = cell.x;
            if (cell.y < newMinY) newMinY = cell.y;
        }

        // 调整回到左上角的坐标系
        for (var i = 0; i < rotated.Count; i++)
        {
            rotated[i] = new Vector2Int(rotated[i].x - newMinX, rotated[i].y - newMinY);
        }

        // // 对旋转后的形状进行排序，确保按行列顺序排列
        // rotated.Sort((a, b) =>
        // {
        //     if (a.x == b.x)
        //         return a.y.CompareTo(b.y);
        //     return a.x.CompareTo(b.x);
        // });

        // // 输出调试信息
        // for (int i = 0; i < rotated.Count; i++)
        // {
        //     Debug.Log("rotated[" + i + "]为：" + rotated[i]);
        // }

        return rotated.ToArray();
    }

    /// <summary>
    ///     计算最小包围矩形的大小
    /// </summary>
    /// <param name="shape">左上角坐标系内的一组形状坐标</param>
    /// <returns>矩形长宽</returns>
    public static Vector2Int CalculateBoundingBox(Vector2Int[] shape)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var point in shape)
        {
            if (point.x < minX) minX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.x > maxX) maxX = point.x;
            if (point.y > maxY) maxY = point.y;
        }

        return new Vector2Int(maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>
    ///     判断一个Rect面积是否“完全”在另一个Rect面积之内
    /// </summary>
    /// <param name="panel">外部Rect</param>
    /// <param name="item">内部Rect</param>
    /// <returns></returns>
    public static bool IsRectangleInside(RectTransform panel, RectTransform item)
    {
        var panelCorners = new Vector3[4];
        var itemCorners = new Vector3[4];

        panel.GetWorldCorners(panelCorners);
        item.GetWorldCorners(itemCorners);

        for (var i = 0; i < 4; i++)
        {
            if (!IsPointInside(panelCorners, itemCorners[i])) return false;
        }

        return true;
    }

    /// <summary>
    ///     判断一个Rect面积的中央Pivot是否在另一个Rect面积之内
    /// </summary>
    /// <param name="panel"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public static bool IsPivotInside(RectTransform panel, RectTransform item)
    {
        // 获取 panel 的世界空间四角点
        var panelCorners = new Vector3[4];
        panel.GetWorldCorners(panelCorners);

        // 获取 item 的中心 pivot 点的世界坐标
        var itemPivotWorldPos = item.position;

        // 判断中心点是否在 panel 的四角点形成的区域内
        return IsPointInside(panelCorners, itemPivotWorldPos);
    }

    /// <summary>
    ///     设置一个Rect锚点为中央且保持位置不变
    /// </summary>
    /// <param name="rectTransform"></param>
    public static void SetPivotToCenter(RectTransform rectTransform)
    {
        // 记录原始的pivot和sizeDelta
        var originalPivot = rectTransform.pivot;
        var sizeDelta = rectTransform.sizeDelta;

        // 计算新的pivot和偏移量
        var newPivot = new Vector2(0.5f, 0.5f);
        var pivotOffset = (newPivot - originalPivot) * sizeDelta;

        // 考虑旋转影响，将偏移量转换到旋转后的坐标系
        var rotation = rectTransform.localRotation;
        var rotatedPivotOffset = rotation * new Vector3(pivotOffset.x, pivotOffset.y, 0);

        // 更新pivot
        rectTransform.pivot = newPivot;

        // 更新anchoredPosition，考虑旋转带来的影响
        rectTransform.anchoredPosition += new Vector2(rotatedPivotOffset.x, rotatedPivotOffset.y);
    }

    /// <summary>
    ///     设置一个Rect锚点为左上角且保持位置不变
    /// </summary>
    /// <param name="rectTransform"></param>
    public static void SetPivotToLeftTop(RectTransform rectTransform)
    {
        // 记录原始的pivot和sizeDelta
        var originalPivot = rectTransform.pivot;
        var sizeDelta = rectTransform.sizeDelta;

        // 计算新的pivot和偏移量
        var newPivot = new Vector2(0f, 1f);
        var pivotOffset = (newPivot - originalPivot) * sizeDelta;

        // 考虑旋转，将偏移量转换到旋转后的坐标系
        var rotation = rectTransform.localRotation;
        var rotatedPivotOffset = rotation * new Vector3(pivotOffset.x, pivotOffset.y, 0);

        // 更新pivot
        rectTransform.pivot = newPivot;

        // 更新anchoredPosition，考虑旋转带来的影响
        rectTransform.anchoredPosition += new Vector2(rotatedPivotOffset.x, rotatedPivotOffset.y);
    }

    public static bool IsPointInside(Vector3[] corners, Vector3 point)
    {
        return point.x >= corners[0].x && point.x <= corners[2].x &&
               point.y >= corners[0].y && point.y <= corners[2].y;
    }

    /// <summary>
    ///     使一个Rect平铺满屏幕
    /// </summary>
    /// <param name="rectTransform"></param>
    public static void SetBackgroundPivot(RectTransform rectTransform)
    {
        // 设置锚点和位置
        rectTransform.anchorMin = new Vector2(0, 0); // 设置锚点的最小值为左下角
        rectTransform.anchorMax = new Vector2(1, 1); // 设置锚点的最大值为右上角
        rectTransform.offsetMin = Vector2.zero; // 重置左下角的偏移
        rectTransform.offsetMax = Vector2.zero; // 重置右上角的偏移
        // 确保图片铺满屏幕
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero; // 设置位置为锚点中心
    }

    /// <summary>
    ///     计算shape坐标数组
    /// </summary>
    /// <param name="rows">由0和1组成的模拟行列式</param>
    /// <returns>一组二维坐标，最左上角为零点</returns>
    public static Vector2Int[] CreateShape(params string[] rows)
    {
        var shapeList = new List<Vector2Int>();

        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                if (rows[y][x] == '1')
                {
                    // 直接使用 y 作为竖直方向索引
                    shapeList.Add(new Vector2Int(x, y));
                }
            }
        }

        return shapeList.ToArray();
    }

    /// <summary>
    ///     比较两个 Vector2Int[] 数组是否相等
    /// </summary>
    /// <param name="shape1"></param>
    /// <param name="shape2"></param>
    /// <returns></returns>
    public static bool ShapesAreEqual(Vector2Int[] shape1, Vector2Int[] shape2)
    {
        if (shape1 == null || shape2 == null) return shape1 == shape2;
        if (shape1.Length != shape2.Length) return false;

        for (var i = 0; i < shape1.Length; i++)
        {
            if (shape1[i] != shape2[i])
                return false;
        }

        return true;
    }
}