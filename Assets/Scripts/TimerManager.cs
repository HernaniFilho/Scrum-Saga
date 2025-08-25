using System;
using System.Collections;
using System.Linq;
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
    private System.Collections.Generic.Dictionary<string, Action> activeTimers = new System.Collections.Generic.Dictionary<string, Action>();
    private string currentDisplayTimerKey = "";
    
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
        // Registrar timer ativo
        if (onComplete != null)
        {
            activeTimers[timerKey] = onComplete;
        }
        
        // Configurar propriedades específicas do timer
        Hashtable props = new Hashtable();
        props[$"Timer_{timerKey}_StartTime"] = PhotonNetwork.ServerTimestamp;
        props[$"Timer_{timerKey}_Duration"] = duration + 1;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        // Se for o primeiro timer, ativar display
        if (!isTimerActive)
        {
            isTimerActive = true;
            currentDisplayTimerKey = timerKey;
            
            if (timerDisplay != null)
            {
                timerContainer.gameObject.SetActive(true);
                timerDisplay.gameObject.SetActive(true);
            }
        }
        
        Debug.Log($"Timer '{timerKey}' iniciado: {FormatTime(duration)}");
    }
    
    private void UpdateTimer()
    {
        if (!isTimerActive || string.IsNullOrEmpty(currentDisplayTimerKey)) return;
        
        // Atualizar timer do display atual
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue($"Timer_{currentDisplayTimerKey}_StartTime", out object startTimeObj) &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue($"Timer_{currentDisplayTimerKey}_Duration", out object durationObj))
        {
            int startTime = (int)startTimeObj;
            float duration = (float)durationObj;
            
            int elapsed = PhotonNetwork.ServerTimestamp - startTime;
            float timeRemaining = duration - (elapsed / 1000f);
            
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                EndTimer(currentDisplayTimerKey);
            }
            
            UpdateDisplay(timeRemaining);
        }
        
        // Verificar outros timers ativos
        CheckOtherTimers();
    }
    
    private void CheckOtherTimers()
    {
        foreach (var timerKey in activeTimers.Keys)
        {
            if (timerKey == currentDisplayTimerKey) continue;
            
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue($"Timer_{timerKey}_StartTime", out object startTimeObj) &&
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue($"Timer_{timerKey}_Duration", out object durationObj))
            {
                int startTime = (int)startTimeObj;
                float duration = (float)durationObj;
                
                int elapsed = PhotonNetwork.ServerTimestamp - startTime;
                float timeRemaining = duration - (elapsed / 1000f);
                
                if (timeRemaining <= 0)
                {
                    EndTimer(timerKey);
                }
            }
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
    
    public void EndTimer(string timerKey = "")
    {
        if (string.IsNullOrEmpty(timerKey))
        {
            timerKey = currentDisplayTimerKey;
        }
        
        Debug.Log($"Timer '{timerKey}' encerrado!");
        
        // Executar callback se existir
        if (activeTimers.ContainsKey(timerKey))
        {
            Action callback = activeTimers[timerKey];
            activeTimers.Remove(timerKey);
            
            // Limpar propriedades específicas deste timer
            StopSpecificTimer(timerKey);
            
            // Se era o timer do display, desativar display ou trocar para outro
            if (currentDisplayTimerKey == timerKey)
            {
                if (activeTimers.Count > 0)
                {
                    // Trocar display para outro timer ativo
                    currentDisplayTimerKey = activeTimers.Keys.First();
                    Debug.Log($"Display trocado para timer '{currentDisplayTimerKey}'");
                }
                else
                {
                    // Nenhum timer ativo, desativar display
                    isTimerActive = false;
                    currentDisplayTimerKey = "";
                    
                    if (timerDisplay != null)
                    {
                        timerDisplay.gameObject.SetActive(false);
                        timerContainer.gameObject.SetActive(false);
                    }
                }
            }
            
            // Executar callback DEPOIS da limpeza
            callback?.Invoke();
        }
    }
    
    private void StopSpecificTimer(string timerKey)
    {
        Hashtable props = new Hashtable();
        props[$"Timer_{timerKey}_StartTime"] = null;
        props[$"Timer_{timerKey}_Duration"] = null;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log($"Timer '{timerKey}' parado!");
    }
    
    public void StopTimer(string timerKey = "")
    {
        if (string.IsNullOrEmpty(timerKey))
        {
            // Parar timer do display atual
            if (!string.IsNullOrEmpty(currentDisplayTimerKey))
            {
                StopSpecificTimer(currentDisplayTimerKey);
                activeTimers.Remove(currentDisplayTimerKey);
                
                // Trocar para outro timer se houver
                if (activeTimers.Count > 0)
                {
                    currentDisplayTimerKey = activeTimers.Keys.First();
                }
                else
                {
                    isTimerActive = false;
                    currentDisplayTimerKey = "";
                    
                    if (timerDisplay != null)
                    {
                        timerDisplay.gameObject.SetActive(false);
                        timerContainer.gameObject.SetActive(false);
                    }
                }
            }
        }
        else
        {
            // Parar timer específico
            StopSpecificTimer(timerKey);
            activeTimers.Remove(timerKey);
            
            if (currentDisplayTimerKey == timerKey)
            {
                if (activeTimers.Count > 0)
                {
                    currentDisplayTimerKey = activeTimers.Keys.First();
                }
                else
                {
                    isTimerActive = false;
                    currentDisplayTimerKey = "";
                    
                    if (timerDisplay != null)
                    {
                        timerDisplay.gameObject.SetActive(false);
                        timerContainer.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
    
    public bool IsTimerActive()
    {
        return isTimerActive;
    }
    
    public float GetTimeRemaining(string timerKey = "")
    {
        string keyToCheck = string.IsNullOrEmpty(timerKey) ? currentDisplayTimerKey : timerKey;
        
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue($"Timer_{keyToCheck}_StartTime", out object startTimeObj) &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue($"Timer_{keyToCheck}_Duration", out object durationObj))
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
        // Verificar novos timers iniciados
        foreach (var prop in propertiesThatChanged)
        {
            string key = prop.Key.ToString();
            
            if (key.StartsWith("Timer_") && key.EndsWith("_StartTime"))
            {
                string timerKey = key.Substring(6, key.Length - 16); // Remove "Timer_" e "_StartTime"
                
                if (prop.Value != null)
                {
                    // Timer iniciado
                    Debug.Log($"Timer '{timerKey}' sincronizado!");
                    
                    // Se não há timer no display, usar este
                    if (!isTimerActive)
                    {
                        isTimerActive = true;
                        currentDisplayTimerKey = timerKey;
                        
                        if (timerDisplay != null)
                        {
                            timerContainer.gameObject.SetActive(true);
                            timerDisplay.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    // Timer parado
                    Debug.Log($"Timer '{timerKey}' parado via sincronização!");
                    
                    activeTimers.Remove(timerKey);
                    
                    if (currentDisplayTimerKey == timerKey)
                    {
                        if (activeTimers.Count > 0)
                        {
                            currentDisplayTimerKey = activeTimers.Keys.First();
                        }
                        else
                        {
                            isTimerActive = false;
                            currentDisplayTimerKey = "";
                            
                            if (timerDisplay != null)
                            {
                                timerDisplay.gameObject.SetActive(false);
                                timerContainer.gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }
        }
    }
}
