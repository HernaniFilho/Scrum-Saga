using UnityEngine;
using UnityEngine.UI;

public class UndoSystem : MonoBehaviour
{
    [Header("Button Settings")]
    public Vector2 buttonSize = new Vector2(60, 60);
    public Vector2 buttonPosition = new Vector2(-120, -10);
    public string buttonText = "⬅";
    
    [Header("Visual Settings")]
    public bool createUI = true;
    public Color buttonColor = new Color(0.9607844f, 0.9607844f, 0.9607844f);
    public Color textColor = Color.black;
    public int fontSize = 42;
    
    // Componentes internos
    private ShapeDrawer shapeDrawer;
    private RawImage drawingBoard;
    private Button undoButton;
    private GameObject buttonGameObject;
    private Canvas buttonCanvas;
    
    // Estado do undo
    private Texture2D lastTexture;
    private GameObject lastShape;
    private bool hasActionToUndo = false;
    
    void Start()
    {
        // Encontra componentes necessários
        shapeDrawer = GetComponent<ShapeDrawer>();
        if (shapeDrawer == null)
        {
            Debug.LogError("UndoSystem deve ser adicionado ao mesmo GameObject que possui ShapeDrawer!");
            return;
        }
        
        drawingBoard = FindObjectOfType<RawImage>();

        if (createUI)
        {
            CreateUndoButton();
        }
    }
    
    private void CreateUndoButton()
    {
        try
        {
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogError("Canvas principal não encontrado!");
                return;
            }
            
            // Cria Canvas para o botão
            GameObject canvasGO = new GameObject("UndoButtonCanvas");
            canvasGO.transform.SetParent(mainCanvas.transform, false);
            
            buttonCanvas = canvasGO.AddComponent<Canvas>();
            buttonCanvas.overrideSorting = true;
            buttonCanvas.sortingOrder = 200;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            
            // Cria o botão
            buttonGameObject = new GameObject("UndoButton");
            buttonGameObject.transform.SetParent(canvasGO.transform, false);
            
            // Adiciona Image
            Image buttonImage = buttonGameObject.AddComponent<Image>();
            buttonImage.color = buttonColor;
            
            // Adiciona outline
            Outline outline = buttonGameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, 2f);
            
            // Adiciona Button
            undoButton = buttonGameObject.AddComponent<Button>();
            undoButton.targetGraphic = buttonImage;
            
            // Configura posição e tamanho
            RectTransform buttonRT = buttonGameObject.GetComponent<RectTransform>();
            buttonRT.sizeDelta = buttonSize;
            buttonRT.anchorMin = new Vector2(1, 1); // Canto superior direito
            buttonRT.anchorMax = new Vector2(1, 1);
            buttonRT.pivot = new Vector2(1, 1);
            buttonRT.anchoredPosition = buttonPosition;
            
            CreateButtonText();
            
            undoButton.onClick.AddListener(UndoLastAction);
            
            buttonGameObject.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao criar botão de undo: {e.Message}");
        }
    }
    
    private void CreateButtonText()
    {
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGameObject.transform, false);
        
        Text buttonTextComponent = textGO.AddComponent<Text>();
        buttonTextComponent.text = buttonText;
        buttonTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonTextComponent.fontSize = fontSize;
        buttonTextComponent.color = textColor;
        buttonTextComponent.alignment = TextAnchor.MiddleCenter;
        buttonTextComponent.fontStyle = FontStyle.Bold;
        
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }
    
    public void SaveStateBeforeAction()
    {
        if (shapeDrawer == null) return;
        
        OnNewActionStarted();
        
        // Salva o estado ANTES de fazer uma ação
        Texture2D currentTexture = shapeDrawer.GetDrawingTexture();
        if (currentTexture != null)
        {
            if (lastTexture != null)
                DestroyImmediate(lastTexture);
                
            lastTexture = new Texture2D(currentTexture.width, currentTexture.height, currentTexture.format, false);
            lastTexture.SetPixels(currentTexture.GetPixels());
            lastTexture.Apply();
        }
    }
    
    public void OnShapeCreated(GameObject shape)
    {
        lastShape = shape;
        hasActionToUndo = true;
        ShowUndoButton();
    }
    
    public void OnFloodFillPerformed()
    {
        hasActionToUndo = true;
        ShowUndoButton();
    }
    
    public void OnNewActionStarted()
    {
        if (hasActionToUndo)
        {
            // Limpa o estado anterior quando uma nova ação começa
            if (lastTexture != null)
            {
                DestroyImmediate(lastTexture);
                lastTexture = null;
            }
            lastShape = null;
        }
    }
    
    public void UndoLastAction()
    {
        if (!hasActionToUndo || shapeDrawer == null) return;
        
        // Remove a última forma criada se existir
        if (lastShape != null)
        {
            DestroyImmediate(lastShape);
            lastShape = null;
        }
        
        // Restaura a textura anterior

        if (drawingBoard == null)
            drawingBoard = FindObjectOfType<RawImage>();

        if (lastTexture != null && drawingBoard != null)
            {
                Texture2D currentTexture = shapeDrawer.GetDrawingTexture();
                if (currentTexture != null)
                {
                    currentTexture.SetPixels(lastTexture.GetPixels());
                    currentTexture.Apply();
                }
            }
        
        Debug.Log("foi");
        hasActionToUndo = false;
        HideUndoButton();
    }
    
    private void ShowUndoButton()
    {
        if (buttonGameObject != null)
            buttonGameObject.SetActive(true);
    }
    
    private void HideUndoButton()
    {
        if (buttonGameObject != null)
            buttonGameObject.SetActive(false);
    }
    
    public void ClearUndoHistory()
    {
        if (lastTexture != null)
        {
            DestroyImmediate(lastTexture);
            lastTexture = null;
        }
        lastShape = null;
        hasActionToUndo = false;
        HideUndoButton();
    }
    
    void OnDestroy()
    {
        if (lastTexture != null)
            DestroyImmediate(lastTexture);
            
        if (buttonCanvas != null)
            DestroyImmediate(buttonCanvas.gameObject);
    }
}
