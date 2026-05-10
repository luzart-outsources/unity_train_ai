using System;
using System.IO;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using UnityEngine;

namespace TrainAI.Systems.Save
{
    public class SaveManager
    {
        public string SavePath => Path.Combine(Application.persistentDataPath, "trainai-save.json");
        public string SaveBackup => SavePath + ".bak";

        public bool HasSave => File.Exists(SavePath);

        public void Save()
        {
            try
            {
                var data = BuildSnapshot();
                var json = JsonUtility.ToJson(data, prettyPrint: true);
                var tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(SavePath))
                {
                    File.Replace(tmp, SavePath, SaveBackup);
                }
                else
                {
                    File.Move(tmp, SavePath);
                }
                GameEvents.RaiseSaved();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e}");
            }
        }

        public bool TryLoad(out GameSaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(SavePath)) return false;
                var json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<GameSaveData>(json);
                ApplySnapshot(data);
                GameEvents.RaiseLoaded();
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e}");
                return false;
            }
        }

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(SaveBackup)) File.Delete(SaveBackup);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Delete failed: {e}");
            }
        }

        private GameSaveData BuildSnapshot()
        {
            var d = new GameSaveData();
            d.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            d.playerName = GameServices.Player != null ? GameServices.Player.Name : "";
            var t = GameServices.Time;
            if (t != null)
            {
                var n = t.Now;
                d.day = n.day; d.hour = n.hour; d.minute = n.minute;
                d.weekdayIndex = (int)n.weekday;
            }
            var s = GameServices.Score;
            if (s != null)
            {
                d.discipline = s.Discipline; d.academic = s.Academic;
            }
            var q = GameServices.Quest;
            if (q != null)
            {
                d.currentQuestIndex = q.CurrentQuestIndex;
            }
            return d;
        }

        private void ApplySnapshot(GameSaveData d)
        {
            if (d == null) return;
            if (GameServices.Player == null) GameServices.Player = new PlayerData(d.playerName);
            else GameServices.Player.Name = d.playerName;

            var t = GameServices.Time;
            if (t != null)
            {
                t.SetTime(d.hour, d.minute);
                // day apply: simplified - khong revert weekday history
            }

            var s = GameServices.Score;
            if (s != null)
            {
                int curD = s.Discipline, curA = s.Academic;
                if (d.discipline < curD) s.PenalizeDiscipline(curD - d.discipline);
                else if (d.discipline > curD) s.RewardDiscipline(d.discipline - curD);
                if (d.academic > curA) s.AddAcademic(d.academic - curA);
            }
        }
    }
}
