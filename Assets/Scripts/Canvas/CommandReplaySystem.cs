using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CommandReplaySystem : MonoBehaviour
{
    [Header("Replay Settings")]
    public float replaySpeed = 0.1f; // Delay entre comandos durante replay
    public bool instantReplay = true; // Se true, executa todos de uma vez
    
    private ShapeDrawer shapeDrawer;
    private Transform shapeContainer;
    private TextureDrawingHandler textureHandler;
    
    void Start()
    {
        shapeDrawer = GetComponent<ShapeDrawer>();
        
        if (shapeDrawer == null)
        {
            Debug.LogError("CommandReplaySystem requer ShapeDrawer no mesmo GameObject!");
        }
        
        // Encontra o container de shapes
        GameObject containerObject = GameObject.Find("ShapesContainer");
        if (containerObject != null)
        {
            shapeContainer = containerObject.transform;
        }
        
        // Acessa o TextureDrawingHandler do ShapeDrawer
        if (shapeDrawer != null)
        {
            var textureHandlerField = typeof(ShapeDrawer).GetField("textureHandler", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (textureHandlerField != null)
            {
                textureHandler = textureHandlerField.GetValue(shapeDrawer) as TextureDrawingHandler;
            }
        }
    }
    
    public void ReplayDrawingSession(DrawingSession session)
    {
        if (session == null)
        {
            Debug.Log("Nenhuma sessão para reproduzir");
            return;
        }

        if (session.commands.Count == 0)
        {
            if (shapeDrawer != null)
            {
                shapeDrawer.ClearAll();
                UnityEngine.Debug.Log("Canvas limpo");
            }
            
            Debug.Log("Nenhum comando para reproduzir");
            return;
        }
        
        Debug.Log($"Reproduzindo desenho de {session.playerName}: {session.commands.Count} comandos");
        
        // REINICIALIZAÇÃO COMPLETA do sistema de texturas
        StartCoroutine(ReinitializeAndReplay(session));
    }
    
    private System.Collections.IEnumerator ReinitializeAndReplay(DrawingSession session)
    {
        UnityEngine.Debug.Log("=== REINICIALIZANDO SISTEMA DE TEXTURAS ===");
        
        // 1. Limpa tudo completamente
        if (shapeDrawer != null)
        {
            shapeDrawer.ClearAll();
            UnityEngine.Debug.Log("Canvas limpo");
        }
        
        // 2. Aguarda 1 frame para garantir limpeza
        yield return new WaitForEndOfFrame();
        
        // 3. Reinicializa handlers forçadamente
        ReinitializeHandlers();
        
        // 4. Aguarda mais 1 frame
        yield return new WaitForEndOfFrame();
        
        // 5. Garante conexão da textura
        EnsureTextureConnection();
        
        // 6. Aguarda mais 1 frame antes do replay
        yield return new WaitForEndOfFrame();
        
        UnityEngine.Debug.Log("Sistema reinicializado - iniciando replay");
        
        // 7. Agora executa o replay com sistema limpo
        if (instantReplay)
        {
            ReplayAllCommandsInstantly(session);
        }
        else
        {
            StartCoroutine(ReplayCommandsSequentially(session));
        }
    }
    
    private void ReinitializeHandlers()
    {
        UnityEngine.Debug.Log("Reinicializando handlers...");
        
        if (shapeDrawer != null)
        {
            // Força reinicialização do TextureHandler
            var initializeHandlersMethod = typeof(ShapeDrawer).GetMethod("InitializeHandlers", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (initializeHandlersMethod != null)
            {
                initializeHandlersMethod.Invoke(shapeDrawer, null);
                UnityEngine.Debug.Log("InitializeHandlers chamado");
            }
            
            // Reacessa o TextureDrawingHandler após reinicialização
            var textureHandlerField = typeof(ShapeDrawer).GetField("textureHandler", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (textureHandlerField != null)
            {
                textureHandler = textureHandlerField.GetValue(shapeDrawer) as TextureDrawingHandler;
                UnityEngine.Debug.Log($"TextureHandler reobtido: {textureHandler != null}");
                
                if (textureHandler != null && textureHandler.DrawingTexture != null)
                {
                    UnityEngine.Debug.Log($"Nova textura: {textureHandler.DrawingTexture.width}x{textureHandler.DrawingTexture.height}");
                }
            }
        }
    }
    
    private void ReplayAllCommandsInstantly(DrawingSession session)
    {
        // Ordena comandos por timestamp para garantir ordem correta
        var sortedCommands = session.commands.OrderBy(c => c.timestamp).ToList();
        
        foreach (DrawingCommand command in sortedCommands)
        {
            ExecuteCommand(command);
        }
        
        // Garante que a textura final seja visível após todos os comandos
        EnsureTextureConnection();
        
        Debug.Log($"Reproduzidos {sortedCommands.Count} comandos instantaneamente");
    }
    
    private IEnumerator ReplayCommandsSequentially(DrawingSession session)
    {
        var sortedCommands = session.commands.OrderBy(c => c.timestamp).ToList();
        
        foreach (DrawingCommand command in sortedCommands)
        {
            ExecuteCommand(command);
            yield return new WaitForSeconds(replaySpeed);
        }
        
        Debug.Log($"Reprodução sequencial concluída: {sortedCommands.Count} comandos");
    }
    
    public void ReplayCommand(DrawingCommand command)
    {
        if (command == null) return;
        ExecuteCommand(command);
    }

    private void ExecuteCommand(DrawingCommand command)
    {
        // Se é um comando de flood fill, executa diferente
        if (command.isFloodFill)
        {
            ExecuteFloodFillCommand(command);
            return;
        }
        
        if (shapeContainer == null)
        {
            Debug.LogError("ShapeContainer não encontrado!");
            return;
        }
        
        // Cria configuração de desenho
        DrawingConfig config = new DrawingConfig(command.color, command.lineThickness);
        
        // Cria um GameObject temporário para usar como preview
        GameObject tempPreview = new GameObject("TempPreview");
        RectTransform tempRect = tempPreview.AddComponent<RectTransform>();
        tempRect.SetParent(shapeContainer, false);
        
        tempRect.anchoredPosition = command.position;
        tempRect.sizeDelta = command.size;
        
        // Lines precisam de pivot especial
        if (command.shapeType == ShapeType.Line)
        {
            tempRect.pivot = new Vector2(0, 0.5f); // Left-center para lines
            UnityEngine.Debug.Log($"Line: aplicado pivot especial (0, 0.5) - posição {command.position}, tamanho {command.size}");
        }
        else
        {
            tempRect.pivot = new Vector2(0.5f, 0.5f); // Center-center para outras formas
        }
        
        // Cria o shape usando o factory estático
        GameObject shape = ShapeFactory.CreatePermanentShape(
            tempRect,
            command.shapeType,
            config,
            shapeContainer
        );
        
        // Remove o preview temporário
        DestroyImmediate(tempPreview);
        
        if (shape != null)
        {
            // Aplica rotação final
            RectTransform rectTransform = shape.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localRotation = Quaternion.Euler(0, 0, command.rotation);
                
                // IMPORTANTE: Para Lines, verifica se pivot está correto após criação
                if (command.shapeType == ShapeType.Line)
                {
                    UnityEngine.Debug.Log($"Line após criação - pivot: {rectTransform.pivot}, posição: {rectTransform.anchoredPosition}, rotação: {command.rotation}");
                }
                
                // IMPORTANTE: Desenha na textura também para o FloodFill funcionar
                if (textureHandler != null)
                {
                    DrawShapeOnTexture(rectTransform, command.shapeType, config);
                }
            }
            
            // Força a aplicação da cor correta
            ApplyColorToShape(shape, command.color);
            
            Debug.Log($"Shape {command.shapeType} recriado na posição {command.position} com cor {command.color}");
        }
        else
        {
            Debug.LogError($"Falha ao criar shape: {command.shapeType}");
        }
    }
    
    // Método removido - usando coordenadas diretas agora
    
    private void DrawShapeOnTexture(RectTransform rectTransform, ShapeType shapeType, DrawingConfig config)
    {
        if (textureHandler == null)
        {
            UnityEngine.Debug.LogError("TextureHandler é null - não pode desenhar na textura!");
            return;
        }
        
        UnityEngine.Debug.Log($"Desenhando {shapeType} na textura - posição: {rectTransform.anchoredPosition}, tamanho: {rectTransform.sizeDelta}");
        
        // Desenha a forma na textura usando o mesmo método que o ShapeDrawer original
        switch (shapeType)
        {
            case ShapeType.Rectangle:
                textureHandler.DrawRectangleOnTexture(rectTransform, config);
                UnityEngine.Debug.Log("DrawRectangleOnTexture executado");
                break;
            case ShapeType.Circle:
                textureHandler.DrawCircleOnTexture(rectTransform, config);
                UnityEngine.Debug.Log("DrawCircleOnTexture executado");
                break;
            case ShapeType.Ellipse:
                textureHandler.DrawEllipseOnTexture(rectTransform, config);
                UnityEngine.Debug.Log("DrawEllipseOnTexture executado");
                break;
            case ShapeType.Line:
                textureHandler.DrawLineOnTexture(rectTransform, config);
                UnityEngine.Debug.Log("DrawLineOnTexture executado");
                break;
        }
        
        // Força aplicação imediata
        if (textureHandler.DrawingTexture != null)
        {
            textureHandler.DrawingTexture.Apply();
            UnityEngine.Debug.Log($"Shape {shapeType} aplicado na textura");
        }
    }
    
    private void EnsureTextureConnection()
    {
        if (shapeDrawer != null && textureHandler != null)
        {
            // Conecta a textura ao RawImage para ficar visível
            var drawingBoardField = typeof(ShapeDrawer).GetField("drawingBoard", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (drawingBoardField != null)
            {
                var rawImage = drawingBoardField.GetValue(shapeDrawer) as UnityEngine.UI.RawImage;
                if (rawImage != null && textureHandler.DrawingTexture != null)
                {
                    // Força Apply() na textura antes de conectar
                    textureHandler.DrawingTexture.Apply();
                    rawImage.texture = textureHandler.DrawingTexture;
                    
                    Debug.Log($"Textura reconectada ao RawImage (tamanho: {textureHandler.DrawingTexture.width}x{textureHandler.DrawingTexture.height})");
                }
                else
                {
                    Debug.LogError($"RawImage ou DrawingTexture null: RawImage={rawImage != null}, Texture={textureHandler.DrawingTexture != null}");
                }
            }
            else
            {
                Debug.LogError("Campo drawingBoard não encontrado no ShapeDrawer");
            }
        }
        else
        {
            Debug.LogError($"ShapeDrawer ou TextureHandler null: ShapeDrawer={shapeDrawer != null}, TextureHandler={textureHandler != null}");
        }
    }
    
    private void ExecuteFloodFillCommand(DrawingCommand command)
    {
        UnityEngine.Debug.Log($"=== EXECUTANDO FLOOD FILL ===");
        UnityEngine.Debug.Log($"Usando EXATAMENTE o mesmo método PerformFloodFill do sistema original");
        
        if (shapeDrawer != null)
        {
            // Chama o método PerformFloodFill original via reflection para garantir 100% de compatibilidade
            var performFloodFillMethod = typeof(ShapeDrawer).GetMethod("PerformFloodFill", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (performFloodFillMethod != null)
            {
                UnityEngine.Debug.Log($"Chamando PerformFloodFill original com posição {command.floodFillPosition} e cor {command.floodFillColor}");
                
                performFloodFillMethod.Invoke(shapeDrawer, new object[] { command.floodFillPosition, command.floodFillColor });
                
                UnityEngine.Debug.Log("PerformFloodFill original executado");
                
                // Garante que a textura modificada seja visível
                EnsureTextureConnection();
            }
            else
            {
                UnityEngine.Debug.LogError("Método PerformFloodFill não encontrado no ShapeDrawer!");
            }
        }
        else
        {
            UnityEngine.Debug.LogError("ShapeDrawer não encontrado!");
        }
    }
    
    private Vector2 ConvertUIToTextureCoordinates(Vector2 uiPos, Texture2D texture)
    {
        if (shapeDrawer != null)
        {
            var drawingAreaField = typeof(ShapeDrawer).GetField("drawingArea", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (drawingAreaField != null)
            {
                var drawingArea = drawingAreaField.GetValue(shapeDrawer) as RectTransform;
                if (drawingArea != null)
                {
                    Vector2 areaSize = drawingArea.rect.size;
                    
                    // USA EXATAMENTE A MESMA FÓRMULA DO SISTEMA ORIGINAL
                    float textureX = (uiPos.x + areaSize.x / 2) * (texture.width / areaSize.x);
                    float textureY = (uiPos.y + areaSize.y / 2) * (texture.height / areaSize.y); // SEM inversão!
                    
                    UnityEngine.Debug.Log($"UI {uiPos} → Textura ({textureX}, {textureY}), Área {areaSize}, Textura {texture.width}x{texture.height}");
                    
                    return new Vector2(textureX, textureY);
                }
            }
        }
        
        return Vector2.zero;
    }
    
    private void ApplyColorToShape(GameObject shape, Color color)
    {
        // Aplica cor a todos os componentes Image filhos (contornos)
        UnityEngine.UI.Image[] images = shape.GetComponentsInChildren<UnityEngine.UI.Image>();
        foreach (var image in images)
        {
            image.color = color;
        }
        
        // Atualiza também o ShapeData
        ShapeData shapeData = shape.GetComponent<ShapeData>();
        if (shapeData != null)
        {
            shapeData.config.shapeColor = color;
        }
    }
    
    public void ReplayCommands(List<DrawingCommand> commands)
    {
        DrawingSession tempSession = new DrawingSession();
        tempSession.commands = commands;
        ReplayDrawingSession(tempSession);
    }
    
    public void SetReplaySpeed(float speed)
    {
        replaySpeed = speed;
    }
    
    public void SetInstantReplay(bool instant)
    {
        instantReplay = instant;
    }
}
