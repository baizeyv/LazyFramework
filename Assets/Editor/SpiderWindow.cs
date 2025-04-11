using System.Collections.Generic;
using F8Framework.Core;
using Solver;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class SpiderWindow : EditorWindow
    {
        private string _inputSeed;

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
            GUILayout.BeginVertical("helpbox");
            {
                _inputSeed = GUILayout.TextField(_inputSeed);
                var f = int.TryParse(_inputSeed, out var v);

                GUI.enabled = f;
                if (GUILayout.Button("Try To Solve"))
                {
                    if (_solverCoroutine != null)
                        _solverCoroutine.Stop();
                    var ss = new Solver.SpiderSolver();
                    var poker = new Poker(v, ss);
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

                if (GUILayout.Button("Stop Solve It"))
                {
                    if (_solverCoroutine != null)
                        _solverCoroutine.Stop();
                    _solverCoroutine = null;
                }
            }
            GUILayout.EndVertical();
        }
    }
}
