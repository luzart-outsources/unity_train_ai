using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class NPCDialogueAI : MonoBehaviour
{
    [Header("NPC Personality Setup")]
    public string npcName = "Đại Đội Trưởng";
    
    [TextArea(5, 10)]
    public string systemPrompt = "Bạn là một Đại Đội Trưởng cực kỳ nghiêm khắc trong khu quân sự. Xưng hô là 'Tôi' và gọi người chơi là 'Đồng chí'. Bạn rất ghét những tân binh vô kỷ luật, trễ giờ hoặc hỏi những câu ngớ ngẩn. Câu trả lời của bạn phải ngắn gọn, đanh thép, mang đậm tính quân đội (tối đa 2-3 câu).";

    [Header("UI References")]
    public TMP_InputField chatInputField; // Ô gõ chữ của người chơi

    [Header("Events")]
    public UnityEvent<string> onResponseReceived;
    public UnityEvent<string> onErrorReceived;

    /// <summary>
    /// Hàm này được gọi khi người chơi bấm nút Gửi (Send Button)
    /// </summary>
    public void ChatWithNPC()
    {
        if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text)) 
        {
            Debug.LogWarning("Chưa nhập chữ hoặc chưa gắn InputField!");
            return;
        }

        string playerInput = chatInputField.text;
        Debug.Log($"[{npcName}] Đang suy nghĩ về câu nói: '{playerInput}'...");
        
        LLMNetworkManager.Instance.SendChatRequest(
            systemPrompt, 
            playerInput, 
            onSuccess, 
            onFail
        );
        
        // Xóa trắng ô text sau khi gửi
        chatInputField.text = "";
    }

    private void onSuccess(string aiResponse)
    {
        Debug.Log($"[{npcName} Trả lời]: {aiResponse}");
        // Bắn event để hiển thị lên UI Dialogue Box
        onResponseReceived?.Invoke(aiResponse);
    }

    private void onFail(string errorMsg)
    {
        Debug.LogError($"[API Error] {errorMsg}");
        onErrorReceived?.Invoke(errorMsg);
    }
}
