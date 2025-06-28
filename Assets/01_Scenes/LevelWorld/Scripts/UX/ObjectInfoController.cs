using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectInfoController : MonoBehaviour
{
    [Header("当前交互状态")]
    [Space(5)]
    [SerializeField] private bool isDragging;
    [SerializeField] private bool isHovering;
    [SerializeField] private bool isVisible; // 信息框是否可见

    [Header("浮窗界面_物品信息")]
    [Space(5)]
    [SerializeField] private RectTransform objInfoBanner; // 悬浮查看信息框
    [SerializeField] private CanvasGroup canvasGroup; // 控制透明度
    [SerializeField] private TextMeshProUGUI nameTMP; // 显示名称
    [SerializeField] private TextMeshProUGUI sizeTMP; // 显示尺寸
    [SerializeField] private TextMeshProUGUI valueTitleTMP; // 价值类型
    // [SerializeField] private LocalizedString valueTitleTMP; // 价值类型
    [SerializeField] private TextMeshProUGUI valueTMP; // 显示价值
    [SerializeField] private TextMeshProUGUI desTMP; // 显示描述

    [Header("浮窗界面_喜恶信息")]
    [Space(5)]
    [SerializeField] private GameObject likeIcon;
    [SerializeField] private GameObject hateIcon;
    [SerializeField] private Image targetImg;

    [Header("浮窗界面_动画参数")]
    [Space(5)]
    [SerializeField] private CanvasScaler canvasScaler; // 用于动态调整偏移量
    [SerializeField] private Vector3 offset = new(10, -10, 0); // 信息框偏移量
    private Tween _fadeTween; // DoTween 动画缓存

    private Vector3 _adjustedOffset;
    private ItemManager _itemManager;

    private void Start()
    {
        var scaleFactor = canvasScaler.scaleFactor;
        _adjustedOffset = offset * scaleFactor;

        _itemManager = LevelStateController.Instance.GetService<ItemManager>();
    }

    private void Update()
    {
        if (isHovering && isVisible) UpdatePosition(Input.mousePosition);
    }

    private void OnEnable()
    {
        ItemInteraction.OnItemHovered += ProcessMouseEnter;
        ItemInteraction.OnItemUnhovered += ProcessMouseExit;
        BackpackInteraction.OnBpHovered += ProcessMouseEnter;
        BackpackInteraction.OnBpUnhovered += ProcessMouseExit;
        DragReminderController.OnObjectDragged += ProcessMouseDrag;
        DragReminderController.OnObjectUndragged += ProcessMouseEndDragged;

        LevelStateController.InteruptionEvent += HideBannerInstant;
    }

    private void OnDisable()
    {
        ItemInteraction.OnItemHovered -= ProcessMouseEnter;
        ItemInteraction.OnItemUnhovered -= ProcessMouseExit;
        BackpackInteraction.OnBpHovered -= ProcessMouseEnter;
        BackpackInteraction.OnBpUnhovered -= ProcessMouseExit;
        DragReminderController.OnObjectDragged -= ProcessMouseDrag;
        DragReminderController.OnObjectUndragged -= ProcessMouseEndDragged;

        LevelStateController.InteruptionEvent += HideBannerInstant;
    }

    #region 悬浮信息

    private void ProcessMouseEnter(ItemData itemData)
    {
        if (isDragging) return;

        if (itemData != null && !isHovering)
        {
            isHovering = true;
            // var objInfo = new ItemInformation(itemData);
            // UpdateBanner(objInfo);
            UpdateBannerWithLikeHate(itemData);
            ShowBanner();
        }
        else if (itemData == null && isHovering)
        {
            isHovering = false;
            HideBanner();
        }
    }

    private void ProcessMouseEnter(BackpackData bpData)
    {
        if (isDragging) return;

        if (bpData != null && !isHovering)
        {
            isHovering = true;
            var objInfo = new BackpackInformation(bpData);
            UpdateBanner(objInfo);
            ShowBanner();
        }
        else if (bpData == null && isHovering)
        {
            isHovering = false;
            HideBanner();
        }
    }

    private void ProcessMouseExit()
    {
        if (isDragging) return;

        HideBanner();
        isHovering = false;
    }

    private void ProcessMouseDrag()
    {
        isDragging = true;
        HideBannerInstant();
    }

    private void ProcessMouseEndDragged()
    {
        isDragging = false;
    }

    private void UpdateBanner(ItemInformation itemInfo)
    {
        nameTMP.text = itemInfo.name;
        sizeTMP.text = $"{itemInfo.size.ToString()}格";
        valueTMP.text = itemInfo.value.ToString();
        desTMP.text = itemInfo.description;

        // TODO: 本地化配置
        valueTitleTMP.text = "配送收入：";
    }

    private void UpdateBanner(BackpackInformation bpInfo)
    {
        nameTMP.text = bpInfo.name;
        sizeTMP.text = $"{bpInfo.size.ToString()}格";
        desTMP.text = bpInfo.description;

        // TODO: 本地化配置
        switch (bpInfo.state)
        {
            case ObjectState.Awaiting:
                valueTitleTMP.text = "买入价格：";
                valueTMP.text = bpInfo.price.ToString();
                break;
            case ObjectState.Resellable:
                valueTitleTMP.text = "卖出价格：";
                valueTMP.text = bpInfo.value.ToString();
                break;
        }
    }

    #endregion

    #region 浮窗显示

    /// <summary>
    ///     显示信息框并设置描述
    /// </summary>
    private void ShowBanner()
    {
        isVisible = true; // 设置为可见状态
        _fadeTween?.Kill(); // 停止当前动画
        canvasGroup.alpha = 0;
        _fadeTween = canvasGroup.DOFade(1f, 0.3f); // 使用 DoTween 淡入
    }

    /// <summary>
    ///     隐藏信息框
    /// </summary>
    private void HideBanner()
    {
        _fadeTween?.Kill();
        _fadeTween = canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        });
        isVisible = false;
    }

    /// <summary>
    ///     立即隐藏信息框
    /// </summary>
    private void HideBannerInstant()
    {
        if (!isVisible) return;

        _fadeTween?.Kill();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        isVisible = false;
    }

    /// <summary>
    ///     更新信息框位置
    /// </summary>
    private void UpdatePosition(Vector3 mousePosition)
    {
        if (!isVisible) return;

        // 直接使用 anchoredPosition 设置 UI 位置
        Vector2 newPosition = mousePosition + _adjustedOffset;
        objInfoBanner.anchoredPosition = newPosition;

        // 防止超出屏幕
        ClampToScreen();
    }

    /// <summary>
    ///     防止信息框超出屏幕
    /// </summary>
    private void ClampToScreen()
    {
        var corners = new Vector3[4];
        objInfoBanner.GetWorldCorners(corners);
        var adjustedPosition = objInfoBanner.anchoredPosition;

        // 获取屏幕宽高
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 计算 UI 组件的宽高
        var bannerWidth = objInfoBanner.rect.width * objInfoBanner.lossyScale.x;
        var bannerHeight = objInfoBanner.rect.height * objInfoBanner.lossyScale.y;

        // 右侧超出 → 移动到鼠标左侧
        if (corners[2].x > screenWidth)
            adjustedPosition.x -= bannerWidth + _adjustedOffset.x;

        // 左侧超出 → 移动到鼠标右侧
        if (corners[0].x < 0)
            adjustedPosition.x += bannerWidth + _adjustedOffset.x;

        // 底部超出 → 移动到鼠标上方
        if (corners[0].y < 0)
            adjustedPosition.y += bannerHeight + _adjustedOffset.y;

        // 顶部超出 → 移动到鼠标下方
        if (corners[1].y > screenHeight)
            adjustedPosition.y -= bannerHeight + _adjustedOffset.y;

        objInfoBanner.anchoredPosition = adjustedPosition;
    }

    #endregion

    #region 喜恶气泡显示

    /// <summary>
    ///     更新浮窗显示like/hate物品信息
    /// </summary>
    private void UpdateBannerWithLikeHate(ItemData itemData)
    {
        // 隐藏原有的信息显示元素
        sizeTMP.gameObject.SetActive(false);
        valueTMP.gameObject.SetActive(false);
        desTMP.gameObject.SetActive(false);
        valueTitleTMP.gameObject.SetActive(false);

        hateIcon.SetActive(false);
        likeIcon.SetActive(false);

        // 显示物品名称
        nameTMP.text = itemData.name;

        // 显示needCorner状态
        if (itemData.needCorner)
        {
            hateIcon.SetActive(true);
            targetImg.sprite = Resources.Load<Sprite>("CornerIcon");
            targetImg.SetNativeSize();
            return;
        }

        // 处理like/hate物品显示
        var hasLike = !string.IsNullOrEmpty(itemData.likeItemName);
        var hasHate = !string.IsNullOrEmpty(itemData.hateItemName);

        if (!hasLike && !hasHate)
        {
            likeIcon.SetActive(true);
            targetImg.sprite = Resources.Load<Sprite>("LoveAllIcon");
            targetImg.SetNativeSize();
            return;
        }

        // 控制like/hate图标显示
        likeIcon.SetActive(hasLike);
        hateIcon.SetActive(hasHate);

        // 更新target sprite显示
        UpdateTargetSprite(itemData, hasLike, hasHate);
    }

    /// <summary>
    ///     更新target sprite显示对应的like/hate物品图片
    /// </summary>
    private void UpdateTargetSprite(ItemData itemData, bool hasLike, bool hasHate)
    {
        ItemData targetItemData = null;

        // 优先显示hate物品，如果没有则显示like物品
        if (hasHate)
            targetItemData = _itemManager.GetItemDataByName(itemData.hateItemName);
        else if (hasLike) targetItemData = _itemManager.GetItemDataByName(itemData.likeItemName);

        // 如果找到了对应的物品数据，更新target sprite
        if (targetItemData != null && targetItemData.itemImage != null)
        {
            targetImg.sprite = targetItemData.itemImage.sprite;
            targetImg.SetNativeSize();
            targetImg.gameObject.SetActive(true);
        }
        else if (hasLike || hasHate)
        {
            // 如果有喜好设置但没找到对应物品，显示默认图片或隐藏
            targetImg.gameObject.SetActive(false);
        }
        else
        {
            // 如果没有任何喜好设置，隐藏target显示
            targetImg.gameObject.SetActive(false);
        }
    }

    #endregion
}