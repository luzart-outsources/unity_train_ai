using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    // Core Stats Keys
    public const string STAT_THELUC = "Stat_TheLuc";
    public const string STAT_KYLUAT = "Stat_KyLuat";
    public const string STAT_CHIENDAU = "Stat_ChienDau";
    public const string STAT_TRITUE = "Stat_TriTue";
    public const string STAT_XAHOI = "Stat_XaHoi";
    public const string STAT_TINHTHAN = "Stat_TinhThan";

    // Subjects
    public const string SUB_DIEULENH = "Sub_DieuLenh";
    public const string SUB_CHINHTRI = "Sub_ChinhTri";
    public const string SUB_CHIENTHUAT = "Sub_ChienThuat";
    public const string SUB_BANSUNG_TH = "Sub_BanSungTH";
    public const string SUB_LICHSU = "Sub_LichSu";
    public const string SUB_BANSUNG_LT = "Sub_BanSungLT";
    public const string SUB_AN_NINH_MANG = "Sub_AnNinhMang";
    public const string SUB_SINHTON = "Sub_SinhTon";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void AddStat(string statKey, int amount)
    {
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        blackboard.IncreaseValue(statKey, amount);
        
        // Clamp 0-100 for Core Stats
        if (blackboard.TryGetValue(statKey, out int val))
        {
            if (val > 100 && IsCoreStat(statKey))
            {
                blackboard.SetValue(statKey, 100);
            }
            if (val < 0)
            {
                blackboard.SetValue(statKey, 0);
            }
        }
        
        // TODO: Update UI
        Debug.Log($"[StatsManager] {statKey} changed by {amount}. New value: {GetStat(statKey)}");
    }

    public int GetStat(string statKey)
    {
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        if (blackboard.TryGetValue(statKey, out int val))
        {
            return val;
        }
        return 0; // Default 0
    }

    private bool IsCoreStat(string key)
    {
        return key.StartsWith("Stat_");
    }
}
