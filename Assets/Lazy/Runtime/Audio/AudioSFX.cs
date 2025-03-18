using System;
using System.Collections.Generic;
using Lazy.Res;
using Lazy.Utility;
using UnityEngine;

namespace Lazy.Audio
{
    /// <summary>
    /// * 音频音效
    /// </summary>
    public class AudioSFX
    {
        /// <summary>
        /// * AudioSource 组件
        /// </summary>
        public AudioSource Source { get; set; }

        /// <summary>
        /// * 播放完成的回调
        /// </summary>
        public Action OnCompleted { get; set; }

        /// <summary>
        /// * 播放优先级
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// * 音效字典,保存已经加载的那些音效资源
        /// </summary>
        private Dictionary<string, AudioClip> _audios = new();

        /// <summary>
        /// * 是否正在播放音效
        /// </summary>
        private bool _isPlaying;

        /// <summary>
        /// * 播放进度 [0,1]
        /// </summary>
        private float _progress;

        /// <summary>
        /// * 播放进度自动属性 [Auto-Property]
        /// </summary>
        public float Progress
        {
            get
            {
                if (Source.clip && Source.clip.length > 0)
                    _progress = Source.time / Source.clip.length;
                return _progress;
            }
            set
            {
                _progress = value;
                Source.time = value * Source.clip.length;
            }
        }

        public void Update()
        {
            if (Source.clip && Source.time > 0)
                _isPlaying = true;

            if (_isPlaying && !Source.isPlaying)
            {
                _isPlaying = false;
                OnCompleted.Fire();
            }
        }

        /// <summary>
        /// * 加载并播放音效
        /// </summary>
        /// <param name="res"></param>
        /// <param name="callback"></param>
        /// <param name="loop"></param>
        /// <param name="priority"></param>
        public void Play(string res, Action callback = null, bool loop = false, int priority = 0)
        {
            Source.loop = loop;
            Priority = priority;

            if (_audios != null && _audios.TryGetValue(res, out var clip) && clip)
                PlayClip(clip, callback);
            else
                // # 异步加载当前音效资源，加载完成后播放
                AssetManager.Instance.LoadAsync<AudioClip>(
                    res,
                    audioClip =>
                    {
                        _audios[res] = audioClip;
                        PlayClip(_audios[res], callback);
                    }
                );
        }

        /// <summary>
        /// * 释放资源
        /// </summary>
        /// <param name="unloadAllLoadedObjects"></param>
        public void Dispose(bool unloadAllLoadedObjects = true)
        {
            foreach (var clip in _audios)
                AssetManager.Instance.UnloadSync(clip.Key, unloadAllLoadedObjects);
            _audios.Clear();
        }

        private void PlayClip(AudioClip audioClip, Action callback = null)
        {
            if (Source.isPlaying)
            {
                _isPlaying = false;
                Source.Stop();
                OnCompleted.Fire();
            }

            // # 赋值新的clip
            Source.clip = audioClip;
            // # 播放完成的回调赋值
            OnCompleted = callback;
            // # 播放新音效
            Source.Play();
        }
    }
}
