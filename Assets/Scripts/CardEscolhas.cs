using TMPro;
using UnityEngine;

public class CardEscolhas : MonoBehaviour
{
    public string textFolder = "Texts/Escolhas";
    public TMP_Text textUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomText();
    }

    // Update is called once per frame
    void Update()
    {

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
}
