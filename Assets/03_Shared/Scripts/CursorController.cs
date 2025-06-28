using UnityEngine;

namespace PackageGame.Global
{
    public class CursorController : MonoBehaviour
    {
        public static CursorController Instance; // 单例，方便全局访问

        [Header("光标 Textures")]
        [SerializeField] private Texture2D normalCursor; // 默认光标
        [SerializeField] private Texture2D readyToDragCursor; // 拖拽开始光标
        [SerializeField] private Texture2D onDragCursor; // 拖拽结束光标
        [SerializeField] private Texture2D searchCursor; // 搜索光标

        [Header("光标设置")]
        public Vector2 hotspot = Vector2.zero; // 光标热点
        public CursorMode cursorMode = CursorMode.Auto; // 光标模式

        private void Awake()
        {
            // 确保只存在一个实例（单例模式）
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 保持在场景切换时不被销毁
            }
            else
                Destroy(gameObject);

            SetNormalCursor();
        }

        /// <summary>
        ///     设置光标为默认状态
        /// </summary>
        public void SetNormalCursor()
        {
            Cursor.SetCursor(normalCursor, hotspot, cursorMode);
        }

        /// <summary>
        ///     设置光标为拖拽开始状态
        /// </summary>
        public void SetReadyToDragCursor()
        {
            Cursor.SetCursor(readyToDragCursor, hotspot, cursorMode);
        }

        /// <summary>
        ///     设置光标为拖拽结束状态
        /// </summary>
        public void SetOnDragCursor()
        {
            Cursor.SetCursor(onDragCursor, hotspot, cursorMode);
        }

        /// <summary>
        ///     设置光标为搜索状态
        /// </summary>
        public void SetSearchCursor()
        {
            Cursor.SetCursor(searchCursor, hotspot, cursorMode);
        }

        /// <summary>
        ///     重置光标为系统默认光标
        /// </summary>
        public void ResetToSystemCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}