using System;
using System.Collections.Generic;
using UnityEngine;

public class StringUtils
{
    /// <summary>
    ///     根据下划线分隔符提取字符串的倒数第二个部分
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string SplitItemName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty; // 处理空或null字符串

        // 使用分隔符“_”分割字符串
        var parts = name.Split('_');

        // 返回最后一部分
        // return parts[parts.Length - 1];

        return parts[parts.Length - 2];
    }

    /// <summary>
    ///     根据下划线分隔符提取字符串的最后一部分
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string SplitBackpackName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty; // 处理空或null字符串

        // 使用分隔符“_”分割字符串
        var parts = name.Split('_');

        // 返回最后一部分
        return parts[parts.Length - 1];
    }

    /// <summary>
    ///     根据短横线分隔符提取字符串的第一部分
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string SplitPipeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty; // 处理空或null字符串

        // 使用分隔符“_”分割字符串
        var parts = name.Split('-');

        // 返回最后一部分
        return parts[0];
    }

    /// <summary>
    ///     获取对应语言的字符串
    /// </summary>
    /// <param name="language"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string GetLanguageString(SystemLanguage language)
    {
        switch (language)
        {
            case SystemLanguage.English:
                return "English";
            case SystemLanguage.Japanese:
                return "Japanese";
            case SystemLanguage.ChineseSimplified:
                return "Simplified Chinese";
            default:
                throw new ArgumentOutOfRangeException(nameof(language), language, null);
        }
    }

    /// <summary>
    ///     比较两个字符串列表是否相等
    /// </summary>
    /// <param name="list1"></param>
    /// <param name="list2"></param>
    /// <returns></returns>
    public static bool ListsAreEqual(List<string> list1, List<string> list2)
    {
        if (list1 == null || list2 == null) return list1 == list2;
        if (list1.Count != list2.Count) return false;

        for (var i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i])
                return false;
        }

        return true;
    }
}