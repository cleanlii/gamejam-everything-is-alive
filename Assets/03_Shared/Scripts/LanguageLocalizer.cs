using System.Collections.Generic;
using System.Linq;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LanguageLocalizer : MonoBehaviour
{
    [Header("语言状态")]
    [Space(5)]
    [SerializeField] private SystemLanguage currentLanguage;
    [SerializeField] private TextMeshProUGUI[] myLanguages;
    [SerializeField] private SystemLanguage[] availableLanguages;
    [SerializeField] private int currentLanguageIndex;
    [SerializeField] private Material highlightTitleMat;
    [SerializeField] private Material normalTitleMat;

    [Header("切换按钮")]
    [Space(5)]
    [SerializeField] private Color32 highlightColor;
    [SerializeField] private Button changeChineseButton;

    [SerializeField] private Button changeEnglishButton;

    [SerializeField] private Button changeJapaneseButton;


    private readonly Dictionary<SystemLanguage, int> _languageMap = new();

    private static LanguageLocalizer _instance; // 单例

    private void Awake()
    {
        // 确保此对象在跨场景中不销毁
        if (_instance != null)
        {
            Destroy(gameObject); // 防止重复实例化
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 注册场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 初始化语言状态
        InitializeLocalization();

        // 初始化按钮事件
        // InitializeUIReferences();
    }

    private void OnDestroy()
    {
        // 注销场景加载事件，防止内存泄漏
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    ///     场景加载完成后重新初始化 UI 引用
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex != 0) return;
        InitializeUIReferences();
    }

    #region 本地化相关

    /// <summary>
    ///     初始化语言状态
    /// </summary>
    private void InitializeLocalization()
    {
        // 动态检测支持的语言
        availableLanguages = new SystemLanguage[myLanguages.Length];
        var index = 0;

        if (myLanguages.Length > 1)
        {
            foreach (var textMesh in myLanguages)
            {
                // 假设每个 textMesh 的名字或标签代表一种语言
                if (textMesh.name.Contains("English"))
                {
                    // English
                    availableLanguages[index] = SystemLanguage.English;
                    _languageMap[SystemLanguage.English] = index;
                    index++;
                }
                else if (textMesh.name.Contains("Japanese"))
                {
                    // Japanese
                    availableLanguages[index] = SystemLanguage.Japanese;
                    _languageMap[SystemLanguage.Japanese] = index;
                    index++;
                }
                else if (textMesh.name.Contains("Chinese"))
                {
                    // Chinese
                    availableLanguages[index] = SystemLanguage.Chinese;
                    _languageMap[SystemLanguage.Chinese] = index;
                    index++;
                }
            }

            // 初始化当前语言
            if (availableLanguages.Contains(availableLanguages[0]))
            {
                currentLanguage = availableLanguages[0];
                SetLanguage(currentLanguage, 0);
            }
            else
            {
                currentLanguage = SystemLanguage.English;
                SetLanguage(currentLanguage, _languageMap[currentLanguage]);
            }
        }
        else
        {
            // 默认情况为中文
            // TODO: I2库设置繁体中文和简体中文
            currentLanguage = SystemLanguage.Chinese;
            SetLanguage(currentLanguage, _languageMap[currentLanguage]);
        }
    }

    /// <summary>
    ///     动态初始化按钮和 UI 引用
    /// </summary>
    private void InitializeUIReferences()
    {
        // 尝试重新获取 Button 引用
        changeChineseButton = GameObject.Find("CN")?.GetComponent<Button>();
        changeEnglishButton = GameObject.Find("EN")?.GetComponent<Button>();
        changeJapaneseButton = GameObject.Find("JA")?.GetComponent<Button>();

        // 尝试重新获取 TMP 引用（假设这些元素有特定的标签或名称）
        myLanguages = GameObject.FindGameObjectsWithTag("Lang")
            .Select(obj => obj.GetComponent<TextMeshProUGUI>())
            .OrderBy(text => text.name) // 按名字排序，确保顺序一致
            .ToArray();

        foreach (var text in myLanguages)
        {
            text.material = new Material(normalTitleMat);
        }

        // 如果按钮存在，添加事件监听
        if (changeChineseButton != null)
        {
            changeChineseButton.onClick.RemoveAllListeners();
            changeChineseButton.onClick.AddListener(ChangeToChinese);
        }

        if (changeEnglishButton != null)
        {
            changeEnglishButton.onClick.RemoveAllListeners();
            changeEnglishButton.onClick.AddListener(ChangeToEnglish);
        }

        if (changeJapaneseButton != null)
        {
            changeJapaneseButton.onClick.RemoveAllListeners();
            changeJapaneseButton.onClick.AddListener(ChangeToJapanese);
        }

        myLanguages[currentLanguageIndex].color = highlightColor;

        Debug.Log("UI 引用已重新初始化");
    }

    private void ChangeToChinese()
    {
        ChangeToLanguage(SystemLanguage.Chinese);
    }

    private void ChangeToEnglish()
    {
        ChangeToLanguage(SystemLanguage.English);
    }

    private void ChangeToJapanese()
    {
        ChangeToLanguage(SystemLanguage.Japanese);
    }

    private void ChangeToLanguage(SystemLanguage language)
    {
        if (language == currentLanguage) return;

        currentLanguage = language;
        currentLanguageIndex = _languageMap[language];
        SetLanguage(currentLanguage, _languageMap[language]);
    }

    /// <summary>
    ///     触发语言切换
    /// </summary>
    private void TriggerLanguageChange()
    {
        // 切换到下一个语言
        currentLanguageIndex = (currentLanguageIndex + 1) % availableLanguages.Length;
        currentLanguage = availableLanguages[currentLanguageIndex];

        // 更新语言并更改按钮颜色
        SetLanguage(currentLanguage, currentLanguageIndex);
    }

    /// <summary>
    ///     设置语言（指定索引）
    /// </summary>
    private void SetLanguage(SystemLanguage language, int index)
    {
        if (LocalizationManager.HasLanguage(language.ToString()))
        {
            // 重置所有字体材质为普通材质
            foreach (var textMesh in myLanguages)
            {
                textMesh.color = Color.white;
            }

            // 高亮当前语言
            myLanguages[currentLanguageIndex].color = highlightColor;

            // 切换语言
            LocalizationManager.CurrentLanguage = language.ToString();
            currentLanguageIndex = index;

            Debug.Log("当前语言: " + language + ", index: " + currentLanguageIndex);
        }
        else
            Debug.LogWarning("找不到语言: " + language);
    }

    #endregion
}