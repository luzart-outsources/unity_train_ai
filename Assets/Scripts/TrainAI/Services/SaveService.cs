using System;
using System.IO;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    public class SaveService : ISaveService
    {
        readonly PlayerStateRSO _player;
        readonly GameClockRSO _clock;
        readonly DayProgressRSO _dayProgress;
        readonly string _baseDir;

        public SaveService(PlayerStateRSO player, GameClockRSO clock, DayProgressRSO dayProgress,
                           string baseDir = null)
        {
            _player = player;
            _clock = clock;
            _dayProgress = dayProgress;
            _baseDir = baseDir ?? Application.persistentDataPath;
        }

        static string SlotPath(string dir, int slot) => Path.Combine(dir, $"save_slot{slot}.json");

        public bool Save(int slot = 1)
        {
            try
            {
                var dto = new SaveDTO
                {
                    version = 1,
                    savedAt = DateTime.UtcNow.ToString("o"),
                    playerName = _player.playerName,
                    day = _clock.day,
                    weekday = (int)_clock.weekday,
                    hour = _clock.hour,
                    minute = _clock.minute,
                    hocTap = _player.hocTap,
                    renLuyen = _player.renLuyen,
                    currentScene = _player.currentScene,
                    posX = _player.lastWorldPos.x,
                    posY = _player.lastWorldPos.y,
                    posZ = _player.lastWorldPos.z,
                };
                if (_dayProgress != null)
                {
                    dto.completedQuestIds = new System.Collections.Generic.List<string>(_dayProgress.completedToday);
                    dto.missedQuestIds = new System.Collections.Generic.List<string>(_dayProgress.missedToday);
                }
                Directory.CreateDirectory(_baseDir);
                File.WriteAllText(SlotPath(_baseDir, slot), JsonUtility.ToJson(dto, prettyPrint: true));
                return true;
            }
            catch (Exception e) { Debug.LogError($"[Save] {e}"); return false; }
        }

        public bool TryLoad(int slot, out SaveDTO dto)
        {
            dto = null;
            try
            {
                var p = SlotPath(_baseDir, slot);
                if (!File.Exists(p)) return false;
                dto = JsonUtility.FromJson<SaveDTO>(File.ReadAllText(p));
                if (dto == null) return false;
                if (_player != null)
                {
                    _player.playerName = dto.playerName ?? "";
                    _player.hocTap = dto.hocTap;
                    _player.renLuyen = dto.renLuyen;
                    _player.currentScene = dto.currentScene ?? "10_World";
                    _player.lastWorldPos = new Vector3(dto.posX, dto.posY, dto.posZ);
                }
                if (_clock != null)
                {
                    _clock.day = dto.day;
                    _clock.hour = dto.hour;
                    _clock.minute = dto.minute;
                    _clock.weekday = (TrainAI.Core.Weekday)dto.weekday;
                }
                return true;
            }
            catch (Exception e) { Debug.LogError($"[Load] {e}"); return false; }
        }
    }
}
