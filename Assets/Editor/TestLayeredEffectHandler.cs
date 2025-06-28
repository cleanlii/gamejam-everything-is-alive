using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EffectDrawer : MaterialPropertyDrawer
{
    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        EditorGUI.BeginChangeCheck();
        EffectType effect = (EffectType)prop.floatValue;
        effect = (EffectType)EditorGUI.EnumPopup(position, label, effect);
        if (EditorGUI.EndChangeCheck())
        {
            prop.floatValue = (float)effect;
        }
    }
}
