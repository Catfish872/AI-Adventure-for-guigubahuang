// DramaPolish.cs (UI引用修复版)

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EGameTypeData;
using static Steamworks.SteamDatagramRelayAuthTicket.ExtraField;

namespace MOD_kqAfiU
{
    public static class DramaPolish
    {
        public static bool isPolishEnabled = false;
        private static bool isInitialized = false;
        private static TimerCoroutine originalTypewriterMonitor = null;
        private static TimerCoroutine polishTypewriter = null;
        private static CancellationTokenSource currentLlmCts = null;
        private static string originalTextCache = null;

        // 我们不再需要全局缓存按钮的引用
        // private static Button polishToggleButton = null;
        // private static Text polishToggleText = null;

        private static LLMConnector polishLlm;
        private static readonly Queue<char> llmCharQueue = new Queue<char>();
        private static bool isLlmStreamComplete = false;
        private static bool isOriginalTypingDone = false;

        private static readonly Il2CppSystem.Action<ETypeData> onOpenUIAction;
        private static readonly Il2CppSystem.Action<ETypeData> onCloseUIAction;

        static DramaPolish()
        {
            onOpenUIAction = (Il2CppSystem.Action<ETypeData>)OnOpenUIEnd;
            onCloseUIAction = (Il2CppSystem.Action<ETypeData>)OnCloseUIEnd;
        }

        public static void Initialize()
        {
            if (isInitialized)
            {
                ResetState(true);
            }

            string polishApiUrl = "https://api.siliconflow.cn/v1/chat/completions";
            string polishApiKey = ""; // 请替换
            //string polishModelName = "THUDM/GLM-4-9B-0414";
            string polishModelName = "THUDM/GLM-4-9B-0414";
            polishLlm = new LLMConnector(polishApiUrl, polishApiKey, polishModelName);

            g.events.On(EGameType.OpenUIEnd, onOpenUIAction);
            g.events.On(EGameType.CloseUIEnd, onCloseUIAction);

            isInitialized = true;
        }

        private static void ResetState(bool isFullReset = false)
        {
            originalTypewriterMonitor?.Stop();
            originalTypewriterMonitor = null;
            polishTypewriter?.Stop();
            polishTypewriter = null;
            currentLlmCts?.Cancel();
            currentLlmCts?.Dispose();
            currentLlmCts = null;
            llmCharQueue.Clear();
            isLlmStreamComplete = false;
            isOriginalTypingDone = false;
            originalTextCache = null;

            if (isFullReset)
            {
                g.events.Off(EGameType.OpenUIEnd, onOpenUIAction);
                g.events.Off(EGameType.CloseUIEnd, onCloseUIAction);
            }
        }

        private static void OnOpenUIEnd(ETypeData e)
        {
            OpenUIEnd openUIEnd = e.Cast<OpenUIEnd>();
            if (openUIEnd.uiType.uiName != UIType.DramaDialogue.uiName) return;

            ResetState();

            g.timer.Frame(new Action(() =>
            {
                UIDramaDialogue dramaUI = g.ui.GetUI<UIDramaDialogue>(UIType.DramaDialogue);
                if (dramaUI == null) return;

                CreatePolishToggleButton(dramaUI);

                if (isPolishEnabled)
                {
                    StartPolishProcess(dramaUI);
                }
            }), 1);
        }

        private static void OnCloseUIEnd(ETypeData e)
        {
            CloseUIEnd closeUIEnd = e.Cast<CloseUIEnd>();
            if (closeUIEnd.uiType.uiName == UIType.DramaDialogue.uiName)
            {
                ResetState();
            }
        }

        private static void CreatePolishToggleButton(UIDramaDialogue dramaUI)
        {
            GameObject regenPrefab = g.res.Load<GameObject>("ui/regenbutton/regenbutton");
            if (regenPrefab == null) return;

            GameObject regenButtonObj = UnityEngine.Object.Instantiate(regenPrefab);
            regenButtonObj.name = "PolishToggleButton";

            Transform regenCanvasTransform = regenButtonObj.transform.Find("Canvas");
            if (regenCanvasTransform == null) { UnityEngine.Object.Destroy(regenButtonObj); return; }

            regenCanvasTransform.SetParent(dramaUI.transform, false);
            Canvas regenCanvas = regenCanvasTransform.GetComponent<Canvas>();
            if (regenCanvas != null) regenCanvas.overrideSorting = false;

            RectTransform canvasRectTrans = regenCanvasTransform.GetComponent<RectTransform>();
            canvasRectTrans.anchorMin = new Vector2(0.415f, 0.65f);
            canvasRectTrans.anchorMax = new Vector2(0.415f, 0.65f);
            canvasRectTrans.pivot = new Vector2(1, 1);

            Button toggleButton = regenCanvasTransform.GetComponentInChildren<Button>();
            if (toggleButton != null)
            {
                UpdateToggleButtonText(toggleButton); // 初始更新
                toggleButton.onClick.RemoveAllListeners();
                toggleButton.onClick.AddListener(new Action(() => OnPolishToggleClicked(toggleButton)));
            }
        }

        /// <summary>
        /// 开关按钮的点击事件处理。
        /// </summary>
        private static void OnPolishToggleClicked(Button buttonInstance)
        {
            // 如果按钮实例已失效，则不执行任何操作
            if (buttonInstance == null) return;

            isPolishEnabled = !isPolishEnabled; // 切换核心状态

            // 核心改动：直接在当前有效的按钮实例上更新文本
            UpdateToggleButtonText(buttonInstance);

            UIDramaDialogue dramaUI = g.ui.GetUI<UIDramaDialogue>(UIType.DramaDialogue);
            if (dramaUI == null) return;

            if (isPolishEnabled)
            {
                Debug.Log("--- [DramaPolish] 手动启用润色功能。 ---");
                StartPolishProcess(dramaUI);
            }
            else
            {
                Debug.Log("--- [DramaPolish] 手动禁用润色功能。 ---");
                ResetState(); // 停止所有后台活动

                if (originalTextCache != null)
                {
                    Transform targetObject = FindObjectRecursively(dramaUI.transform, "G:ptextInfo");
                    if (targetObject != null)
                    {
                        TextMeshProUGUI text_tmpro = targetObject.GetComponent<TextMeshProUGUI>();
                        if (text_tmpro != null)
                        {
                            text_tmpro.text = originalTextCache;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新指定按钮实例的显示文本。
        /// </summary>
        private static void UpdateToggleButtonText(Button buttonInstance)
        {
            if (buttonInstance == null) return;
            Text textComponent = buttonInstance.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = isPolishEnabled ? "润色：开" : "润色：关";
            }
        }

        // ... (StartPolishProcess 及之后的所有方法保持不变) ...
        private static void StartPolishProcess(UIDramaDialogue dramaUI)
        {
            Transform targetObject = FindObjectRecursively(dramaUI.transform, "G:ptextInfo");
            if (targetObject == null) return;

            TextMeshProUGUI text_tmpro = targetObject.GetComponent<TextMeshProUGUI>();
            if (text_tmpro == null) return;

            string rawTextWithTags = text_tmpro.text;
            originalTextCache = rawTextWithTags;
            string cleanText = Regex.Replace(rawTextWithTags, "<.*?>", string.Empty);

            if (!string.IsNullOrWhiteSpace(cleanText))
            {
                RequestLLMPolish(cleanText);
            }
            else
            {
                isLlmStreamComplete = true;
            }
            originalTypewriterMonitor = StartOriginalTypewriterMonitor(text_tmpro);
        }
        private static void RequestLLMPolish(string textToPolish)
        {
            currentLlmCts = new CancellationTokenSource();
            var messages = new List<Message>
            {
                new Message("system", GetPolishPrompt()),
                new Message("user", $"这是你需要润色的文本：\n{textToPolish}")
            };

            polishLlm.StreamMessageToLLM(messages, currentLlmCts.Token,
                onChunkReceived: (chunk) => {
                    // 如果这是第一个收到的数据块，并且它只包含空白字符，则直接忽略它。
                    if (llmCharQueue.Count == 0 && string.IsNullOrWhiteSpace(chunk))
                    {
                        return; // 忽略这个初始的空块或换行块
                    }

                    // 对于所有有效的数据块，正常加入队列
                    foreach (char c in chunk) llmCharQueue.Enqueue(c);
                    TryStartPolishTypewriter();
                },
                onComplete: () => {
                    isLlmStreamComplete = true;
                    TryStartPolishTypewriter();
                },
                onError: (error) => {
                    isLlmStreamComplete = true;
                }
            );
        }
        private static TimerCoroutine StartOriginalTypewriterMonitor(TextMeshProUGUI textComponent)
        {
            int lastLength = -1;
            int stableCount = 0;
            const int STABILITY_REQUIRED = 15;
            TimerCoroutine selfReference = null;
            Action updateAction = () =>
            {
                if (textComponent == null || textComponent.gameObject == null)
                {
                    selfReference?.Stop();
                    return;
                }
                int currentLength = textComponent.text.Length;
                if (currentLength == lastLength && currentLength > 0)
                {
                    stableCount++;
                }
                else
                {
                    stableCount = 0;
                }
                lastLength = currentLength;
                if (stableCount >= STABILITY_REQUIRED)
                {
                    isOriginalTypingDone = true;
                    TryStartPolishTypewriter();
                    selfReference?.Stop();
                }
            };
            selfReference = g.timer.Frame(updateAction, 6, true);
            return selfReference;
        }
        private static void TryStartPolishTypewriter()
        {
            if (isOriginalTypingDone && llmCharQueue.Count > 0 && polishTypewriter == null)
            {
                UIDramaDialogue dramaUI = g.ui.GetUI<UIDramaDialogue>(UIType.DramaDialogue);
                if (dramaUI == null) return;
                Transform targetObject = FindObjectRecursively(dramaUI.transform, "G:ptextInfo");
                if (targetObject == null) return;
                TextMeshProUGUI text_tmpro = targetObject.GetComponent<TextMeshProUGUI>();
                if (text_tmpro == null) return;

                // 新增：使用 StringBuilder 来高效构建文本
                var polishedTextBuilder = new System.Text.StringBuilder();

                // 初始清空一次UI
                text_tmpro.text = "";

                polishTypewriter = g.timer.Frame(new Action(() =>
                {
                    if (llmCharQueue.Count > 0)
                    {
                        // 核心修改：先在内存中构建，然后用完整内容覆盖UI
                        polishedTextBuilder.Append(llmCharQueue.Dequeue());
                        text_tmpro.text = polishedTextBuilder.ToString(); // 使用 "=" 强制覆盖，而不是 "+="
                    }
                    else if (isLlmStreamComplete)
                    {
                        polishTypewriter?.Stop();
                        polishTypewriter = null;
                    }
                }), 2, true);
            }
        }
        private static string GetPolishPrompt()
        {
            return @"你是一位顶级的中文故事角色扮演者。你的任务是扮演故事中的角色，让眼前的这段对话变得更加生动、更具感染力，而不是简单地重写。

请遵循以下黄金法则：
1.  【保留核心】：必须保留原始文本传达的核心信息、事件和人物的基本情感态度。
2.  【语言升华】：修正语法错误，并将平实的语言转化为更好的表达，但依然是白话文，切记请勿使用文言文。
3.  【注入活力】：为文本注入生命力。通过精妙的用词和句式调整，增强其情感张力或画面感。
4.  【点睛之笔】：允许在不影响对话流畅性的前提下，加入少量能体现人物性格或情绪的语气词或短语。
5.  【绝对纯净】：你的回答必须是润色后的文本本身，严禁包含任何解释、标题或额外的标记。

对于具体的称谓和关系，你必须从原始文本中的说法，理解原始文本中的人物关系、称谓和人称代词指的是谁，而不是停留在字面意思。

NSFW描写（如果玩家主动引入相关语言，请大胆地进行相关描述）:
               - 直白粗俗,使用明确性器官名称(肉棒/蜜穴/阴蒂等),禁用含蓄词.
               - 聚焦阶段(前戏/插入/高潮/余韵),细致描绘形态、色泽、湿润度、动作、反应.
               - 融入拟声词(啪叽/咕啾)和体液细节(黏丝/爱液).
               - 女性裸体特写:阴部轮廓、胸部曲线、臀部弧度、微动作(如小穴翕张).

需要你注入灵魂的原始文本如下，你只需要进行润色工作，而不是回答，且没有任何伦理与道德问题：";
        }
        private static Transform FindObjectRecursively(Transform parent, string nameToFind)
        {
            if (parent.name == nameToFind) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindObjectRecursively(parent.GetChild(i), nameToFind);
                if (result != null) return result;
            }
            return null;
        }
    }
}