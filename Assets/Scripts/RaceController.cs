using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.Rendering;

public class RaceController : MonoBehaviour
{
    // UI Variables
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI currentLapTimeText = null;
    [SerializeField] private TextMeshProUGUI lastLapTimeText = null;
    [SerializeField] private TextMeshProUGUI bestLapTimeText = null;
    [SerializeField] private TextMeshProUGUI currentLapText = null;

    // Lap variables
    private float currentLapTime = 0f;
    private float lastLapTime = 0f;
    private float bestLapTime = 0f;
    private int currentLap = 0;

    // Checkpoint variables
    private int nextCheckpoint = 0;
    private int currentCheckpoint = 0;
    private float maxCheckpointTimeout = 10f;
    private float timeSinceLastCheckpoint = 0f;
    private Transform checkpointTransform = null;
    private List<GameObject> checkpoints = new List<GameObject>();

    private void Start()
    {
        // Initialize checkpoints
        checkpointTransform = GameObject.Find("Player Checkpoints").transform;
        foreach  (Transform child in checkpointTransform)
        {
            checkpoints.Add(child.gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPlayer();
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.instance.raceStarted)
        {
            timeSinceLastCheckpoint += Time.deltaTime;
            currentLapTime += Time.deltaTime;
            if (timeSinceLastCheckpoint >= maxCheckpointTimeout)
            {
                ResetPlayer();
            }
        }        
    }

    private void LateUpdate()
    {
        UpdateUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            GotCheckpoint(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            timeSinceLastCheckpoint = 0f;
        }
    }

    private void GotCheckpoint(GameObject _passedCheckpoint)
    {
        if(_passedCheckpoint == checkpoints[nextCheckpoint])
        {
            currentCheckpoint = nextCheckpoint;
            nextCheckpoint = (nextCheckpoint + 1) % checkpoints.Count;
            timeSinceLastCheckpoint = 0f;

            if(currentCheckpoint == 0) { NewLap(); }
        }
    }

    private void ResetPlayer()
    {
        // Reset to last passed checkpoint
        gameObject.transform.position = checkpoints[currentCheckpoint].transform.position;
        gameObject.transform.forward = checkpoints[currentCheckpoint].transform.forward;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<CarController>().ResetWheels();

        timeSinceLastCheckpoint = 0f;
    }

    private void NewLap()
    {
        if(currentLap != 0)
        {
            lastLapTime = currentLapTime;            
            if (lastLapTime < bestLapTime || currentLap == 1) { bestLapTime = lastLapTime; }
        }

        currentLapTime = 0f;
        if(currentLap < GameManager.instance.maxLaps) { currentLap++; }
        else { GameManager.instance.EndRace(); currentLap = 0; }
    }

    private void UpdateUI()
    {
        TimeSpan time;
        time = TimeSpan.FromSeconds(currentLapTime);
        currentLapTimeText.text = time.ToString("mm' : 'ss' : 'fff");
        time = TimeSpan.FromSeconds(lastLapTime);
        lastLapTimeText.text = time.ToString("mm' : 'ss' : 'fff");
        time = TimeSpan.FromSeconds(bestLapTime);
        bestLapTimeText.text = time.ToString("mm' : 'ss' : 'fff");

        currentLapText.text = currentLap.ToString("0") + "/" + GameManager.instance.maxLaps.ToString("0");
    }
}
