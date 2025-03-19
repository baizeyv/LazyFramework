using System;
using System.Collections.Generic;
using Lazy.Manage;
using Lazy.Serializer;
using Lazy.Singleton;
using UnityEngine;
using UnityEngine.Audio;

namespace Lazy.Audio
{
    [MonoSingletonPath("Lazy/SFX/AudioManager")]
    [ManagerUpdate]
    public class AudioManager : MonoSingleton<AudioManager>, IManager
    {
        /// <summary>
        /// * 每个通道对应的SFX
        /// ! 默认通道:
        /// # reservedSfx: 保留通道,在未找到已知通道的情况下临时使用
        /// # bgmSfx: 背景音乐通道 BGM
        /// # normalSfx: 普通特效音效通道
        /// # uiSfx: UI音效通道
        /// # voiceSfx: 人声通道
        /// </summary>
        private Dictionary<string, AudioSFX> _channels = new();

        /// <summary>
        /// * 每个通道对应的音量
        /// </summary>
        private Dictionary<string, float> _channelVolumes = new();

        /// <summary>
        /// * 每个通道对应的开关
        /// </summary>
        private Dictionary<string, bool> _channelSwitchers = new();

        /// <summary>
        /// * 一次性特效音效
        /// </summary>
        private AudioEffect _audioEffect3D;

        /// <summary>
        /// * 一次性音效混合组
        /// </summary>
        private AudioMixerGroup _audioEffectMixerGroup;

        /// <summary>
        /// * 一次性音效音量
        /// </summary>
        private float _volumeAudioEffect = 1f;

        /// <summary>
        /// * 一次性音效开关
        /// </summary>
        private bool _switcherAudioEffect = true;

        private AudioMixer _audioMixer;

        private const string VolumeKey = "Volume";
        private const string SwitcherKey = "Switcher";
        private const string EffectVolumeKey = "EffectVolume";
        private const string EffectSwitcherKey = "EffectSwitcher";

        private AudioManager() { }

        public override void OnSingletonInitialize()
        {
            // # 初始化默认的5个通道
            AddChannel(AudioConstant.ReservedChannel, "%Reserved%SFX");
            AddChannel(AudioConstant.BGMChannel, "%BGM%SFX");
            AddChannel(AudioConstant.NormalChannel, "%Normal%SFX");
            AddChannel(AudioConstant.UIChannel, "%UI%SFX");
            AddChannel(AudioConstant.VoiceSfx, "%Voice%SFX");

            // # 初始化一次性特效
            _audioEffect3D = new AudioEffect();
            _volumeAudioEffect = StorageManager.Instance.GetFloat(EffectVolumeKey, 1f);
            _switcherAudioEffect = StorageManager.Instance.Get(EffectSwitcherKey, true);
            // # 从本地读取音量以及开关值,例如使用 PlayersPref
            foreach (var item in _channelSwitchers.Keys)
            {
                var val = StorageManager.Instance.Get<bool>(item + SwitcherKey);
                _channelSwitchers[item] = val;
                var vol = StorageManager.Instance.GetFloat(item + VolumeKey);
                _channelVolumes[item] = vol;
            }
        }

        /// <summary>
        /// * 添加音效通道
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="objName"></param>
        public void AddChannel(string channelName, string objName = "")
        {
            if (_channels.ContainsKey(channelName))
            {
                Log.Log.MsgE($"已存在通道:{channelName}");
                return;
            }

            var reservedChannel = new GameObject(
                string.IsNullOrEmpty(objName) ? $"{channelName}Channel" : objName,
                typeof(AudioSource)
            );
            reservedChannel.transform.SetParent(transform);
            var tmpSfx = new AudioSFX { Source = reservedChannel.GetComponent<AudioSource>() };
            tmpSfx.Source.playOnAwake = false;
            tmpSfx.Source.loop = false;
            if (_audioMixer)
                tmpSfx.Source.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("Master/SFX")[
                    0
                ];
            _channels.Add(channelName, tmpSfx);
            _channelVolumes.Add(channelName, 1f);
            _channelSwitchers.Add(channelName, true);
        }

        /// <summary>
        /// * 设置混音组
        /// </summary>
        /// <param name="audioMixer"></param>
        public void SetAudioMixer(AudioMixer audioMixer)
        {
            foreach (var item in _channels.Values)
                item.Source.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("Master/SFX")[0];
            _channels[AudioConstant.BGMChannel].Source.outputAudioMixerGroup =
                _audioMixer.FindMatchingGroups("Master/BGM")[0];
            _channels[AudioConstant.VoiceSfx].Source.outputAudioMixerGroup =
                _audioMixer.FindMatchingGroups("Master/VOICE")[0];
            _audioEffectMixerGroup = _audioMixer.FindMatchingGroups("Master/SFX")[0];
            _audioMixer = audioMixer;
        }

        /// <summary>
        /// * 设置完成回调
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="callback"></param>
        public void SetCompletedCallback(string channel, Action callback)
        {
            if (string.IsNullOrEmpty(channel))
                return;
            if (_channels.TryGetValue(channel, out var sfx))
                sfx.OnCompleted = callback;
        }

        /// <summary>
        /// * 播放指定通道
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <param name="loop"></param>
        /// <param name="priority"></param>
        public void Play(
            string channel,
            string assetName,
            Action callback = null,
            bool loop = false,
            int priority = 0
        )
        {
            if (string.IsNullOrEmpty(channel))
                return;
            if (!_channels.TryGetValue(channel, out var sfx))
                return;
            if (!_channelSwitchers[channel])
                return;
            if (priority < sfx.Priority)
                return;
            sfx.Play(assetName, callback, loop, priority);
        }

        /// <summary>
        /// * 播放一次性3D音效特效
        /// </summary>
        /// <param name="assetName">资产名称</param>
        /// <param name="isRandom">是否随机音量音高</param>
        /// <param name="audioPosition">音频播放位置</param>
        /// <param name="volume">音量</param>
        /// <param name="spatialBlend">2d到3d的比例</param>
        /// <param name="maxNum">最大同时播放个数</param>
        /// <param name="callback">播放完成的回调</param>
        public void Play(
            string assetName,
            bool isRandom = false,
            Vector3? audioPosition = null,
            float volume = 1f,
            float spatialBlend = 1f,
            int maxNum = 5,
            Action callback = null
        )
        {
            if (!_switcherAudioEffect)
                return;
            var actualPosition = audioPosition.GetValueOrDefault(transform.position);
            var actualVolume = volume * _volumeAudioEffect;
            _audioEffect3D.Play(
                assetName,
                actualPosition,
                actualVolume,
                spatialBlend,
                maxNum,
                callback,
                _audioEffectMixerGroup,
                isRandom
            );
        }

        /// <summary>
        /// * 获取指定通道的音量
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public float GetVolume(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
                return 0;
            return _channelVolumes.GetValueOrDefault(channelName, 0);
        }

        /// <summary>
        /// * 设置指定通道的音量
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="volume"></param>
        public void SetVolume(string channelName, float volume)
        {
            if (string.IsNullOrEmpty(channelName))
                return;
            if (_channelVolumes.TryGetValue(channelName, out _))
                _channelVolumes[channelName] = volume;
            // # PlayersPref 保存
            StorageManager.Instance.SetFloat(channelName + VolumeKey, volume);
        }

        public void SetAllVolume(float volume)
        {
            foreach (var nm in _channels.Keys)
                SetVolume(nm, volume);
            _volumeAudioEffect = volume;
            StorageManager.Instance.SetFloat(EffectVolumeKey, volume);
        }

        /// <summary>
        /// * 获取指定通道的音效开关
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public bool GetSwitcher(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
                return true;
            return _channelSwitchers.GetValueOrDefault(channelName, true);
        }

        /// <summary>
        /// * 设置指定通道的音效开关
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="switcher"></param>
        public void SetSwitcher(string channelName, bool switcher)
        {
            if (string.IsNullOrEmpty(channelName))
                return;
            if (_channelVolumes.TryGetValue(channelName, out _))
            {
                _channelSwitchers[channelName] = switcher;
                _channels[channelName].Source.Stop();
            }

            // # PlayersPref 保存
            StorageManager.Instance.Set(channelName + SwitcherKey, switcher);
        }

        public void SetAllSwitcher(bool switcher)
        {
            foreach (var nm in _channels.Keys)
                SetSwitcher(nm, switcher);

            _switcherAudioEffect = switcher;
            StorageManager.Instance.Set(EffectSwitcherKey, switcher);
        }

        /// <summary>
        /// * 获取SFX音效播放进度
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public float GetProgress(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
                return 0f;
            if (_channels.TryGetValue(channelName, out var sfx))
                return sfx.Progress;

            return 0f;
        }

        /// <summary>
        /// * 设置音效播放进度
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="val"></param>
        public void SetProgress(string channelName, float val)
        {
            if (string.IsNullOrEmpty(channelName))
                return;
            if (_channels.TryGetValue(channelName, out var sfx))
                sfx.Progress = val;
        }

        public void ResumeAll()
        {
            foreach (var item in _channels.Values)
                item.Source.Play();
        }

        public void PauseAll()
        {
            foreach (var item in _channels.Values)
                item.Source.Pause();
        }

        public void StopAll()
        {
            foreach (var item in _channels.Values)
                item.Source.Stop();
        }

        public void OnUpdate()
        {
            foreach (var item in _channels.Values)
                item.Update();
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroy()
        {
            StopAll();
            Destroy(gameObject);
        }
    }
}
