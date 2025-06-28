using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeIconController : MonoBehaviour
{
    public Camera mainCamera;
    public RenderTexture renderTexture;
    public Animator fadeIconAnimator;
    public Image image;

    private readonly float animationDuration = 0.3f; // 单次Shine时长
    private readonly int repeatCount = 1; // Shine次数

    public void Trigger()
    {
        if (fadeIconAnimator != null)
            fadeIconAnimator.SetTrigger("FadeIcon");
        else
            Debug.LogError("FadeIcon Animator component not found!");
    }

    public void AnimateShine()
    {
        // AudioManager.PlaySound("小世界_SE_Tag激活", "SE", false);
        StartCoroutine(AnimateShine(image.materialForRendering, animationDuration, repeatCount));
    }

    private IEnumerator AnimateShine(Material material, float duration, int repeats)
    {
        for (var i = 0; i < repeats; i++)
        {
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                var value = Mathf.Lerp(0f, 1f, time / duration);
                material.SetFloat("_ShineLocation", value);
                yield return null;
            }
        }
    }

    // 启用 RenderTexture
    private void EnableRenderTexture()
    {
        mainCamera.targetTexture = renderTexture;
    }

    // 禁用 RenderTexture
    private void DisableRenderTexture()
    {
        mainCamera.targetTexture = null;
    }
}