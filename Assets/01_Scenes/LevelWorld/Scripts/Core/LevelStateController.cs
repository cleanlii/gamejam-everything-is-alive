using System;
using System.Collections;
using System.Collections.Generic;
using cfg.level;
using UnityEngine;

// using UnityEngine.iOS;

public class LevelStateController : MonoBehaviour
{
    // private RectTransform currentInventoryPos;

    private static LevelStateController _instance;

    [Header("配置文件引用")]
    [Space(5)]
    public ItemLibrary itemLibrary;
    public InventoryLibrary inventoryLibrary;

    [Header("必要组件引用")]
    [Space(5)]
    public PropManager propManager;
    public ItemManager itemManager;
    public InventoryManager inventoryManager;

    [Header("小世界全局状态")]
    [Space(5)]
    // [SerializeField] private LevelState currentState;
    [SerializeField] private List<ItemTag> currentTags;
    [SerializeField] private bool isAnyObjDragging; // 防止双指操作
    [SerializeField] private int animationCount; // 防止动画时误操作
    [SerializeField] private ScreenMode currentScreenMode;
    // [SerializeField] private float minDragDistance;
    public List<int> fuels;
    private bool _isHudActive = true;

    public static LevelStateController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelStateController>();
                if (_instance == null) Debug.LogError("No LevelStateController found in the scene.");
            }

            return _instance;
        }
    }

    /// <summary>
    ///     中止各种动画
    /// </summary>
    public static event Action InteruptionEvent;

    // UI交替事件
    public static event Action OnFadeIconTrigger;
    public static event Action OnFadeBlockTrigger;

    // 状态切换事件
    public static event Action OnObjDragging;
    public static event Action OnObjUndragged;

    private RectTransform _originalInventoryPos;
    private Vector2Int _originalVehiclePos;

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

        ResetIndex();
        // SetupCameras();
        // ApplyLetterbox();

        InitializeStateController();
        SetScreenModeBasedOnDevice();
        // SetMinDragDistance();
    }

    private void Start()
    {
        InitializeLibraries();

        StartCoroutine(Frame());

        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }

        InitializeServices();
        InitializeGameplay();
        InitializeUI();
        InitializeVFX();

        // SetStateOpening();
    }

    #region 初始化小世界配置

    private void InitializeStateController()
    {
        currentTags = new List<ItemTag>();
    }

    private void InitializeLibraries()
    {
        itemLibrary.InitializeItem();
        itemLibrary.InitializeProp();
        inventoryLibrary.InitializeBackpack();
    }

    private void InitializeServices()
    {
        itemManager.Initialize();
        inventoryManager.Initialize();
        propManager.Initialize();
    }

    private void InitializeUI()
    {
    }

    private void InitializeGameplay()
    {
        inventoryManager.CreateMyInventory();

        itemManager.CreateMyItems();
    }

    private void InitializeVFX()
    {
        itemManager.InitializePutDownEffect();
    }

    private void ResetIndex()
    {
    }

    #endregion

    #region 动画状态控制

    public void StartResultAnimation()
    {
        StartAnyAnimating();
        // StartCoroutine(StartResultRoutine());
    }


    public void TriggerFadeIcon()
    {
        OnFadeIconTrigger?.Invoke();
    }

    public void TriggerFadeBlock()
    {
        OnFadeBlockTrigger?.Invoke();
    }

    public void StartAnyAnimating()
    {
        animationCount++;
    }

    public void StopAnyAnimating()
    {
        if (animationCount > 0) animationCount--;
    }

    public bool IsAnyAnimating()
    {
        return animationCount > 0;
    }

    public bool IsAnyDragging()
    {
        return itemManager.pickingItem != null || inventoryManager.pickingBp != null;
    }

    #endregion

    #region UI状态控制

    /// <summary>
    ///     设置车厢初始位置
    /// </summary>
    public RectTransform OriginalInventoryPos
    {
        get => _originalInventoryPos;
        set
        {
            if (_originalInventoryPos != value) _originalInventoryPos = value;
        }
    }

    #endregion

    #region 关卡状态控制

    // public enum LevelState
    // {
    //     Opening,
    //     Tasking,
    //     GameOver
    // }
    //
    // public static event Action<LevelState> OnLevelStateChanged;
    //
    // public bool IsGameOver()
    // {
    //     return currentState == LevelState.GameOver;
    // }
    //
    // public LevelState GetCurrentState()
    // {
    //     return currentState;
    // }
    //
    // public void SetLevelState(LevelState newState)
    // {
    //     if (currentState == newState)
    //     {
    //         // Debug.Log("The state is already " + newState);
    //         return;
    //     }
    //
    //     currentState = newState;
    //     // Debug.Log("State changed to " + newState);
    // }
    //
    // public void SetStateOpening()
    // {
    //     // StartCoroutine(iconManager.ShowIcons());
    //     currentState = LevelState.Opening;
    //     OnLevelStateChanged?.Invoke(currentState);
    // }
    //
    // public void SetStateTasking()
    // {
    //     currentState = LevelState.Tasking;
    //     OnLevelStateChanged?.Invoke(currentState);
    // }
    //
    // public bool IsStateTasking()
    // {
    //     return currentState == LevelState.Tasking;
    // }
    //
    // public bool IsStateOpening()
    // {
    //     return currentState == LevelState.Opening;
    // }

    #endregion

    #region 当前回合状态控制

    public void AddTag(ItemTag tag)
    {
        if (!currentTags.Contains(tag))
            currentTags.Add(tag);
    }

    public void RemoveTag(ItemTag tag)
    {
        if (currentTags.Contains(tag))
            currentTags.Remove(tag);
        else
            Debug.LogWarning("Can't remove tag becuz it's not included");
    }

    #endregion

    #region 各类获取接口

    public T GetService<T>() where T : class
    {
        foreach (var field in Instance.GetType().GetFields())
        {
            if (field.FieldType == typeof(T)) return field.GetValue(Instance) as T;
        }

        Debug.LogWarning($"Service of type {typeof(T)} not found.");
        return null;
    }

    public InventoryInitializer GetDefaultInventoryInit()
    {
        return inventoryManager.defaultInitializer;
    }

    public GameObject GetDefaultGridObj()
    {
        return inventoryManager.defaultGridObj;
    }

    #endregion

    #region 获取设备信息

    public ScreenMode GetCurrentScreenMode()
    {
        return currentScreenMode;
    }

    // public float GetCurrentMinDragDistance()
    // {
    //     return minDragDistance;
    // }

    private ScreenMode GetScreenModeBasedOnDevice()
    {
        if (IsIPad()) return ScreenMode.DoubleScale;

        if (IsIPhone()) return ScreenMode.SingleScale;

        return ScreenMode.SingleScale;
    }

    public void SetScreenModeBasedOnDevice()
    {
        if (IsIPad())
            currentScreenMode = ScreenMode.DoubleScale;
        else if (IsIPhone())
            currentScreenMode = ScreenMode.SingleScale;
        else
            currentScreenMode = ScreenMode.SingleScale;
    }

    // 根据设备类型或屏幕尺寸设置最小拖拽距离
    // private void SetMinDragDistance()
    // {
    //     var generation = Device.generation;
    //
    //     // 如果是 iPad，设置较大的拖拽距离
    //     if (IsIPad())
    //         minDragDistance = 5f; // iPad 屏幕较大，设置更大的拖拽距离
    //     // 如果是 iPhone，设置较小的拖拽距离
    //     else if (IsIPhone())
    //         minDragDistance = 2f; // iPhone 屏幕较小，使用默认的较小拖拽距离
    //     else
    //     {
    //         // 未知设备，使用默认的 5f
    //         minDragDistance = 2f;
    //     }
    // }

    // 判断是否为 iPhone
    private bool IsIPhone()
    {
        // DeviceGeneration generation = Device.generation;

        // return generation == DeviceGeneration.iPhoneUnknown ||
        //        (generation >= DeviceGeneration.iPhone && generation <= DeviceGeneration.iPhone14ProMax);
        if (SystemInfo.deviceModel.Contains("iPhone")) return true;

        return false;
    }

    // 判断是否为 iPad
    private bool IsIPad()
    {
        // DeviceGeneration generation = Device.generation;

        // return generation == DeviceGeneration.iPadUnknown ||
        //        (generation >= DeviceGeneration.iPad1Gen && generation <= DeviceGeneration.iPadPro11Inch4Gen);
        if (SystemInfo.deviceModel.Contains("iPad")) return true;

        return false;
    }

    // private void PrintDeviceName()
    // {
    //     var generation = Device.generation;
    //
    //     Debug.Log("!!!!!Generation Name!!!!!!!: " + generation);
    //     Debug.Log("!!!!!Device Name!!!!!!!: " + SystemInfo.deviceModel);
    // }

    #endregion

    // #region 圣器触发相关
    //
    //
    //
    // #endregion
}