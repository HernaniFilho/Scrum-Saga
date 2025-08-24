using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CardImprevistos : MonoBehaviour
{
    [Header("Caminho dos textos usados na descrição")]
    public string textFolder = "Texts/Imprevistos";
    [Header("Texto que será atualizado com o texto aleatório")]
    public TMP_Text textUI;

    [Header("Caminho das imagens de natureza usadas no card")]
    public string imageFolder = "Images/Naturezas";
    [Header("Caminho do material branco usado para preencher espaços vazios")]
    public string whiteMaterialPath = "Materials/White";
    public MeshRenderer[] meshRenderers;
    [Header("Debuff máxima e minima para atribuição aleatória, ATENÇÃO: Os valores serão usados como negativos no codigo")]
    public int maxDebuff = 2;
    public int minDebuff = 1;

    [Header("Botoes com os handlers para as naturezas")]
    private Dictionary<string, int> natures = new Dictionary<string, int>();
    public Button natureButton_1;
    public Button natureButton_2;

    [Header("Textos com os debuffs")]
    private Dictionary<string, int> debuffs = new Dictionary<string, int>();
    public TMP_Text debuffText_1;
    public TMP_Text debuffText_2;
    public TMP_Text debuffText_3;
    private bool useFirstOnly = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomText();
        randomImages();
        loadScoreVariables();
        if (natureButton_1 == null || natureButton_2 == null)
        {
            Debug.LogWarning("Botões de pontuação não atribuídos. Verifique se estão configurados no Inspector.");
            return;
        }

        if (debuffText_1 == null || debuffText_2 == null || debuffText_3 == null)
        {
            Debug.LogWarning("Textos de debuff não atribuídos. Verifique se estão configurados no Inspector.");
            return;
        }

        int i = 0;
        if (useFirstOnly)
        {
            string debuff1 = debuffs.Keys.ElementAt(0);
            debuffText_3.text = debuffs[debuff1] + " " + debuff1;
            debuffText_1.gameObject.SetActive(false);
            debuffText_2.gameObject.SetActive(false);
        }
        else
        {
            string debuff1 = debuffs.Keys.ElementAt(0);
            string debuff2 = debuffs.Keys.ElementAt(1);
            debuffText_1.text = debuffs[debuff1] + " " + debuff1;
            debuffText_2.text = debuffs[debuff2] + " " + debuff2;
            debuffText_3.gameObject.SetActive(false);
        }

        if (natures == null)
        {
            Debug.LogWarning("Naturezas não atribuídas. Verifique se as pontuações foram carregadas corretamente.");
            return;
        }

        if (natures.Count > 0)
        {
            setupNatureButton(natureButton_1, natures.Keys.ElementAt(0));
        }
        
        if (natures.Count > 1)
        {
            setupNatureButton(natureButton_2, natures.Keys.ElementAt(1));
        }
        else
        {
            natureButton_2.gameObject.SetActive(false);
        }
    }

    void OnMouseDown()
    {
        Debug.Log("Card de Imprevisto clicado.");
        // Checa se o clique está sobre algum dos dois botões de score
        if (IsPointerOverButton(natureButton_1) || IsPointerOverButton(natureButton_2))
        {
            Debug.Log("Clique ignorado, estava sobre um botão de pontuação.");
            return;
        }
        string debuff1 = debuffs.Keys.ElementAt(0);
        Debug.Log($"Pontuação: {debuff1} com valor {debuffs[debuff1]}");
        ScoreManager.Instance.UpdateScore(debuff1, debuffs[debuff1]);
        
        if (!useFirstOnly && debuffs.Count > 1)
        {
            string debuff2 = debuffs.Keys.ElementAt(1);
            Debug.Log($"Pontuação: {debuff2} com valor {debuffs[debuff2]}");
            ScoreManager.Instance.UpdateScore(debuff2, debuffs[debuff2]);
        }
            
        Destroy(gameObject); // Destroi o objeto quando clicado
    }

    void randomText()
    {
        TextAsset[] texts = Resources.LoadAll<TextAsset>(textFolder);
        if (texts.Length == 0)
        {
            Debug.LogWarning("Nenhum texto encontrado em: " + textFolder);
            return;
        }

        TextAsset randomText = texts[Random.Range(0, texts.Length)];
        textUI.text = randomText.text;
    }

    void randomImages()
    {
        if (meshRenderers == null)
        {
            Debug.LogWarning("Nenhum MeshRenderer atribuído para receber as imagens aleatórias.");
            return;
        }
        Texture2D[] textures = Resources.LoadAll<Texture2D>(imageFolder);
        Material whiteMaterial = Resources.Load<Material>(whiteMaterialPath);

        if (textures.Length == 0)
        {
            Debug.LogWarning("Nenhuma textura encontrada em: " + imageFolder);
            return;
        }

        if (whiteMaterial == null)
        {
            Debug.LogWarning("Material branco não encontrado em: " + whiteMaterialPath);
            return;
        }

        int quantity = Random.Range(1, 3); // 1 ou 2


        System.Random random = new System.Random();
        Texture2D[] randomTextures = textures.OrderBy(x => random.Next()).ToArray();

        string nature;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (i < quantity)
            {
                // Aplica textura aleatória com shader compatível WebGL
                Material mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = randomTextures[i % randomTextures.Length];

                nature = randomTextures[i % randomTextures.Length].name;
                Debug.Log("Natureza selecionada: " + nature + " sendo adicionada ao dicionário de natures.");
                natures.Add(nature, -1);

                mat.SetTexture("_MainTex", randomTextures[i % randomTextures.Length]);
                meshRenderers[i].material = mat;
            }
            else
            {
                // Preenche com branco
                meshRenderers[i].material = whiteMaterial;
            }
        }
    }

    void loadScoreVariables()
    {
        ScoreManager.ScoreType[] randomTypes = GetTwoRandomScoreTypes();
        if (randomTypes.Length < 2)
        {
            Debug.LogWarning("Não há pontuações suficientes para selecionar aleatoriamente.");
            return;
        }
        debuffs.Clear();
        
        // Decide se usa 1 ou 2 debuffs
        bool useOnlyOneDebuff = Random.Range(0, 2) == 0; // 50% chance
        int debuffCount = useOnlyOneDebuff ? 1 : 2;
        
        for (int i = 0; i < debuffCount; i++)
        {
            string varName = randomTypes[i].ToString();
            int debuffValue;
            
            if (useOnlyOneDebuff)
            {
                debuffValue = -maxDebuff; // Um debuff com -2
                useFirstOnly = true;
            }
            else
            {
                debuffValue = -minDebuff; // Dois debuffs com -1 cada
                useFirstOnly = false;
            }
            
            if (!debuffs.ContainsKey(varName))
            {
                debuffs.Add(varName, debuffValue);
            }
        }
    }

    ScoreManager.ScoreType[] GetTwoRandomScoreTypes()
    {
        ScoreManager.ScoreType[] values = (ScoreManager.ScoreType[])Enum.GetValues(typeof(ScoreManager.ScoreType));
        if (values.Length < 2)
        {
            Debug.LogWarning("Não há pontuações suficientes para selecionar aleatoriamente.");
            return new ScoreManager.ScoreType[0];
        }
        List<ScoreManager.ScoreType> list = new List<ScoreManager.ScoreType>(values);

        // Embaralha a lista
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]); // Troca
        }

        // Pega os dois primeiros elementos
        return new ScoreManager.ScoreType[] { list[0], list[1] };
    }

    void setupNatureButton(Button button, string varName)
    {
        int value = natures[varName];

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            Debug.Log($"Clicou no botão de natureza: {varName} com valor {value}");
            bool updated = ScoreManager.Instance.UpdateScore(varName, value);
            if (!updated)
            {
                Debug.LogWarning($"Atualização negada ao atualizar a natureza para {varName} com valor {value}");
                return;
            }
            Destroy(gameObject); // Exemplo de ação ao clicar no botão
        });
    }

    private bool IsPointerOverButton(Button button)
    {
        if (button == null) return false;   
        
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == button.gameObject)
                return true;
        }
        return false;
    }
}