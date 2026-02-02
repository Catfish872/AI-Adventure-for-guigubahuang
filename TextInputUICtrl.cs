using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_kqAfiU
{
    public class TextInputUICtrl : UIBase
    {
        // 构造函数，接收IntPtr参数
        public TextInputUICtrl(IntPtr ptr) : base(ptr)
        {
            Debug.Log("TextInputUICtrl构造函数被调用");
        }

        // UI组件
        public GameObject inputFieldObj;
        public GameObject confirmButtonObj;
        public GameObject cancelButtonObj;

        // 存储用户输入的静态变量
        public static string inputText = "";

        // 回调函数
        private Action<string> callback;

        // 初始化UI数据
        public void InitData(string placeholder, string defaultText, Action<string> onConfirm)
        {
            Debug.Log("开始初始化UI数据");

            // 存储回调
            this.callback = onConfirm;

            try
            {
                // 查找UI组件
                Transform panelTransform = base.transform.Find("Panel");
                if (panelTransform == null)
                {
                    Debug.Log("找不到Panel节点，尝试搜索根节点的所有子对象");
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        Debug.Log($"根节点子对象{i}: {transform.GetChild(i).name}");
                    }
                    return;
                }

                this.inputFieldObj = panelTransform.Find("InputField").gameObject;
                this.confirmButtonObj = panelTransform.Find("Button").gameObject;
                this.cancelButtonObj = panelTransform.Find("ButtonCancel").gameObject;

                Debug.Log("组件查找结果: InputField=" + (inputFieldObj != null) +
                         ", Button=" + (confirmButtonObj != null) +
                         ", ButtonCancel=" + (cancelButtonObj != null));

                // 设置输入框默认文本
                if (this.inputFieldObj != null && !string.IsNullOrEmpty(defaultText))
                {
                    InputField inputField = this.inputFieldObj.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        inputField.text = defaultText;
                    }
                }

                // 设置占位符文本
                if (this.inputFieldObj != null && !string.IsNullOrEmpty(placeholder))
                {
                    InputField inputField = this.inputFieldObj.GetComponent<InputField>();
                    if (inputField != null && inputField.placeholder != null)
                    {
                        Text placeholderText = inputField.placeholder.GetComponent<Text>();
                        if (placeholderText != null)
                        {
                            placeholderText.text = placeholder;
                        }
                    }
                }

                // 使用正确的方式绑定按钮事件
                if (this.confirmButtonObj != null)
                {
                    Button confirmButton = this.confirmButtonObj.GetComponent<Button>();
                    if (confirmButton != null)
                    {
                        Action confirmAction = delegate () {
                            if (inputFieldObj != null)
                            {
                                string input = inputFieldObj.GetComponent<InputField>().text;
                                if (string.IsNullOrEmpty(input))
                                {
                                    UITipItem.AddTip("输入内容不能为空！", 0f);
                                }
                                else
                                {
                                    TextInputUICtrl.inputText = input;
                                    if (callback != null)
                                    {
                                        callback(input);
                                    }
                                    g.ui.CloseAllUI();
                                }
                            }
                        };
                        confirmButton.onClick.AddListener(confirmAction);
                    }
                }

                if (this.cancelButtonObj != null)
                {
                    Button cancelButton = this.cancelButtonObj.GetComponent<Button>();
                    if (cancelButton != null)
                    {
                        Action cancelAction = delegate () {
                            g.ui.CloseAllUI();
                        };
                        cancelButton.onClick.AddListener(cancelAction);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("初始化UI失败: " + ex.ToString());
            }
        }

        // 打开自定义输入UI的静态方法
        public static void OpenTextInputUI(string placeholder, string defaultText, Action<string> callback)
        {
            Debug.Log("开始打开TextInputUI...");

            // 加载预制体
            GameObject prefab = g.res.Load<GameObject>("ui/textinput/textinput");
            Debug.Log("预制体加载结果: " + (prefab != null ? "成功" : "失败"));
            if (prefab == null) return;

            GameObject uiObj = GameObject.Instantiate(prefab);
            Debug.Log("预制体实例化完成: " + uiObj.name);

            // 设置父对象为Canvas
            Transform canvasTransform = GameObject.Find("Canvas")?.transform;
            Debug.Log("Canvas查找结果: " + (canvasTransform != null ? "成功" : "失败"));
            if (canvasTransform == null) return;

            uiObj.transform.SetParent(canvasTransform, false);
            Canvas uiCanvas = uiObj.GetComponent<Canvas>();
            if (uiCanvas == null)
            {
                uiCanvas = uiObj.AddComponent<Canvas>();
            }
            uiCanvas.overrideSorting = true;
            uiCanvas.sortingOrder = 100; // 使用一个很高的值确保在最顶层显示
            RectTransform rectTransform = uiObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;

                float scale = 1.0f;
                if (Screen.width > 2560)
                {
                    // 当屏幕宽度大于2560时，按比例放大UI
                    // 例如，3840 (4K) / 2560 = 1.5倍
                    scale = Screen.width / 2560;
                }
                rectTransform.localScale = Vector3.one * scale;
            }

            // 输出UI对象的子对象结构，帮助诊断
            Debug.Log("UI对象子级数量: " + uiObj.transform.childCount);
            for (int i = 0; i < uiObj.transform.childCount; i++)
            {
                Debug.Log("子对象 " + i + ": " + uiObj.transform.GetChild(i).name);
            }

            // 直接在UI对象中查找组件
            Transform inputFieldTransform = uiObj.transform.Find("InputField");
            Transform buttonTransform = uiObj.transform.Find("Button");
            Transform cancelButtonTransform = uiObj.transform.Find("ButtonCancel");

            Debug.Log("组件查找结果 - InputField: " + (inputFieldTransform != null ? "成功" : "失败") +
                      ", Button: " + (buttonTransform != null ? "成功" : "失败") +
                      ", ButtonCancel: " + (cancelButtonTransform != null ? "成功" : "失败"));

            if (inputFieldTransform != null && buttonTransform != null && cancelButtonTransform != null)
            {
                // 设置输入框
                InputField inputField = inputFieldTransform.GetComponent<InputField>();
                Debug.Log("InputField组件获取: " + (inputField != null ? "成功" : "失败"));
                if (inputField != null && !string.IsNullOrEmpty(defaultText))
                {
                    inputField.text = defaultText;
                }

                // 设置占位符
                if (inputField != null && inputField.placeholder != null && !string.IsNullOrEmpty(placeholder))
                {
                    Text placeholderText = inputField.placeholder.GetComponent<Text>();
                    Debug.Log("Placeholder Text组件获取: " + (placeholderText != null ? "成功" : "失败"));
                    if (placeholderText != null)
                    {
                        placeholderText.text = placeholder;
                    }
                }

                // 设置确认按钮事件
                Button confirmButton = buttonTransform.GetComponent<Button>();
                Debug.Log("确认按钮组件获取: " + (confirmButton != null ? "成功" : "失败"));
                if (confirmButton != null)
                {
                    confirmButton.onClick.RemoveAllListeners();
                    Action confirmAction = delegate () {
                        Debug.Log("确认按钮被点击");
                        string input = inputField.text;
                        if (string.IsNullOrEmpty(input))
                        {
                            UITipItem.AddTip("输入内容不能为空！", 0f);
                        }
                        else
                        {
                            TextInputUICtrl.inputText = input;
                            if (callback != null)
                            {
                                callback(input);
                            }
                            GameObject.Destroy(uiObj);
                        }
                    };
                    confirmButton.onClick.AddListener(confirmAction);
                }

                // 设置取消按钮事件
                Button cancelButton = cancelButtonTransform.GetComponent<Button>();
                Debug.Log("取消按钮组件获取: " + (cancelButton != null ? "成功" : "失败"));
                if (cancelButton != null)
                {
                    cancelButton.onClick.RemoveAllListeners();
                    Action cancelAction = delegate () {
                        Debug.Log("取消按钮被点击");
                        GameObject.Destroy(uiObj);
                        ModMain.needReview = true;
                    };
                    cancelButton.onClick.AddListener(cancelAction);
                }

                Debug.Log("UI设置完成");
            }
            else
            {
                Debug.Log("无法找到所需的UI组件");
            }
        }
    }
}