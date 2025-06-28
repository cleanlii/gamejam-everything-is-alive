using System;
using cfg.level;
using UnityEngine;

public class GridShapeCalculator
{
    /// <summary>
    ///     根据枚举类型获取Shape值
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Vector2Int[] GetShapeByType(GridShape shape)
    {
        switch (shape)
        {
            case GridShape.RECT_SQUARE_1x1:
                return RectUtils.CreateShape(
                    "1"
                );
            case GridShape.RECT_SQUARE_2x2:
                return RectUtils.CreateShape(
                    "11",
                    "11"
                );
            case GridShape.RECT_SQUARE_3x3:
                return RectUtils.CreateShape(
                    "111",
                    "111",
                    "111"
                );
            case GridShape.Z_SHAPE:
                return RectUtils.CreateShape(
                    "110",
                    "011"
                );
            case GridShape.Z_SHAPE_MIRRORED:
                return RectUtils.CreateShape(
                    "011",
                    "110"
                );
            case GridShape.X_SHAPE:
                return RectUtils.CreateShape(
                    "101",
                    "010",
                    "101"
                );
            case GridShape.CROSS_SHAPE:
                return RectUtils.CreateShape(
                    "010",
                    "111",
                    "010"
                );
            case GridShape.L_SHAPE_VERTICAL_FLAT:
                return RectUtils.CreateShape(
                    "10",
                    "10",
                    "11"
                );
            case GridShape.L_SHAPE_HORIZONTAL_FLAT:
                return RectUtils.CreateShape(
                    "110",
                    "010"
                );
            case GridShape.L_SHAPE_VERTICAL:
                return RectUtils.CreateShape(
                    "10",
                    "10",
                    "11"
                );
            case GridShape.L_SHAPE_HORIZONTAL:
                return RectUtils.CreateShape(
                    "111",
                    "100"
                );
            case GridShape.L_SHAPE_VERTICAL_SHORT:
                return RectUtils.CreateShape(
                    "10",
                    "11"
                );
            case GridShape.U_SHAPE_SMALL:
                return RectUtils.CreateShape(
                    "101",
                    "111"
                );
            case GridShape.U_SHAPE_BIG:
                return RectUtils.CreateShape(
                    "101",
                    "111",
                    "111"
                );
            case GridShape.C_SHAPE:
                return RectUtils.CreateShape(
                    "110",
                    "101",
                    "011"
                );
            case GridShape.RECT_HORIZONTAL_1x2:
                return RectUtils.CreateShape(
                    "11"
                );
            case GridShape.RECT_HORIZONTAL_1x3:
                return RectUtils.CreateShape(
                    "111"
                );
            case GridShape.RECT_HORIZONTAL_1x4:
                return RectUtils.CreateShape(
                    "1111"
                );
            case GridShape.RECT_HORIZONTAL_2x3:
                return RectUtils.CreateShape(
                    "111",
                    "111"
                );
            case GridShape.RECT_HORIZONTAL_2x4:
                return RectUtils.CreateShape(
                    "1111",
                    "1111"
                );
            case GridShape.RECT_VERTICAL_2x1:
                return RectUtils.CreateShape(
                    "1",
                    "1"
                );
            case GridShape.RECT_VERTICAL_3x1:
                return RectUtils.CreateShape(
                    "1",
                    "1",
                    "1"
                );
            case GridShape.RECT_VERTICAL_4x1:
                return RectUtils.CreateShape(
                    "1",
                    "1",
                    "1",
                    "1"
                );
            case GridShape.RECT_VERTICAL_3x2:
                return RectUtils.CreateShape(
                    "11",
                    "11",
                    "11"
                );
            case GridShape.T_SHAPE:
                return RectUtils.CreateShape(
                    "111",
                    "010",
                    "010"
                );
            case GridShape.T_SHAPE_REVERSED:
                return RectUtils.CreateShape(
                    "010",
                    "010",
                    "111"
                );
            case GridShape.T_SHAPE_SHORT:
                return RectUtils.CreateShape(
                    "111",
                    "010"
                );
            // 如果新增形状，在这里添加
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }
}