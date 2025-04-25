using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SFB;
using Solver;
using Solver.Exporter;

namespace Lazy.Melody
{
    public class UIOperationDialogData : IPanelData { }

    public partial class UIOperationDialog
    {
        #region Vita Export

        private const string VitaInputJsonKey = "VitaInputJson";

        private const string VitaOutputCsvKey = "VitaOutputCsv";

        private const string VitaStepLimitKey = "VitaStepLimit";

        /// <summary>
        /// * Vita 输入的Json文件路径
        /// </summary>
        private readonly ReactiveVariable<string> _vitaInputJson = new();

        /// <summary>
        /// * Vita 输出的Csv文件所在目录
        /// </summary>
        private readonly ReactiveVariable<string> _vitaOutputCsv = new();

        /// <summary>
        /// * Vita 导出的步骤限制,-1为不进行限制
        /// </summary>
        private readonly ReactiveVariable<int> _vitaStepLimit = new(-1);

        /// <summary>
        /// * Vita 导出的Calc查询次数
        /// </summary>
        private readonly ReactiveVariable<string> _vitaExportCalc = new();

        /// <summary>
        /// * 当前正在导出的Vita的解决器
        /// </summary>
        private SpiderSolver _currentVitaExportSolver;

        /// <summary>
        /// * 当前正在导出的Vita的Poker
        /// </summary>
        private Poker _currentVitaExportPoker;

        private bool _stopExportVitaFlag;

        #endregion

        #region PlayValve Export

        private const string PlayValveInputTxtKey = "PlayValveInputTxt";

        private const string PlayValveOutputCsvKey = "PlayValveOutputCsv";

        private const string PlayValveStepLimitKey = "PlayValveStepLimit";

        private const string PlayValveSuitCountKey = "PlayValveSuitCount";

        /// <summary>
        /// * PlayValve 输入的Txt文件路径
        /// </summary>
        private readonly ReactiveVariable<string> _playValveInputTxt = new();

        /// <summary>
        /// * PlayValve 输出的Csv文件所在目录
        /// </summary>
        private readonly ReactiveVariable<string> _playValveOutputCsv = new();

        /// <summary>
        /// * PlayValve 导出的步骤限制,-1为不进行限制
        /// </summary>
        private readonly ReactiveVariable<int> _playValveStepLimit = new(-1);

        /// <summary>
        /// * PlayValve 1花色导出的Calc查询次数
        /// </summary>
        private readonly ReactiveVariable<string> _playValveSuit1ExportCalc = new();

        /// <summary>
        /// * PlayValve 2花色导出的Calc查询次数
        /// </summary>
        private readonly ReactiveVariable<string> _playValveSuit2ExportCalc = new();

        /// <summary>
        /// * PlayValve 3花色导出的Calc查询次数
        /// </summary>
        private readonly ReactiveVariable<string> _playValveSuit3ExportCalc = new();

        /// <summary>
        /// * PlayValve 4花色导出的Calc查询次数
        /// </summary>
        private readonly ReactiveVariable<string> _playValveSuit4ExportCalc = new();

        /// <summary>
        /// * PlayValve 导出的花色数量选择 (1-4)
        /// </summary>
        private readonly ReactiveVariable<int> _playValveExportSuitCount = new(1);

        /// <summary>
        /// * 当前正在导出的PlayValve 1花色的解决器
        /// </summary>
        private SpiderSolver _currentPlayValveSuit1ExportSolver;

        /// <summary>
        /// * 当前正在导出的PlayValve 2花色的解决器
        /// </summary>
        private SpiderSolver _currentPlayValveSuit2ExportSolver;

        /// <summary>
        /// * 当前正在导出的PlayValve 3花色的解决器
        /// </summary>
        private SpiderSolver _currentPlayValveSuit3ExportSolver;

        /// <summary>
        /// * 当前正在导出的PlayValve 4花色的解决器
        /// </summary>
        private SpiderSolver _currentPlayValveSuit4ExportSolver;

        /// <summary>
        /// * 当前正在导出的PlayValve 1花色的Poker
        /// </summary>
        private Poker _currentPlayValveSuit1ExportPoker;

        /// <summary>
        /// * 当前正在导出的PlayValve 2花色的Poker
        /// </summary>
        private Poker _currentPlayValveSuit2ExportPoker;

        /// <summary>
        /// * 当前正在导出的PlayValve 3花色的Poker
        /// </summary>
        private Poker _currentPlayValveSuit3ExportPoker;

        /// <summary>
        /// * 当前正在导出的PlayValve 4花色的Poker
        /// </summary>
        private Poker _currentPlayValveSuit4ExportPoker;

        private bool _stopExportPlayValveSuit1Flag;
        private bool _stopExportPlayValveSuit2Flag;
        private bool _stopExportPlayValveSuit3Flag;
        private bool _stopExportPlayValveSuit4Flag;

        #endregion

        #region Life Cycle

        protected override void OnSetup()
        {
            #region Vita Export

            mExportVitaBtn.onClick.AddListener(OnClickExportVita);
            mStopExportVitaBtn.onClick.AddListener(OnClickStopExportVita);
            mSelectVitaInputBtn.onClick.AddListener(OnClickSelectVitaInput);
            mSelectVitaOutputBtn.onClick.AddListener(OnClickSelectVitaOutput);
            mVitaStepLimit.onEndEdit.AddListener(OnVitaStepLimitEndEdit);
            mQueryVitaExportCalcBtn.onClick.AddListener(OnClickQueryVitaExportCalc);

            #endregion

            #region PlayValve Export

            mExportPVBtn.onClick.AddListener(OnClickExportPlayValve);
            mStopExportPVBtn.onClick.AddListener(OnClickStopExportPlayValve);
            mSelectPVInputBtn.onClick.AddListener(OnClickSelectPlayValveInput);
            mSelectPVOutputBtn.onClick.AddListener(OnClickSelectPlayValveOutput);
            mPVStepLimit.onEndEdit.AddListener(OnPlayValveStepLimitEndEdit);
            mQueryPVExportSuit1CalcBtn.onClick.AddListener(OnClickQueryPlayValveSuit1ExportCalc);
            mQueryPVExportSuit2CalcBtn.onClick.AddListener(OnClickQueryPlayValveSuit2ExportCalc);
            mQueryPVExportSuit3CalcBtn.onClick.AddListener(OnClickQueryPlayValveSuit3ExportCalc);
            mQueryPVExportSuit4CalcBtn.onClick.AddListener(OnClickQueryPlayValveSuit4ExportCalc);

            mSuit1Toggle.onValueChanged.AddListener(x =>
            {
                if (x)
                {
                    _playValveExportSuitCount.Value = 1;
                    App.Storage.SetInt(PlayValveSuitCountKey, 1);
                }
            });
            mSuit2Toggle.onValueChanged.AddListener(x =>
            {
                if (x)
                {
                    _playValveExportSuitCount.Value = 2;
                    App.Storage.SetInt(PlayValveSuitCountKey, 2);
                }
            });
            mSuit3Toggle.onValueChanged.AddListener(x =>
            {
                if (x)
                {
                    _playValveExportSuitCount.Value = 3;
                    App.Storage.SetInt(PlayValveSuitCountKey, 3);
                }
            });
            mSuit4Toggle.onValueChanged.AddListener(x =>
            {
                if (x)
                {
                    _playValveExportSuitCount.Value = 4;
                    App.Storage.SetInt(PlayValveSuitCountKey, 4);
                }
            });

            #endregion
        }

        protected override void OnOpen() { }

        protected override void OnShow()
        {
            #region Vita Export

            _vitaInputJson.SubscribeToTMPText(mVitaInputJson, x => x?.Replace("\\", "/"));
            _vitaOutputCsv.SubscribeToTMPText(mVitaOutputCsv, x => x?.Replace("\\", "/"));
            _vitaExportCalc.SubscribeToTMPText(mQueryCalcLbl);
            _vitaStepLimit.SubscribeToTMPInputField(mVitaStepLimit);
            _vitaInputJson.Value = App.Storage.GetString(VitaInputJsonKey).Replace("\\", "/");
            _vitaOutputCsv.Value = App.Storage.GetString(VitaOutputCsvKey).Replace("\\", "/");
            _vitaStepLimit.Value = App.Storage.GetInt(VitaStepLimitKey);

            #endregion

            #region PlayValve Export

            _playValveInputTxt.SubscribeToTMPText(mPVInputTxt, x => x?.Replace("\\", "/"));
            _playValveOutputCsv.SubscribeToTMPText(mPVOutputCsv, x => x?.Replace("\\", "/"));
            _playValveSuit1ExportCalc.SubscribeToTMPText(mQueryPVSuit1CalcLbl);
            _playValveSuit2ExportCalc.SubscribeToTMPText(mQueryPVSuit2CalcLbl);
            _playValveSuit3ExportCalc.SubscribeToTMPText(mQueryPVSuit3CalcLbl);
            _playValveSuit4ExportCalc.SubscribeToTMPText(mQueryPVSuit4CalcLbl);
            _playValveStepLimit.SubscribeToTMPInputField(mPVStepLimit);
            _playValveInputTxt.Value = App
                .Storage.GetString(PlayValveInputTxtKey)
                .Replace("\\", "/");
            _playValveOutputCsv.Value = App
                .Storage.GetString(PlayValveOutputCsvKey)
                .Replace("\\", "/");
            _playValveStepLimit.Value = App.Storage.GetInt(PlayValveStepLimitKey);
            _playValveExportSuitCount.Subscribe(x =>
            {
                switch (x)
                {
                    case 1:
                        mSuit1Toggle.isOn = true;
                        break;
                    case 2:
                        mSuit2Toggle.isOn = true;
                        break;
                    case 3:
                        mSuit3Toggle.isOn = true;
                        break;
                    case 4:
                        mSuit4Toggle.isOn = true;
                        break;
                }
            });
            _playValveExportSuitCount.Value = App.Storage.GetInt(PlayValveSuitCountKey);

            #endregion

            OnClickQueryVitaExportCalc();
            OnClickQueryPlayValveSuit1ExportCalc();
            OnClickQueryPlayValveSuit2ExportCalc();
            OnClickQueryPlayValveSuit3ExportCalc();
            OnClickQueryPlayValveSuit4ExportCalc();
        }

        protected override void OnHide()
        {
            #region Vita Export

            _vitaInputJson.Dispose();
            _vitaOutputCsv.Dispose();
            _vitaStepLimit.Dispose();
            _vitaExportCalc.Dispose();

            #endregion

            #region PlayValve Export

            _playValveInputTxt.Dispose();
            _playValveOutputCsv.Dispose();
            _playValveStepLimit.Dispose();
            _playValveSuit1ExportCalc.Dispose();
            _playValveSuit2ExportCalc.Dispose();
            _playValveSuit3ExportCalc.Dispose();
            _playValveSuit4ExportCalc.Dispose();

            #endregion
        }

        protected override void OnShowTweenEnd() { }

        protected override void OnEndTweenBegin() { }

        protected override void OnClose() { }

        protected override void OnBack() { }

        protected override void OnUIDestroy() { }

        #endregion

        #region Vita Export

        /// <summary>
        /// * 点击选择Vita输入Json
        /// </summary>
        private void OnClickSelectVitaInput()
        {
            var paths = StandaloneFileBrowser.OpenFilePanel(
                "Select Vita Input Json",
                "",
                "json",
                false
            );
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                _vitaInputJson.Value = paths[0];
                App.Storage.SetString(VitaInputJsonKey, _vitaInputJson.Value);
            }
        }

        /// <summary>
        /// * 点击选择Vita导出Csv所在目录
        /// </summary>
        private void OnClickSelectVitaOutput()
        {
            var folderPath = StandaloneFileBrowser.OpenFolderPanel(
                "Select Vita Output Folder",
                "",
                false
            );
            if (folderPath.Length > 0 && !string.IsNullOrEmpty(folderPath[0]))
            {
                _vitaOutputCsv.Value = folderPath[0];
                App.Storage.SetString(VitaOutputCsvKey, _vitaOutputCsv.Value);
            }
        }

        private void OnVitaStepLimitEndEdit(string text)
        {
            if (int.TryParse(text, out var stepLimit))
            {
                if (stepLimit >= 0)
                    _vitaStepLimit.Value = stepLimit;
                else
                    _vitaStepLimit.Value = -1;
            }
            else
            {
                _vitaStepLimit.Value = 0; // # 为了刷新显示,先改一个无效值,再改到正确的-1
                _vitaStepLimit.Value = -1;
            }

            App.Storage.SetInt(VitaStepLimitKey, _vitaStepLimit.Value);
        }

        private void OnClickQueryVitaExportCalc()
        {
            var prefix = _currentVitaExportPoker != null ? _currentVitaExportPoker.Mark : "???";
            _vitaExportCalc.Value =
                prefix
                + "\n"
                + (
                    _currentVitaExportSolver != null
                        ? _currentVitaExportSolver.Calc.ToString()
                        : "0"
                );
        }

        private void OnClickExportVita()
        {
            if (string.IsNullOrEmpty(_vitaInputJson.Value))
                return;
            if (string.IsNullOrEmpty(_vitaOutputCsv.Value))
                return;
            if (!File.Exists(_vitaInputJson.Value))
                return;
            if (_currentVitaExportSolver != null || _currentVitaExportPoker != null)
                return;
            Task.Run(ExportVita);
        }

        private void OnClickStopExportVita()
        {
            if (_currentVitaExportSolver != null && !_stopExportVitaFlag)
                _stopExportVitaFlag = true;
        }

        private async void ExportVita()
        {
            try
            {
                _currentVitaExportSolver = new SpiderSolver();
                var json = await File.ReadAllTextAsync(_vitaInputJson.Value);
                var bean = JsonConvert.DeserializeObject<VitaBean>(json, Constant.JsonSetting);
                var filePrefix = _vitaOutputCsv.Value + @"\VitaColor";

                var time = DateTimeOffset.UtcNow;
                var m = time.ToUnixTimeMilliseconds();
                var fileSuffix = "-" + m + ".csv";

                foreach (var x in bean.SelectMany(item => item.Value))
                {
                    _currentVitaExportSolver = new SpiderSolver();
                    var poker = new Poker(x.question);
                    _currentVitaExportPoker = poker;
                    _currentVitaExportSolver.SuitCount = poker.GetSuitCount();
                    await _currentVitaExportSolver.TaskDepthFirstSearch(
                        poker,
                        null,
                        filePrefix + _currentVitaExportSolver.SuitCount + fileSuffix,
                        x.id,
                        stepLimit: _vitaStepLimit.Value
                    );
                    if (_stopExportVitaFlag)
                        break;
                }
            }
            catch (Exception e)
            {
                Log.MsgE(e.Message);
            }
            finally
            {
                _stopExportVitaFlag = false;
                _currentVitaExportSolver = null;
                _currentVitaExportPoker = null;
            }
        }

        #endregion

        #region PlayValve Export

        private void OnClickExportPlayValve()
        {
            var suit = _playValveExportSuitCount.Value;
            if (string.IsNullOrEmpty(_playValveInputTxt.Value))
                return;
            if (string.IsNullOrEmpty(_playValveOutputCsv.Value))
                return;
            if (!File.Exists(_playValveInputTxt.Value))
                return;
            if (
                suit == 1
                && (
                    _currentPlayValveSuit1ExportSolver != null
                    || _currentPlayValveSuit1ExportPoker != null
                )
            )
                return;
            if (
                suit == 2
                && (
                    _currentPlayValveSuit2ExportSolver != null
                    || _currentPlayValveSuit2ExportPoker != null
                )
            )
                return;
            if (
                suit == 3
                && (
                    _currentPlayValveSuit3ExportSolver != null
                    || _currentPlayValveSuit3ExportPoker != null
                )
            )
                return;
            if (
                suit == 4
                && (
                    _currentPlayValveSuit4ExportSolver != null
                    || _currentPlayValveSuit4ExportPoker != null
                )
            )
                return;
            Task.Run(ExportPlayValve);
        }

        private async void ExportPlayValve()
        {
            var suit = _playValveExportSuitCount.Value;
            var step = _playValveStepLimit.Value;
            try
            {
                var filePrefix = _playValveOutputCsv.Value + @"\PlayValveSuit";
                var time = DateTimeOffset.UtcNow;
                var m = time.ToUnixTimeMilliseconds();
                var fileSuffix = "-" + m + ".csv";
                switch (suit)
                {
                    case 1:
                        _currentPlayValveSuit1ExportSolver = new SpiderSolver();
                        break;
                    case 2:
                        _currentPlayValveSuit2ExportSolver = new SpiderSolver();
                        break;
                    case 3:
                        _currentPlayValveSuit3ExportSolver = new SpiderSolver();
                        break;
                    case 4:
                        _currentPlayValveSuit4ExportSolver = new SpiderSolver();
                        break;
                }

                var txt = await File.ReadAllTextAsync(_playValveInputTxt.Value);
                var array = txt.Split(',');
                var seeds = array.Select(int.Parse).ToArray();
                var fullFile = filePrefix + suit + fileSuffix;
                foreach (var seed in seeds)
                {
                    var poker = new Poker(seed, suit);
                    switch (suit)
                    {
                        case 1:
                            _currentPlayValveSuit1ExportSolver = new SpiderSolver
                            {
                                SuitCount = suit,
                            };
                            _currentPlayValveSuit1ExportPoker = poker;
                            await _currentPlayValveSuit1ExportSolver.TaskDepthFirstSearch(
                                poker,
                                null,
                                fullFile,
                                seed,
                                stepLimit: step
                            );
                            break;
                        case 2:
                            _currentPlayValveSuit2ExportSolver = new SpiderSolver
                            {
                                SuitCount = suit,
                            };
                            _currentPlayValveSuit2ExportPoker = poker;
                            await _currentPlayValveSuit2ExportSolver.TaskDepthFirstSearch(
                                poker,
                                null,
                                fullFile,
                                seed,
                                stepLimit: step
                            );
                            break;
                        case 3:
                            _currentPlayValveSuit3ExportSolver = new SpiderSolver
                            {
                                SuitCount = suit,
                            };
                            _currentPlayValveSuit3ExportPoker = poker;
                            await _currentPlayValveSuit3ExportSolver.TaskDepthFirstSearch(
                                poker,
                                null,
                                fullFile,
                                seed,
                                stepLimit: step
                            );
                            break;
                        case 4:
                            _currentPlayValveSuit4ExportSolver = new SpiderSolver
                            {
                                SuitCount = suit,
                            };
                            _currentPlayValveSuit4ExportPoker = poker;
                            await _currentPlayValveSuit4ExportSolver.TaskDepthFirstSearch(
                                poker,
                                null,
                                fullFile,
                                seed,
                                stepLimit: step
                            );
                            break;
                    }

                    if (suit == 1 && _stopExportPlayValveSuit1Flag)
                        break;
                    if (suit == 2 && _stopExportPlayValveSuit2Flag)
                        break;
                    if (suit == 3 && _stopExportPlayValveSuit3Flag)
                        break;
                    if (suit == 4 && _stopExportPlayValveSuit4Flag)
                        break;
                }
            }
            catch (Exception e)
            {
                Log.MsgE(e.Message);
            }
            finally
            {
                switch (suit)
                {
                    case 1:
                        _stopExportPlayValveSuit1Flag = false;
                        _currentPlayValveSuit1ExportSolver = null;
                        _currentPlayValveSuit1ExportPoker = null;
                        break;
                    case 2:
                        _stopExportPlayValveSuit2Flag = false;
                        _currentPlayValveSuit2ExportSolver = null;
                        _currentPlayValveSuit2ExportPoker = null;
                        break;
                    case 3:
                        _stopExportPlayValveSuit3Flag = false;
                        _currentPlayValveSuit3ExportSolver = null;
                        _currentPlayValveSuit3ExportPoker = null;
                        break;
                    case 4:
                        _stopExportPlayValveSuit4Flag = false;
                        _currentPlayValveSuit4ExportSolver = null;
                        _currentPlayValveSuit4ExportPoker = null;
                        break;
                }
            }
        }

        private void OnClickStopExportPlayValve()
        {
            switch (_playValveExportSuitCount.Value)
            {
                case 1:
                    if (
                        _currentPlayValveSuit1ExportSolver != null
                        && !_stopExportPlayValveSuit1Flag
                    )
                        _stopExportPlayValveSuit1Flag = true;
                    break;
                case 2:
                    if (
                        _currentPlayValveSuit2ExportSolver != null
                        && !_stopExportPlayValveSuit2Flag
                    )
                        _stopExportPlayValveSuit2Flag = true;
                    break;
                case 3:
                    if (
                        _currentPlayValveSuit3ExportSolver != null
                        && !_stopExportPlayValveSuit3Flag
                    )
                        _stopExportPlayValveSuit3Flag = true;
                    break;
                case 4:
                    if (
                        _currentPlayValveSuit4ExportSolver != null
                        && !_stopExportPlayValveSuit4Flag
                    )
                        _stopExportPlayValveSuit4Flag = true;
                    break;
            }
        }

        private void OnClickSelectPlayValveInput()
        {
            var paths = StandaloneFileBrowser.OpenFilePanel(
                "Select PlayValve Input Txt",
                "",
                "txt",
                false
            );
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                _playValveInputTxt.Value = paths[0];
                App.Storage.SetString(PlayValveInputTxtKey, _playValveInputTxt.Value);
            }
        }

        private void OnClickSelectPlayValveOutput()
        {
            var folderPath = StandaloneFileBrowser.OpenFolderPanel(
                "Select PlayValve Output Folder",
                "",
                false
            );
            if (folderPath.Length > 0 && !string.IsNullOrEmpty(folderPath[0]))
            {
                _playValveOutputCsv.Value = folderPath[0];
                App.Storage.SetString(PlayValveOutputCsvKey, _playValveOutputCsv.Value);
            }
        }

        private void OnPlayValveStepLimitEndEdit(string text)
        {
            if (int.TryParse(text, out var stepLimit))
            {
                if (stepLimit >= 0)
                    _playValveStepLimit.Value = stepLimit;
                else
                    _playValveStepLimit.Value = -1;
            }
            else
            {
                _playValveStepLimit.Value = 0; // # 为了刷新显示,先改一个无效值,再改到正确的-1
                _playValveStepLimit.Value = -1;
            }

            App.Storage.SetInt(PlayValveStepLimitKey, _playValveStepLimit.Value);
        }

        private void OnClickQueryPlayValveSuit1ExportCalc()
        {
            var prefix =
                _currentPlayValveSuit1ExportPoker != null
                    ? _currentPlayValveSuit1ExportPoker.Mark
                    : "???";
            _playValveSuit1ExportCalc.Value =
                prefix
                + "\n"
                + (
                    _currentPlayValveSuit1ExportSolver != null
                        ? _currentPlayValveSuit1ExportSolver.Calc.ToString()
                        : "0"
                );
        }

        private void OnClickQueryPlayValveSuit2ExportCalc()
        {
            var prefix =
                _currentPlayValveSuit2ExportPoker != null
                    ? _currentPlayValveSuit2ExportPoker.Mark
                    : "???";
            _playValveSuit2ExportCalc.Value =
                prefix
                + "\n"
                + (
                    _currentPlayValveSuit2ExportSolver != null
                        ? _currentPlayValveSuit2ExportSolver.Calc.ToString()
                        : "0"
                );
        }

        private void OnClickQueryPlayValveSuit3ExportCalc()
        {
            var prefix =
                _currentPlayValveSuit3ExportPoker != null
                    ? _currentPlayValveSuit3ExportPoker.Mark
                    : "???";
            _playValveSuit3ExportCalc.Value =
                prefix
                + "\n"
                + (
                    _currentPlayValveSuit3ExportSolver != null
                        ? _currentPlayValveSuit3ExportSolver.Calc.ToString()
                        : "0"
                );
        }

        private void OnClickQueryPlayValveSuit4ExportCalc()
        {
            var prefix =
                _currentPlayValveSuit4ExportPoker != null
                    ? _currentPlayValveSuit4ExportPoker.Mark
                    : "???";
            _playValveSuit4ExportCalc.Value =
                prefix
                + "\n"
                + (
                    _currentPlayValveSuit4ExportSolver != null
                        ? _currentPlayValveSuit4ExportSolver.Calc.ToString()
                        : "0"
                );
        }

        #endregion

        private void Update()
        {
            #region Vita Export

            if (_stopExportVitaFlag)
                _currentVitaExportSolver?.StopTask();

            #endregion

            #region PlayValve Export

            if (_stopExportPlayValveSuit1Flag)
                _currentPlayValveSuit1ExportSolver?.StopTask();
            if (_stopExportPlayValveSuit2Flag)
                _currentPlayValveSuit2ExportSolver?.StopTask();
            if (_stopExportPlayValveSuit3Flag)
                _currentPlayValveSuit3ExportSolver?.StopTask();
            if (_stopExportPlayValveSuit4Flag)
                _currentPlayValveSuit4ExportSolver?.StopTask();

            #endregion
        }
    }
}
// Generation Time: Friday, April 25, 2025 8:47:41 AM
// Generation ID: dc4844a6-934e-44ef-b985-5a2fe94e74d4
