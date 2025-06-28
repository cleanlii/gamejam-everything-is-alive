using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionController : MonoBehaviour
{
    [SerializeField] Animator fadeIconAnimator;
    [SerializeField] Animator fadeBlockAnimator;

    private void OnEnable()
    {
        // 订阅事件
        LevelStateController.OnFadeIconTrigger += TriggerFadeIcon;
        LevelStateController.OnFadeBlockTrigger += TriggerFadeBlock;
    }

    private void OnDisable()
    {
        // 取消订阅事件
        LevelStateController.OnFadeIconTrigger -= TriggerFadeIcon;
        LevelStateController.OnFadeBlockTrigger -= TriggerFadeBlock;
    }

    private void TriggerFadeIcon()
    {
        if (fadeIconAnimator != null)
        {
            fadeIconAnimator.SetTrigger("FadeIcon");
        }
        else
        {
            Debug.LogError("FadeIcon Animator component not found!");
        }
    }

    private void TriggerFadeBlock()
    {
        if (fadeBlockAnimator != null)
        {
            fadeBlockAnimator.SetTrigger("FadeBlock");
        }
        else
        {
            Debug.LogError("FadeBlock Animator component not found!");
        }
    }
}
