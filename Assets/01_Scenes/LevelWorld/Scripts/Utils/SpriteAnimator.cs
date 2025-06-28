using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] sprites;
    public float frameRate = 0.5f;
    public bool isAnimating = true;
    public AnimationLoopType loopType = AnimationLoopType.Forward;

    [SerializeField] private bool playOnStart = true;

    private SpriteRenderer _spriteRenderer;
    private Image _imageComponent;
    private int _currentSpriteIndex;
    private int _direction = 1;
    private float _nextFrameTime;
    private bool _isInitialized;

    // 缓存数组长度避免重复访问
    private int _spriteCount;

    public enum AnimationLoopType
    {
        Forward, // 0 -> N -> 0
        PingPong, // 0 -> N -> 0 -> N
        Loop // 0 -> N -> 0 -> N
    }

    private void Awake()
    {
        // 在Awake中初始化组件引用，比Start更早
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (_isInitialized) return;

        // 尝试获取SpriteRenderer
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 如果没有SpriteRenderer，尝试获取Image组件
        if (_spriteRenderer == null)
            _imageComponent = GetComponent<Image>();

        if (sprites != null && sprites.Length > 0)
        {
            // 缓存数组长度，避免在Update中重复获取
            _spriteCount = sprites.Length;

            // 显示第一帧
            UpdateSprite();

            _isInitialized = true;
        }
    }

    private void OnEnable()
    {
        if (!_isInitialized)
            InitializeComponents();

        // 重置计时器
        _nextFrameTime = Time.time + frameRate;

        // 如果设置为自动播放，则开始动画
        if (playOnStart && isAnimating)
            StartAnimation();
    }

    private void Update()
    {
        // 快速检查，如果不在播放则直接返回
        if (!isAnimating || _spriteCount == 0) return;

        // 使用Time.time而不是累加计时器，更精准且避免浮点累积误差
        if (Time.time >= _nextFrameTime)
        {
            // 更新下一帧时间
            _nextFrameTime = Time.time + frameRate;

            // 更新当前帧
            AdvanceFrame();
            UpdateSprite();
        }
    }

    private void AdvanceFrame()
    {
        // 根据循环类型更新帧索引
        switch (loopType)
        {
            case AnimationLoopType.Forward:
                _currentSpriteIndex = (_currentSpriteIndex + 1) % _spriteCount;
                break;

            case AnimationLoopType.PingPong:
                // 移动到当前方向
                _currentSpriteIndex += _direction;

                // 在端点改变方向
                if (_currentSpriteIndex >= _spriteCount - 1)
                {
                    _currentSpriteIndex = _spriteCount - 1;
                    _direction = -1;
                }
                else if (_currentSpriteIndex <= 0)
                {
                    _currentSpriteIndex = 0;
                    _direction = 1;
                }

                break;

            case AnimationLoopType.Loop:
                // 按方向循环
                _currentSpriteIndex = (_currentSpriteIndex + _direction + _spriteCount) % _spriteCount;
                break;
        }
    }

    private void UpdateSprite()
    {
        // 优化：仅在需要的渲染器上设置精灵
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = sprites[_currentSpriteIndex];
        else if (_imageComponent != null)
            _imageComponent.sprite = sprites[_currentSpriteIndex];
    }

    // 公共API，优化为避免不必要的操作

    public void SetFrames(Sprite[] newSprites)
    {
        if (newSprites == null || newSprites.Length == 0) return;

        sprites = newSprites;
        _spriteCount = newSprites.Length;
        _currentSpriteIndex = 0;
        UpdateSprite();
    }

    public void StartAnimation()
    {
        if (!isAnimating)
        {
            isAnimating = true;
            _nextFrameTime = Time.time + frameRate;
        }
    }

    public void StopAnimation()
    {
        isAnimating = false;
    }

    public void SetDirection(int newDirection)
    {
        _direction = newDirection >= 0 ? 1 : -1;
    }

    public void ResetToFirstFrame()
    {
        _currentSpriteIndex = 0;
        UpdateSprite();
    }

    public void SetFrame(int frameIndex)
    {
        // 添加安全检查，避免越界
        if (frameIndex >= 0 && frameIndex < _spriteCount)
        {
            _currentSpriteIndex = frameIndex;
            UpdateSprite();
        }
    }
}