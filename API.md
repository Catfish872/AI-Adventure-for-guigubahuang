我已阅读并理解了您提供的文档。下面是润色和细化后的鬼谷八荒Mod开发API指南，从全局对象开始：

# 鬼谷八荒Mod开发API指南

## 1. 全局对象 `g`

`g`是游戏的全局访问入口，包含多个管理器：

### g.world - 世界管理器

- 管理游戏世界实例，如宗门、城镇、事件、NPC等
- 获取玩家单位：`g.world.playerUnit`
- 获取NPC：`g.world.unit.GetUnit("NPC_ID")`
- 获取所有NPC：`g.world.unit.GetUnits(true)`

### g.timer - 计时器管理器

- 创建帧定时器：`g.timer.Frame(Action回调, 帧数, 是否循环)`
- 创建时间定时器：`g.timer.Time(Action回调, 秒数, 是否循环)`
- 停止定时器：`g.timer.Stop(定时器实例)`

### g.events - 事件管理器

- 监听事件：`g.events.On(事件类型, 回调函数, 优先级, 是否只执行一次)`
- 取消监听：`g.events.Off(事件类型, 回调函数)`
- 事件分类：`EGameType`(通用)、`EMapType`(大地图)、`EBattleType`(战斗)

### g.res - 资源管理器

- 加载资源：`g.res.Load<类型>("资源路径")`
- 例如：`g.res.Load<GameObject>("Effect/Battle/Skill/jueyingjian")`

### g.sounds - 声音管理器

- 播放效果音：`g.sounds.PlayEffect("音效路径", 音量, null, null, 是否循环)`
- 播放背景音：`g.sounds.PlayBG("背景音乐路径", 音量, null, null, 是否循环)`

### g.conf - 配置管理器

- 获取游戏配置数据：`g.conf.配置分类.GetItem(id)`
- 例如：`g.conf.roleGrade.GetGradeItem(1, 1)`

### g.data - 数据管理器

- 获取存档数据：`g.data.unit.GetUnit(g.data.world.playerUnitID)`

### g.ui - UI管理器

- 打开UI：`g.ui.OpenUI(UIType.类型)`
- 打开泛型UI：`g.ui.OpenUI<UI类型>(UIType.类型)`
- 关闭UI：`g.ui.CloseUI(UIType.类型)`

### g.root - 根GameObject

- 添加组件：`g.root.AddComponent<组件类型>()`

## 2. 计时器系统 (Timer)

### 帧定时器

```csharp
// 每1帧执行一次，执行10次后停止
TimerCoroutine cor1 = null;
int frameCount = 0;
cor1 = g.timer.Frame(new Action(() => {
    frameCount++;
    if (frameCount >= 10) {
        g.timer.Stop(cor1);
    }
}), 1, true);
```

### 时间定时器

```csharp
// 延迟2秒执行一次
g.timer.Time(new Action(() => {
    Debug.Log("延迟2秒后执行");
}), 2f, false);

// 每3秒执行一次，无限循环
TimerCoroutine cor2 = g.timer.Time(new Action(() => {
    Debug.Log("每3秒执行一次");
}), 3f, true);
```

## 3. 事件系统 (Events)

### 监听事件

```csharp
// 创建事件回调
Action<ETypeData> onBattleStart = new Action<ETypeData>((e) => {
    Debug.Log("战斗开始");
});

// 监听战斗开始事件
g.events.On(EBattleType.BattleStart, onBattleStart, 0, false);
```

### 取消监听

```csharp
// 取消监听
g.events.Off(EBattleType.BattleStart, onBattleStart);
```

### 常用事件类型

```csharp
// 游戏通用事件
EGameType.IntoWorld      // 进入游戏世界
EGameType.OpenUIEnd      // UI打开完成
EGameType.SaveData       // 保存数据
EGameType.WorldRunStart  // 世界运行开始
EGameType.WorldRunEnd    // 世界运行结束

// 战斗事件
EBattleType.BattleStart  // 战斗开始
EBattleType.UnitHitDynIntHandler // 单位受击事件
```

### 事件处理示例

```csharp
// 战斗受击事件处理
private void OnUnitHitDynIntHandler(ETypeData e)
{
    // 转换事件数据为具体类型
    UnitHitDynIntHandler edata = e.Cast<UnitHitDynIntHandler>();
    
    // 获取被击中的单位名称
    Debug.Log("战斗中被击的单位：" + edata.hitUnit.data.name);
    
    // 修改伤害值为0
    edata.dynV.baseValue = 0;
    edata.dynV.ClearCall();
}
```

## 4. 世界系统 (World)

### 玩家单位访问

```csharp
// 获取玩家名称
string playerName = g.world.playerUnit.data.unitData.propertyData.GetName();

// 获取玩家位置
Vector2Int playerPos = g.world.playerUnit.data.unitData.GetPoint();

// 获取玩家境界
int playerGrade = g.world.playerUnit.data.unitData.propertyData.grade;
```

### NPC操作

```csharp
// 获取指定ID的NPC
WorldUnitBase npc = g.world.unit.GetUnit("NPCID_某人");

// 获取所有NPC
foreach (WorldUnitBase unit in g.world.unit.GetUnits(true)) {
    string name = unit.data.unitData.propertyData.GetName();
    Debug.Log("NPC名称: " + name);
}
```

### 单位动作

```csharp
// 添加气运行为
g.world.playerUnit.CreateAction(new UnitActionLuckAdd(120));

// 双人交互行为 - 论道
WorldUnitBase npc = g.world.unit.GetUnit("NPCID_某人");
npc.CreateAction(new UnitActionRoleTrains(g.world.playerUnit));
```

### 地图事件

```csharp
// 在玩家位置创建山洞事件
g.world.mapEvent.AddGridEvent(g.world.playerUnit.data.unitData.GetPoint(), 6);
```

### 进入副本

```csharp
// 进入练气后期副本(ID=1011, 级别=5)
g.world.battle.IntoBattle(new DataMap.MonstData() { id = 1011, level = 5 });
```

### 条件判断

```csharp
// 判断玩家是否为练气期
bool isQiPractitioner = UnitConditionTool.Condition("grade_0_1_1", 
    new UnitConditionData(g.world.playerUnit, null));
```

## 5. 剧情系统 (Drama)

### 打开剧情

```csharp
// 打开剧情ID为610011的对话
DramaTool.OpenDrama(610011, new DramaData() { 
    unitLeft = g.world.playerUnit,  // 左侧角色
    unitRight = npc                 // 右侧角色
});
```

### 自定义剧情

```csharp
// 创建自定义剧情
UICustomDramaDyn drama = new UICustomDramaDyn(610011);

// 设置对话文本
drama.dramaData.dialogueText[610011] = "这是一段对话文本";

// 添加选项
drama.dramaData.dialogueOptions[6100111] = "选项1";
drama.dramaData.dialogueOptionsAddText[1001] = "额外增加的按钮";

// 设置选项回调
Action onOptionClick = () => {
    Debug.Log("选项被点击");
};
drama.SetOptionCall(6100111, onOptionClick);

// 设置对话角色
drama.dramaData.unitLeft = g.world.playerUnit;
drama.dramaData.unitRight = npc;

// 打开剧情UI
drama.OpenUI();
```

### 剧情命令

```csharp
// 执行打开玩家信息UI的剧情命令
DramaFunctionTool.OptionsFunction("openUI_" + UIType.PlayerInfo.uiName);
```

## 6. 战斗系统 (Battle)

### 创建友方NPC

```csharp
// 获取NPC数据
WorldUnitBase npc = g.world.unit.GetUnit("NPCID_某人");

// 创建友方NPC在战斗场景
UnitCtrlHumanNPC battleNPC = SceneType.battle.unit.CreateUnitHuman<UnitCtrlHumanNPC>(
    npc.data, UnitType.PlayerNPC);
    
// 设置NPC位置到场景中心
battleNPC.move.SetPosition(SceneType.battle.battleMap.roomCenterPosi);
```

### 创建敌方怪物

```csharp
// 创建ID为7210的怪物到战斗场景
SceneType.battle.unit.CreateUnitMonst(7210, 
    SceneType.battle.battleMap.roomCenterPosi, UnitType.Monst);
```

### 添加战斗效果

```csharp
// 给玩家添加效果ID为51612的效果
SceneType.battle.battleMap.playerUnitCtrl.AddEffect(51612, 
    SceneType.battle.battleMap.playerUnitCtrl, new SkillCreateData());
```

### 副本命令

```csharp
// 在副本中创建怪物的命令
BattleFunctionTool.OptionsFunction("createMonst_7210_0_0_3");
```

我将继续补充和完善剩余的API指南内容。您希望我继续详细说明哪些部分？

我将继续补充和完善API指南，确保只包含文档中存在的真实可用API。

## 7. UI系统 (UI)

### 打开UI

```csharp
// 打开常规UI
g.ui.OpenUI(UIType.PlayerInfo);

// 打开泛型UI，可指定类型和初始化数据
g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("提示", "这是有两个按钮的提示框", 2, 
    new Action(() => { Debug.Log("点击了确定"); }), null);
```

### 提示框

```csharp
// 显示普通提示信息
UITipItem.AddTip("提示信息", 0f);  // 持续时间为0表示使用默认时间
UITipItem.AddTip("提示信息", 3f);  // 持续3秒

// 显示确认对话框
g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("提示", "确认框内容", 2, 
    new Action(() => { Debug.Log("点击了确定"); }), null);
```

### 文本信息界面

```csharp
// 显示普通文本信息
g.ui.OpenUI<UITextInfo>(UIType.TextInfo).InitData("提示", "这是文本信息", "", null);

// 显示长文本信息
g.ui.OpenUI<UITextInfoLong>(UIType.TextInfoLong).InitData("提示", "这是很长的文本信息", "", null, false);
```

### 自定义UI

```csharp
// 打开自定义UI
g.ui.OpenUI(new UIType.UITypeBase("UI预制体名称", UILayer.UI));

// 关闭所有UI
SceneType.map.world.CloseAllUI(false, null);
```

## 8. 资源和配置 (Resources & Config)

### 资源加载

```csharp
// 加载预制体
GameObject goEffect = g.res.Load<GameObject>("Effect/Battle/Skill/jueyingjian");

// 播放音效(注意：不需要Effect文件夹前缀)
g.sounds.PlayEffect("Battle/jineng/jian/jueyingjian", 1, null, null, true);
```

### 配置访问

```csharp
// 获取境界配置(练气前期)
ConfRoleGradeItem gradeItem = g.conf.roleGrade.GetGradeItem(1, 1);
string gradeName = GameTool.LS(gradeItem.gradeName);

// 遍历配置表
foreach (ConfItemPropsItem item in g.conf.itemProps._allConfList) {
    string itemName = GameTool.LS(item.name);
    // 处理每个道具配置
}
```

### 工具函数

```csharp
// 获取国际化文本
string text = GameTool.LS("common_tishi");  // "提示"文本的国际化

// 生成随机数 (0-9范围)
int randomValue = CommonTool.Random(0, 10, null);
```

## 9. 物品和道具系统 (Items & Props)

### 创建物品

```csharp
// 创建新物品(ID为10001，数量为5)
DataProps.PropsData propsData = DataProps.PropsData.NewProps(10001, 5);
```

### 物品操作

```csharp
// 获取指定ID的物品
List<DataProps.PropsData> props = npc.data.unitData.propData.GetProps(10001);

// 遍历单位的所有物品
foreach (DataProps.PropsData prop in npc.data.unitData.propData.allProps) {
    // 获取物品名称
    string propName = prop.propsInfoBase.name;
    // 获取物品数量
    int count = prop.propsCount;
}

// 删除物品
npc.data.unitData.propData.DelProps(props[0].soleID, props[0].propsCount);

// 清空所有物品
npc.data.unitData.propData.ClearAllProps();
```

### 物品复制

```csharp
// 克隆物品数据
DataProps.PropsData clonedProp = originalProp.Clone();
```

## 10. NPC数据操作 (NPC Data)

### 基础信息

```csharp
// 获取NPC姓名
string npcName = npc.data.unitData.propertyData.GetName();

// 修改NPC属性
npc.data.unitData.propertyData.beauty = 900;  // 设置美貌值

// 设置年龄 (单位为月)
npc.data.unitData.propertyData.age = 16 * 12;  // 16岁
```

### 关系数据

```csharp
// 获取亲密度
int intimacy = npc.data.unitData.relationData.GetIntim(g.world.playerUnit.data.unitData.unitID);

// 获取所有好感关系的NPC
List<string> goodRelations = g.world.playerUnit.data.unitData.relationData.GetAllGoodRelationUnitID(true, false);

// 添加亲属关系
g.world.playerUnit.data.unitData.relationData.children.Add(npc.data.unitData.unitID);  // 添加为子女
npc.data.unitData.relationData.parent[0] = g.world.playerUnit.data.unitData.unitID;    // 设置父母
```

### 自定义数据

```csharp
// 保存字符串数据
npc.data.unitData.objData.SetString("Messages", jsonData);

// 读取字符串数据
string value = npc.data.unitData.objData.GetString("Messages");

// 保存整数数据
npc.data.unitData.objData.SetInt("isLoadProp", 1);

// 读取整数数据
int value = npc.data.unitData.objData.GetInt("isLoadProp");

// 检查是否存在键
bool exists = npc.data.unitData.objData.ContainsKey("Messages");

// 删除数据
npc.data.unitData.objData.DelString("Messages");
```

## 11. 信件系统 (Letter System)

### 发送信件

```csharp
// 准备信件内容
string[] letterContent = new string[] { "这是信件内容" };

// 准备附带物品
List<DataProps.PropsData> items = new List<DataProps.PropsData>();
items.Add(DataProps.PropsData.NewProps(10001, 100));  // 添加100灵石

// 发送信件(类型ID为99990001)
g.data.world.AddLetter(npc, 99990001, letterContent, items);
```

### 获取游戏时间

```csharp
// 获取游戏内日期
int year = g.world.run.roundMonth / 12 + 1;
int month = g.world.run.roundMonth % 12 + 1;
int day = g.world.run.roundDay + 1;
string date = $"[{year}年{month}月{day}日]";
```

## 12. 自定义MonoBehaviour类

### 创建自定义组件

```csharp
// 自定义MonoBehaviour类
public class ExampleTestMono : MonoBehaviour
{
    // 必须有这行代码，否则无法AddComponent
    public ExampleTestMono(IntPtr ptr) : base(ptr) { }

    void Update()
    {
        Debug.Log(Time.frameCount + "，每帧打印");
        Console.WriteLine(Time.frameCount + "，每帧打印");
    }
}
```

### 注册和使用

```csharp
// 注册IL2CPP类型
ClassInjector.RegisterTypeInIl2Cpp<ExampleTestMono>();

// 添加到GameObject
g.root.AddComponent<ExampleTestMono>();
```

## 13. 战斗场景管理 (Scene Management)

### 场景访问

```csharp
// 战斗场景访问
SceneType.battle    // 战斗场景访问入口
SceneType.map       // 大地图场景访问入口

// 获取战斗场景中心位置
Vector2 centerPos = SceneType.battle.battleMap.roomCenterPosi;
```

### 战斗单位获取

```csharp
// 获取玩家战斗单位
UnitCtrlBase playerUnit = SceneType.battle.battleMap.playerUnitCtrl;

// 获取所有战斗单位
List<UnitCtrlBase> allUnits = SceneType.battle.unit.GetAllUnit(false);
```

## 14. MOD初始化和销毁

### MOD生命周期

```csharp
// MOD初始化
public void Init()
{
    // 创建Harmony实例
    if (harmony == null)
    {
        harmony = new Harmony("MOD_名称");
    }
    
    // 注册所有补丁
    harmony.PatchAll(Assembly.GetExecutingAssembly());
    
    // 注册事件监听
    intoWorldCall = new Action<ETypeData>(OnIntoWorld);
    g.events.On(EGameType.IntoWorld, intoWorldCall, 0, false);
    
    // 其他事件监听
    saveDataCall = new Action<ETypeData>(OnSaveData);
    g.events.On(EGameType.SaveData, saveDataCall, 0, false);
}

// MOD销毁
public void Destroy()
{
    // 取消事件监听
    g.events.Off(EGameType.IntoWorld, intoWorldCall);
    g.events.Off(EGameType.SaveData, saveDataCall);
    
    // 取消其他事件监听
    // ...
}
```

### 异步处理

```csharp
// 使用异步处理
g.timer.Time(async delegate()
{
    // 异步处理代码
    await Task.Delay(100);  // 示例延迟
    
    // 其他处理逻辑
}, 0f, false);
```

## 15. JSON数据处理

### 序列化与反序列化

```csharp
// 将对象序列化为JSON字符串
string jsonStr = JsonConvert.SerializeObject(data);

// 将JSON字符串反序列化为对象
SomeData data = JsonConvert.DeserializeObject<SomeData>(jsonStr);
```

### 保存与加载MOD数据

```csharp
// 保存MOD数据到全局数据
g.data.dataObj.data.SetString("apiUrl", "https://example.com/api");
g.data.dataObj.data.SetString("apiKey", "your_api_key");

// 读取MOD数据
if (g.data.dataObj.data.ContainsKey("apiUrl"))
{
    string apiUrl = g.data.dataObj.data.GetString("apiUrl");
}
```

## 16. 游戏扩展函数

### 境界与属性

```csharp
// 获取境界名称
string gradeName = MartialUtil.getGradeName(1);  // 获取境界1的名称

// 判断玩家是否为某个境界
bool isFirstGrade = UnitConditionTool.Condition("grade_0_1_1", 
    new UnitConditionData(g.world.playerUnit, null));
```

### 判断字符串

```csharp
// 判断字符串是否为空
bool isEmpty = StringExtensions.IsEmpty(someString);

// 判断字符串是否为数字
bool isNumber = StringExtensions.IsInt(someString);
```

以上是基于您提供的文档整理出的可用API和示例。如果您需要对特定部分进行更详细的说明，或有其他方面需要补充，请告诉我。