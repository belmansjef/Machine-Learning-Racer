using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cinemachine;
using UnityEditor;
using Unity.MLAgents;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager instance;

    public int maxLaps = 3;
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject gameCanvas;
    [SerializeField] private GameObject playerCar;
    [SerializeField] private GameObject agentCar;
    [SerializeField] private Transform playerStart;
    [SerializeField] private Transform agentStart;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private CinemachineVirtualCamera menuCamera;
    [SerializeField] private CinemachineVirtualCamera raceCamera;

    [HideInInspector] public bool raceStarted = false;

    private bool startCountdown = false;
    private float raceCountdownTime = 3f;
    private float currentCountdownTime = 0f;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        currentCountdownTime = raceCountdownTime;        
    }

    private void FixedUpdate()
    {
        if (startCountdown)
        {
            currentCountdownTime -= Time.deltaTime;
            UpdateCountdownUI();
            if(currentCountdownTime <= 0f)
            {
                StartRace();
            }
        }
    }

    private void UpdateCountdownUI()
    {
        countdownText.text = Mathf.CeilToInt(currentCountdownTime).ToString("0");
    }

    public void StartRaceCountdown()
    {
        startCountdown = true;

        CarAgent.instance.PreRace();

        ResetCar(playerCar, playerStart);
        ResetCar(agentCar, agentStart);
        
        agentCar.transform.position = agentStart.position;
        agentCar.transform.forward = agentStart.forward;

        menuCamera.gameObject.SetActive(false);
        raceCamera.gameObject.SetActive(true);

        menuCanvas.SetActive(false);
        gameCanvas.SetActive(true);
    }

    private void ResetCar(GameObject _car, Transform _start)
    {
        _car.transform.position = _start.position;
        _car.transform.forward = _start.forward;

        _car.GetComponent<Rigidbody>().velocity = Vector3.zero;
        _car.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        _car.GetComponent<CarController>().ResetWheels();
    }

    private void StartRace()
    {
        raceStarted = true;
        startCountdown = false;

        StartCoroutine("FadeCountdown");

        currentCountdownTime = raceCountdownTime;        
    }

    public void EndRace()
    {
        raceStarted = false;

        if (CarAgent.instance.isFinished)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "You lost!";
        }
        else
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "You won!";
        }

        StartCoroutine("BackToMainMenu");
    }

    IEnumerator FadeCountdown()
    {
        countdownText.text = "GO!";
        yield return new WaitForSeconds(1.5f);
        countdownText.gameObject.SetActive(false);
    }

    IEnumerator BackToMainMenu()
    {
        yield return new WaitForSeconds(2f);

        menuCanvas.SetActive(true);
        gameCanvas.SetActive(false);

        menuCamera.gameObject.SetActive(true);
        raceCamera.gameObject.SetActive(false);
    }
}
