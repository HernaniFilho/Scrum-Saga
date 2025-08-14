using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script de setup automático para o sistema de Product Owner
/// Adicione este script a um GameObject na cena para configurar automaticamente o sistema
/// Segue o mesmo padrão do MultiplayerSetup
/// </summary>
public class ProductOwnerSetup : MonoBehaviour
{
    [Header("Product Owner Settings")]
    [SerializeField] private bool enableProductOwner = true;
    
    [Header("Configuração Automática")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    [Header("UI Elements to Create")]
    [SerializeField] private bool createProductOwnerUI = true;
    
    [Header("UI References (Optional - se não atribuído, será criado automaticamente)")]
    [SerializeField] private Button becomeProductOwnerButton;
    [SerializeField] private TextMeshProUGUI productOwnerStatusText;
    
    private Canvas uiCanvas;
    private ProductOwnerManager productOwnerManager;
    private NetworkManager networkManager;
    
    void Start()
    {
        if (autoSetupOnStart && enableProductOwner)
        {
            SetupProductOwnerSystem();
        }

        if (!enableProductOwner)
        {
            if (becomeProductOwnerButton != null)
            {
                becomeProductOwnerButton.gameObject.SetActive(false);
            }
        }
    }
    
    [ContextMenu("Setup Product Owner System")]
    public void SetupProductOwnerSystem()
    {        
        Debug.Log("Configurando Sistema de Product Owner...");
        
        SetupCanvas();
        
        SetupProductOwnerManager();
        
        if (createProductOwnerUI) CreateProductOwnerUI();
        
        Debug.Log("Configuração de Product Owner concluída!");
    }
    
    void SetupCanvas()
    {
        uiCanvas = FindObjectOfType<Canvas>();
        if (uiCanvas == null)
        {
            GameObject canvasObj = new GameObject("UI Canvas");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Garantir que existe EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            Debug.Log("Canvas criado para Product Owner");
        }
    }
    
    void SetupProductOwnerManager()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        if (productOwnerManager == null)
        {
            GameObject poManagerObj = new GameObject("ProductOwnerManager");
            productOwnerManager = poManagerObj.AddComponent<ProductOwnerManager>();
            Debug.Log("ProductOwnerManager criado");
        }
        else
        {
            Debug.Log("ProductOwnerManager já existe");
        }
        
        // Encontrar NetworkManager existente
        networkManager = FindObjectOfType<NetworkManager>();
    }
    
    void CreateProductOwnerUI()
    {
        // Criar painel do Product Owner se não existir
        if (becomeProductOwnerButton == null)
        {
            GameObject panelObj = new GameObject("ProductOwnerPanel");
            panelObj.transform.SetParent(uiCanvas.transform, false);
            
            // Configurar painel
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(160, -300);
            panelRect.sizeDelta = new Vector2(300, 80);
            
            // Adicionar imagem de fundo
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Layout vertical
            VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            // Criar o botão
            GameObject buttonObj = new GameObject("BecomeProductOwnerButton");
            buttonObj.transform.SetParent(panelObj.transform, false);
            
            becomeProductOwnerButton = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.6f, 0.9f, 1f);
            
            // Criar texto do botão
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            productOwnerStatusText = textObj.AddComponent<TextMeshProUGUI>();
            productOwnerStatusText.text = "Tornar-se Product Owner";
            productOwnerStatusText.fontSize = 14;
            productOwnerStatusText.color = Color.white;
            productOwnerStatusText.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            // Configurar altura do botão
            LayoutElement buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.preferredHeight = 30;
            
            Debug.Log("Product Owner UI criada automaticamente");
        }
        else
        {
            Debug.Log("Product Owner Button já atribuído no editor");
        }
        
        // Conectar ao ProductOwnerManager
        if (productOwnerManager != null)
        {
            productOwnerManager.becomeProductOwnerButton = becomeProductOwnerButton;
            productOwnerManager.buttonText = productOwnerStatusText;
        }
        
        Debug.Log("Product Owner UI configurada");
    }
    
    [ContextMenu("Remove Product Owner UI")]
    public void RemoveProductOwnerUI()
    {
        if (becomeProductOwnerButton != null)
        {
            DestroyImmediate(becomeProductOwnerButton.transform.parent.gameObject);
        }
    }
}
