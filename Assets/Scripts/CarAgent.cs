using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using TMPro;

public class CarAgent : Agent
{
    // Singleton
    public static CarAgent instance;

    // Exposed variables
    public bool isFinished = false;

    [SerializeField] private List<GameObject> checkpoints;
    [SerializeField] private List<GameObject> ignoreCheckpoints;
    [SerializeField] private TextMeshPro cumulativeRewardText = null;
    [SerializeField] private CarController carController;
    [SerializeField] private bool isTraining = false;
    [SerializeField] private int stepTimeout = 500;
    [SerializeField] private float maxCheckpointTimeout = 10f;    

    private float currentCheckpointTimeout = 0f;
    private int currentCheckpoint = 0;
    private int nextCheckpoint = 0;
    private int nextStepTimeout = 0;
    private int currentLap = 0;
    private bool isRespawned = true;
    private Rigidbody rb = null;

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Initial setup, gets called when agent is enabled
    /// </summary>
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();        
    }

    /// <summary>
    /// Gets called at the beginning of each episode
    /// </summary>
    public override void OnEpisodeBegin()
    {
        ResetCar();
        nextStepTimeout = stepTimeout;
    }

    /// <summary>
    /// Executes code based on the actions received from the NN
    /// </summary>
    /// <param name="vectorAction">Actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {        
        // Floor to int for error control
        float forwardAmount = Mathf.FloorToInt(vectorAction[0]);
        float steerAmount = Mathf.FloorToInt(vectorAction[1]);

        if (!GameManager.instance.raceStarted || isFinished) { forwardAmount = 0f; steerAmount = 0f; return; }

        if (forwardAmount == 0f)
        {
            carController.Accelerate(0f);
        }
        else if(forwardAmount == 1f)
        {
            carController.Accelerate(1f);
        }
        else if(forwardAmount == 2f)
        {
            carController.Accelerate(-1f);
        }
        
        if(steerAmount == 0f)
        {
            carController.Steer(0f);
        }
        else if(steerAmount == 1f)
        {
            carController.Steer(1f);
        }
        else if (steerAmount == 2f)
        {
            carController.Steer(-1f);
        }

        // Add reward for actions
        AddReward(CalculateRewardFromRPM());

        if (isTraining)
        {
            if(StepCount > nextStepTimeout)
            {
                AddReward(-.5f);
                EndEpisode();
            }
        }
    }

    /// <summary>
    /// Allows user to control the agent
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = 0f;
        actionsOut[1] = 0f;

        if(Input.GetAxis("Vertical") == 1f) { actionsOut[0] = 1f; }
        else if(Input.GetAxis("Vertical") == -1f) { actionsOut[0] = 2f; }

        if (Input.GetAxis("Horizontal") == 1f) { actionsOut[1] = 1f; }
        else if (Input.GetAxis("Horizontal") == -1f) { actionsOut[1] = 2f; }
    }

    /// <summary>
    /// Gets called every fixed interval
    /// </summary>
    private void FixedUpdate()
    {
        if (cumulativeRewardText != null) { cumulativeRewardText.text = GetCumulativeReward().ToString("0.00"); }
        if (!isTraining && GameManager.instance.raceStarted && !isFinished)
        {
            currentCheckpointTimeout += Time.deltaTime;
            if(currentCheckpointTimeout >= maxCheckpointTimeout)
            {
                ResetCar();
            }
        }
    }

    public float CalculateRewardFromRPM()
    {
        float reward = 0f;
        reward = (carController.wheelColliderLeftBack.rpm + carController.wheelColliderRightBack.rpm) / 2f;
        reward *= 0.00001f;
        return reward;
    }

    /// <summary>
    /// Used to reset the car position and checkpoints
    /// </summary>
    public void ResetCar()
    {
        // Get random checkpoint and spawn car at that checkpoint
        if (isTraining) { RespawnToRandomCheckpoint(); }
        else if (GameManager.instance.raceStarted) { RespawnToLastCheckpoint(); }

        // Reset car velocity and wheels
        rb.velocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        carController.ResetWheels();

        // Reset timers
        currentCheckpointTimeout = 0f;
        nextStepTimeout = StepCount + stepTimeout;

        isRespawned = true;

    }

    private void RespawnToRandomCheckpoint()
    {
        // Get random checkpoint
        int rnd = Random.Range(0, checkpoints.Count);
        rnd = (rnd + 1) % checkpoints.Count;

        // Reset to random checkpoint
        nextCheckpoint = rnd;
        gameObject.transform.position = checkpoints[nextCheckpoint].transform.position;
        gameObject.transform.forward = checkpoints[nextCheckpoint].transform.forward;        
    }

    private void RespawnToLastCheckpoint()
    {
        // Reset to last passed checkpoint
        foreach(GameObject go in ignoreCheckpoints)
        {
            if(go.name == checkpoints[currentCheckpoint].name)
            {
                currentCheckpoint++;
                nextCheckpoint++;
            }
        }
        gameObject.transform.position = checkpoints[currentCheckpoint].transform.position;
        gameObject.transform.forward = checkpoints[currentCheckpoint].transform.forward;        
    }

    /// <summary>
    /// Gets called when the object collides with another object
    /// </summary>
    /// <param name="collision">the collided object</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall") && isTraining)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    /// <summary>
    /// Gets called when the objects enters a trigger
    /// </summary>
    /// <param name="other">The trigger object</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            if(other.gameObject == checkpoints[nextCheckpoint])
            {
                nextStepTimeout = StepCount + stepTimeout;
                currentCheckpointTimeout = 0f;  
                           
                currentCheckpoint = nextCheckpoint;
                nextCheckpoint = (nextCheckpoint + 1) % checkpoints.Count;

                if(currentCheckpoint == 0)
                {
                    NewLap();
                }

                // Don't add reward on respawn
                if (isRespawned)
                {
                    isRespawned = false;
                    return;
                }

                AddReward(1f);
            }
            else
            {
                AddReward(-1f);
                EndEpisode();
            }
        }
    }

    /// <summary>
    /// Collects observations to base decisions on
    /// </summary>
    /// <param name="sensor">Observer</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.forward); // 3 values
        sensor.AddObservation(rb.velocity); // 3 values
        sensor.AddObservation(Vector3.Distance(checkpoints[nextCheckpoint].transform.position, transform.position)); // 1 value
        sensor.AddObservation((checkpoints[nextCheckpoint].transform.position - transform.position).normalized); // 3 values
        sensor.AddObservation(checkpoints[nextCheckpoint].transform.forward); // 3 values

        // 13 values total
    }

    private void NewLap()
    {
        if(currentLap < GameManager.instance.maxLaps) { currentLap++; }
        else
        {
            isFinished = true;
        }
    }

    public void PreRace()
    {
        currentLap = 0;
        nextCheckpoint = 0;
        currentCheckpoint = 0;
        isFinished = false;
    }
}
