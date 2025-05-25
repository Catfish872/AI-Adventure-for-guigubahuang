using System;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace MOD_kqAfiU
{
    [Serializable]
    public class LLMResponse
    {
        public string content;
    }

    [Serializable]
    public class Message
    {
        public string role;
        public string content;

        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    public class LLMRequest
    {
        public string model;
        public List<Message> messages;
        public double temperature;
        public int max_tokens;
        public int top_k;
        public double top_p;
        public double frequency_penalty;

        public LLMRequest(string model, List<Message> messages, double temperature = 1.0, int max_tokens = 1024,
                 int top_k = 50, double top_p = 0.8, double frequency_penalty = 0.0)
        {
            this.model = model;
            this.messages = messages;
            this.temperature = temperature;
            this.max_tokens = max_tokens;
            this.top_k = top_k;
            this.top_p = top_p;
            this.frequency_penalty = frequency_penalty;
        }
    }

    // 新增：对话上下文类，用于存储对话历史
    public class ConversationContext
    {
        public string SystemPrompt { get; set; }
        public List<Message> MessageHistory { get; private set; }
        public string SessionId { get; private set; }

        public ConversationContext(string systemPrompt = "", string sessionId = null)
        {
            SystemPrompt = systemPrompt;
            SessionId = sessionId ?? Guid.NewGuid().ToString();
            MessageHistory = new List<Message>();

            // 如果有系统提示，添加到历史
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                MessageHistory.Add(new Message("system", systemPrompt));
            }
        }

        public void AddUserMessage(string content)
        {
            MessageHistory.Add(new Message("user", content));
        }

        public void AddAssistantMessage(string content)
        {
            MessageHistory.Add(new Message("assistant", content));
        }

        public List<Message> GetMessages()
        {
            return new List<Message>(MessageHistory);
        }
    }

    public class LLMConnector
    {
        private string apiUrl;
        private string apiKey;
        private string modelName;
        private HttpClient httpClient;

        // 存储会话上下文的字典
        private Dictionary<string, ConversationContext> conversations;

        public LLMConnector(string apiUrl, string apiKey, string modelName)
        {
            this.apiUrl = apiUrl;
            this.apiKey = apiKey;
            this.modelName = modelName;
            this.httpClient = new HttpClient();
            this.conversations = new Dictionary<string, ConversationContext>();

            this.httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
        }

        // 创建新的对话上下文
        public string CreateConversation(string systemPrompt = "")
        {
            var context = new ConversationContext(systemPrompt);
            conversations[context.SessionId] = context;
            return context.SessionId;
        }

        // 获取现有对话上下文
        public ConversationContext GetConversation(string sessionId)
        {
            if (conversations.TryGetValue(sessionId, out var context))
            {
                return context;
            }
            return null;
        }
        // 向LLM发送消息列表
        public Task<string> SendMessageToLLM(List<Message> messages)
        {
            var tcs = new TaskCompletionSource<string>();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // 创建请求对象
                    var request = new LLMRequest(
                        modelName,
                        messages,
                        ModMain.temperature,
                        ModMain.max_tokens,
                        ModMain.top_k,
                        ModMain.top_p,
                        ModMain.frequency_penalty
                    );
                    var requestJson = JsonConvert.SerializeObject(request);
                    var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    // 发送请求
                    var response = httpClient.PostAsync(apiUrl, content).GetAwaiter().GetResult();
                    var responseJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    ModMain.RunOnMainThread(() =>
                    {
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseJson);

                        if (jsonResponse == null || jsonResponse.choices == null || jsonResponse.choices.Count == 0 || jsonResponse.choices[0].message == null)
                        {
                            tcs.SetResult("对不起，我暂时无法回应");
                            return;
                        }

                        string result = jsonResponse.choices[0].message.content.ToString();
                        if (string.IsNullOrEmpty(result))
                            result = "对不起，我暂时无法回应";

                        tcs.SetResult(result);
                    });
                }
                catch
                {
                    ModMain.RunOnMainThread(() => tcs.SetResult("对不起，连接失败，请稍后再试"));
                }
            });

            return tcs.Task;
        }
    }
}