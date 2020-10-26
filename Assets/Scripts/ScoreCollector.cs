using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using TMPro;

public class ScoreCollector : MonoBehaviour
{
    // Singleton
    public static ScoreCollector instance;
    [SerializeField] private TextMeshProUGUI highestLapText;

    private StatsRecorder statsRecorder;
    private int highestLap = 0;

    private void Awake()
    {
        instance = this;
        statsRecorder = Academy.Instance.StatsRecorder;
    }

    public void AddLaps(int lap)
    {
        if(lap > highestLap)
        {
            highestLap = lap;
            highestLapText.text = highestLap.ToString("000");
            statsRecorder.Add("Highest Lap", highestLap, StatAggregationMethod.MostRecent);
        }
    }
}
