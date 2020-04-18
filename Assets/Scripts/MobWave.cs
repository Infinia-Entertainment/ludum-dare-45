﻿using UnityEngine;
using System;

[Serializable]
public struct MobWave
{
    public enum WaveType
    {
        Monster,
        Boss
    }

    public WaveType Type;
    public GameObject Prefab;
    public int Count;
    public float DelayInBetween;
    public float DelayAfterWave;
}
