using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class LocalAgentClient
{
    public const string DefaultUrl = "http://localhost:11434/v1/chat/completions";
    public const string DefaultModel = "deepseek-coder-v2:16b";

    public static UnityWebRequest CreateChatRequest(
        string url,
        string model,
        string prompt,
        float temperature = 0.7f,
        int maxTokens = 1024)
    {
        return CreateChatRequest(
            url,
            model,
            new[] { new ChatMessage { role = "user", content = prompt } },
            temperature,
            maxTokens);
    }

    public static UnityWebRequest CreateChatRequest(
        string url,
        string model,
        ChatMessage[] messages,
        float temperature = 0.7f,
        int maxTokens = 1024)
    {
        var payload = new ChatRequest
        {
            model = model,
            messages = messages,
            temperature = temperature,
            max_tokens = maxTokens
        };

        string json = JsonUtility.ToJson(payload);

        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    public static string ParseReply(string responseText)
    {
        if (string.IsNullOrEmpty(responseText))
            return null;

        OllamaResponse ollamaResp = JsonUtility.FromJson<OllamaResponse>(responseText);
        return ollamaResp?.choices?[0]?.message?.content;
    }

    public static string FormatError(UnityWebRequest request)
    {
        string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        return string.IsNullOrEmpty(body)
            ? request.error ?? "Unknown request error."
            : $"{request.error}\n{body}";
    }
}
