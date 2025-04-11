using System.Collections.Generic;
using F8Framework.Core;
using Solver;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class SpiderWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        private string _inputSeed;

        private string _inputVita;

        private EditorCoroutine _solverCoroutine;

        [MenuItem("Spider/Spider Window")]
        private static void OpenSpiderWindow()
        {
            if (HasOpenInstances<SpiderWindow>())
                GetWindow<SpiderWindow>("Spider").Close();
            else
                GetWindow<SpiderWindow>("Spider");
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
                        }
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
                        }
                    );
                    _solverCoroutine = EditorCoroutine.Start(solver);
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