using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public enum drivetrain
{
    FWD,
    RWD,
    AWD
}

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider wheelColliderRightFront;
    [SerializeField] private WheelCollider wheelColliderLeftFront;
    public WheelCollider wheelColliderRightBack;
    public WheelCollider wheelColliderLeftBack;

    [Header("Transforms")]
    [SerializeField] private Transform wheelRightFront;
    [SerializeField] private Transform wheelLeftFront;
    [SerializeField] private Transform wheelRightBack;
    [SerializeField] private Transform wheelLeftBack;

    [Header("Car Properties")]
    [SerializeField] private bool isAI = false;
    [SerializeField] private Transform centerOfMass;
    [SerializeField] private float motorTorque = 500f;
    [SerializeField] private float brakeTorque = 1500f;
    [SerializeField] private float maxSteer = 20f;
    [SerializeField] private float maxRpm = 800f;
    [SerializeField] private drivetrain carDrivetrain = drivetrain.RWD;

    [Header("Skid marks")]
    [SerializeField] private List<TrailRenderer> tireMarks = new List<TrailRenderer>();
    private bool isEmittingTrail = false;

    // Private variables
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;
    }

    private void Update()
    {
        UpdateWheels();
        CheckFriction();
    }

    private void FixedUpdate()
    {
        if (!isAI && GameManager.instance.raceStarted)
        {
            Accelerate(Input.GetAxis("Vertical"));
            Steer(Input.GetAxis("Horizontal"));
            if (Input.GetButton("Brake"))
            {
                Brake(1f);
            }
            else
            {
                Brake(0f);
            }
        }
        else if (!GameManager.instance.raceStarted)
        {
            Accelerate(0f);
            Steer(0f);
        }
    }

    public void Accelerate(float _forwardAmount)
    {
        wheelColliderRightFront.brakeTorque = 0f;
        wheelColliderLeftFront.brakeTorque = 0f;
        wheelColliderRightBack.brakeTorque = 0f;
        wheelColliderLeftBack.brakeTorque = 0f;

        switch (carDrivetrain)
        {
            case drivetrain.FWD:
                wheelColliderRightFront.motorTorque = Mathf.Clamp(motorTorque * _forwardAmount, -maxRpm / 2, maxRpm);
                wheelColliderLeftFront.motorTorque = Mathf.Clamp(motorTorque * _forwardAmount, -maxRpm / 2, maxRpm);
                break;

            case drivetrain.RWD:
                wheelColliderRightBack.motorTorque = Mathf.Clamp(motorTorque * _forwardAmount, -maxRpm / 2, maxRpm);
                wheelColliderLeftBack.motorTorque = Mathf.Clamp(motorTorque * _forwardAmount, -maxRpm / 2, maxRpm);
                break;

            case drivetrain.AWD:
                wheelColliderRightFront.motorTorque = Mathf.Clamp(motorTorque * _forwardAmount, -maxRpm / 2, maxRpm);
                wheelColliderLeftFront.motorTorque = Mathf.Clamp(motorTorque * _forwardAmount, -maxRpm / 2, maxRpm);
                wheelColliderRightBack.motorTorque = Mathf.Clamp(motorTorque * _forwardAmount, -maxRpm / 2, maxRpm);
                wheelColliderLeftBack.motorTorque = Mathf.Clamp(motorTorque * _forwardAmount, -maxRpm / 2, maxRpm);
                break;

            default:
                Debug.LogError($"Drivtrain for {this.name} not set");
                break;
        }
        
    }

    public void Brake(float _brake)
    {
        if (_brake == 1f)
        {
            wheelColliderRightFront.brakeTorque = brakeTorque;
            wheelColliderLeftFront.brakeTorque = brakeTorque;
            wheelColliderRightBack.brakeTorque = brakeTorque;
            wheelColliderLeftBack.brakeTorque = brakeTorque;
        }
        if(_brake == 0f)
        {
            wheelColliderRightFront.brakeTorque = 0f;
            wheelColliderLeftFront.brakeTorque = 0f;
            wheelColliderRightBack.brakeTorque = 0f;
            wheelColliderLeftBack.brakeTorque = 0f;
        }
    }

    public void Steer(float _steerAmount)
    {
        wheelColliderLeftFront.steerAngle = maxSteer * _steerAmount;
        wheelColliderRightFront.steerAngle = maxSteer * _steerAmount;
    }

    public void Boost(float _vectorAction)
    {
        if (Input.GetButtonDown("Brake") || _vectorAction == 1f)
        {
            rb.AddForce(transform.forward * 10000f, ForceMode.Impulse);
        }
    }

    private void UpdateWheels()
    {
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        wheelColliderRightFront.GetWorldPose(out pos, out rot);
        wheelRightFront.position = pos;
        wheelRightFront.rotation = rot;

        wheelColliderLeftFront.GetWorldPose(out pos, out rot);
        wheelLeftFront.position = pos;
        wheelLeftFront.rotation = rot;

        wheelColliderRightBack.GetWorldPose(out pos, out rot);
        wheelRightBack.position = pos;
        wheelRightBack.rotation = rot;

        wheelColliderLeftBack.GetWorldPose(out pos, out rot);
        wheelLeftBack.position = pos;
        wheelLeftBack.rotation = rot
            ;
    }

    public void ResetWheels()
    {
        wheelColliderRightFront.brakeTorque = Mathf.Infinity;
        wheelColliderLeftFront.brakeTorque = Mathf.Infinity;
        wheelColliderRightBack.brakeTorque = Mathf.Infinity;
        wheelColliderLeftBack.brakeTorque = Mathf.Infinity;
    }

    public void CheckFriction()
    {
        if(Input.GetButtonDown("Brake") && (wheelColliderLeftBack.rpm + wheelColliderRightBack.rpm) / 2 > 100f)
        {
            StartEmitter();
        }
        else if(Input.GetButtonUp("Brake"))
        {
            StopEmitter();
        }
    }

    private void StartEmitter()
    {
        if(isEmittingTrail) { return; }
        foreach(TrailRenderer T in tireMarks)
        {
            T.emitting = true;
        }

        isEmittingTrail = true;
    }

    private void StopEmitter()
    {
        if(!isEmittingTrail) { return; }
        foreach(TrailRenderer T in tireMarks)
        {
            T.emitting = false;
        }

        isEmittingTrail = false;
    }
}
