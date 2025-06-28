using System;
using System.Collections;
using UnityEngine;

public class AioShaderAnimator : MonoBehaviour
{
    private readonly float animationDuration = 0.4f; // 单次Shine时长
    private readonly int repeatCount = 1; // Shine次数

    public void AnimateShader(Material material, string parameterName, Action onComplete)
    {
        AudioManager.PlaySound("小世界_SE_Tag激活", AudioType.SE, false);
        StartCoroutine(AnimateParameter(material, parameterName, animationDuration, repeatCount, onComplete));
    }

    private IEnumerator AnimateParameter(Material material, string parameterName, float duration, int repeats, Action onComplete)
    {
        for (var i = 0; i < repeats; i++)
        {
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                var value = Mathf.Lerp(0, 1, time / duration);
                material.SetFloat(parameterName, value);
                yield return null;
            }
        }

        onComplete?.Invoke();
    }
}