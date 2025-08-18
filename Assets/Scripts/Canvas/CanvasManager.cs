using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class CanvasManager : MonoBehaviourPunCallbacks
{
    [Header("Game Board")]
    [SerializeField] private GameObject tabuleiro;
    
    [Header("Canvas Components")]
    [SerializeField] private GameObject canvasContainer;
    [SerializeField] private GameObject canvasToolbar;
    
    private const string CANVAS_STATE_KEY = "CanvasActive";
    private const string TOOLBAR_STATE_KEY = "ToolbarActive";
    
    public static CanvasManager Instance { get; private set; }

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
        if (tabuleiro == null)
            tabuleiro = GameObject.Find("Tabuleiro");
        if (canvasContainer == null)
            canvasContainer = GameObject.Find("Canvas Container");

        if (canvasContainer != null && canvasToolbar == null)
        {
            canvasToolbar = FindChildByName(canvasContainer.transform, "Toolbar")?.gameObject;
        }

        LoadStatesFromRoom();
    }

    public void ActivateCanvas()
    {
        ApplyCanvasState(true);
    }
    
    public void ActivateCanvasForAll()
    {
        BroadcastCanvasState(true);
    }
    
    public void ActivateCanvasForOthers()
    {
        BroadcastCanvasStateExcludingSelf(true);
    }
    
    public void BroadcastCanvasState(bool isActive)
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[CANVAS_STATE_KEY] = isActive;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"Estado do canvas {(isActive ? "ativado" : "desativado")} enviado para todos os jogadores");
        }
        else
        {
            ApplyCanvasState(isActive);
        }
    }
    
    public void BroadcastCanvasStateExcludingSelf(bool isActive)
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[CANVAS_STATE_KEY] = isActive;
            props[CANVAS_STATE_KEY + "_exclude"] = PhotonNetwork.LocalPlayer.UserId;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"Estado do canvas {(isActive ? "ativado" : "desativado")} enviado para outros jogadores");
        }
    }
    
    private void ApplyCanvasState(bool isActive)
    {
        if (isActive)
        {
            if (tabuleiro != null)
            {
                tabuleiro.SetActive(false);
            }

            if (canvasContainer != null)
            {
                canvasContainer.SetActive(true);
            }
            
            Debug.Log("Canvas ativado!");
        }
        else
        {
            if (tabuleiro != null)
            {
                tabuleiro.SetActive(true);
            }
            
            if (canvasContainer != null)
            {
                canvasContainer.SetActive(false);
            }
            
            Debug.Log("Canvas desativado!");
        }
    }

    public void DeactivateCanvas()
    {
        ApplyCanvasState(false);
    }
    
    public void DeactivateCanvasForAll()
    {
        BroadcastCanvasState(false);
    }

    public void ActivateToolbar()
    {
        ApplyToolbarState(true);
    }
    
    public void ActivateToolbarForAll()
    {
        BroadcastToolbarState(true);
    }

    public void DeactivateToolbar()
    {
        ApplyToolbarState(false);
    }
    
    public void DeactivateToolbarForAll()
    {
        BroadcastToolbarState(false);
    }
    
    public void BroadcastToolbarState(bool isActive)
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[TOOLBAR_STATE_KEY] = isActive;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"Estado da toolbar {(isActive ? "ativada" : "desativada")} enviado para todos os jogadores");
        }
        else
        {
            ApplyToolbarState(isActive);
        }
    }
    
    private void ApplyToolbarState(bool isActive)
    {
        if (canvasToolbar != null)
        {
            canvasToolbar.SetActive(isActive);
        }
        
        Debug.Log($"Canvas Toolbar {(isActive ? "ativada" : "desativada")}!");
    }
    
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(CANVAS_STATE_KEY))
        {
            bool isActive = (bool)propertiesThatChanged[CANVAS_STATE_KEY];
            
            // Verifica se a propriedade tem exclusão (para o Product Owner)
            string excludePlayerKey = CANVAS_STATE_KEY + "_exclude";
            if (propertiesThatChanged.ContainsKey(excludePlayerKey))
            {
                string excludedPlayerId = (string)propertiesThatChanged[excludePlayerKey];
                if (PhotonNetwork.LocalPlayer.UserId == excludedPlayerId)
                {
                    // Este jogador deve ser excluído, então não aplica o estado
                    return;
                }
            }
            
            ApplyCanvasState(isActive);
        }
        
        if (propertiesThatChanged.ContainsKey(TOOLBAR_STATE_KEY))
        {
            bool isActive = (bool)propertiesThatChanged[TOOLBAR_STATE_KEY];
            ApplyToolbarState(isActive);
        }
    }
    
    private void LoadStatesFromRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CANVAS_STATE_KEY))
            {
                bool currentCanvasState = (bool)PhotonNetwork.CurrentRoom.CustomProperties[CANVAS_STATE_KEY];
                ApplyCanvasState(currentCanvasState);
                Debug.Log($"Estado do canvas carregado da sala: {(currentCanvasState ? "ativado" : "desativado")}");
            }
            
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(TOOLBAR_STATE_KEY))
            {
                bool currentToolbarState = (bool)PhotonNetwork.CurrentRoom.CustomProperties[TOOLBAR_STATE_KEY];
                ApplyToolbarState(currentToolbarState);
                Debug.Log($"Estado da toolbar carregado da sala: {(currentToolbarState ? "ativada" : "desativada")}");
            }
        }
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("Entrou na sala, carregando estados atuais...");
        LoadStatesFromRoom();
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
