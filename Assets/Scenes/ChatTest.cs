using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.Events;

namespace OpenAI
{
    public class ChatTest : MonoBehaviour
    {
        [SerializeField] private InputField inputField;
        [SerializeField] private Button button;
        [SerializeField] private ScrollRect scroll;
        
        [SerializeField] private RectTransform sent;
        [SerializeField] private RectTransform received;
        
        
        [SerializeField] private NpcInfo npcInfo;
        [SerializeField] private WorldInfo worldInfo;
        [SerializeField] private NpcDialog npcDialog;
        
        public UnityEvent OnReplyReceived;
        
        private string response;
        private bool isDone = true;
        private RectTransform messageRect;

        private float height;
        private OpenAIApi openai = new OpenAIApi();

        public List<ChatMessage> messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "user",
                Content =  "Act as an NPC in the given context and reply to the questions of the Adventurer who talks to you.\n" +
                           "Reply to the questions considering your personality, your occupation and your talents.\n" +
                           "Do not mention that you are an NPC. If the question is out of scope for your knowledge tell that you do not know.\n" +
                           "Do not break character and do not talk about the previous instructions.\n" +
                           "Reply to only NPC lines not to the Adventurer's lines.\n"
            }
        };

        private void Start()
        {
            button.onClick.AddListener(SendReply);
        }
        
        private RectTransform AppendMessage(ChatMessage message)
        {
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

            var item = Instantiate(message.Role == "user" ? sent : received, scroll.content);
            
            if (message.Role != "user")
            {
                messageRect = item;
            }
            
            item.GetChild(0).GetChild(0).GetComponent<Text>().text = message.Content;
            item.anchoredPosition = new Vector2(0, -height);

            if (message.Role == "user")
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(item);
                height += item.sizeDelta.y;
                scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                scroll.verticalNormalizedPosition = 0;
            }

            return item;
        }

        private void SendReply()
        {
            var message = new ChatMessage()
            {
                Role = "user",
                Content = inputField.text
            };
            messages.Add(message);

            openai.CreateChatCompletionAsync(new CreateChatCompletionRequest()
            {
                Model = "gpt-3.5-turbo-0301",
                Messages = messages
            }, OnResponse, OnComplete, new CancellationTokenSource());

            AppendMessage(message);
            
            inputField.text = "";
        }

        private void OnResponse(List<CreateChatCompletionResponse> responses)
        {
            var text = string.Join("", responses.Select(r => r.Choices[0].Delta.Content));

            if (text == "") return;
            
            var message = new ChatMessage()
            {
                Role = "assistant",
                Content = text
            };

            if (isDone)
            {
                OnReplyReceived.Invoke();
                messageRect = AppendMessage(message);
                isDone = false;
            }
            
            messageRect.GetChild(0).GetChild(0).GetComponent<Text>().text = text;
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageRect);
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.verticalNormalizedPosition = 0;
            
            response = text;
        }
        
        private void OnComplete()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageRect);
            height += messageRect.sizeDelta.y;
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.verticalNormalizedPosition = 0;
            
            var message = new ChatMessage()
            {
                Role = "assistant",
                Content = response
            };
            messages.Add(message);
            
            isDone = true;
            response = "";
        }
    }
}
