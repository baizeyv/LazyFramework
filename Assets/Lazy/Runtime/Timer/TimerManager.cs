using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Lazy.Event;
using Lazy.Manage;
using Lazy.Rx;
using Lazy.Utility;
using UnityEngine;

namespace Lazy.Timer
{
    [ManagerUpdate]
    public class TimerManager : Singleton.Singleton<TimerManager>, IManager
    {
        /// <summary>
        /// * 服务器UTC时间的 '整点' 订阅事件 (1点 3点 5点 ......)
        /// </summary>
        private IntEvent _utcServerEvent = new();

        /// <summary>
        /// * 本地UTC时间的 整点 订阅事件
        /// </summary>
        private IntEvent _utcLocalEvent = new();

        /// <summary>
        /// * 服务器当前时区的整点订阅事件
        /// </summary>
        private IntEvent _localServerEvent = new();

        /// <summary>
        /// * 本地当前时区的整点订阅事件
        /// </summary>
        private IntEvent _localLocalEvent = new();

        public TimeRequestStatus RequestStatus { get; private set; }

        /// <summary>
        /// * 是否已经设置了服务器时间了
        /// </summary>
        private bool _isSetServerTime;

        /// <summary>
        /// * 是否已经设置了本地时间了
        /// </summary>
        private bool _isSetLocalTime;

        /// <summary>
        /// * 服务器时间 (毫秒)
        /// </summary>
        private long _serverTime;

        private long _localTime;

        /// <summary>
        /// * 当获取到服务器时间时的 Time.realtimeSinceStartup
        /// </summary>
        private float _beginWhenGetServer;

        /// <summary>
        /// * 当获取到本地时间时的 Time.realtimeSinceStartup
        /// </summary>
        private float _beginWhenGetLocal;

        private List<Timer> _timers = new();

        /// <summary>
        /// * 上一帧的时间 TODO:
        /// </summary>
        private DateTimeOffset _lastFrameDateTime;

        private TimerManager() { }

        public override void OnSingletonInitialize()
        {
            SetLocalTime(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _ = RequestNtp();
        }

        #region API

        /// <summary>
        /// * 时钟订阅事件 (默认localLocal 本地当前时区的时间)
        /// </summary>
        /// <param name="clock"></param>
        /// <param name="callback"></param>
        /// <param name="utc"></param>
        /// <param name="server"></param>
        public void SubscribeClock(
            int clock,
            Observer<Unit> callback,
            bool utc = false,
            bool server = false
        )
        {
            if (utc)
            {
                if (server)
                    // # utcServer
                    _utcServerEvent.Subscribe(clock, callback);
                else
                    // # utcLocal
                    _utcLocalEvent.Subscribe(clock, callback);
            }
            else
            {
                if (server)
                    // # localServer
                    _localServerEvent.Subscribe(clock, callback);
                else
                    // # localLocal
                    _localLocalEvent.Subscribe(clock, callback);
            }
        }

        /// <summary>
        /// * 获取本地时间戳
        /// </summary>
        /// <returns></returns>
        public void GetLocalTime(out DateTimeOffset utc, out DateTimeOffset local)
        {
            if (!_isSetLocalTime)
            {
                var offset = (long)((Time.realtimeSinceStartup - _beginWhenGetLocal) * 1000);
                var nowTimestamp = _serverTime + offset;
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(nowTimestamp);
                utc = dt.UtcDateTime;
                local = dt.LocalDateTime;
            }
            else
            {
                var offset = (long)((Time.realtimeSinceStartup - _beginWhenGetLocal) * 1000);
                var nowTimestamp = _localTime + offset;
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(nowTimestamp);
                utc = dt.UtcDateTime;
                local = dt.LocalDateTime;
            }
        }

        /// <summary>
        /// * 获取服务器时间戳
        /// </summary>
        /// <returns></returns>
        public bool GetServerTime(out DateTimeOffset utc, out DateTimeOffset local)
        {
            if (!_isSetServerTime)
            {
                utc = DateTime.MinValue;
                local = DateTime.MinValue;
                return false;
            }

            var offset = (long)((Time.realtimeSinceStartup - _beginWhenGetServer) * 1000);
            var nowTimestamp = _serverTime + offset;
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(nowTimestamp);
            utc = dt.UtcDateTime;
            local = dt.LocalDateTime;
            return true;
        }

        /// <summary>
        /// * 注册一个秒计时器
        /// </summary>
        /// <param name="step"></param>
        /// <param name="delay"></param>
        /// <param name="count"></param>
        /// <param name="onProcess"></param>
        /// <param name="onCompleted"></param>
        /// <returns></returns>
        public int AddTimer(
            float step = 1f,
            float delay = 0f,
            int count = 0,
            Action onProcess = null,
            Action onCompleted = null
        )
        {
            var timer = new Timer(step, delay, count, onProcess, onCompleted, false);
            _timers.Add(timer);
            return timer.ID;
        }

        /// <summary>
        /// * 添加一个帧计时器
        /// </summary>
        /// <param name="step">隔多久执行一次</param>
        /// <param name="delay">延迟</param>
        /// <param name="count">执行次数</param>
        /// <param name="onProcess"></param>
        /// <param name="onCompleted"></param>
        /// <returns></returns>
        public int AddFrameTimer(
            float step = 1f,
            float delay = 0f,
            int count = 0,
            Action onProcess = null,
            Action onCompleted = null
        )
        {
            var timer = new Timer(step, delay, count, onProcess, onCompleted, true);
            _timers.Add(timer);
            return timer.ID;
        }

        /// <summary>
        /// * 移除指定ID的计时器
        /// </summary>
        /// <param name="id"></param>
        public void RemoveTimer(int id)
        {
            foreach (var timer in _timers.Where(timer => timer.ID == id))
            {
                timer.IsFinished = true;
                break;
            }
        }

        #endregion

        #region Private Method

        private void OnTimerCompleted(Timer timer)
        {
            timer.IsFinished = true;
            timer.OnCompleted.Fire();
        }

        private void SetLocalTime(long localTime)
        {
            if (_isSetLocalTime)
                return;
            _isSetLocalTime = true;
            _localTime = localTime;
            _beginWhenGetLocal = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// * 设置服务器时间
        /// </summary>
        private void SetServerTime(long serverTime)
        {
            if (_isSetServerTime)
                return;
            _isSetServerTime = true;
            RequestStatus = TimeRequestStatus.Success;
            _serverTime = serverTime;
            _beginWhenGetServer = Time.realtimeSinceStartup;
        }

        private async Task RequestNtp()
        {
            if (RequestStatus is TimeRequestStatus.Requesting or TimeRequestStatus.Fail)
                // # 正在请求或请求失败
                return;

            var ntpServerAddresses = TimerConstant.NtpServerList;
            RequestStatus = TimeRequestStatus.Requesting;

            var tasks = ntpServerAddresses
                .Select(address => Task.Run(async () => await GetNtpTimeAsync(address, 2000)))
                .ToArray();

            while (tasks.Length > 0)
            {
                var completedTask = await Task.WhenAny(tasks);
                tasks = tasks.Where(t => t != completedTask).ToArray();
                var networkDateTime = completedTask.Result;
                if (networkDateTime != DateTime.MinValue)
                {
                    Log.Log.MsgD($"获取网络时间：{networkDateTime}");
                    SetServerTime(((DateTimeOffset)networkDateTime).ToUnixTimeMilliseconds());
                    return;
                }
            }
        }

        private async Task<DateTime> GetNtpTimeAsync(
            string ntpServer,
            int timeoutMilliseconds = 5000
        )
        {
            try
            {
                const int udpPort = 123;
                var ntpData = new byte[48];
                ntpData[0] = 0x1B;

                var addresses = await Dns.GetHostAddressesAsync(ntpServer);
                var ipEndPoint = new IPEndPoint(addresses[0], udpPort);
                var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp
                );

                // # 设置超时时间
                socket.ReceiveTimeout = timeoutMilliseconds;

                await socket.ConnectAsync(ipEndPoint);
                await socket.SendAsync(new ArraySegment<byte>(ntpData), SocketFlags.None);
                var receiveBuffer = new byte[48];
                await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), SocketFlags.None);
                socket.Dispose();

                const byte serverReplyTime = 40;
                ulong intPart = BitConverter.ToUInt32(receiveBuffer, serverReplyTime);
                ulong fracPart = BitConverter.ToUInt32(receiveBuffer, serverReplyTime + 4);
                intPart = SwapEndianness(intPart);
                fracPart = SwapEndianness(fracPart);
                var milliseconds = intPart * 1000 + fracPart * 1000 / 0x100000000L;
                var networkDateTime = new DateTime(1900, 1, 1).AddMilliseconds((long)milliseconds);

                var serverTimeZone = TimeZoneInfo.Local;
                networkDateTime = TimeZoneInfo.ConvertTimeFromUtc(networkDateTime, serverTimeZone);
                return networkDateTime;
            }
            catch (Exception e)
            {
                // 出现异常，返回 null 或抛出错误，视情况而定
                Log.Log.MsgE($"获取网络时间失败: {e.Message}");
                return DateTime.MinValue;
            }
        }

        // 交换字节顺序，将大端序转换为小端序或反之
        private uint SwapEndianness(ulong x)
        {
            return (uint)(
                ((x & 0x000000ff) << 24)
                + ((x & 0x0000ff00) << 8)
                + ((x & 0x00ff0000) >> 8)
                + ((x & 0xff000000) >> 24)
            );
        }

        /// <summary>
        /// * 整点切换的时候会触发事件,例如 6.59->7.00,触发7事件
        /// ! 可触发 0-23 共24个事件
        /// </summary>
        private void OnClockUpdate()
        {
            // TODO:
        }

        #endregion

        public void OnUpdate()
        {
            OnClockUpdate();

            if (_timers.Count <= 0)
                return;

            var deltaTime = Time.deltaTime;
            for (var i = 0; i < _timers.Count; i++)
            {
                var timer = _timers[i];
                if (timer.IsFinished)
                {
                    _timers.RemoveAt(i);
                    i--;
                    continue;
                }

                // # 调用计时器
                var triggerCount = timer.IsFrameTimer ? timer.Update(1) : timer.Update(deltaTime);
                if (triggerCount > 0)
                {
                    if (timer.IsFinished)
                        continue;

                    // # 计时器剩余次数
                    var cnt = timer.Count;

                    for (var j = 0; j < triggerCount; j++)
                    {
                        cnt = cnt > 0 ? cnt - 1 : cnt;
                        timer.Count = cnt;
                        timer.OnProcess.Fire();
                        if (cnt == 0)
                            OnTimerCompleted(timer);
                    }
                }
            }
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroy()
        {
            _timers.Clear();
            _utcServerEvent.Dispose();
            _utcLocalEvent.Dispose();
            _localLocalEvent.Dispose();
            _localServerEvent.Dispose();
        }
    }
}
