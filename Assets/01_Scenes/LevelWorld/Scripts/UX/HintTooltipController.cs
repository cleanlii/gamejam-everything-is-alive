using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackageGame.Level.UI
{
    public class HintTooltipController : MonoBehaviour
    {
        private static HintTooltipController _instance;

        [SerializeField] private RectTransform tooltipBanner; // 悬浮框的 RectTransform
        [SerializeField] private CanvasGroup canvasGroup; // 控制透明度的 CanvasGroup
        [SerializeField] private TextMeshProUGUI tooltipText; // 悬浮框显示的文本
        [SerializeField] private CanvasScaler canvasScaler; // 动态缩放的 CanvasScaler
        [SerializeField] private Vector3 offset = new(10, -10, 0); // 悬浮框偏移量

        private Tween _fadeTween; // DoTween 动画缓存
        private bool _isVisible; // 是否可见
        private Vector3 _adjustedOffset;

        public static HintTooltipController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HintTooltipController>();
                    if (_instance == null) Debug.LogError("No HintTooltipController found in the scene.");
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                HideInstant();
            }
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            var scaleFactor = canvasScaler.scaleFactor;
            _adjustedOffset = offset * scaleFactor;
        }

        private void OnEnable()
        {
            LevelStateController.InteruptionEvent += HideInstant;
        }

        private void OnDisable()
        {
            LevelStateController.InteruptionEvent -= HideInstant;
        }

        /// <summary>
        ///     显示悬浮框并设置文本
        /// </summary>
        /// <param name="text">提示文本</param>
        public void Show(string text)
        {
            tooltipText.text = text;
            _isVisible = true;

            UpdatePosition(Input.mousePosition); // 更新位置

            _fadeTween?.Kill(); // 停止任何进行中的动画
            canvasGroup.alpha = 0;
            _fadeTween = canvasGroup.DOFade(1f, 0.3f); // DoTween 淡入动画
        }

        /// <summary>
        ///     隐藏悬浮框
        /// </summary>
        public void Hide()
        {
            if (!_isVisible) return;

            _fadeTween?.Kill(); // 停止动画
            _fadeTween = canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            });
            _isVisible = false;
        }

        /// <summary>
        ///     立即隐藏悬浮框
        /// </summary>
        public void HideInstant()
        {
            if (!_isVisible) return;

            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            _isVisible = false;
        }

        /// <summary>
        ///     更新悬浮框位置
        /// </summary>
        /// <param name="mousePosition">鼠标位置</param>
        private void UpdatePosition(Vector3 mousePosition)
        {
            if (!_isVisible) return;

            // 直接使用 anchoredPosition 设置 UI 位置
            Vector2 newPosition = mousePosition + _adjustedOffset;
            tooltipBanner.anchoredPosition = newPosition;

            // 防止超出屏幕
            ClampToScreen();
        }

        /// <summary>
        ///     防止悬浮框超出屏幕边界
        /// </summary>
        private void ClampToScreen()
        {
            var corners = new Vector3[4];
            tooltipBanner.GetWorldCorners(corners);
            var adjustedPosition = tooltipBanner.anchoredPosition;

            // 获取屏幕宽高
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 计算 UI 组件的宽高
            var bannerWidth = tooltipBanner.rect.width * tooltipBanner.lossyScale.x;
            var bannerHeight = tooltipBanner.rect.height * tooltipBanner.lossyScale.y;

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

            tooltipBanner.anchoredPosition = adjustedPosition;
        }
    }
}