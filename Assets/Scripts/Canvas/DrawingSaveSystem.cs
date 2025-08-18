using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Photon.Pun;
using ExitGames.Client.Photon;

[System.Serializable]
public class SavedShape
{
    public ShapeType shapeType;
    public Vector2 position;
    public Vector2 size;
    public float rotation;
    public Color color;
    public float lineThickness;
    
    public SavedShape(ShapeData shapeData, RectTransform rectTransform)
    {
        shapeType = shapeData.shapeType;
        position = rectTransform.anchoredPosition;
        size = rectTransform.sizeDelta;
        rotation = rectTransform.localRotation.eulerAngles.z;
        color = shapeData.config.shapeColor;
        lineThickness = shapeData.config.lineThickness;
    }
}

[System.Serializable]
public class SavedDrawing
{
    public string name;
    public string timestamp;
    public byte[] textureData;
    public int width;
    public int height;
    public SavedShape[] shapes;
    
    public SavedDrawing(string drawingName, Texture2D texture, SavedShape[] savedShapes)
    {
        name = drawingName;
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        textureData = texture.EncodeToPNG();
        width = texture.width;
        height = texture.height;
        shapes = savedShapes;
    }
    
    public Texture2D ToTexture()
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.LoadImage(textureData);
        texture.filterMode = FilterMode.Point;
        return texture;
    }
}

public class DrawingSaveSystem : MonoBehaviourPunCallbacks
{
    [Header("Save Settings")]
    [SerializeField] private int maxSavedDrawings = 4;
    
    private List<SavedDrawing> savedDrawings = new List<SavedDrawing>();
    private ShapeDrawer shapeDrawer;
    
    [Header("Network Sync")]
    private SavedDrawing[] allPlayerDrawings = new SavedDrawing[4];
    private int drawingsSyncCount = 0;
    private const string DRAWING_DATA_KEY = "PlayerDrawingData";
    
    public List<SavedDrawing> SavedDrawings => savedDrawings;
    public int MaxSavedDrawings => maxSavedDrawings;
    public SavedDrawing[] AllPlayerDrawings => allPlayerDrawings;
    
    void Start()
    {
        shapeDrawer = GetComponent<ShapeDrawer>();
        if (shapeDrawer == null)
        {
            Debug.LogError("DrawingSaveSystem precisa estar no mesmo GameObject que ShapeDrawer!");
        }
    }
    
    public void SaveCurrentDrawing(string drawingName = "")
    {
        if (shapeDrawer == null)
        {
            Debug.LogError("ShapeDrawer não encontrado!");
            return;
        }
        
        // Força a renderização de todas as formas na textura antes de salvar
        Debug.Log("Iniciando processo de salvamento...");
        shapeDrawer.RenderAllShapesToTexture();
        Debug.Log("Renderização concluída, coletando dados das formas...");
        
        // Coleta dados de todas as formas
        SavedShape[] shapesData = shapeDrawer.GetAllShapesData();
        
        Texture2D currentTexture = shapeDrawer.GetDrawingTexture();
        if (currentTexture == null)
        {
            Debug.LogError("Textura de desenho não encontrada!");
            return;
        }
        
        // Gera nome automático se não fornecido
        if (string.IsNullOrEmpty(drawingName))
        {
            drawingName = $"Desenho {savedDrawings.Count + 1}";
        }
        
        SavedDrawing newDrawing = new SavedDrawing(drawingName, currentTexture, shapesData);
        savedDrawings.Add(newDrawing);
        
        // Remove o desenho mais antigo se exceder o limite
        if (savedDrawings.Count > maxSavedDrawings)
        {
            savedDrawings.RemoveAt(0);
        }
        
        Debug.Log($"Desenho '{drawingName}' salvo! Total: {savedDrawings.Count}");
    }
    
    /// <summary>
    /// Carrega um desenho salvo no canvas
    /// </summary>
    public void LoadDrawing(int index)
    {
        if (index < 0 || index >= savedDrawings.Count)
        {
            Debug.LogError($"Índice inválido: {index}");
            return;
        }
        
        if (shapeDrawer == null) return;
        
        SavedDrawing drawing = savedDrawings[index];
        Texture2D loadedTexture = drawing.ToTexture();
        
        // Limpa o canvas atual (remove formas visuais)
        shapeDrawer.ClearAll();
        
        // Copia os pixels da textura carregada para a textura atual
        Texture2D currentTexture = shapeDrawer.GetDrawingTexture();
        Color[] loadedPixels = loadedTexture.GetPixels();
        currentTexture.SetPixels(loadedPixels);
        currentTexture.Apply();
        
        // Limpa a textura temporária
        DestroyImmediate(loadedTexture);
        
        // Restaura as formas visuais
        if (drawing.shapes != null && drawing.shapes.Length > 0)
        {
            shapeDrawer.RestoreShapesFromData(drawing.shapes);
        }
        
        Debug.Log($"Desenho '{drawing.name}' carregado com {drawing.shapes?.Length ?? 0} formas!");
    }
    
    /// <summary>
    /// Remove um desenho salvo
    /// </summary>
    public void DeleteDrawing(int index)
    {
        if (index < 0 || index >= savedDrawings.Count)
        {
            Debug.LogError($"Índice inválido: {index}");
            return;
        }
        
        string drawingName = savedDrawings[index].name;
        savedDrawings.RemoveAt(index);
        Debug.Log($"Desenho '{drawingName}' removido!");
    }
    
    /// <summary>
    /// Limpa todos os desenhos salvos
    /// </summary>
    public void ClearAllSavedDrawings()
    {
        savedDrawings.Clear();
        Debug.Log("Todos os desenhos salvos foram removidos!");
    }
    
    /// <summary>
    /// Obtém informações sobre um desenho salvo
    /// </summary>
    public string GetDrawingInfo(int index)
    {
        if (index < 0 || index >= savedDrawings.Count)
        {
            return "Índice inválido";
        }
        
        SavedDrawing drawing = savedDrawings[index];
        return $"{drawing.name} - {drawing.timestamp}";
    }
    
    /// <summary>
    /// Verifica se há desenhos salvos
    /// </summary>
    public bool HasSavedDrawings()
    {
        return savedDrawings.Count > 0;
    }
    
    /// <summary>
    /// Obtém o número de desenhos salvos
    /// </summary>
    public int GetSavedDrawingsCount()
    {
        return savedDrawings.Count;
    }
    
    /// <summary>
    /// Salva e sincroniza desenhos de todos os players via rede
    /// </summary>
    public void SaveAndSyncAllPlayerDrawings()
    {
        // Salva o desenho atual do player local
        SaveCurrentPlayerDrawing();
    }
    
    private void SaveCurrentPlayerDrawing()
    {
        // Força o salvamento do desenho atual
        string playerDrawingName = $"Player_{PhotonNetwork.LocalPlayer.UserId}_Drawing";
        SaveCurrentDrawing(playerDrawingName);
        
        // Pega o desenho mais recente salvo
        if (HasSavedDrawings())
        {
            SavedDrawing latestDrawing = savedDrawings[savedDrawings.Count - 1];
            
            // Envia o desenho via Photon custom properties
            SendDrawingToAllPlayers(latestDrawing);
            Debug.Log($"Desenho do jogador {PhotonNetwork.LocalPlayer.UserId} salvo e enviado!");
        }
    }
    
    private void SendDrawingToAllPlayers(SavedDrawing drawing)
    {
        if (PhotonNetwork.InRoom && drawing != null)
        {
            // Serializa o desenho em uma string JSON
            string drawingJson = JsonUtility.ToJson(drawing);
            
            // Cria um identificador único baseado no player ID
            string playerDrawingKey = DRAWING_DATA_KEY + "_" + PhotonNetwork.LocalPlayer.UserId;
            
            Hashtable props = new Hashtable();
            props[playerDrawingKey] = drawingJson;
            
            // Envia via room custom properties para sincronização
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            
            Debug.Log($"Desenho enviado para todos os players com chave: {playerDrawingKey}");
        }
    }
    
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        foreach (var prop in propertiesThatChanged)
        {
            string key = prop.Key.ToString();
            
            // Verifica se é um desenho de player
            if (key.StartsWith(DRAWING_DATA_KEY))
            {
                string playerId = key.Replace(DRAWING_DATA_KEY + "_", "");
                string drawingJson = prop.Value.ToString();
                
                try
                {
                    SavedDrawing receivedDrawing = JsonUtility.FromJson<SavedDrawing>(drawingJson);
                    
                    // Determina o índice do array baseado na ordem dos players
                    int playerIndex = GetPlayerIndex(playerId);
                    if (playerIndex >= 0 && playerIndex < allPlayerDrawings.Length)
                    {
                        allPlayerDrawings[playerIndex] = receivedDrawing;
                        drawingsSyncCount++;
                        
                        Debug.Log($"Desenho do player {playerId} recebido e armazenado no índice {playerIndex}");
                        
                        // Atualiza a lista local de desenhos salvos
                        UpdateLocalSavedDrawings();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Erro ao deserializar desenho do player {playerId}: {e.Message}");
                }
            }
        }
    }
    
    private int GetPlayerIndex(string playerId)
    {
        if (PhotonNetwork.InRoom)
        {
            var players = PhotonNetwork.PlayerList;
            for (int i = 0; i < players.Length && i < 4; i++)
            {
                if (players[i].UserId == playerId)
                {
                    return i;
                }
            }
        }
        return -1;
    }
    
    private void UpdateLocalSavedDrawings()
    {
        // Limpa os desenhos atuais exceto o último (que é o do player atual)
        var currentPlayerDrawing = savedDrawings.LastOrDefault();
        savedDrawings.Clear();
        
        // Adiciona todos os desenhos recebidos dos players
        foreach (SavedDrawing drawing in allPlayerDrawings)
        {
            if (drawing != null)
            {
                savedDrawings.Add(drawing);
            }
        }
        
        Debug.Log($"Sistema local atualizado com {savedDrawings.Count} desenhos de todos os players");
    }
    
    // Métodos públicos para UI
    public void SaveCurrentDrawingWithAutoName() => SaveCurrentDrawing();
    public void SaveCurrentDrawingWithName(string name) => SaveCurrentDrawing(name);
}
