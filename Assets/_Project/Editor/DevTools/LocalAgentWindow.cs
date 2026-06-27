using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class LocalAgentWindow : EditorWindow
{
    private const string UrlPrefKey = "LocalAgentWindow.Url";
    private const string ModelPrefKey = "LocalAgentWindow.Model";
    private const string TemperaturePrefKey = "LocalAgentWindow.Temperature";
    private const string MaxTokensPrefKey = "LocalAgentWindow.MaxTokens";
    private const string IncludeContextPrefKey = "LocalAgentWindow.IncludeContext";
    private const string IncludeScriptPrefKey = "LocalAgentWindow.IncludeScript";

    private string url = LocalAgentClient.DefaultUrl;
    private string model = LocalAgentClient.DefaultModel;
    private float temperature = 0.7f;
    private int maxTokens = 4096;
    private bool includeUnityContext = true;
    private bool includeSelectedScript = true;
    private string prompt = string.Empty;
    private string status = "Ready.";
    private Vector2 promptScroll;
    private Vector2 conversationScroll;
    private bool isWaiting;
    private bool scrollToBottom;
    private UnityWebRequest activeRequest;

    private readonly List<LocalAgentConversationEntry> conversation = new List<LocalAgentConversationEntry>();
    private readonly List<ChatMessage> apiMessages = new List<ChatMessage>();
    private GUIStyle userBubbleStyle;
    private GUIStyle assistantBubbleStyle;
    private GUIStyle systemBubbleStyle;
    private GUIStyle messageHeaderStyle;

    [MenuItem("Tools/Local Agent")]
    public static void ShowWindow()
    {
        GetWindow<LocalAgentWindow>("Local Agent").minSize = new Vector2(640, 720);
    }

    private void OnEnable()
    {
        url = EditorPrefs.GetString(UrlPrefKey, LocalAgentClient.DefaultUrl);
        model = EditorPrefs.GetString(ModelPrefKey, LocalAgentClient.DefaultModel);
        temperature = EditorPrefs.GetFloat(TemperaturePrefKey, 0.7f);
        maxTokens = EditorPrefs.GetInt(MaxTokensPrefKey, 4096);
        includeUnityContext = EditorPrefs.GetBool(IncludeContextPrefKey, true);
        includeSelectedScript = EditorPrefs.GetBool(IncludeScriptPrefKey, true);
        EnsureSystemMessage();
    }

    private void OnDisable()
    {
        EditorApplication.update -= PollRequest;
        activeRequest?.Dispose();
        activeRequest = null;
    }

    private void EnsureSystemMessage()
    {
        if (apiMessages.Count > 0 && apiMessages[0].role == "system")
            return;

        apiMessages.Insert(0, new ChatMessage
        {
            role = "system",
            content = LocalAgentUnityBridge.SystemPrompt
        });
    }

    private void InitStyles()
    {
        if (userBubbleStyle != null)
            return;

        messageHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11
        };

        userBubbleStyle = new GUIStyle(EditorStyles.helpBox)
        {
            wordWrap = true,
            richText = false,
            padding = new RectOffset(10, 10, 8, 8)
        };

        assistantBubbleStyle = new GUIStyle(EditorStyles.helpBox)
        {
            wordWrap = true,
            richText = false,
            padding = new RectOffset(10, 10, 8, 8)
        };

        systemBubbleStyle = new GUIStyle(EditorStyles.helpBox)
        {
            wordWrap = true,
            fontStyle = FontStyle.Italic,
            padding = new RectOffset(10, 10, 8, 8)
        };
    }

    private void OnGUI()
    {
        InitStyles();

        EditorGUILayout.LabelField("Local Agent — Unity Bridge", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Chat with your local Ollama model using Unity project context. Responses stay in the log below. " +
            "Code blocks tagged with // File: Assets/... can be applied to the project.",
            MessageType.Info);

        DrawSettingsSection();
        DrawPromptSection();
        DrawConversationSection();
        DrawFooterSection();
    }

    private void DrawSettingsSection()
    {
        EditorGUI.BeginDisabledGroup(isWaiting);
        url = EditorGUILayout.TextField("API URL", url);
        model = EditorGUILayout.TextField("Model", model);
        temperature = EditorGUILayout.Slider("Temperature", temperature, 0f, 1.5f);
        maxTokens = EditorGUILayout.IntField("Max Tokens", maxTokens);

        includeUnityContext = EditorGUILayout.ToggleLeft("Include Unity project context", includeUnityContext);
        using (new EditorGUI.DisabledScope(!includeUnityContext))
            includeSelectedScript = EditorGUILayout.ToggleLeft("Include selected script source", includeSelectedScript);
        EditorGUI.EndDisabledGroup();
    }

    private void DrawPromptSection()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(isWaiting);
        promptScroll = EditorGUILayout.BeginScrollView(promptScroll, GUILayout.Height(110));
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginDisabledGroup(isWaiting || string.IsNullOrWhiteSpace(prompt));
            if (GUILayout.Button(isWaiting ? "Sending..." : "Send Prompt", GUILayout.Height(30)))
                SendPrompt();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(isWaiting);
            if (GUILayout.Button("Clear Prompt", GUILayout.Width(100), GUILayout.Height(30)))
                prompt = string.Empty;
            EditorGUI.EndDisabledGroup();
        }
    }

    private void DrawConversationSection()
    {
        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Conversation ({conversation.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(conversation.Count == 0);
            if (GUILayout.Button("Save .txt", GUILayout.Width(90)))
                SaveTranscriptToFile();
            if (GUILayout.Button("Clear Log", GUILayout.Width(90)))
                ClearConversation();
            EditorGUI.EndDisabledGroup();
        }

        Rect scrollRect = GUILayoutUtility.GetRect(0f, 320f, GUILayout.ExpandHeight(true));
        float contentHeight = CalculateConversationHeight(scrollRect.width - 24f);
        Rect viewRect = new Rect(0f, 0f, scrollRect.width - 18f, contentHeight);

        conversationScroll = GUI.BeginScrollView(scrollRect, conversationScroll, viewRect);
        if (conversation.Count == 0)
        {
            GUI.Label(new Rect(8f, 8f, viewRect.width - 16f, 40f), "No messages yet. Send a prompt to start the log.", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            float y = 8f;
            foreach (LocalAgentConversationEntry entry in conversation)
                y = DrawConversationEntry(viewRect.width, y, entry);
        }

        if (scrollToBottom)
        {
            conversationScroll.y = Mathf.Max(0f, contentHeight - scrollRect.height);
            scrollToBottom = false;
        }

        GUI.EndScrollView();

        string lastAssistant = GetLastAssistantMessage();
        EditorGUI.BeginDisabledGroup(isWaiting || string.IsNullOrEmpty(lastAssistant));
        if (GUILayout.Button("Apply Edits From Last Response", GUILayout.Height(28)))
            ApplyLastResponseEdits();
        EditorGUI.EndDisabledGroup();
    }

    private void DrawFooterSection()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
    }

    private float DrawConversationEntry(float width, float y, LocalAgentConversationEntry entry)
    {
        GUIStyle headerStyle = messageHeaderStyle;
        GUIStyle bubbleStyle = entry.role switch
        {
            "user" => userBubbleStyle,
            "assistant" => assistantBubbleStyle,
            _ => systemBubbleStyle
        };

        string label = entry.role switch
        {
            "user" => "You",
            "assistant" => "Agent",
            _ => "System"
        };

        float headerHeight = headerStyle.CalcHeight(new GUIContent($"{label}  {entry.timestamp:HH:mm:ss}"), width);
        GUI.Label(new Rect(8f, y, width - 16f, headerHeight), $"{label}  {entry.timestamp:HH:mm:ss}", headerStyle);
        y += headerHeight + 2f;

        float textHeight = bubbleStyle.CalcHeight(new GUIContent(entry.content), width - 16f);
        textHeight = Mathf.Max(textHeight, 40f);
        GUI.Box(new Rect(8f, y, width - 16f, textHeight), entry.content, bubbleStyle);
        y += textHeight + 12f;
        return y;
    }

    private float CalculateConversationHeight(float width)
    {
        if (conversation.Count == 0)
            return 80f;

        float height = 8f;
        foreach (LocalAgentConversationEntry entry in conversation)
        {
            GUIStyle bubbleStyle = entry.role switch
            {
                "user" => userBubbleStyle ?? EditorStyles.helpBox,
                "assistant" => assistantBubbleStyle ?? EditorStyles.helpBox,
                _ => systemBubbleStyle ?? EditorStyles.helpBox
            };

            height += messageHeaderStyle?.CalcHeight(new GUIContent("Header"), width - 16f) ?? 18f;
            height += 2f;
            height += Mathf.Max(bubbleStyle.CalcHeight(new GUIContent(entry.content), width - 16f), 40f);
            height += 12f;
        }

        return height + 8f;
    }

    private void SendPrompt()
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            status = "Enter a prompt first.";
            return;
        }

        SaveSettings();
        EnsureSystemMessage();

        string userPrompt = prompt.Trim();
        string payloadPrompt = userPrompt;

        if (includeUnityContext)
        {
            string context = LocalAgentUnityBridge.BuildProjectContext(includeUnityContext, includeSelectedScript);
            payloadPrompt = context + "\n\n[User Request]\n" + userPrompt;
        }

        AddConversationEntry("user", userPrompt);
        apiMessages.Add(new ChatMessage { role = "user", content = payloadPrompt });

        activeRequest?.Dispose();
        activeRequest = LocalAgentClient.CreateChatRequest(url, model, apiMessages.ToArray(), temperature, maxTokens);
        activeRequest.SendWebRequest();

        isWaiting = true;
        status = "Waiting for Ollama...";
        prompt = string.Empty;
        scrollToBottom = true;
        EditorApplication.update += PollRequest;
        Repaint();
    }

    private void PollRequest()
    {
        if (activeRequest == null || !activeRequest.isDone)
            return;

        EditorApplication.update -= PollRequest;
        isWaiting = false;

        if (activeRequest.result == UnityWebRequest.Result.Success)
        {
            string raw = activeRequest.downloadHandler.text;
            string reply = LocalAgentClient.ParseReply(raw);
            string content = string.IsNullOrEmpty(reply) ? raw : reply;

            AddConversationEntry("assistant", content);
            apiMessages.Add(new ChatMessage { role = "assistant", content = content });
            status = "Response added to conversation.";
            scrollToBottom = true;
            Debug.Log($"Local Agent response:\n{content}");
        }
        else
        {
            string error = LocalAgentClient.FormatError(activeRequest);
            AddConversationEntry("assistant", error);
            status = "Request failed. Is Ollama running at localhost:11434?";
            scrollToBottom = true;
            Debug.LogError($"Local Agent error:\n{error}");
        }

        activeRequest.Dispose();
        activeRequest = null;
        Repaint();
    }

    private void AddConversationEntry(string role, string content)
    {
        conversation.Add(new LocalAgentConversationEntry(role, content));
    }

    private string GetLastAssistantMessage()
    {
        for (int i = conversation.Count - 1; i >= 0; i--)
        {
            if (conversation[i].role == "assistant")
                return conversation[i].content;
        }

        return string.Empty;
    }

    private void ApplyLastResponseEdits()
    {
        string lastAssistant = GetLastAssistantMessage();
        if (string.IsNullOrEmpty(lastAssistant))
        {
            status = "No assistant response to apply.";
            return;
        }

        int applied = LocalAgentUnityBridge.TryApplyFileEdits(lastAssistant, out List<string> results);
        string summary = string.Join("\n", results);
        AddConversationEntry("system", "[Apply Edits]\n" + summary);
        scrollToBottom = true;
        status = applied > 0 ? $"Applied {applied} file edit(s)." : "No file edits applied.";
        EditorUtility.DisplayDialog(
            applied > 0 ? "Edits Applied" : "No Edits Applied",
            summary,
            "OK");
        Repaint();
    }

    private void SaveTranscriptToFile()
    {
        string defaultName = $"LocalAgent_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string path = EditorUtility.SaveFilePanel(
            "Save Local Agent Transcript",
            Application.dataPath,
            defaultName,
            "txt");

        if (string.IsNullOrEmpty(path))
            return;

        File.WriteAllText(path, LocalAgentUnityBridge.FormatTranscript(conversation));
        status = $"Saved transcript: {path}";
        EditorUtility.RevealInFinder(path);
    }

    private void ClearConversation()
    {
        if (!EditorUtility.DisplayDialog(
                "Clear Conversation",
                "Clear all saved responses in this window?",
                "Clear",
                "Cancel"))
            return;

        conversation.Clear();
        apiMessages.Clear();
        EnsureSystemMessage();
        status = "Conversation cleared.";
        conversationScroll = Vector2.zero;
        Repaint();
    }

    private void SaveSettings()
    {
        EditorPrefs.SetString(UrlPrefKey, url);
        EditorPrefs.SetString(ModelPrefKey, model);
        EditorPrefs.SetFloat(TemperaturePrefKey, temperature);
        EditorPrefs.SetInt(MaxTokensPrefKey, maxTokens);
        EditorPrefs.SetBool(IncludeContextPrefKey, includeUnityContext);
        EditorPrefs.SetBool(IncludeScriptPrefKey, includeSelectedScript);
    }
}
