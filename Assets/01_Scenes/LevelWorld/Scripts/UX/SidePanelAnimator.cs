using UnityEngine;

public abstract class SidePanelAnimator : MonoBehaviour
{
    // 是否启用动画
    protected bool isAnimating;

    /// <summary>
    ///     开始动画（由子类实现具体逻辑）
    /// </summary>
    public virtual void StartAnimation()
    {
        isAnimating = true;
    }

    /// <summary>
    ///     停止动画（由子类实现具体逻辑）
    /// </summary>
    public virtual void StopAnimation()
    {
        isAnimating = false;
    }
}