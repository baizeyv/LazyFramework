using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lazy
{
    public class SpriteSequenceFrame : MonoBehaviour
    {
        public Sprite[] sprites;
        public string atlasName = "";
        public int aniNum = 16;
        public float loopInterval = 0;
        public int frameRate = 16;
        public bool loop = false;
        public bool autoPlay = false;

        private Image _image;
        private SpriteRenderer _spriteRenderer;
        private int _currentFrame;
        private int _totalFrame;
        private bool _isPlay;
        private float _updateDeltaTime;
        private float _lastUpdateTime;

        private readonly List<string> _spriteNames = new();

        private void Awake()
        {
            for (var i = 0; i < aniNum; i++)
                _spriteNames.Add(atlasName + "_" + i);
        }

        private void OnEnable()
        {
            if (autoPlay)
                Play();
        }

        [ContextMenu("Play")]
        public void Play()
        {
            if (_image == null)
                _image = GetComponent<Image>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_image || _spriteRenderer)
            {
                _currentFrame = 0;
                _totalFrame = aniNum;
                _updateDeltaTime = 1 / (float)frameRate;
                _lastUpdateTime = Time.time;
                _isPlay = true;
                SetTexture();
            }
        }

        [ContextMenu("Stop")]
        public void Stop()
        {
            _isPlay = false;
        }

        // Update is called once per frame
        private void Update()
        {
            if (_isPlay)
            {
                var deltaTime = Time.time - _lastUpdateTime;
                if (deltaTime > _updateDeltaTime)
                {
                    _currentFrame = _currentFrame + 1;
                    if (_currentFrame >= _totalFrame)
                    {
                        if (loop)
                        {
                            _currentFrame = _currentFrame - _totalFrame;
                        }
                        else
                        {
                            Stop();
                            return;
                        }
                    }

                    SetTexture();
                    if (loop && _currentFrame == _totalFrame - 1)
                        _lastUpdateTime += _updateDeltaTime + loopInterval;
                    else
                        _lastUpdateTime += _updateDeltaTime;
                }
            }
        }

        private void SetTexture()
        {
            if (_image)
                _image.sprite =
                    sprites.Length > 0
                        ? sprites[_currentFrame % aniNum]
                        : AssetManager.Instance.LoadSync<Sprite>(
                            atlasName,
                            _spriteNames[_currentFrame % aniNum]
                        );
            else if (_spriteRenderer)
                _spriteRenderer.sprite =
                    sprites.Length > 0
                        ? sprites[_currentFrame % aniNum]
                        : AssetManager.Instance.LoadSync<Sprite>(
                            atlasName,
                            _spriteNames[_currentFrame % aniNum]
                        );
        }
    }
}
