using UnityEngine;
using System.Collections.Generic;

public class CommandRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    public bool isRecording = true;
    
    private DrawingSession currentSession;
    private ShapeDrawer shapeDrawer;
    
    public static CommandRecorder Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        shapeDrawer = GetComponent<ShapeDrawer>();
        StartNewSession();
    }
    
    public void StartNewSession()
    {
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            string playerName = Photon.Pun.PhotonNetwork.LocalPlayer.NickName;
            string playerId = Photon.Pun.PhotonNetwork.LocalPlayer.UserId;
            
            Debug.Log($"MULTIPLAYER MODE - Nickname: '{playerName}', UserId: '{playerId}', ActorNumber: {Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber}");
            
            // Fallback se o nickname não foi definido
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"Player_{Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber}";
                Debug.Log($"Nickname vazio! Usando fallback: {playerName}");
            }
            
            currentSession = new DrawingSession(playerName, playerId);
        }
        else
        {
            Debug.Log("OFFLINE MODE - usando Local Player");
            currentSession = new DrawingSession("Local Player", "local");
        }
        
        Debug.Log($"Nova sessão de desenho iniciada: {currentSession.sessionId} - Player: {currentSession.playerName}");
    }
    
    public void RecordCommand(ShapeType shapeType, Vector2 position, Vector2 size, float rotation, Color color, float thickness)
    {
        if (!isRecording || currentSession == null) return;
        
        string playerName = GetCurrentPlayerName();
        DrawingCommand command = new DrawingCommand(shapeType, position, size, rotation, color, thickness, playerName);
        currentSession.AddCommand(command);
        
        Debug.Log($"Comando gravado: {shapeType} na posição {position} por {playerName} (total: {currentSession.GetCommandCount()})");
    }
    
    public void RecordCommand(DrawingCommand command)
    {
        if (!isRecording || currentSession == null) return;
        
        currentSession.AddCommand(command);
        Debug.Log($"Comando gravado: {command.shapeType} (total: {currentSession.GetCommandCount()})");
    }
    
    public void RecordShapeFromGameObject(GameObject shape)
    {
        if (!isRecording || currentSession == null || shape == null) return;
        
        ShapeData shapeData = shape.GetComponent<ShapeData>();
        RectTransform rectTransform = shape.GetComponent<RectTransform>();
        
        if (shapeData != null && rectTransform != null)
        {
            string playerName = GetCurrentPlayerName();
            DrawingCommand command = new DrawingCommand(shapeData, rectTransform, playerName);
            currentSession.AddCommand(command);
            
            Debug.Log($"Shape gravado como comando: {command.shapeType} na posição {command.position} com cor {command.color} por {playerName} (total: {currentSession.GetCommandCount()})");
        }
    }
    
    public void RecordFloodFill(Vector2 position, Color fillColor)
    {
        if (!isRecording || currentSession == null) return;
        
        string playerName = GetCurrentPlayerName();
        DrawingCommand floodFillCommand = new DrawingCommand(position, fillColor, playerName);
        currentSession.AddCommand(floodFillCommand);
        
        Debug.Log($"Flood fill gravado: posição {position} com cor {fillColor} por {playerName} (total: {currentSession.GetCommandCount()})");
    }
    
    public DrawingSession GetCurrentSession()
    {
        return currentSession;
    }
    
    private string GetCurrentPlayerName()
    {
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            string playerName = Photon.Pun.PhotonNetwork.LocalPlayer.NickName;
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"Player_{Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber}";
            }
            return playerName;
        }
        else
        {
            return "Local Player";
        }
    }
    
    public void ClearCurrentSession()
    {
        if (currentSession != null)
        {
            currentSession.ClearCommands();
            Debug.Log("Sessão atual limpa");
        }
    }
    
    public string GetSessionJson()
    {
        if (currentSession == null) return "";
        
        string json = currentSession.ToJson();
        Debug.Log($"Sessão serializada: {json.Length} bytes");
        return json;
    }
    
    public int GetSessionCommandCount()
    {
        return currentSession?.GetCommandCount() ?? 0;
    }
    
    public void SetRecording(bool recording)
    {
        isRecording = recording;
        Debug.Log($"Gravação {(recording ? "ativada" : "desativada")}");
    }
    
    public void PauseRecording()
    {
        isRecording = false;
    }
    
    public void ResumeRecording()
    {
        isRecording = true;
    }
}
