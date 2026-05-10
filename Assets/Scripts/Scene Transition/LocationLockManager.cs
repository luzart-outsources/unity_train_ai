using System;
using System.Collections.Generic;
using UnityEngine;

public class LocationLockManager 
{
    List<(SceneTransitionManager.Location, bool)> locks = new List<(SceneTransitionManager.Location, bool)>();

    public LocationLockManager()
    {
        SceneTransitionManager.Location[] locs = (SceneTransitionManager.Location[]) Enum.GetValues(typeof(SceneTransitionManager.Location));

        foreach(SceneTransitionManager.Location loc in locs)
        {
            locks.Add((loc, false));
        }
    }

    /// <summary>
    /// Check if the location is locked
    /// </summary>
    /// <param name="loc">Location</param>
    /// <returns></returns>
    public bool Locked(SceneTransitionManager.Location loc)
    {
        return locks.Find(x => x.Item1 == loc).Item2;
    }

    /// <summary>
    /// Set the lock status of a location
    /// </summary>
    /// <param name="loc">Location</param>
    /// <param name="locked">Whether it is going to be locked</param>
    public void Lock(SceneTransitionManager.Location loc, bool locked)
    {
        int index = locks.FindIndex(x => x.Item1 == loc);
        locks[index] = (loc, locked); 
    }
}
