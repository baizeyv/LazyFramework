using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using F8Framework.Core;
using Lazy;
using Newtonsoft.Json;
using Solver;
using Solver.Exporter;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class SpiderWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        private string _inputSeed;

        private string _inputVita;

        private string _outputCsvPath;

        private EditorCoroutine _solverCoroutine;

        [MenuItem("Spider/Spider Window")]
        private static void OpenSpiderWindow()
        {
            if (HasOpenInstances<SpiderWindow>())
                GetWindow<SpiderWindow>("Spider").Close();
            else
                GetWindow<SpiderWindow>("Spider");
        }

        private IEnumerator _coroutine;

        private IEnumerator VitaColor1()
        {
            var json = File.ReadAllText(@"C:\Users\baizeyv\Documents\a\gameColor1.json");
            var bean = JsonConvert.DeserializeObject<VitaBean>(json, Constant.JsonSetting);
            foreach (var x in bean.SelectMany(item => item.Value))
            {
                var solver = new SpiderSolver() { SuitCount = 1 };
                var poker = new Poker(x.question);
                yield return solver.DepthFirstSearch(poker, null, _outputCsvPath, x.id);
            }
        }

        private void OnGUI()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            GUILayout.BeginVertical("helpbox");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("PlayValve Seed:", GUILayout.Width(100));
                    _inputSeed = GUILayout.TextField(_inputSeed);
                }
                GUILayout.EndHorizontal();
                var f = int.TryParse(_inputSeed, out var v);

                GUI.enabled = f;
                if (GUILayout.Button("Solve PlayValve", GUILayout.Height(35)))
                {
                    if (_solverCoroutine != null)
                        _solverCoroutine.Stop();
                    var ss = new SpiderSolver();
                    var poker = new Poker(v);
                    var solver = ss.DepthFirstSearch(
                        poker,
                        () =>
                        {
                            if (_solverCoroutine != null)
                                _solverCoroutine.Stop();
                            _solverCoroutine = null;
                        },
                        @"C:\Users\baizeyv\Documents\a\TestSpiderSolver.csv",
                        0
                    );
                    _solverCoroutine = EditorCoroutine.Start(solver);
                }

                GUI.enabled = true;
            }
            GUILayout.EndVertical();

            GUILayout.Space(5);

            GUILayout.BeginVertical("helpbox");
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
                    if (_solverCoroutine != null)
                        _solverCoroutine.Stop();
                    var ss = new SpiderSolver();
                    var poker = new Poker(_inputVita);
                    var solver = ss.DepthFirstSearch(
                        poker,
                        () =>
                        {
                            if (_solverCoroutine != null)
                                _solverCoroutine.Stop();
                            _solverCoroutine = null;
                        },
                        @"C:\Users\baizeyv\Documents\a\TestSpiderSolver.csv",
                        0
                    );
                    _solverCoroutine = EditorCoroutine.Start(solver);
                }

                GUI.enabled = true;
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("helpbox");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Save Path:", GUILayout.Width(80));
                    _outputCsvPath = GUILayout.TextArea(_outputCsvPath);
                }
                GUILayout.EndHorizontal();

                GUI.enabled = !string.IsNullOrEmpty(_outputCsvPath);
                if (GUILayout.Button("Export Vita"))
                {
                    if (_coroutine == null)
                    {
                        _coroutine = VitaColor1();
                        EditorCoroutine.Start(_coroutine);
                    }
                    else
                    {
                        Debug.Log("ZZZ");
                    }
                }

                GUI.enabled = true;
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            if (GUILayout.Button("Stop Solve It", GUILayout.Height(35)))
            {
                if (_solverCoroutine != null)
                    _solverCoroutine.Stop();
                _solverCoroutine = null;
            }
        }
    }
}
