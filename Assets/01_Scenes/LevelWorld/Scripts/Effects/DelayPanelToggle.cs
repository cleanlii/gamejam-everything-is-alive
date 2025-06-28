using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DelayPanelToggle : MonoBehaviour
{
    public List<ButtonPanelPair> buttonPanelPairs; // 所有按钮和面板对
    [SerializeField] GameObject planPanel;
    [SerializeField] GameObject planMapBg;
    public float sceneStartDelay = 2.0f; // 场景开始时的延迟时间（以秒为单位）

    private void Start()
    {
        // 为每个按钮添加点击事件监听器
        foreach (var pair in buttonPanelPairs)
        {
            if (pair.button != null)
            {
                pair.button.onClick.AddListener(() => OnButtonClick(pair));
            }
        }
    }

    // public void OpenPlanPanel()
    // {
    //     StartCoroutine(ShowPanelWithDelay(planPanel, 1.5f));
    //     StartCoroutine(ShowPanelWithDelay(planMapBg, 1.5f));
    // }

    private void OnButtonClick(ButtonPanelPair pair)
    {
        if (!pair.isButtonClickEnabled)
        {
            return;
        }

        // 隐藏指定面板
        if (pair.panelToHide != null)
        {
            foreach (var panel in pair.panelToHide)
            {
                if (panel != null)
                {
                    StartCoroutine(HidePanelWithDelay(panel, pair.hideDelay));
                }
            }
        }

        // 显示指定面板
        if (pair.panelToShow != null)
        {
            foreach (var panel in pair.panelToShow)
            {
                if (panel != null)
                {
                    StartCoroutine(ShowPanelWithDelay(panel, pair.showDelay));
                }
            }
        }
    }

    private IEnumerator ShowPanelWithDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        Animator animator = panel.GetComponent<Animator>();
        if (animator != null)
        {
            panel.SetActive(true);
            animator.SetTrigger("Enter");
        }
        else
        {
            panel.SetActive(true);
        }
    }

    private IEnumerator HidePanelWithDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        Animator animator = panel.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Exit");
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        }
        panel.SetActive(false);
    }

    public void SetButtonClickEnabled(Button button, bool isEnabled)
    {
        var pair = buttonPanelPairs.Find(b => b.button == button);
        if (pair != null)
        {
            pair.isButtonClickEnabled = isEnabled;
            button.interactable = isEnabled;
        }
    }
}

[Serializable]
public class ButtonPanelPair
{
    public Button button;
    public List<GameObject> panelToHide;
    public List<GameObject> panelToShow;
    public float showDelay = 1.0f; // 显示延迟时间
    public float hideDelay = 1.0f; // 隐藏延迟时间
    public bool isButtonClickEnabled = true;
}
