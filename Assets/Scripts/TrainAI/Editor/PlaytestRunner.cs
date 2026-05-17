using System.IO;
using System.Reflection;
using System.Text;
using TrainAI.Services;
using TrainAI.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.Editor
{
    // Hands-free playtest harness, survives the Play-mode domain reload via SessionState.
    // Loads Bootstrap, enters play, navigates UI, advances 30 game-days, writes a markdown report.
    [InitializeOnLoad]
    public static class PlaytestRunner
    {
        const string ReportPath = "Library/playtest_report.md";
        const string KeyStep = "TrainAI.Playtest.Step";
        const string KeyDeadline = "TrainAI.Playtest.Deadline";
        const string KeyReport = "TrainAI.Playtest.Report";
        const string KeyActive = "TrainAI.Playtest.Active";

        static PlaytestRunner()
        {
            // Re-subscribe after every domain reload if a playtest is in progress.
            if (SessionState.GetBool(KeyActive, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Build Game/Playtest - Auto 30 Days (v3)", false, 250)]
        public static void Run()
        {
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene("Assets/Scenes/TrainAI/00_Bootstrap.unity", OpenSceneMode.Single);
            SessionState.SetBool(KeyActive, true);
            SessionState.SetInt(KeyStep, 0);
            SessionState.SetFloat(KeyDeadline, 0f);
            var sb = new StringBuilder();
            sb.AppendLine("# TrainAI playtest report");
            sb.AppendLine($"_generated: {System.DateTime.Now:O}_");
            SessionState.SetString(KeyReport, sb.ToString());
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(KeyActive, false)) { EditorApplication.update -= Tick; return; }
            if (!EditorApplication.isPlaying)
            {
                // play mode exited before we finished -> abort
                Finish("aborted (play exited)");
                return;
            }
            float now = (float)EditorApplication.timeSinceStartup;
            float deadline = SessionState.GetFloat(KeyDeadline, 0f);
            if (now < deadline) return;

            int step = SessionState.GetInt(KeyStep, 0);
            switch (step)
            {
                case 0:
                    Wait(now, 6); Advance(); break;

                case 1:
                {
                    var go = GameObject.Find("MainMenuCanvas");
                    if (go == null) { Wait(now, 2); break; }
                    var ctrl = go.GetComponent<UIMainMenuController>();
                    var btn = GetField<Button>(ctrl, "newGameButton");
                    if (btn == null) { Fail("NewGame button missing"); break; }
                    btn.onClick.Invoke();
                    Log("- OK clicked NewGame");
                    Wait(now, 4); Advance(); break;
                }

                case 2:
                {
                    var go = GameObject.Find("CreateCharCanvas");
                    if (go == null) { Wait(now, 2); break; }
                    var ctrl = go.GetComponent<UICreateCharController>();
                    var input = GetField<TMPro.TMP_InputField>(ctrl, "nameInput");
                    var btn = GetField<Button>(ctrl, "confirmButton");
                    if (input == null || btn == null) { Fail("CreateChar fields missing"); break; }
                    input.text = "AutoTester";
                    btn.onClick.Invoke();
                    Log("- OK CreateChar: name 'AutoTester' + Confirm");
                    Wait(now, 4); Advance(); break;
                }

                case 3:
                {
                    var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    if (active.name != "10_World") { Wait(now, 2); break; }
                    var loc = FirstLocator();
                    if (loc == null) { Fail("ServiceLocator missing"); break; }
                    Log("- OK World loaded; services bootstrapped=" + loc.IsBootstrapped);
                    Log($"- start: day={loc.clock.day} hour={loc.clock.hour}:{loc.clock.minute:D2} hocTap={loc.playerState.hocTap} renLuyen={loc.playerState.renLuyen}");
                    SessionState.SetFloat("TrainAI.Playtest.HoldStart", now);
                    Wait(now, 1); Advance(); break;
                }

                case 4:
                {
                    // Hold in 10_World and rotate the scene camera for 60 real
                    // seconds. This is a GPU stress test — earlier crashes hit
                    // 'D3D12 Device removed' after sustained rendering, so we
                    // want to confirm the scene survives a minute of frames
                    // before we trust it.
                    float holdStart = SessionState.GetFloat("TrainAI.Playtest.HoldStart", now);
                    if (now - holdStart < 60f)
                    {
                        var rig = GameObject.Find("CameraRig");
                        if (rig != null)
                            rig.transform.Rotate(0f, 30f * UnityEngine.Time.unscaledDeltaTime, 0f, Space.World);
                        // Don't Wait; let every Editor update tick rotate a bit.
                        break;
                    }
                    Log("- OK 60s rotation stress survived (GPU stable)");
                    Wait(now, 1); Advance(); break;
                }

                case 5:
                {
                    var loc = FirstLocator();
                    if (loc == null) { Fail("ServiceLocator dropped mid-test"); break; }
                    int errors = 0;
                    for (int d = 0; d < 30; d++)
                    {
                        try
                        {
                            loc.Clock.AdvanceDay();
                            loc.Clock.SkipTo(5, 0);
                        }
                        catch (System.Exception e)
                        {
                            errors++;
                            Log($"  ERR day {d}: {e.Message}");
                        }
                    }
                    Log($"- advanced 30 days; errors={errors}");
                    Log($"- after: day={loc.clock.day} weekday={loc.clock.weekday} hocTap={loc.playerState.hocTap} renLuyen={loc.playerState.renLuyen}");
                    Log(errors == 0 ? "## RESULT: PASS (60s stress + 30-day clock loop stable)" : $"## RESULT: FAIL ({errors} errors)");
                    Finish("done");
                    break;
                }
            }
        }

        static void Wait(float now, float seconds) => SessionState.SetFloat(KeyDeadline, now + seconds);
        static void Advance() => SessionState.SetInt(KeyStep, SessionState.GetInt(KeyStep, 0) + 1);

        static void Fail(string reason)
        {
            Log("- FAIL " + reason);
            Log("## RESULT: FAIL");
            Finish(reason);
        }

        static void Finish(string note)
        {
            File.WriteAllText(ReportPath, SessionState.GetString(KeyReport, ""));
            Debug.Log($"[PlaytestRunner] {note}; report -> {ReportPath}");
            SessionState.SetBool(KeyActive, false);
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
            // If we were launched via -executeMethod, also drop a sentinel so
            // the external CLI watcher knows we finished normally (not a crash
            // exit). The watcher tails this file's existence.
            File.WriteAllText("Library/playtest_done.flag", System.DateTime.Now.ToString("O") + " " + note);
        }

        static void Log(string line)
        {
            string s = SessionState.GetString(KeyReport, "") + line + "\n";
            SessionState.SetString(KeyReport, s);
        }

        static ServiceLocatorSO FirstLocator()
        {
            var arr = Resources.FindObjectsOfTypeAll<ServiceLocatorSO>();
            return arr != null && arr.Length > 0 ? arr[0] : null;
        }

        static T GetField<T>(object owner, string field) where T : class
        {
            if (owner == null) return null;
            var fi = owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return fi != null ? fi.GetValue(owner) as T : null;
        }
    }
}
