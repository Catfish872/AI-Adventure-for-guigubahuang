using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace MOD_kqAfiU
{
    [Serializable]
    public class ModConfig
    {
        // API设置
        public string ApiUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string ModelName { get; set; } = "";

        // 模型参数设置
        public int MaxTokens { get; set; } = 1024;
        public float Temperature { get; set; } = (float)1.0;
        public int TopK { get; set; } = 50;
        public float TopP { get; set; } = (float)0.8;
        public float FrequencyPenalty { get; set; } = 0;

        public List<string> EnvironmentFeatures { get; set; } = new List<string>();
        public List<string> EventTypes { get; set; } = new List<string>();
        public List<string> NpcTraits { get; set; } = new List<string>();
        public List<string> CoreConflicts { get; set; } = new List<string>();
        public string PromptPrefix { get; set; } = "";
        public string PromptSuffix { get; set; } = "";
        public float EncounterProbability { get; set; } = 0.05f;
    }

    public static class Config
    {
        private static string configPath = "config.json";

        /// <summary>
        /// 初始化配置文件 - 如果配置文件不存在，尝试从游戏读取配置，否则创建空模板
        /// </summary>
        public static void InitConfig()
        {
            try
            {
                ModConfig configTemplate = new ModConfig
                {
                    // 默认值设置
                    ApiUrl = "",
                    ApiKey = "",
                    ModelName = "",
                    MaxTokens = 1024,
                    Temperature = 1.0f,
                    TopK = 50,
                    TopP = 0.8f,
                    FrequencyPenalty = 0f,
                    EnvironmentFeatures = new List<string>(),
                    EventTypes = new List<string>(),
                    NpcTraits = new List<string>(),
                    CoreConflicts = new List<string>(),
                    PromptPrefix = "",
                    PromptSuffix = "",
                    EncounterProbability = 0.05f
                };

                bool needUpdate = false;
                ModConfig existingConfig = null;

                // 检查配置文件是否存在
                if (File.Exists(configPath))
                {
                    try
                    {
                        // 读取现有配置
                        string existingJson = File.ReadAllText(configPath);
                        existingConfig = JsonConvert.DeserializeObject<ModConfig>(existingJson);
                        Debug.Log("已读取现有配置文件");

                        // 检查是否缺少新字段（通过反射比较）
                        var templateProps = typeof(ModConfig).GetProperties();
                        var jsonObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(existingJson);

                        foreach (var prop in templateProps)
                        {
                            if (jsonObj[prop.Name] == null)
                            {
                                Debug.Log($"配置文件缺少字段: {prop.Name}，将添加该字段");
                                needUpdate = true;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"读取现有配置文件失败: {ex.Message}，将创建新配置");
                        needUpdate = true;
                        existingConfig = null;
                    }
                }
                else
                {
                    Debug.Log("配置文件不存在，将创建新配置");
                    needUpdate = true;
                }

                // 如果是新建配置，尝试从游戏数据中读取API配置和模型参数
                if (existingConfig == null && g.data != null && g.data.dataObj != null && g.data.dataObj.data != null)
                {
                    // API相关配置
                    if (g.data.dataObj.data.ContainsKey("apiUrl"))
                    {
                        configTemplate.ApiUrl = g.data.dataObj.data.GetString("apiUrl");
                    }

                    if (g.data.dataObj.data.ContainsKey("apiKey"))
                    {
                        configTemplate.ApiKey = g.data.dataObj.data.GetString("apiKey");
                    }

                    if (g.data.dataObj.data.ContainsKey("modelName"))
                    {
                        configTemplate.ModelName = g.data.dataObj.data.GetString("modelName");
                    }

                    // 模型参数
                    if (g.data.dataObj.data.ContainsKey("max_tokens"))
                    {
                        int maxTokens;
                        if (int.TryParse(g.data.dataObj.data.GetString("max_tokens"), out maxTokens))
                        {
                            configTemplate.MaxTokens = maxTokens;
                        }
                    }

                    if (g.data.dataObj.data.ContainsKey("temperature"))
                    {
                        float temperature;
                        if (float.TryParse(g.data.dataObj.data.GetString("temperature"), out temperature))
                        {
                            configTemplate.Temperature = temperature;
                        }
                    }

                    if (g.data.dataObj.data.ContainsKey("top_k"))
                    {
                        int topK;
                        if (int.TryParse(g.data.dataObj.data.GetString("top_k"), out topK))
                        {
                            configTemplate.TopK = topK;
                        }
                    }

                    if (g.data.dataObj.data.ContainsKey("top_p"))
                    {
                        float topP;
                        if (float.TryParse(g.data.dataObj.data.GetString("top_p"), out topP))
                        {
                            configTemplate.TopP = topP;
                        }
                    }

                    if (g.data.dataObj.data.ContainsKey("frequency_penalty"))
                    {
                        float frequencyPenalty;
                        if (float.TryParse(g.data.dataObj.data.GetString("frequency_penalty"), out frequencyPenalty))
                        {
                            configTemplate.FrequencyPenalty = frequencyPenalty;
                        }
                    }

                    Debug.Log("已从游戏数据读取API配置和模型参数");
                }

                // 如果需要更新（文件不存在或缺少字段）或者有空值需要填充
                if (needUpdate || existingConfig != null)
                {
                    ModConfig finalConfig;

                    if (existingConfig != null)
                    {
                        // 检查现有配置中是否有空的API配置或模型参数
                        bool hasEmptyValues = string.IsNullOrEmpty(existingConfig.ApiUrl) ||
                             string.IsNullOrEmpty(existingConfig.ApiKey) ||
                             string.IsNullOrEmpty(existingConfig.ModelName) ||
                             existingConfig.MaxTokens == 0 ||
                             existingConfig.Temperature == 0 ||
                             existingConfig.TopK == 0 ||
                             existingConfig.TopP == 0 ||
                             existingConfig.EncounterProbability == 0; 

                        // 如果有空值且可以从游戏数据获取，则填充
                        if (hasEmptyValues && g.data != null && g.data.dataObj != null && g.data.dataObj.data != null)
                        {
                            needUpdate = true;

                            // API配置
                            if (string.IsNullOrEmpty(existingConfig.ApiUrl) && g.data.dataObj.data.ContainsKey("apiUrl"))
                            {
                                existingConfig.ApiUrl = g.data.dataObj.data.GetString("apiUrl");
                                Debug.Log("从游戏数据填充空的ApiUrl");
                            }

                            if (string.IsNullOrEmpty(existingConfig.ApiKey) && g.data.dataObj.data.ContainsKey("apiKey"))
                            {
                                existingConfig.ApiKey = g.data.dataObj.data.GetString("apiKey");
                                Debug.Log("从游戏数据填充空的ApiKey");
                            }

                            if (string.IsNullOrEmpty(existingConfig.ModelName) && g.data.dataObj.data.ContainsKey("modelName"))
                            {
                                existingConfig.ModelName = g.data.dataObj.data.GetString("modelName");
                                Debug.Log("从游戏数据填充空的ModelName");
                            }

                            // 模型参数
                            if (existingConfig.MaxTokens == 0 && g.data.dataObj.data.ContainsKey("max_tokens"))
                            {
                                int maxTokens;
                                if (int.TryParse(g.data.dataObj.data.GetString("max_tokens"), out maxTokens))
                                {
                                    existingConfig.MaxTokens = maxTokens;
                                    Debug.Log("从游戏数据填充空的MaxTokens");
                                }
                            }

                            if (existingConfig.Temperature == 0 && g.data.dataObj.data.ContainsKey("temperature"))
                            {
                                float temperature;
                                if (float.TryParse(g.data.dataObj.data.GetString("temperature"), out temperature))
                                {
                                    existingConfig.Temperature = temperature;
                                    Debug.Log("从游戏数据填充空的Temperature");
                                }
                            }

                            if (existingConfig.TopK == 0 && g.data.dataObj.data.ContainsKey("top_k"))
                            {
                                int topK;
                                if (int.TryParse(g.data.dataObj.data.GetString("top_k"), out topK))
                                {
                                    existingConfig.TopK = topK;
                                    Debug.Log("从游戏数据填充空的TopK");
                                }
                            }

                            if (existingConfig.TopP == 0 && g.data.dataObj.data.ContainsKey("top_p"))
                            {
                                float topP;
                                if (float.TryParse(g.data.dataObj.data.GetString("top_p"), out topP))
                                {
                                    existingConfig.TopP = topP;
                                    Debug.Log("从游戏数据填充空的TopP");
                                }
                            }

                            if (existingConfig.FrequencyPenalty == 0 && g.data.dataObj.data.ContainsKey("frequency_penalty"))
                            {
                                float frequencyPenalty;
                                if (float.TryParse(g.data.dataObj.data.GetString("frequency_penalty"), out frequencyPenalty))
                                {
                                    existingConfig.FrequencyPenalty = frequencyPenalty;
                                    Debug.Log("从游戏数据填充空的FrequencyPenalty");
                                }
                            }
                        }

                        // 检查是否存在空的列表
                        var existingJson = JsonConvert.SerializeObject(existingConfig);
                        var existingJObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(existingJson);
                        var templateJson = JsonConvert.SerializeObject(configTemplate);
                        var templateJObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(templateJson);

                        foreach (var prop in templateJObj.Properties())
                        {
                            if (existingJObj[prop.Name] == null)
                            {
                                existingJObj[prop.Name] = prop.Value;
                                needUpdate = true;
                                Debug.Log($"添加新字段: {prop.Name}");
                            }
                        }

                        finalConfig = JsonConvert.DeserializeObject<ModConfig>(existingJObj.ToString());

                        // 检查是否有新增字段需要添加（在已有配置中不存在的）
                        var existingProps = typeof(ModConfig).GetProperties();
                        var templateProps = typeof(ModConfig).GetProperties();

                        foreach (var templateProp in templateProps)
                        {
                            bool propExists = false;
                            foreach (var existingProp in existingProps)
                            {
                                if (existingProp.Name == templateProp.Name)
                                {
                                    propExists = true;
                                    break;
                                }
                            }

                            if (!propExists)
                            {
                                // 如果是新字段，添加默认值
                                var defaultValue = templateProp.GetValue(configTemplate);
                                var propInfo = typeof(ModConfig).GetProperty(templateProp.Name);
                                if (propInfo != null && propInfo.CanWrite)
                                {
                                    propInfo.SetValue(finalConfig, defaultValue);
                                    needUpdate = true;
                                    Debug.Log($"添加新字段: {templateProp.Name}");
                                }
                            }
                        }
                    }
                    else
                    {
                        finalConfig = configTemplate;
                        needUpdate = true;
                    }

                    // 如果需要更新，写入配置文件
                    if (needUpdate)
                    {
                        string jsonContent = JsonConvert.SerializeObject(finalConfig, Formatting.Indented);
                        File.WriteAllText(configPath, jsonContent);
                        Debug.Log("配置文件已更新");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"初始化配置时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取配置文件并返回配置对象
        /// </summary>
        /// <returns>配置对象，如果读取失败则返回null</returns>
        public static ModConfig ReadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string jsonContent = File.ReadAllText(configPath);
                    ModConfig config = JsonConvert.DeserializeObject<ModConfig>(jsonContent);
                    Debug.Log("成功读取配置文件");
                    return config;
                }
                else
                {
                    Debug.LogWarning("配置文件不存在，请先初始化配置");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取配置时发生错误: {ex.Message}");
                return null;
            }
        }
    }
}