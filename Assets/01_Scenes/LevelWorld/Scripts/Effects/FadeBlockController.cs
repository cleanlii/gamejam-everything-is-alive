using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

public class FadeBlockController : MonoBehaviour
{
    [SerializeField] Sprite taskSprite;
    [SerializeField] Sprite shopSprite;
    [SerializeField] UnityEngine.UI.Image iconImg;
    [SerializeField] RectTransform iconRect;

    public void FlipIcon()
    {
        iconRect.rotation = new Quaternion(0, 180, 0, 0);
        transform.rotation = new Quaternion(0, 180, 0, 0);
    }

    public void SwapIcon()
    {
        // 重置镜像状态
        iconRect.rotation = new Quaternion(0, 0, 0, 0);
        transform.rotation = new Quaternion(0, 0, 0, 0);

        // if (LevelStateController.Instance.IsStateTasking())
        // {
        //     iconImg.sprite = taskSprite;
        // }
    }
}
