using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using F8Framework.Core;
using Lazy;
using Newtonsoft.Json;
using Solver;
using Solver.Exporter;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Editor
{
    public class SpiderWindow : EditorWindow
    {
        private Thread _thread;

        private Thread _playValveThread;

        private Thread _vitaThread;

        private bool _showSolver = true;

        private bool _showExporter;

        private bool _showGenerator;

        private bool _showCalculation;

        private bool _showOutput;

        private string _exportStepLimitText;

        // ##################################################

        private int _searchCalc;

        private int _searchStep = -1;

        // ##################################################

        private Vector2 _scrollPos;

        private Vector2 _scrollPos2;

        private Vector2 _scrollPos3;

        // ##################################################

        private string _inputSeed;

        private string _inputVita;

        // ##################################################

        /// <summary>
        /// * Vita 的Json文件路径
        /// </summary>
        private string _inputVitaJsonPath;

        /// <summary>
        /// * Vita 的Csv输出文件路径
        /// </summary>
        private string _outputVitaCsvPath;

        /// <summary>
        /// * PlayValve 的种子列表,用`,`分割
        /// </summary>
        private string _inputPlayValveSeeds;

        /// <summary>
        /// * PlayValve 的Csv输出文件路径
        /// </summary>
        private string _outputPlayValveCsvPath;

        private List<SpiderGenerator> _generators = new();

        // ##################################################

        /// <summary>
        /// * 生成关卡的CSV文件路径
        /// </summary>
        private string _generationFileCsvPath;

        /// <summary>
        /// * 生成关卡的花色数量索引
        /// </summary>
        private int _generationSuitCountIndex;

        /// <summary>
        /// * 生成关卡的步骤最大限制, -1为不限制
        /// </summary>
        private string _generationStepLimit;

        /// <summary>
        /// * 生成关卡的最小种子
        /// </summary>
        private string _generationMinSeedText;

        /// <summary>
        /// * 生成关卡的最大种子,-1为int.MaxValue
        /// </summary>
        private string _generationMaxSeedText;

        // ##################################################

        private readonly string[] _suitOptions = { " Suit 1", " Suit 2", " Suit 3", " Suit 4" };

        private int _playValveSelectedOption;

        private int _selectedOption;

        // ##################################################

        private int _valuation = -99999;

        // ##################################################

        private EditorCoroutine _playValveEditorCoroutine;

        private EditorCoroutine _vitaEditorCoroutine;

        // ##################################################

        private SpiderSolver _solver;

        [MenuItem("Spider/Spider Window")]
        private static void OpenSpiderWindow()
        {
            if (HasOpenInstances<SpiderWindow>())
            {
                GetWindow<SpiderWindow>("Spider").Close();
            }
            else
            {
                var window = GetWindow<SpiderWindow>("Spider");
                window.minSize = new Vector2(1200, 600);
            }
        }

        private IEnumerator _vitaCoroutine;

        private IEnumerator _playValveCoroutine;

        private IEnumerator VitaColor()
        {
            var json = File.ReadAllText(_inputVitaJsonPath);
            var bean = JsonConvert.DeserializeObject<VitaBean>(json, Constant.JsonSetting);
            foreach (var x in bean.SelectMany(item => item.Value))
            {
                var solver = new SpiderSolver();
                var poker = new Poker(x.question);
                solver.SuitCount = poker.GetSuitCount();
                yield return solver.DepthFirstSearch(poker, null, _outputVitaCsvPath, x.id);
            }
        }

        private void ThreadVitaColor()
        {
            var json = File.ReadAllText(_inputVitaJsonPath);
            var bean = JsonConvert.DeserializeObject<VitaBean>(json, Constant.JsonSetting);
            var step = int.Parse(_exportStepLimitText);
            _vitaThread = new Thread(() =>
            {
                foreach (var x in bean.SelectMany(item => item.Value))
                {
                    var solver = new SpiderSolver();
                    var poker = new Poker(x.question);
                    solver.SuitCount = poker.GetSuitCount();
                    solver.ThreadDfs(poker, null, _outputVitaCsvPath, x.id, stepLimit: step);
                }
            });
            _vitaThread.Start();
        }

        private IEnumerator PlayValveSeed()
        {
            var array = _inputPlayValveSeeds.Split(',');
            var seeds = array.Select(int.Parse).ToArray();
            for (var i = 0; i < seeds.Length; i++)
            {
                var solver = new SpiderSolver { SuitCount = _playValveSelectedOption + 1 };
                var poker = new Poker(seeds[i], _playValveSelectedOption + 1);
                yield return solver.DepthFirstSearch(poker, null, _outputPlayValveCsvPath, i + 1);
            }
        }

        private void ThreadPlayValveSeed()
        {
            var array = _inputPlayValveSeeds.Split(',');
            var seeds = array.Select(int.Parse).ToArray();
            var step = int.Parse(_exportStepLimitText);
            _playValveThread = new Thread(() =>
            {
                for (var i = 0; i < seeds.Length; i++)
                {
                    var solver = new SpiderSolver { SuitCount = _playValveSelectedOption + 1 };
                    var poker = new Poker(seeds[i], _playValveSelectedOption + 1);
                    solver.ThreadDfs(poker, null, _outputPlayValveCsvPath, i + 1, stepLimit: step);
                }
            });
            _playValveThread.Start();
        }

        private void DrawSolvePlayValve()
        {
            GUILayout.BeginVertical(
                "helpbox",
                GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 10)
            );
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("PlayValve Seed:", GUILayout.Width(100));
                    _inputSeed = GUILayout.TextField(_inputSeed);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Suit Count:", GUILayout.Width(100));
                    for (var i = 0; i < _suitOptions.Length; i++)
                    {
                        var isSelected = _selectedOption == i;
                        var toggle = GUILayout.Toggle(isSelected, _suitOptions[i], "Radio");
                        if (toggle && !isSelected)
                        {
                            _selectedOption = i;
                            GUI.FocusControl(null);
                        }
                    }
                }
                GUILayout.EndHorizontal();

                var f = int.TryParse(_inputSeed, out var v);

                GUI.enabled = f;
                if (GUILayout.Button("Solve PlayValve", GUILayout.Height(35)))
                {
                    if (_thread == null)
                    {
                        _thread = new Thread(() =>
                        {
                            var ss = new SpiderSolver();
                            var poker = new Poker(v, _selectedOption + 1);
                            ss.SuitCount = _selectedOption + 1;
                            _solver = ss;
                            ss.ThreadDfs(
                                poker,
                                () =>
                                {
                                    _thread = null;
                                }
                            );
                        });
                        _thread.Start();
                    }
                    else
                    {
                        Debug.Log("Thread Running !");
                    }
                }

                GUI.enabled = true;
            }
            GUILayout.EndVertical();
        }

        private void DrawSolveVita()
        {
            GUILayout.BeginVertical(
                "helpbox",
                GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 10)
            );
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Vita Question:", GUILayout.Width(100));
                    _inputVita = GUILayout.TextArea(_inputVita);
                }
                GUILayout.EndHorizontal();

                if (string.IsNullOrEmpty(_inputVita) || _inputVita.Length != 136)
                    GUI.enabled = false;
                if (GUILayout.Button("Solve Vita", GUILayout.Height(35)))
                {
                    if (_thread == null)
                    {
                        _thread = new Thread(() =>
                        {
                            var ss = new SpiderSolver();
                            var poker = new Poker(_inputVita);
                            ss.SuitCount = poker.GetSuitCount();
                            _solver = ss;
                            ss.ThreadDfs(
                                poker,
                                () =>
                                {
                                    _thread = null;
                                }
                            );
                        });
                        _thread.Start();
                    }
                    else
                    {
                        Debug.Log("Thread Running !");
                    }
                }

                GUI.enabled = true;
            }
            GUILayout.EndVertical();
        }

        private void DrawExportPlayValve()
        {
            GUILayout.BeginVertical(
                "helpbox",
                GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 10)
            );
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("PlayValve Input:", GUILayout.Width(100));
                    _inputPlayValveSeeds = GUILayout.TextArea(_inputPlayValveSeeds);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Suit Count:", GUILayout.Width(100));
                    for (var i = 0; i < _suitOptions.Length; i++)
                    {
                        var isSelected = _playValveSelectedOption == i;
                        var toggle = GUILayout.Toggle(isSelected, _suitOptions[i], "Radio");
                        if (toggle && !isSelected)
                        {
                            _playValveSelectedOption = i;
                            GUI.FocusControl(null); // # 防止连续点击无效
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Save Path:", GUILayout.Width(100));
                    _outputPlayValveCsvPath = GUILayout.TextArea(_outputPlayValveCsvPath);
                }
                GUILayout.EndHorizontal();

                GUI.enabled =
                    !string.IsNullOrEmpty(_outputPlayValveCsvPath)
                    && !string.IsNullOrEmpty(_inputPlayValveSeeds);
                if (GUILayout.Button("Export PlayValve", GUILayout.Height(35)))
                    if (_playValveThread == null)
                        ThreadPlayValveSeed();
                // if (_playValveCoroutine == null)
                // {
                //     _playValveCoroutine = PlayValveSeed();
                //     _playValveEditorCoroutine = EditorCoroutine.Start(_playValveCoroutine);
                // }
                // else
                // {
                //     Debug.Log("Exporting PlayValve !");
                // }
                GUI.enabled = true;
                if (GUILayout.Button("Stop Export PlayValve", GUILayout.Height(30)))
                    if (_playValveThread != null)
                    {
                        _playValveThread.Abort();
                        _playValveThread = null;
                    }
                // _playValveEditorCoroutine?.Stop();
                // _playValveEditorCoroutine = null;
                // _playValveCoroutine = null;
            }
            GUILayout.EndVertical();
        }

        private void DrawExportVita()
        {
            GUILayout.BeginVertical(
                "helpbox",
                GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 10)
            );
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Vita Input:", GUILayout.Width(80));
                    _inputVitaJsonPath = GUILayout.TextArea(_inputVitaJsonPath);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Save Path:", GUILayout.Width(80));
                    _outputVitaCsvPath = GUILayout.TextArea(_outputVitaCsvPath);
                }
                GUILayout.EndHorizontal();

                GUI.enabled =
                    !string.IsNullOrEmpty(_outputVitaCsvPath)
                    && !string.IsNullOrEmpty(_inputVitaJsonPath)
                    && _inputVitaJsonPath.EndsWith(".json")
                    && File.Exists(_inputVitaJsonPath);
                if (GUILayout.Button("Export Vita", GUILayout.Height(35)))
                    if (_vitaThread == null)
                        ThreadVitaColor();
                // if (_vitaCoroutine == null)
                // {
                //     _vitaCoroutine = VitaColor();
                //     _vitaEditorCoroutine = EditorCoroutine.Start(_vitaCoroutine);
                // }
                // else
                // {
                //     Debug.Log("Exporting Vita !");
                // }
                GUI.enabled = true;
                if (GUILayout.Button("Stop Export Vita", GUILayout.Height(30)))
                    if (_vitaThread != null)
                    {
                        _vitaThread.Abort();
                        _vitaThread = null;
                    }
                // _vitaEditorCoroutine?.Stop();
                // _vitaEditorCoroutine = null;
                // _vitaCoroutine = null;
            }
            GUILayout.EndVertical();
        }

        private void DrawGenerator()
        {
            GUILayout.BeginVertical("helpbox");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Save Path:", GUILayout.Width(80));
                    _generationFileCsvPath = GUILayout.TextField(_generationFileCsvPath);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Suit Count:", GUILayout.Width(100));
                    for (var i = 0; i < _suitOptions.Length; i++)
                    {
                        var isSelected = _generationSuitCountIndex == i;
                        var toggle = GUILayout.Toggle(isSelected, _suitOptions[i], "Radio");
                        if (toggle && !isSelected)
                        {
                            _generationSuitCountIndex = i;
                            GUI.FocusControl(null); // # 防止连续点击无效
                        }
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Step Limit:", GUILayout.Width(100));
                    _generationStepLimit = GUILayout.TextField(_generationStepLimit);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Seed Range:", GUILayout.Width(100));
                    GUILayout.Label("Min", GUILayout.Width(30));
                    _generationMinSeedText = GUILayout.TextField(_generationMinSeedText);
                    GUILayout.Space(5);
                    GUILayout.Label("Max", GUILayout.Width(30));
                    _generationMaxSeedText = GUILayout.TextField(_generationMaxSeedText);
                }
                GUILayout.EndHorizontal();
                // # 步骤限制
                var stepLimitFlag = int.TryParse(_generationStepLimit, out var stepLimit);
                // # 最小种子限制
                var minSeedFlag = int.TryParse(_generationMinSeedText, out var minSeed);
                // # 最大种子限制
                var maxSeedFlag = int.TryParse(_generationMaxSeedText, out var maxSeed);
                GUILayout.BeginHorizontal();
                {
                    GUI.enabled =
                        !string.IsNullOrEmpty(_generationFileCsvPath)
                        && stepLimitFlag
                        && minSeedFlag
                        && maxSeedFlag
                        && (maxSeed > minSeed || maxSeed == -1);
                    if (GUILayout.Button("Generate", GUILayout.Height(35)))
                    {
                        var generator = new SpiderGenerator();
                        if (maxSeed == -1)
                            maxSeed = int.MaxValue;
                        // # 生成关卡
                        generator.GenerateLevel(
                            minSeed,
                            maxSeed,
                            _generationSuitCountIndex + 1,
                            _generationFileCsvPath,
                            stepLimit
                        );
                        _generators.Add(generator);
                    }

                    GUI.enabled = true;
                    if (
                        GUILayout.Button(
                            "Stop All Generation",
                            GUILayout.Height(35),
                            GUILayout.Width(150)
                        )
                    )
                    {
                        foreach (var generator in _generators)
                            generator.StopGeneration();
                        _generators.Clear();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawCalculationValuation()
        {
            GUILayout.BeginVertical("helpbox");
            {
                GUILayout.BeginVertical("helpbox");
                {
                    GUILayout.BeginHorizontal();
                    {
                        for (var i = 0; i < _hiddenReorderableList.Count; i++)
                        {
                            if (i != 0)
                                GUILayout.Space(4);
                            GUILayout.BeginVertical();
                            {
                                GUILayout.Label("Hidden " + i);
                                _hiddenReorderableList[i]?.DoLayoutList();
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("helpbox");
                {
                    GUILayout.BeginHorizontal();
                    {
                        for (var i = 0; i < _visibleReorderableList.Count; i++)
                        {
                            if (i != 0)
                                GUILayout.Space(4);
                            GUILayout.BeginVertical();
                            {
                                GUILayout.Label("Visible " + i);
                                _visibleReorderableList[i]?.DoLayoutList();
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.Space(2);
                if (_valuation != -99999)
                    GUILayout.Label("Valuation: " + _valuation);
                else
                    GUILayout.Label("Valuation: NaN");
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Calculate Valuation", GUILayout.Height(35)))
                    {
                        List<List<Card>> visibleGroup = new();
                        List<List<Card>> hiddenGroup = new();
                        for (var i = 0; i < 10; i++)
                        {
                            visibleGroup.Add(new List<Card>());
                            hiddenGroup.Add(new List<Card>());
                        }

                        for (var i = 0; i < _hiddenList.Count; i++)
                        {
                            var n = new List<EditorCard>(_hiddenList[i]);
                            n.Reverse();
                            foreach (var x in n)
                                hiddenGroup[i].Add(x.ToCard());
                        }

                        for (var i = 0; i < _visibleList.Count; i++)
                        {
                            var n = new List<EditorCard>(_visibleList[i]);
                            n.Reverse();
                            foreach (var x in n)
                                visibleGroup[i].Add(x.ToCard());
                        }

                        var poker = new Poker(visibleGroup, hiddenGroup, new List<Card>());
                        _valuation = poker.Valuation;
                    }

                    if (GUILayout.Button("Clear", GUILayout.Height(35), GUILayout.Width(50)))
                    {
                        foreach (var x in _hiddenList)
                            x.Clear();
                        foreach (var x in _visibleList)
                            x.Clear();
                        _valuation = -99999;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawOutput()
        {
            if (_solver == null)
                return;
            if (_solver.AllStep.Count <= 0)
                return;
            GUILayout.BeginVertical("helpbox");
            {
                GUILayout.BeginHorizontal();
                {
                    var e = Event.current;
                    if (e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow)
                        NextCalc();
                    if (e.type == EventType.KeyDown && e.keyCode == KeyCode.UpArrow)
                        PrevCalc();
                    if (e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow && e.shift)
                        LastCalc();
                    if (e.type == EventType.KeyDown && e.keyCode == KeyCode.UpArrow && e.shift)
                        FirstCalc();
                    GUILayout.Label("Calc:", GUILayout.Width(35));
                    if (
                        !int.TryParse(
                            GUILayout.TextField(_searchCalc.ToString(), GUILayout.Width(50)),
                            out _searchCalc
                        )
                    )
                    {
                        _searchCalc = 0;
                    }
                    else
                    {
                        if (_searchCalc < 0)
                            _searchCalc = 0;
                    }

                    GUILayout.Label("Step:", GUILayout.Width(35));
                    if (
                        !int.TryParse(
                            GUILayout.TextField(_searchStep.ToString(), GUILayout.Width(50)),
                            out _searchStep
                        )
                    )
                    {
                        _searchStep = -1;
                    }
                    else
                    {
                        if (_searchStep < 0)
                            _searchStep = -1;
                    }

                    GUILayout.Label("Total: " + (_solver == null ? "NaN" : _solver.AllStep.Count));

                    if (GUILayout.Button("First", GUILayout.Width(50)))
                        FirstCalc();
                    if (GUILayout.Button("Last", GUILayout.Width(50)))
                        LastCalc();
                    if (GUILayout.Button("Prev", GUILayout.Width(50)))
                        PrevCalc();
                    if (GUILayout.Button("Next", GUILayout.Width(50)))
                        NextCalc();
                }
                GUILayout.EndHorizontal();
                if (_searchStep == -1)
                {
                    if (_solver.AllStep.Count > _searchCalc)
                    {
                        var poker = _solver.AllStep[_searchCalc];
                        GUILayout.BeginVertical("helpbox");
                        {
                            GUILayout.Label(
                                poker.ToString(),
                                new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true }
                            );
                        }
                        GUILayout.EndVertical();
                    }
                }
                else
                {
                    var array = _solver.AllStep.Where(x => x.History.Count == _searchStep);
                    _scrollPos3 = GUILayout.BeginScrollView(_scrollPos3);
                    GUILayout.BeginVertical();
                    {
                        foreach (var x in array)
                        {
                            GUILayout.BeginVertical("helpbox");
                            {
                                GUILayout.Label(
                                    x.ToString(),
                                    new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true }
                                );
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndScrollView();
                }
            }
            GUILayout.EndVertical();
        }

        private void FirstCalc()
        {
            if (_solver != null)
                _searchCalc = 0;
        }

        private void LastCalc()
        {
            if (_solver != null)
                _searchCalc = _solver.AllStep.Count - 1;
        }

        private void PrevCalc()
        {
            if (_solver != null)
            {
                _searchCalc--;
                if (_searchCalc < 0)
                    _searchCalc = 0;
            }
        }

        private void NextCalc()
        {
            if (_solver != null)
            {
                _searchCalc++;
                if (_searchCalc >= _solver.AllStep.Count)
                    _searchCalc = _solver.AllStep.Count - 1;
            }
        }

        private void OnEnable()
        {
            InitializeEditorCardReorderableList();
        }

        private void OnDisable()
        {
            if (_thread != null)
            {
                _thread.Abort();
                _thread = null;
            }

            foreach (var generator in _generators)
                generator.StopGeneration();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal("helpbox");
            {
                _showSolver = GUILayout.Toggle(_showSolver, "Show Solver", GUILayout.Width(100));
                _showExporter = GUILayout.Toggle(
                    _showExporter,
                    "Show Exporter",
                    GUILayout.Width(110)
                );
                _showGenerator = GUILayout.Toggle(
                    _showGenerator,
                    "Show Generator",
                    GUILayout.Width(120)
                );
                _showCalculation = GUILayout.Toggle(
                    _showCalculation,
                    "Show Calculation",
                    GUILayout.Width(120)
                );
                _showOutput = GUILayout.Toggle(_showOutput, "Show Output", GUILayout.Width(120));
            }
            GUILayout.EndHorizontal();
            if (_showSolver)
            {
                GUILayout.BeginVertical("helpbox");
                {
                    GUILayout.BeginHorizontal();
                    {
                        DrawSolvePlayValve();
                        GUILayout.Space(2);
                        DrawSolveVita();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                    if (GUILayout.Button("Stop Solve It", GUILayout.Height(35)))
                        if (_thread != null)
                        {
                            _thread.Abort();
                            _thread = null;
                        }
                    // if (_solverCoroutine != null)
                    //     _solverCoroutine.Stop();
                    // _solverCoroutine = null;
                }
                GUILayout.EndVertical();
                GUILayout.Space(15);
            }

            if (_showExporter)
            {
                GUILayout.BeginVertical("helpbox");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Step Limit:", GUILayout.Width(100));
                        _exportStepLimitText = GUILayout.TextField(_exportStepLimitText);
                        if (!int.TryParse(_exportStepLimitText, out var val))
                            _exportStepLimitText = "-1";
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.BeginHorizontal("helpbox");
                    {
                        DrawExportPlayValve();
                        GUILayout.Space(2);
                        DrawExportVita();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(15);
            }

            if (_showGenerator)
                DrawGenerator();

            if (_showCalculation)
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);
                DrawCalculationValuation();
                GUILayout.EndScrollView();
                GUILayout.Space(15);
            }

            if (_showOutput)
            {
                _scrollPos2 = GUILayout.BeginScrollView(_scrollPos2);
                DrawOutput();
                GUILayout.EndScrollView();
            }

            GUILayout.Space(5);
        }

        /// <summary>
        /// * 隐藏牌的ReorderableList的数组
        /// </summary>
        private readonly List<ReorderableList> _hiddenReorderableList = new(10);

        /// <summary>
        /// * 隐藏牌的牌数组
        /// </summary>
        private readonly List<List<EditorCard>> _hiddenList = new(10);

        /// <summary>
        /// * 可见牌的ReorderableList的数组
        /// </summary>
        private readonly List<ReorderableList> _visibleReorderableList = new(10);

        /// <summary>
        /// * 可见牌的牌数组
        /// </summary>
        private readonly List<List<EditorCard>> _visibleList = new(10);

        private void InitializeEditorCardReorderableList()
        {
            for (var i = 0; i < 10; i++)
            {
                _hiddenList.Add(new List<EditorCard>());
                _visibleList.Add(new List<EditorCard>());
            }

            for (var i = 0; i < 10; i++)
            {
                _hiddenReorderableList.Add(
                    new ReorderableList(_hiddenList[i], typeof(EditorCard), true, false, true, true)
                );
                _visibleReorderableList.Add(
                    new ReorderableList(
                        _visibleList[i],
                        typeof(EditorCard),
                        true,
                        false,
                        true,
                        true
                    )
                );
            }

            for (var i = 0; i < _hiddenReorderableList.Count; i++)
            {
                var finalI = i;
                _hiddenReorderableList[i].drawElementCallback = (
                    rect,
                    index,
                    isActive,
                    isFocused
                ) =>
                {
                    rect.y += 2;
                    var w = rect.width / 2f;
                    const float gap = 2f;
                    _hiddenList[finalI][index].suitType = (SuitType)
                        EditorGUI.EnumPopup(
                            new Rect(rect.x, rect.y, w - gap, EditorGUIUtility.singleLineHeight),
                            _hiddenList[finalI][index].suitType
                        );
                    if (
                        !int.TryParse(
                            EditorGUI.TextField(
                                new Rect(
                                    rect.x + w + gap,
                                    rect.y,
                                    rect.width - w - gap,
                                    EditorGUIUtility.singleLineHeight
                                ),
                                _hiddenList[finalI][index].value.ToString()
                            ),
                            out _hiddenList[finalI][index].value
                        )
                    )
                    {
                        _hiddenList[finalI][index].value = 1;
                    }
                    else
                    {
                        if (
                            _hiddenList[finalI][index].value > 13
                            || _hiddenList[finalI][index].value < 1
                        )
                            _hiddenList[finalI][index].value = 1;
                    }
                };

                _hiddenReorderableList[i].onAddCallback = l =>
                {
                    _hiddenList[finalI]
                        .Add(new EditorCard { suitType = SuitType.Heart, value = 1 });
                };
                _hiddenReorderableList[i].onRemoveCallback = l =>
                {
                    if (l.index >= 0 && l.index <= _hiddenList[finalI].Count)
                        _hiddenList[finalI].RemoveAt(l.index);
                };
            }

            for (var i = 0; i < _visibleReorderableList.Count; i++)
            {
                var finalI = i;
                _visibleReorderableList[i].drawElementCallback = (
                    rect,
                    index,
                    isActive,
                    isFocused
                ) =>
                {
                    rect.y += 2;
                    var w = rect.width / 2f;
                    const float gap = 2f;
                    _visibleList[finalI][index].suitType = (SuitType)
                        EditorGUI.EnumPopup(
                            new Rect(rect.x, rect.y, w - gap, EditorGUIUtility.singleLineHeight),
                            _visibleList[finalI][index].suitType
                        );
                    if (
                        !int.TryParse(
                            EditorGUI.TextField(
                                new Rect(
                                    rect.x + w + gap,
                                    rect.y,
                                    rect.width - w - gap,
                                    EditorGUIUtility.singleLineHeight
                                ),
                                _visibleList[finalI][index].value.ToString()
                            ),
                            out _visibleList[finalI][index].value
                        )
                    )
                    {
                        _visibleList[finalI][index].value = 1;
                    }
                    else
                    {
                        if (
                            _visibleList[finalI][index].value > 13
                            || _visibleList[finalI][index].value < 1
                        )
                            _visibleList[finalI][index].value = 1;
                    }
                };

                _visibleReorderableList[i].onAddCallback = l =>
                {
                    _visibleList[finalI]
                        .Add(new EditorCard { suitType = SuitType.Heart, value = 1 });
                };
                _visibleReorderableList[i].onRemoveCallback = l =>
                {
                    if (l.index >= 0 && l.index <= _visibleList[finalI].Count)
                        _visibleList[finalI].RemoveAt(l.index);
                };
            }
        }

        [Serializable]
        private class EditorCard
        {
            public SuitType suitType;

            public int value;

            public Card ToCard()
            {
                switch (suitType)
                {
                    case SuitType.Heart:
                        return new HeartCard(value + 13);
                    case SuitType.Diamond:
                        return new DiamondCard(value + 39);
                    case SuitType.Spades:
                        return new SpadeCard(value);
                    case SuitType.Clubs:
                        return new ClubsCard(value + 26);
                    default:
                        return new SpadeCard(value);
                }
            }
        }

        private enum SuitType
        {
            [InspectorName("♥️")]
            Heart,

            [InspectorName("♦️")]
            Diamond,

            [InspectorName("♠️")]
            Spades,

            [InspectorName("♣️")]
            Clubs,
        }
    }
}
