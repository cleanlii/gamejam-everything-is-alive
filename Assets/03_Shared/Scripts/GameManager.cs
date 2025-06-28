using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using cfg;
using cfg.world;
using SimpleJSON;
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
    private GameState gameState = GameState.World;
    public int levelRounds;
    public List<int> levelKeyRounds;

    public static GameState GameState
    {
        // 外界访问私有成员变量的方法
        get => Instance.gameState; // setter访问器
        set => Instance.gameState = value; // getter访问器
    }
    
    private Tables _tables;

    // TODO: 临时判断是否第一次进入大世界
    public bool isFirstPlay = true;

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
    }
    
    
    #region 场景跳转

    public void LoadSceneWithFade(string sceneName)
    {
        SceneController.Instance.FadeToScene(sceneName);
    }

    public void LoadMainWorld()
    {
        SceneController.Instance.ExecuteCoroutines(
            AudioManager.Current.FadeOutAllSounds(0.5f),
            SceneController.Instance.FadeOutAndLoadScene("TestMainWorld"),
            AudioManager.Current.FadeInAndPlaySound("大世界_BGM_欢乐曲", AudioType.BGM, true, 0.1f)
        );
        Instance.gameState = GameState.World;
    }

    public void LoadLevelWorld()
    {
        SceneController.Instance.ExecuteCoroutines(
            AudioManager.Current.FadeOutAllSounds(0.5f),
            SceneController.Instance.FadeOutAndLoadScene("TestLevelWorld"),
            AudioManager.Current.FadeInAndPlaySound("登录界面_BGM_MainTitle", AudioType.BGM, true, 0.1f)
        );
        Instance.gameState = GameState.Level;
    }

    // TODO: 根据剧情ID跳转or读取对应剧本
    public void LoadDialogWorld()
    {
        SceneController.Instance.ExecuteCoroutines(
            SceneController.Instance.FadeOutAndLoadScene("TestDialogSystem")
        );
    }

    #endregion
    
}


[Serializable]
public enum GameState
{
    MainMenu,
    World,
    Level
}