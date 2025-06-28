using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public ItemLibrary buffLibrary; // 引用BuffList ScriptableObject

    public void EnableBuff(string buffName)
    {
        var buff = buffLibrary.buffs.Find(b => b.buffName == buffName);

        if (buff == null)
        {
            Debug.LogWarning($"Buff with name {buffName} not found");
            return;
        }

        buff.SetFixedValues(); // 设置固定值

        // 应用buff的效果
        foreach (var modifier in buff.modifiers)
        {
            switch (modifier.Key)
            {
                case "energy":
                    Debug.Log($"Applied energy modifier: {modifier.Value}");
                    break;
                case "gold":
                    // player.gold += modifier.Value;
                    Debug.Log($"Applied gold modifier: {modifier.Value}");
                    break;
                // 根据需要添加其他modifier的应用逻辑
            }
        }

        Debug.Log($"Applied Buff: {buff.buffName} - Type: {buff.effect}");
    }

    public void DisableBuff(string buffName)
    {
        var buff = buffLibrary.buffs.Find(b => b.buffName == buffName);

        if (buff == null)
        {
            Debug.LogWarning($"Buff with name {buffName} not found");
            return;
        }

        // 移除buff的效果
        foreach (var modifier in buff.modifiers)
        {
            switch (modifier.Key)
            {
                case "energy":
                    Debug.Log($"Removed energy modifier: {modifier.Value}");
                    break;
                case "gold":
                    // player.gold-= modifier.Value;
                    Debug.Log($"Removed gold modifier: {modifier.Value}");
                    break;
                // 根据需要添加其他modifier的移除逻辑
            }
        }

        Debug.Log($"Removed Buff: {buff.buffName} - Type: {buff.effect}");
    }

    // private void Start()
    // {
    //     ApplyBuff("能量+1");  // 应用buff
    // }
}