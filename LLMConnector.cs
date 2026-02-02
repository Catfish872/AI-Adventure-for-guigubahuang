using System;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using System.IO; // 需要为流式读取添加
using System.Threading; // 需要为 CancellationToken 添加

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

        public LLMRequest(string model, List<Message> messages)
        {
            this.model = model;
            this.messages = messages;
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

        public void StreamMessageToLLM(List<Message> messages, CancellationToken cancellationToken, Action<string> onChunkReceived, Action onComplete, Action<string> onError)
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    // 创建包含 stream: true 的请求对象
                    var requestData = new
                    {
                        model = this.modelName,
                        messages = messages,
                        stream = true
                    };


                    var requestJson = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    // 使用 HttpCompletionOption.ResponseHeadersRead 来支持流式读取
                    var request = new HttpRequestMessage(HttpMethod.Post, this.apiUrl) { Content = content };
                    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            // 检查取消请求
                            if (cancellationToken.IsCancellationRequested)
                            {
                                // 在主线程上执行回调，通知任务被取消
                                ModMain.RunOnMainThread(() => onError?.Invoke("请求被取消。"));
                                return;
                            }

                            var line = await reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // LLM流式响应通常以 "data: " 开头
                            if (line.StartsWith("data: "))
                            {
                                string jsonData = line.Substring(6);
                                if (jsonData.Trim() == "[DONE]")
                                {
                                    break; // 流结束
                                }

                                dynamic jsonChunk = JsonConvert.DeserializeObject(jsonData);
                                if (jsonChunk.choices != null && jsonChunk.choices.Count > 0)
                                {
                                    string textChunk = jsonChunk.choices[0].delta.content;
                                    if (!string.IsNullOrEmpty(textChunk))
                                    {
                                        // 将收到的文本块安全地传递回主线程
                                        ModMain.RunOnMainThread(() => onChunkReceived?.Invoke(textChunk));
                                    }
                                }
                            }
                        }
                    }
                    // 整个流处理完毕，在主线程上调用完成回调
                    ModMain.RunOnMainThread(() => onComplete?.Invoke());
                }
                catch (OperationCanceledException)
                {
                    // 这是由 CancellationToken 触发的正常取消
                    ModMain.RunOnMainThread(() => onError?.Invoke("请求被用户取消。"));
                }
                catch (Exception ex)
                {
                    // 其他所有错误
                    ModMain.RunOnMainThread(() => onError?.Invoke($"连接或解析失败: {ex.Message}"));
                }
            });
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
                        messages
                    );
                    var requestJson = JsonConvert.SerializeObject(request);
                    string jsonOutput = JsonConvert.SerializeObject(request);
                    
                    //Debug.Log($"实际序列化的JSON: {jsonOutput}");
                    var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    // 发送请求
                    var response = httpClient.PostAsync(apiUrl, content).GetAwaiter().GetResult();
                    var responseJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    

                    ModMain.RunOnMainThread(() =>
                    {
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseJson);
                        Debug.Log($"实际序列化的JSON: {jsonOutput}");
                        if (jsonResponse == null || jsonResponse.choices == null || jsonResponse.choices.Count == 0 || jsonResponse.choices[0].message == null)
                        {
                            tcs.SetResult($"对不起，我暂时无法回应。请尝试1.如果修改过神识传音配置，请找个NPC点开，点击白色的配置按钮，复制神识配置并保存 2.尝试重新进入存档 3.点开一名NPC，找到白色的配置按钮，检查API配置是否正常");
                            return;
                        }

                        string result = jsonResponse.choices[0].message.content.ToString();
                        if (string.IsNullOrEmpty(result))
                            result = $"对不起，我暂时无法回应:{result}";

                        tcs.SetResult(result);
                    });
                }
                catch
                {
                    ModMain.RunOnMainThread(() => tcs.SetResult("对不起，连接失败，请稍后再试。请尝试1.如果是初次使用mod，请大退游戏一次让mod正常初始化  2.点开一名NPC，找到白色的配置按钮，检查api配置是否正确"));
                }
            });

            return tcs.Task;
        }
    }
}