using System;
using System.Collections;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

public class TimerManager : MonoBehaviourPunCallbacks
{
    public static TimerManager Instance { get; private set; }
    
    [Header("UI")]
    public GameObject timerContainer;
    public TMP_Text timerDisplay;
    
    private bool isTimerActive = false;
    private Action onTimerComplete;
    private string currentTimerKey = "";
    private const string TIMER_START_TIME_KEY = "TimerStartTime";
    private const string TIMER_DURATION_KEY = "TimerDuration";
    private const string TIMER_KEY_KEY = "TimerKey";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (isTimerActive)
        {
            UpdateTimer();
        }
    }
    
    public void StartTimer(float duration, Action onComplete = null, string timerKey = "DefaultTimer")
    {
        Hashtable props = new Hashtable();
        props[TIMER_START_TIME_KEY] = PhotonNetwork.ServerTimestamp;
        props[TIMER_DURATION_KEY] = duration + 1;
        props[TIMER_KEY_KEY] = timerKey;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        onTimerComplete = onComplete;
        Debug.Log($"Timer '{timerKey}' iniciado: {FormatTime(duration)}");
    }
    
    private void UpdateTimer()
    {
        if (!isTimerActive) return;
        
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TIMER_START_TIME_KEY, out object startTimeObj) &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TIMER_DURATION_KEY, out object durationObj))
        {
            int startTime = (int)startTimeObj;
            float duration = (float)durationObj;
            
            int elapsed = PhotonNetwork.ServerTimestamp - startTime;
            float timeRemaining = duration - (elapsed / 1000f);
            
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                EndTimer();
            }
            
            UpdateDisplay(timeRemaining);
        }
    }
    
    private void UpdateDisplay(float timeRemaining)
    {
        if (timerDisplay == null) return;
        
        timerDisplay.text = FormatTime(timeRemaining);
    }
    
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:0}:{1:00}", minutes, seconds);
    }
    
    public void EndTimer()
    {
        isTimerActive = false;
        
        if (timerDisplay != null)
        {
            timerDisplay.gameObject.SetActive(false);
        }
        
        Debug.Log($"Timer '{currentTimerKey}' encerrado!");
        
        if (onTimerComplete != null)
        {
            onTimerComplete.Invoke();
            onTimerComplete = null;
            
            StopTimer();
        }
    }
    
    public void StopTimer()
    {
        Hashtable props = new Hashtable();
        props[TIMER_START_TIME_KEY] = null;
        props[TIMER_DURATION_KEY] = null;
        props[TIMER_KEY_KEY] = null;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log($"Timer '{currentTimerKey}' parado!");
    }
    
    public bool IsTimerActive()
    {
        return isTimerActive;
    }
    
    public float GetTimeRemaining()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TIMER_START_TIME_KEY, out object startTimeObj) &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TIMER_DURATION_KEY, out object durationObj))
        {
            int startTime = (int)startTimeObj;
            float duration = (float)durationObj;
            
            int elapsed = PhotonNetwork.ServerTimestamp - startTime;
            return Mathf.Max(0, duration - (elapsed / 1000f));
        }
        return 0f;
    }
    
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Verificar se timer foi iniciado ou parado
        if (propertiesThatChanged.ContainsKey(TIMER_START_TIME_KEY))
        {
            if (propertiesThatChanged[TIMER_START_TIME_KEY] != null)
            {
                // Timer iniciado
                isTimerActive = true;
                currentTimerKey = PhotonNetwork.CurrentRoom.CustomProperties[TIMER_KEY_KEY]?.ToString() ?? "";
                
                if (timerDisplay != null)
                {
                    timerContainer.gameObject.SetActive(true);
                    timerDisplay.gameObject.SetActive(true);
                }
                
                Debug.Log($"Timer '{currentTimerKey}' sincronizado!");
            }
            else
            {
                // Timer parado
                isTimerActive = false;
                
                if (timerDisplay != null)
                {
                    timerDisplay.gameObject.SetActive(false);
                    timerContainer.gameObject.SetActive(false);
                }
                
                onTimerComplete = null;
                Debug.Log("Timer parado via sincronização!");
            }
        }
    }
}
