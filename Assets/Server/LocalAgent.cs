using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ChatRequest
{
    public string model;
    public ChatMessage[] messages;
    public float temperature;
    public int max_tokens;
}

[Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[Serializable]
public class OllamaResponse
{
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public OllamaMessage message;
}

[Serializable]
public class OllamaMessage
{
    public string content;
}

public class LocalAgent : MonoBehaviour
{
    [SerializeField] private string url = LocalAgentClient.DefaultUrl;
    [SerializeField] private string model = LocalAgentClient.DefaultModel;
    [SerializeField] private float temperature = 0.7f;
    [SerializeField] private int maxTokens = 1024;

    public async void AskAgent(string prompt, Action<string> onResponse)
    {
        using UnityWebRequest request = LocalAgentClient.CreateChatRequest(url, model, prompt, temperature, maxTokens);
        await request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Raw Response: " + responseText);
            onResponse?.Invoke(LocalAgentClient.ParseReply(responseText) ?? "No response");
        }
        else
        {
            Debug.LogError(LocalAgentClient.FormatError(request));
            onResponse?.Invoke(null);
        }
    }
}
