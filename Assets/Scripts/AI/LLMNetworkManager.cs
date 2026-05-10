using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMNetworkManager : MonoBehaviour
{
    public static LLMNetworkManager Instance { get; private set; }

    [Header("API Settings")]
    [Tooltip("Ví dụ: https://api.groq.com/openai/v1/chat/completions hoặc URL của FreeLLMAPI")]
    public string apiURL = "https://api.groq.com/openai/v1/chat/completions";
    
    [Tooltip("Dán API Key của bạn vào đây")]
    public string apiKey = "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";
    
    [Tooltip("Tên model muốn sử dụng, VD: llama3-8b-8192")]
    public string modelName = "llama3-8b-8192";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Gửi tin nhắn của người chơi cho NPC và nhận câu trả lời qua Callback
    /// </summary>
    public void SendChatRequest(string systemPrompt, string userMessage, Action<string> onComplete, Action<string> onError)
    {
        StartCoroutine(PostRequest(systemPrompt, userMessage, onComplete, onError));
    }

    private IEnumerator PostRequest(string systemPrompt, string userMessage, Action<string> onComplete, Action<string> onError)
    {
        // 1. Tạo JSON body theo chuẩn OpenAI (phù hợp với hầu hết các Free LLM API)
        string jsonBody = $@"{{
            ""model"": ""{modelName}"",
            ""messages"": [
                {{
                    ""role"": ""system"",
                    ""content"": ""{EscapeString(systemPrompt)}""
                }},
                {{
                    ""role"": ""user"",
                    ""content"": ""{EscapeString(userMessage)}""
                }}
            ],
            ""temperature"": 0.7,
            ""max_tokens"": 150
        }}";

        // 2. Khởi tạo UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(apiURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // 3. Setup Headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            // 4. Gửi Request và đợi
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[LLM Error] {request.error} - {request.downloadHandler.text}");
                onError?.Invoke("Mất kết nối với AI: " + request.error);
            }
            else
            {
                // 5. Bóc tách JSON trả về (Rất cơ bản, có thể cần SimpleJSON để an toàn hơn)
                string responseText = request.downloadHandler.text;
                
                // Tìm kiếm chuỗi "content":"
                string contentKey = "\"content\":\"";
                int startIndex = responseText.IndexOf(contentKey);
                
                if (startIndex != -1)
                {
                    startIndex += contentKey.Length;
                    int endIndex = responseText.IndexOf("\"", startIndex);
                    // Bỏ qua escape quotes
                    while (endIndex != -1 && responseText[endIndex - 1] == '\\')
                    {
                        endIndex = responseText.IndexOf("\"", endIndex + 1);
                    }
                    
                    if (endIndex != -1)
                    {
                        string finalAnswer = responseText.Substring(startIndex, endIndex - startIndex);
                        // Decode các ký tự \n, \t
                        finalAnswer = finalAnswer.Replace("\\n", "\n").Replace("\\\"", "\"");
                        onComplete?.Invoke(finalAnswer);
                    }
                    else
                    {
                        onError?.Invoke("Không parse được JSON từ API.");
                    }
                }
                else
                {
                    onError?.Invoke("API trả về sai định dạng.");
                }
            }
        }
    }

    private string EscapeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("\"", "\\\"").Replace("\n", " ");
    }
}
