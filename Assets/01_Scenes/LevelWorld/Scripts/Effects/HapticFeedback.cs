using UnityEngine;

public class HapticFeedback : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _TriggerHapticFeedback(int style);
#endif

    public enum HapticFeedbackStyle
    {
        Light,
        Medium,
        Heavy,
        Selection,
        Success,
        Warning,
        Failure
    }

    public static void TriggerHapticFeedback(HapticFeedbackStyle style)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            _TriggerHapticFeedback((int)style);
        }
// #else
        // Debug.Log($"Haptic feedback triggered: {style} (No-op on non-iOS platforms)");
#endif
    }
}