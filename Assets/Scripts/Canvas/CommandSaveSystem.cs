using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class CommandSaveSystem : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private int maxSavedDrawings = 4;
    
    [Header("Load Buttons")]
    [SerializeField] private Button[] loadButtons = new Button[4];
    
    [Header("Player Name Display")]
    [SerializeField] private TMP_Text playerNameText;
    
    private List<DrawingSession> savedSessions = new List<DrawingSession>();
    private CommandRecorder commandRecorder;
    private CommandReplaySystem replaySystem;
    private int currentlyLoadedIndex = -1;
    
    private Color originalButtonColor = Color.white;
    private Color loadedButtonColor = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f);
    private float loadedButtonScale = 1f;
    
    public List<DrawingSession> SavedSessions => savedSessions;
    
    public void ClearSavedSessions()
    {
        savedSessions.Clear();
        UpdateButtonColors();
    }
    
    public void AddSavedSession(DrawingSession session)
    {
        savedSessions.Add(session);
        UpdateButtonColors();
    }
    public int MaxSavedDrawings => maxSavedDrawings;
    
    void Start()
    {
        commandRecorder = FindObjectOfType<CommandRecorder>();
        replaySystem = FindObjectOfType<CommandReplaySystem>();
        
        if (commandRecorder == null)
        {
            Debug.LogError("CommandSaveSystem precisa de CommandRecorder na cena!");
        }
        
        if (replaySystem == null)
        {
            Debug.LogError("CommandSaveSystem precisa de CommandReplaySystem na cena!");
        }
        
        SetupLoadButtons();
    }
    
    private void SetupLoadButtons()
    {
        for (int i = 0; i < loadButtons.Length; i++)
        {
            if (loadButtons[i] != null)
            {
                int capturedIndex = i;
                loadButtons[i].onClick.RemoveAllListeners();
                loadButtons[i].onClick.AddListener(() => LoadDrawing(capturedIndex));
            }
        }
    }
    
    private void UpdateButtonColors()
    {
        for (int i = 0; i < loadButtons.Length; i++)
        {
            if (loadButtons[i] != null)
            {
                // Se tem desenho salvo, mostra o botão
                if (i < savedSessions.Count && savedSessions[i] != null)
                {
                    loadButtons[i].gameObject.SetActive(true);
                    
                    Image buttonImage = loadButtons[i].GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        if (i == currentlyLoadedIndex)
                        {
                            buttonImage.color = loadedButtonColor;
                            loadButtons[i].transform.localScale = Vector3.one * loadedButtonScale;
                        }
                        else
                        {
                            buttonImage.color = originalButtonColor;
                            loadButtons[i].transform.localScale = Vector3.one;
                        }
                    }
                    
                    Debug.Log($"Slot {i} visível: {savedSessions[i].playerName} ({savedSessions[i].GetCommandCount()} comandos)");
                }
                else
                {
                    // Se não tem desenho, esconde o botão
                    loadButtons[i].gameObject.SetActive(false);
                    Debug.Log($"Slot {i} escondido: sem desenho");
                }
            }
        }
    }
    
    public void SaveCurrentSession(string sessionName = "")
    {
        if (commandRecorder == null)
        {
            Debug.LogError("CommandRecorder não encontrado!");
            return;
        }
        
        DrawingSession currentSession = commandRecorder.GetCurrentSession();
        if (currentSession == null || currentSession.GetCommandCount() == 0)
        {
            Debug.LogWarning("Nenhuma sessão ativa ou sem comandos para salvar");
            return;
        }
        
        // Gera nome automático se não fornecido
        if (string.IsNullOrEmpty(sessionName))
        {
            sessionName = $"Desenho {savedSessions.Count + 1}";
        }
        
        DrawingSession sessionToSave = CloneSession(currentSession, sessionName);
        savedSessions.Add(sessionToSave);
        
        if (savedSessions.Count > maxSavedDrawings)
        {
            savedSessions.RemoveAt(0);
        }
        
        Debug.Log($"Sessão '{sessionName}' salva com {sessionToSave.GetCommandCount()} comandos! Total: {savedSessions.Count}");
        
        UpdateButtonColors();
    }
    
    private DrawingSession CloneSession(DrawingSession original, string newName)
    {
        // Clona a sessão via serialização JSON
        string json = original.ToJson();
        DrawingSession clone = DrawingSession.FromJson(json);
        clone.playerName = newName;
        return clone;
    }
    
    public void LoadDrawing(int index)
    {
        if (index < 0 || index >= savedSessions.Count)
        {
            Debug.LogError($"Índice inválido: {index}");
            return;
        }
        
        if (replaySystem == null)
        {
            Debug.LogError("CommandReplaySystem não encontrado!");
            return;
        }
        
        DrawingSession session = savedSessions[index];
        
        Debug.Log($"=== CARREGANDO SLOT {index} ===");
        Debug.Log($"SessionId: {session.sessionId}");
        Debug.Log($"PlayerName: '{session.playerName}'");
        Debug.Log($"PlayerId: '{session.playerId}'");
        Debug.Log($"Comandos: {session.GetCommandCount()}");
        
        // Pausa a gravação durante o replay para não gravar comandos do replay
        if (commandRecorder != null)
        {
            commandRecorder.PauseRecording();
        }
        
        // Reproduz a sessão
        replaySystem.ReplayDrawingSession(session);
        
        // Retoma a gravação
        if (commandRecorder != null)
        {
            commandRecorder.ResumeRecording();
        }
        
        currentlyLoadedIndex = index;
        UpdateButtonColors();
        
        // Atualiza o texto com o nome do player do desenho selecionado
        string displayName = GetPlayerNameFromSession(session);
        UpdatePlayerNameDisplay(displayName);
        
        Debug.Log($"Sessão '{session.playerName}' carregada com {session.GetCommandCount()} comandos!");
    }
    
    public void DeleteDrawing(int index)
    {
        if (index < 0 || index >= savedSessions.Count)
        {
            Debug.LogError($"Índice inválido: {index}");
            return;
        }
        
        string sessionName = savedSessions[index].playerName;
        savedSessions.RemoveAt(index);
        
        // Reajusta o índice do desenho carregado
        if (currentlyLoadedIndex == index)
        {
            currentlyLoadedIndex = -1;
            UpdatePlayerNameDisplay(""); // Limpa o nome quando remove o desenho ativo
        }
        else if (currentlyLoadedIndex > index)
        {
            currentlyLoadedIndex--;
        }
        
        UpdateButtonColors();
        Debug.Log($"Sessão '{sessionName}' removida!");
    }
    
    public void ClearAllSavedSessions()
    {
        savedSessions.Clear();
        currentlyLoadedIndex = -1;
        UpdateButtonColors();
        UpdatePlayerNameDisplay(""); // Limpa o nome quando remove todos os desenhos
        Debug.Log("Todas as sessões salvas foram removidas!");
    }
    
    public string GetSessionInfo(int index)
    {
        if (index < 0 || index >= savedSessions.Count)
        {
            return "Índice inválido";
        }
        
        DrawingSession session = savedSessions[index];
        return $"{session.playerName} - {session.timestamp} ({session.GetCommandCount()} comandos)";
    }
    
    public bool HasSavedSessions()
    {
        return savedSessions.Count > 0;
    }
    
    public int GetSavedSessionsCount()
    {
        return savedSessions.Count;
    }
    
    public DrawingSession GetLatestSavedSession()
    {
        if (savedSessions.Count == 0) return null;
        return savedSessions[savedSessions.Count - 1];
    }
    
    public string GetSessionJson(int index)
    {
        if (index < 0 || index >= savedSessions.Count) return "";
        return savedSessions[index].ToJson();
    }
    
    public void LoadSessionFromJson(string json)
    {
        try
        {
            DrawingSession session = DrawingSession.FromJson(json);
            if (session != null && replaySystem != null)
            {
                if (commandRecorder != null) commandRecorder.PauseRecording();
                replaySystem.ReplayDrawingSession(session);
                if (commandRecorder != null) commandRecorder.ResumeRecording();
                
                Debug.Log($"Sessão carregada de JSON: {session.GetCommandCount()} comandos");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao carregar sessão de JSON: {e.Message}");
        }
    }
    
    public void RefreshSlotVisibility()
    {
        UpdateButtonColors();
        Debug.Log("Visibilidade dos slots atualizada manualmente");
    }
    
    private void UpdatePlayerNameDisplay(string playerName)
    {
        if (playerNameText != null)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                playerNameText.text = "";
                playerNameText.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                playerNameText.text = $"Rascunho de: {playerName}";
                playerNameText.transform.parent.gameObject.SetActive(true);
            }
        }
    }
    
    private string GetPlayerNameFromSession(DrawingSession session)
    {
        // Primeiro tenta pegar o nome do primeiro comando
        if (session.commands != null && session.commands.Count > 0)
        {
            var firstCommand = session.commands[0];
            if (!string.IsNullOrEmpty(firstCommand.playerName) && firstCommand.playerName != "Local Player")
            {
                Debug.Log($"Usando nome do comando: '{firstCommand.playerName}'");
                return firstCommand.playerName;
            }
        }
        
        // Fallback para o nome da sessão
        Debug.Log($"Usando nome da sessão: '{session.playerName}'");
        return session.playerName;
    }
    
    // Métodos públicos para UI (compatibilidade)
    public void SaveCurrentDrawingWithAutoName() => SaveCurrentSession();
    public void SaveCurrentDrawingWithName(string name) => SaveCurrentSession(name);
}
