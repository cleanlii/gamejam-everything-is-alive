using System;
using System.Collections;
using System.Collections.Generic;
using cfg;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static readonly string configPath = "LubanOutput/Data";

    public static GameManager Instance;

    [Header("数据统计")]
    [Space(5)]
    [SerializeField] private float timeCount; // 总游玩时间
    [SerializeField] private int deathCount;
    [SerializeField] private int levelCount;

    [Header("剧情进度状态")]
    [Space(5)]
    public bool[] gallery;

    [Header("游戏状态")]
    [Space(5)]
    [SerializeField]
    private GameState gameState = GameState.MainMenu;
    public int levelRounds;
    public List<int> levelKeyRounds;

    [Header("关卡进度")]
    [Space(5)]
    public LevelProgress levelProgress;

    public static GameState GameState
    {
        get => Instance.gameState;
        set => Instance.gameState = value;
    }

    private Tables _tables;
    public bool isFirstPlay = true;

    // 事件：关卡完成时触发
    public static event Action<int> OnLevelCompleted;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;

        // 初始化关卡进度
        if (levelProgress == null) levelProgress = new LevelProgress();
    }

    private void Start()
    {
        // 订阅关卡完成事件
        OnLevelCompleted += HandleLevelCompleted;
    }

    private void OnDestroy()
    {
        // 取消订阅
        OnLevelCompleted -= HandleLevelCompleted;
    }

    /// <summary>
    ///     处理关卡完成
    /// </summary>
    /// <param name="levelIndex">完成的关卡索引 (1, 2, 3)</param>
    private void HandleLevelCompleted(int levelIndex)
    {
        // var wasNewlyCompleted = levelProgress.CompleteLevel(levelIndex);
        //
        // if (wasNewlyCompleted)
        // {
        //     Debug.Log($"关卡 {levelIndex} 首次完成！");
        //
        //     // 标记需要播放圆满动画
        //     levelProgress.SetPendingCompletionAnimation(levelIndex);
        //
        //     // 检查是否有新关卡解锁
        //     var nextLevel = levelIndex + 1;
        //     if (nextLevel <= 3 && levelProgress.IsLevelUnlocked(nextLevel))
        //     {
        //         // 标记需要播放解锁动画
        //         levelProgress.SetPendingUnlockAnimation(nextLevel);
        //         Debug.Log($"关卡 {nextLevel} 已解锁，准备播放解锁动画");
        //     }
        // }
    }

    /// <summary>
    ///     检查并播放待播放的解锁动画（在LevelSelect场景中调用）
    /// </summary>
    public void CheckAndPlayUnlockAnimations()
    {
        var pendingAnimations = levelProgress.GetAndClearPendingAnimations();
        foreach (var levelToUnlock in pendingAnimations)
        {
            OnPlayUnlockAnimation?.Invoke(levelToUnlock);
            Debug.Log($"播放关卡 {levelToUnlock} 解锁动画");
        }
    }

    #region 场景跳转

    public void LoadSceneWithFade(string sceneName)
    {
        SceneController.Instance.FadeToScene(sceneName);
    }

    public void LoadLevelSelect()
    {
        SceneController.Instance.ExecuteCoroutines(
            AudioManager.Current.FadeOutAllSounds(0.5f),
            SceneController.Instance.FadeOutAndLoadScene("LevelSelect"),
            AudioManager.Current.FadeInAndPlaySound("BGM_LevelSelect", AudioType.BGM, true, 0.1f)
        );
        Instance.gameState = GameState.Select;
    }

    public void LoadLevel1()
    {
        if (!levelProgress.IsLevelUnlocked(1))
        {
            Debug.LogWarning("关卡1未解锁！");
            return;
        }

        AudioManager.PlaySound("BGS_EnterLevel1", AudioType.BGS, false);

        SceneController.Instance.ExecuteCoroutines(
            WaitForSecondsCoroutine(1f),
            SceneController.Instance.FadeOutAndLoadScene("Level1"),
            AudioManager.Current.FadeInAndPlaySound("BGM_Level1", AudioType.BGM, true, 2f)
        );
        Instance.gameState = GameState.Level;
    }

    public void LoadLevel2()
    {
        if (!levelProgress.IsLevelUnlocked(2))
        {
            Debug.LogWarning("关卡2未解锁！");
            return;
        }

        AudioManager.PlaySound("BGS_EnterLevel2", AudioType.BGS, false);

        SceneController.Instance.ExecuteCoroutines(
            WaitForSecondsCoroutine(1f),
            SceneController.Instance.FadeOutAndLoadScene("Level2"),
            AudioManager.Current.FadeInAndPlaySound("BGM_Level2", AudioType.BGM, true, 2f)
        );
        Instance.gameState = GameState.Level;
    }

    public void LoadLevel3()
    {
        if (!levelProgress.IsLevelUnlocked(3))
        {
            Debug.LogWarning("关卡3未解锁！");
            return;
        }

        AudioManager.PlaySound("BGS_EnterLevel3", AudioType.BGS, false);

        SceneController.Instance.ExecuteCoroutines(
            WaitForSecondsCoroutine(1f),
            SceneController.Instance.FadeOutAndLoadScene("Level3"),
            AudioManager.Current.FadeInAndPlaySound("BGM_Level3", AudioType.BGM, true, 2f)
        );
        Instance.gameState = GameState.Level;
    }

    public void LoadMainMenu()
    {
        SceneController.Instance.ExecuteCoroutines(
            AudioManager.Current.FadeOutAllSounds(0.5f),
            SceneController.Instance.FadeOutAndLoadScene("MainMenu"),
            AudioManager.Current.FadeInAndPlaySound("BGM_MainMenu", AudioType.BGM, true, 0.1f)
        );
        Instance.gameState = GameState.MainMenu;
    }

    private IEnumerator WaitForSecondsCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    #endregion

    #region 关卡解锁

    // 事件定义
    public static event Action<int> OnPlayCompletionAnimation; // 播放关卡按钮圆满动画
    public static event Action<int> OnPlayUnlockAnimation; // 播放关卡解锁动画
    public static event Action OnPlayEndingAnimation; // 播放结局动画
    public static event Action<int> OnSetLevelUnlocked; // 直接设置关卡为已解锁状态（跳过动画）

    /// <summary>
    ///     完成当前关卡（由关卡脚本调用）
    /// </summary>
    /// <param name="levelIndex">完成的关卡索引</param>
    /// <param name="completionTime">完成时间</param>
    public void CompleteCurrentLevel(int levelIndex, float completionTime = 0f)
    {
        // 更新最佳时间
        if (completionTime > 0f) levelProgress.UpdateBestTime(levelIndex, completionTime);

        // **关键修复：在修改状态之前检查是否为首次完成**
        var isFirstTimeComplete = !levelProgress.IsLevelCompleted(levelIndex);

        // 触发关卡完成事件（用于统计等其他逻辑）
        OnLevelCompleted?.Invoke(levelIndex);

        // 完成关卡（这里会修改levelCompleted状态）
        levelProgress.CompleteLevel(levelIndex);

        // 基于之前检查的结果决定是否播放动画
        if (isFirstTimeComplete)
        {
            levelProgress.SetPendingCompletionAnimation(levelIndex);
            Debug.Log($"关卡 {levelIndex} 首次完成，标记播放圆满动画");
        }
        else
            Debug.Log($"关卡 {levelIndex} 重复完成，跳过圆满动画");

        // 根据关卡决定跳转逻辑和解锁下一关
        if (levelIndex == 1)
        {
            // L1完成：解锁L2，标记L2需要播放解锁动画（仅首次）
            if (!levelProgress.IsLevelUnlocked(2))
            {
                levelProgress.UnlockLevel(2);
                levelProgress.SetPendingUnlockAnimation(2);
                Debug.Log("L1完成，L2已解锁，准备播放L2解锁动画");
            }

            LoadLevelSelect();
        }
        else if (levelIndex == 2)
        {
            // L2完成：解锁L3，标记L3需要播放解锁动画（仅首次）
            if (!levelProgress.IsLevelUnlocked(3))
            {
                levelProgress.UnlockLevel(3);
                levelProgress.SetPendingUnlockAnimation(3);
                Debug.Log("L2完成，L3已解锁，准备播放L3解锁动画");
            }

            LoadLevelSelect();
        }
        else if (levelIndex == 3)
        {
            // L3完成：设置待播放结局动画标记（仅首次），然后回到关卡选择界面
            if (isFirstTimeComplete)
            {
                levelProgress.SetPendingEndingAnimation();
                Debug.Log("L3首次完成，准备播放结局动画");
            }
            else
                Debug.Log("L3重复完成，跳过结局动画");

            LoadLevelSelect();
        }
    }

    /// <summary>
    ///     检查并播放待播放的动画（在LevelSelect场景中调用）
    /// </summary>
    public void CheckAndPlayPendingAnimations()
    {
        // 如果是首次进入游戏，标记为已进入
        if (levelProgress.IsFirstTimeEntering())
        {
            levelProgress.MarkGameEntered();
            Debug.Log("首次进入LevelSelect场景，将播放L1解锁动画");
        }

        // 首先初始化所有已解锁关卡的状态（跳过不需要播放动画的关卡）
        InitializeUnlockedLevelsState();

        // 开始播放动画序列
        StartCoroutine(PlayAnimationSequence());
    }

    /// <summary>
    ///     初始化已解锁关卡的状态（跳过解锁动画）
    /// </summary>
    private void InitializeUnlockedLevelsState()
    {
        // 获取待播放动画的关卡列表（不清除）
        var pendingUnlocks = levelProgress.GetPendingAnimations();
        var pendingCompletions = levelProgress.GetPendingCompletionAnimations();

        // 检查所有关卡的解锁和完成状态
        for (var i = 1; i <= 3; i++)
        {
            var isUnlocked = levelProgress.IsLevelUnlocked(i);
            var isCompleted = levelProgress.IsLevelCompleted(i);

            // 如果关卡已解锁且不在待播放解锁动画列表中，直接设置为已解锁状态
            if (isUnlocked && !pendingUnlocks.Contains(i))
            {
                OnSetLevelUnlocked?.Invoke(i);
                Debug.Log($"关卡 {i} 已解锁，跳过解锁动画，直接设置为解锁状态");
            }

            // 如果关卡已完成且不在待播放圆满动画列表中，确保显示为完成状态
            if (isCompleted && !pendingCompletions.Contains(i))
            {
                // 这里不需要额外事件，因为OnSetLevelUnlocked会根据完成状态设置正确的显示
                Debug.Log($"关卡 {i} 已完成，跳过圆满动画");
            }
        }
    }

    /// <summary>
    ///     播放动画序列
    /// </summary>
    private IEnumerator PlayAnimationSequence()
    {
        // 获取待播放的动画
        var pendingCompletions = levelProgress.GetAndClearPendingCompletionAnimations();
        var pendingUnlocks = levelProgress.GetAndClearPendingAnimations();
        var hasEndingAnimation = levelProgress.HasPendingEndingAnimation();

        // 1. 播放圆满动画
        foreach (var completedLevel in pendingCompletions)
        {
            OnPlayCompletionAnimation?.Invoke(completedLevel);
            Debug.Log($"播放关卡 {completedLevel} 圆满动画");
            yield return new WaitForSeconds(2f); // 等待圆满动画播放
        }

        // 2. 如果有结局动画，播放结局动画
        if (hasEndingAnimation)
        {
            Debug.Log("开始播放Level3结局动画");
            levelProgress.ClearPendingEndingAnimation();
            OnPlayEndingAnimation?.Invoke();

            // 等待结局动画播放完成
            yield return new WaitForSeconds(GetEndingAnimationDuration());

            Debug.Log("Level3结局动画播放完成，即将返回主菜单");

            // 结局动画播放完后返回主菜单
            yield return new WaitForSeconds(1f); // 短暂停顿
            LoadMainMenu();
            yield break; // 结束协程，不再播放解锁动画
        }

        // 3. 播放解锁动画（如果没有结局动画）
        foreach (var levelToUnlock in pendingUnlocks)
        {
            OnPlayUnlockAnimation?.Invoke(levelToUnlock);
            Debug.Log($"播放关卡 {levelToUnlock} 解锁动画");
            yield return new WaitForSeconds(2f); // 等待解锁动画播放
        }
    }

    /// <summary>
    ///     获取结局动画时长
    /// </summary>
    private float GetEndingAnimationDuration()
    {
        return 8f; // 结局动画时长，根据实际调整
    }

    #endregion
}

[Serializable]
public enum GameState
{
    MainMenu,
    Select,
    Level
}

[Serializable]
public class LevelProgress
{
    [Header("关卡状态")]
    [SerializeField] private bool[] levelCompleted = new bool[4]; // 索引0不使用，1-3对应L1-L3
    [SerializeField] private bool[] levelUnlocked = new bool[4]; // 索引0不使用，1-3对应L1-L3
    [SerializeField] private bool isFirstTimeEnteringGame = true; // 新增：是否为首次进入游戏

    [Header("动画状态")]
    [SerializeField] private List<int> pendingUnlockAnimations = new(); // 待播放解锁动画的关卡
    [SerializeField] private List<int> pendingCompletionAnimations = new(); // 待播放圆满动画的关卡

    [Header("关卡数据")]
    [SerializeField] private float[] levelBestTimes = new float[4]; // 最佳通关时间
    [SerializeField] private int[] levelPlayCounts = new int[4]; // 关卡游玩次数

    public LevelProgress()
    {
        // 初始化：L1默认解锁
        levelUnlocked[1] = true;
        levelUnlocked[2] = false;
        levelUnlocked[3] = false;

        levelCompleted[1] = false;
        levelCompleted[2] = false;
        levelCompleted[3] = false;

        // 首次进入游戏时，标记L1需要播放解锁动画
        if (isFirstTimeEnteringGame)
        {
            pendingUnlockAnimations.Add(1);
            Debug.Log("首次进入游戏，L1需要播放解锁动画");
        }
    }

    public bool CompleteLevel(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > 3)
        {
            Debug.LogError($"无效的关卡索引: {levelIndex}");
            return false;
        }

        var wasFirstTime = !levelCompleted[levelIndex];
        levelCompleted[levelIndex] = true;
        levelPlayCounts[levelIndex]++;

        // 解锁下一关卡的逻辑已经移到GameManager.CompleteCurrentLevel中处理
        // 这里只处理完成状态的设置，避免重复逻辑

        if (wasFirstTime)
            Debug.Log($"关卡 {levelIndex} 首次完成！");
        else
            Debug.Log($"关卡 {levelIndex} 重复完成");

        return wasFirstTime;
    }

    /// <summary>
    ///     检查关卡是否已解锁
    /// </summary>
    /// <param name="levelIndex">关卡索引 (1-3)</param>
    /// <returns>是否已解锁</returns>
    public bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > 3)
            return false;

        return levelUnlocked[levelIndex];
    }

    /// <summary>
    ///     检查关卡是否已完成
    /// </summary>
    /// <param name="levelIndex">关卡索引 (1-3)</param>
    /// <returns>是否已完成</returns>
    public bool IsLevelCompleted(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > 3)
            return false;

        return levelCompleted[levelIndex];
    }

    /// <summary>
    ///     设置待播放解锁动画
    /// </summary>
    /// <param name="levelIndex">需要播放解锁动画的关卡索引</param>
    public void SetPendingUnlockAnimation(int levelIndex)
    {
        if (!pendingUnlockAnimations.Contains(levelIndex)) pendingUnlockAnimations.Add(levelIndex);
    }

    /// <summary>
    ///     设置待播放圆满动画
    /// </summary>
    /// <param name="levelIndex">需要播放圆满动画的关卡索引</param>
    public void SetPendingCompletionAnimation(int levelIndex)
    {
        if (!pendingCompletionAnimations.Contains(levelIndex)) pendingCompletionAnimations.Add(levelIndex);
    }

    /// <summary>
    ///     更新关卡最佳时间
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    /// <param name="time">通关时间</param>
    public void UpdateBestTime(int levelIndex, float time)
    {
        if (levelIndex < 1 || levelIndex > 3)
            return;

        if (levelBestTimes[levelIndex] == 0 || time < levelBestTimes[levelIndex])
        {
            levelBestTimes[levelIndex] = time;
            Debug.Log($"关卡 {levelIndex} 新纪录: {time:F2}秒");
        }
    }

    /// <summary>
    ///     获取关卡最佳时间
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    /// <returns>最佳时间</returns>
    public float GetBestTime(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > 3)
            return 0f;

        return levelBestTimes[levelIndex];
    }

    /// <summary>
    ///     获取关卡游玩次数
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    /// <returns>游玩次数</returns>
    public int GetPlayCount(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > 3)
            return 0;

        return levelPlayCounts[levelIndex];
    }

    /// <summary>
    ///     获取总进度百分比
    /// </summary>
    /// <returns>进度百分比 (0-100)</returns>
    public float GetOverallProgress()
    {
        var completedCount = 0;
        for (var i = 1; i <= 3; i++)
        {
            if (levelCompleted[i])
                completedCount++;
        }

        return completedCount / 3f * 100f;
    }

    /// <summary>
    ///     解锁指定关卡（不标记完成状态）
    /// </summary>
    /// <param name="levelIndex">关卡索引 (1-3)</param>
    public void UnlockLevel(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > 3)
        {
            Debug.LogError($"无效的关卡索引: {levelIndex}");
            return;
        }

        if (!levelUnlocked[levelIndex])
        {
            levelUnlocked[levelIndex] = true;
            Debug.Log($"关卡 {levelIndex} 已解锁！");
        }
    }

    /// <summary>
    ///     获取待播放的解锁动画列表（不清除）
    /// </summary>
    /// <returns>待播放解锁动画的关卡索引列表</returns>
    public List<int> GetPendingAnimations()
    {
        return new List<int>(pendingUnlockAnimations);
    }

    /// <summary>
    ///     获取待播放的圆满动画列表（不清除）
    /// </summary>
    /// <returns>待播放圆满动画的关卡索引列表</returns>
    public List<int> GetPendingCompletionAnimations()
    {
        return new List<int>(pendingCompletionAnimations);
    }

    /// <summary>
    ///     获取并清空待播放的解锁动画列表
    /// </summary>
    /// <returns>待播放解锁动画的关卡索引列表</returns>
    public List<int> GetAndClearPendingAnimations()
    {
        var result = new List<int>(pendingUnlockAnimations);
        pendingUnlockAnimations.Clear();
        return result;
    }

    /// <summary>
    ///     获取并清空待播放的圆满动画列表
    /// </summary>
    /// <returns>待播放圆满动画的关卡索引列表</returns>
    public List<int> GetAndClearPendingCompletionAnimations()
    {
        var result = new List<int>(pendingCompletionAnimations);
        pendingCompletionAnimations.Clear();
        return result;
    }

    // 结局动画相关方法
    [SerializeField] private bool pendingEndingAnimation;

    public void SetPendingEndingAnimation()
    {
        pendingEndingAnimation = true;
        Debug.Log("设置结局动画待播放标记");
    }

    public bool HasPendingEndingAnimation()
    {
        return pendingEndingAnimation;
    }

    public void ClearPendingEndingAnimation()
    {
        pendingEndingAnimation = false;
        Debug.Log("清除结局动画标记");
    }

    /// <summary>
    ///     标记已进入过游戏（首次进入LevelSelect场景时调用）
    /// </summary>
    public void MarkGameEntered()
    {
        if (isFirstTimeEnteringGame)
        {
            isFirstTimeEnteringGame = false;
            Debug.Log("已标记为进入过游戏");
        }
    }

    /// <summary>
    ///     检查是否为首次进入游戏
    /// </summary>
    /// <returns>是否为首次进入游戏</returns>
    public bool IsFirstTimeEntering()
    {
        return isFirstTimeEnteringGame;
    }

    // ... 其他方法保持不变

    /// <summary>
    ///     重置所有进度（调试用） - 更新版本
    /// </summary>
    public void ResetAllProgress()
    {
        for (var i = 1; i <= 3; i++)
        {
            levelCompleted[i] = false;
            levelUnlocked[i] = i == 1; // 只有L1保持解锁
            levelBestTimes[i] = 0f;
            levelPlayCounts[i] = 0;
        }

        pendingUnlockAnimations.Clear();
        pendingCompletionAnimations.Clear();
        pendingEndingAnimation = false;

        // 重置时也要重置首次进入标记，并重新添加L1解锁动画
        isFirstTimeEnteringGame = true;
        pendingUnlockAnimations.Add(1);

        Debug.Log("所有关卡进度已重置，L1重新标记为需要播放解锁动画");
    }
}