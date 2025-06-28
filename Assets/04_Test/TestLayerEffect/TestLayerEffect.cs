using UnityEngine;
using UnityEditor;

public enum EffectType
{
    Normarl正常,
    Multiply正片叠底,           // Opacity / Muiltiply 正片叠底
    Screen滤色,            // 滤色
    ColorDodge颜色减淡,        // 颜色减淡
    ColorBurn颜色加深,         // 颜色加深
    LinearDodge线形减淡,       // 线形减淡
    LinearBurn线形加深,        // 线形加深
    Overlay叠加,           // 叠加
    HardLight强光,         // 强光
    SoftLight柔光,         // 柔光
    VividLight亮光,        // 亮光
    LinearLight线性光,       // 线性光
    PinLight点光,          // 点光
    HardMix混合实色,           // 混合实色
    Difference差值,        // 差值
    Exclusion排除,         // 排除
    Hue色相                // 色相
}