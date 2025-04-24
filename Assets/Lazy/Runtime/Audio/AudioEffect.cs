using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Lazy
{
    /// <summary>
    /// * 3D声音特效
    /// </summary>
    public class AudioEffect
    {
        /// <summary>
        /// * 音效字典
        /// </summary>
        private Dictionary<string, AudioClip> _effects = new();

        /// <summary>
        /// * 音效的可播放数量
        /// </summary>
        private Dictionary<string, int> _effectsNum = new();

        /// <summary>
        /// * 使用这个作为预制体建一个对象池
        /// </summary>
        private readonly GameObject _oneShotAudio;

        private const float MinVolume = 0.6f;

        private const float MaxVolume = 1f;

        private const float MinPitch = 0.8f;

        private const float MaxPitch = 1.2f;

        public AudioEffect(Transform parent)
        {
            _oneShotAudio = new GameObject("AudioEffect3D", typeof(AudioSource));
            _oneShotAudio.transform.SetParent(parent);
        }

        /// <summary>
        /// * 加载及播放SFX
        /// </summary>
        /// <param name="res"></param>
        /// <param name="position"></param>
        /// <param name="volume"></param>
        /// <param name="spatialBlend"></param>
        /// <param name="maxNum"></param>
        /// <param name="callback"></param>
        /// <param name="audioEffectMixerGroup"></param>
        /// <param name="isRandom"></param>
        public void Play(
            string res,
            Vector3 position,
            float volume = 1f,
            float spatialBlend = 1f,
            int maxNum = 5,
            Action callback = null,
            AudioMixerGroup audioEffectMixerGroup = null,
            bool isRandom = false
        )
        {
            if (!_effectsNum.TryGetValue(res, out var count))
                _effectsNum[res] = 1;
            else if (count >= maxNum)
                return;

            _effectsNum[res] = count + 1;

            if (_effects.TryGetValue(res, out var clip) && clip)
                PlayClip(
                    res,
                    clip,
                    position,
                    volume,
                    spatialBlend,
                    callback,
                    audioEffectMixerGroup,
                    isRandom
                );
            else
                AssetManager.Instance.LoadAsync<AudioClip>(
                    res,
                    audioClip =>
                    {
                        _effects[res] = audioClip;
                        PlayClip(
                            res,
                            _effects[res],
                            position,
                            volume,
                            spatialBlend,
                            callback,
                            audioEffectMixerGroup,
                            isRandom
                        );
                    }
                );
        }

        public void Dispose(bool unloadAllLoadedObjects = true)
        {
            foreach (var clip in _effects)
                AssetManager.Instance.UnloadSync(clip.Key, unloadAllLoadedObjects);
            _effects.Clear();
            _effectsNum.Clear();
        }

        private void PlayClip(
            string res,
            AudioClip clip,
            Vector3 position,
            float volume = 1f,
            float spatialBlend = 1f,
            Action callback = null,
            AudioMixerGroup audioEffectMixerGroup = null,
            bool isRandom = false
        )
        {
            var obj = PoolManager.Instance.GameObjectPool.Spawn(_oneShotAudio);
            obj.transform.position = position;
            var source = obj.GetComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = spatialBlend;
            source.volume = isRandom ? Random.Range(MinVolume, MaxVolume) : volume;
            source.pitch = isRandom ? Random.Range(MinPitch, MaxPitch) : 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            if (audioEffectMixerGroup)
                source.outputAudioMixerGroup = audioEffectMixerGroup;
            source.Play();

            var time =
                clip.length * (Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale);
            TimerManager.Instance.AddTimer(
                1f,
                time,
                1,
                null,
                () =>
                {
                    // # 放回对象池中
                    PoolManager.Instance.GameObjectPool.Despawn(obj);
                    if (_effectsNum.TryGetValue(res, out var num))
                        _effectsNum[res] = num - 1;
                    callback.Fire();
                }
            );
        }
    }
}
