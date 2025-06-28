using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using cfg.level;
using DG.Tweening;
using PackageGame.Global;
using PackageGame.Level.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LevelViewController : MonoBehaviour
{
    [Header("各大主要界面_全局引用")]
    [Space(5)]
    public RectTransform inventoryPanel;
    public GameObject resultPanel;
    public SidePanelHandler leftStorePanel;
    [SerializeField] private DragReminderController dragReminder;
    [SerializeField] private float panelDelay = 1.0f; // 等待时间，单位为秒

    [Header("组件引用_车厢界面")]
    [Space(5)]
    [SerializeField] private Image closeTargetArea;
    [SerializeField] private SidePanelHandler leftTaskPanel;
    [SerializeField] private SidePanelHandler leftCabinetPanel;
    [SerializeField] private SidePanelHandler rightInventoryPanel;
    // [SerializeField] private CanvasGroup taskCanvasGroup;
    // [SerializeField] private CanvasGroup storeCanvasGroup;
    [SerializeField] private CanvasGroup backpackCanvasGroup;

    [Header("数值显示_拖拽物品信息板")]
    [Space(5)]
    [SerializeField] private SidePanelHandler draggingInfoReminder;
    [SerializeField] private TextMeshProUGUI draggingInfoName;
    [SerializeField] private TextMeshProUGUI draggingInfoNumber;
    [SerializeField] private GameObject draggingCargoIcon;
    [SerializeField] private GameObject draggingGoodsIcon;
    [SerializeField] private ItemTagUIElementDictionary draggingItemTagBind;

    [Header("Tag标签_Prefab引用")]
    [Space(5)]
    [SerializeField] private List<GameObject> coolTagHandler;
    [SerializeField] private List<GameObject> naturalTagHandler;
    [SerializeField] private List<GameObject> fragileTagHandler;

    private static LevelViewController _instance;

    public static LevelViewController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelViewController>();
                if (_instance == null) Debug.LogError("No UIController found in the scene.");
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // 检查是否已有实例存在且不是当前实例
        if (_instance != null && _instance != this)
            Destroy(gameObject);
        else
        {
            _instance = this;
            // 不要在场景切换时保留
            // DontDestroyOnLoad(gameObject);
        }

        coolTagHandler = new List<GameObject>();
        naturalTagHandler = new List<GameObject>();
        fragileTagHandler = new List<GameObject>();
    }

    private void Start()
    {
    }

    private void Update()
    {
    }

    private void OnEnable()
    {
        // 订阅事件
        // LevelStateController.OnObjDragging += DisableButtonInc;
        // LevelStateController.OnObjUndragged += EnableButtonInc;

        // 弹窗事件
        // ItemManager.OnDraggingItemInfo += UpdateDraggingInfo;
        // InventoryManager.OnDraggingBpInfo += UpdateDraggingInfo;
    }

    private void OnDisable()
    {
        // 取消订阅事件
        // LevelStateController.OnObjDragging -= DisableButtonInc;
        // LevelStateController.OnObjUndragged -= EnableButtonInc;

        // ItemManager.OnDraggingItemInfo -= UpdateDraggingInfo;
        // InventoryManager.OnDraggingBpInfo -= UpdateDraggingInfo;
    }
    
    #region 拖拽中物品信息UI

    private void UpdateDraggingInfo(ObjectInformation objInfo)
    {
        // 根据具体类型进行处理
        if (objInfo is ItemInformation itemInfo)
            UpdateDraggingInfoUI(itemInfo);
        else if (objInfo is PropInformation propInfo)
            UpdateDraggingInfoUI(propInfo);
        else if (objInfo is BackpackInformation bpInfo)
            UpdateDraggingInfoUI(bpInfo);
        else
            Debug.LogWarning("Unsupported information type passed to DraggingInfoPanel.");

        // draggingInfoReminder.Show();
    }

    public void ResetDraggingInfo()
    {
        // foreach (var tag in draggingItemTagBind)
        // {
        //     SetActiveForAll(tag.Value.data, false);
        // }

        // draggingInfoReminder.Hide();
    }

    private void UpdateDraggingInfoUI(ItemInformation itemInfo)
    {
        draggingInfoName.text = itemInfo.name;
        draggingInfoNumber.text = itemInfo.value.ToString();

        foreach (var tag in draggingItemTagBind)
        {
            SetActiveForAll(tag.Value.data, false);

            if (itemInfo.tags.Any(t => t == tag.Key))
            {
                if (tag.Value.data.Count > 1)
                    tag.Value.data[1].SetActive(true);
                else
                    tag.Value.data[0].SetActive(true);
            }
        }

        draggingGoodsIcon.SetActive(false);
        draggingCargoIcon.SetActive(true);
    }

    private void UpdateDraggingInfoUI(PropInformation propInfo)
    {
        // Debug.Log("Updating propInfo" + propInfo.name);

        draggingInfoName.text = propInfo.name;
        draggingInfoNumber.text = propInfo.value.ToString();

        foreach (var tag in draggingItemTagBind)
        {
            // SetActiveForAll(tag.Value.data, false);

            if (propInfo.tags.Any(t => t == tag.Key))
            {
                tag.Value.data[0].SetActive(true);
                // Debug.Log("Updating propInfo" + propInfo.tags[0]);
                // Debug.Log("Activate Tag: " + tag.Value.data[0]);
            }
        }

        draggingGoodsIcon.SetActive(true);
        draggingCargoIcon.SetActive(false);
    }

    private void UpdateDraggingInfoUI(BackpackInformation bpInfo)
    {
        draggingInfoName.text = bpInfo.name;
        draggingInfoNumber.text = bpInfo.value.ToString();

        foreach (var tag in draggingItemTagBind)
        {
            SetActiveForAll(tag.Value.data, false);

            if (bpInfo.tag == tag.Key) SetActiveForAll(tag.Value.data, true);
        }

        draggingGoodsIcon.SetActive(true);
        draggingCargoIcon.SetActive(false);
    }

    #endregion

    #region 拖拽中提示信息

    public void ShowRotateReminder()
    {
        // rotateReminder.Show();
        dragReminder.ProcessMouseDrag();
    }

    public void HideRotateReminder()
    {
        // rotateReminder.Hide();
        dragReminder.ProcessMouseDrop();
    }

    #endregion
    
    #region UI组件获取接口

    public RectTransform GetInventoryPanel()
    {
        return inventoryPanel.GetComponent<RectTransform>();
    }

    public RectTransform GetResultPanel()
    {
        return resultPanel.GetComponent<RectTransform>();
    }

    // public Sprite GetBuildingIconSprite(BuildingType source)
    // {
    //     return buildings[source];
    // }

    # endregion

    #region UI激活控制

    public void OpenMyBackpackInstant()
    {
        backpackCanvasGroup.alpha = 1;
        closeTargetArea.raycastTarget = true;
        rightInventoryPanel.ShowInstant();
        // AudioManager.PlaySound("小世界_SE_打开货箱", AudioType.SE, false);
    }

    public void CloseMyBackpackInstant()
    {
        // leftCabinetPanel.HideInstant();
        rightInventoryPanel.HideInstant(() => { backpackCanvasGroup.alpha = 0; });
        closeTargetArea.raycastTarget = false;
        // AudioManager.PlaySound("小世界_SE_关闭货箱", AudioType.SE, false);
    }

    public void OpenMyBackpack()
    {
        backpackCanvasGroup.interactable = true;
        backpackCanvasGroup.blocksRaycasts = true;
        // backpackCanvasGroup.alpha = 1;
        closeTargetArea.raycastTarget = true;
        rightInventoryPanel.SetVisibility(true);
        AudioManager.PlaySound("小世界_SE_打开货箱", AudioType.SE, false);
    }

    public void CloseMyBackpack()
    {
        leftCabinetPanel.Hide();
        // rightInventoryPanel.Hide(() => { backpackCanvasGroup.alpha = 0; });
        rightInventoryPanel.Hide();
        closeTargetArea.raycastTarget = false;
        backpackCanvasGroup.interactable = false;
        backpackCanvasGroup.blocksRaycasts = false;
        AudioManager.PlaySound("小世界_SE_关闭货箱", AudioType.SE, false);
    }

    private void SetActiveForAll(List<GameObject> handlers, bool isActive)
    {
        foreach (var handler in handlers)
        {
            handler.SetActive(isActive);
        }
    }
    
    private void EnableButtonInc()
    {
        // planRevertBtn.interactable = true;
    }

    private void DisableButtonInc()
    {
        // planRevertBtn.interactable = false;
    }

    private IEnumerator OpenPanelWithDelay(GameObject panel, bool isFading)
    {
        if (isFading)
        {
            LevelStateController.Instance.TriggerFadeBlock(); // 触发淡出动画
            yield return new WaitForSeconds(panelDelay); // 等待指定时间
        }

        // yield return StartCoroutine(iconManager.HideIcons());
        panel.SetActive(true); // 激活面板
    }

    private IEnumerator ClosePanelWithDelay(GameObject panel, bool isFading)
    {
        if (isFading)
        {
            LevelStateController.Instance.TriggerFadeBlock(); // 触发淡出动画
            yield return new WaitForSeconds(panelDelay); // 等待指定时间
        }

        // yield return StartCoroutine(iconManager.ShowIcons());
        panel.SetActive(false); // 隐藏面板
    }

    #endregion
}

public enum SlideDirection
{
    Up,
    Down,
    Left,
    Right
}