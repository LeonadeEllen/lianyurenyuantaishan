# 《与你听潮》客户端工程（原型 v0.1）

Unity 6 LTS · URP · Android / iOS 双平台 · **美术全部留白**（几何体与色块占位）

策划依据：`design/与你听潮_策划案_v0.2.md`、`与你听潮_玩法框架_v0.1.md`、`与你听潮_美术资源清单_v0.1.md`

---

## 快速开始

1. 安装 **Unity 6000.0 LTS**（Unity Hub 里勾选 Android Build Support 与 iOS Build Support 模块）。
2. 用 Unity Hub「Add project from disk」打开本目录。首次打开会自动导入包、生成 `.meta` 和 ProjectSettings。
   - 如果 Hub 提示版本不同，选择你装的 6000.0.x 版本打开即可。
3. 菜单 **与你听潮 → 1. 初始化工程**：设置双平台参数、创建并启用 URP、创建主场景。
4. 点 **Play**。

> 也可以不初始化直接在任意场景点 Play：`GameRoot` 会在运行时自动创建。

### 电脑调试按键
| 按键 | 功能 |
|---|---|
| WASD | 移动 |
| J | 攻击（4 段连击） |
| K / 空格 | 闪避 |
| L（按住） | 格挡 |
| U | 技能 |
| I | 协攻（男主） |
| O | 共鸣技（共鸣值满） |

手机上用屏幕左下的虚拟摇杆和右下的按键。

### 打包
| 平台 | 菜单 | 产物 |
|---|---|---|
| Android 测试包 | 与你听潮 → 打包 → Android APK | `Builds/Android/*.apk` |
| Android 上架包 | 与你听潮 → 打包 → Android AAB | `Builds/Android/*.aab` |
| iOS | 与你听潮 → 打包 → iOS | `Builds/iOS/`（Xcode 工程，需在 Mac 上签名） |

命令行 / CI：
```bash
Unity -batchmode -quit -projectPath . -executeMethod HearTheTide.EditorTools.BuildScript.BuildAndroidApk -logFile -
Unity -batchmode -quit -projectPath . -executeMethod HearTheTide.EditorTools.BuildScript.BuildIos -logFile -
# 加 -development 出开发包
```

上线前必须修改 `Assets/_Project/Scripts/Editor/ProjectSetup.cs` 里的 `CompanyName` 和 `BundleId`。包名上架后不能再改。

---

## 目录结构

```
Assets/_Project/
├─ Scripts/
│  ├─ Core/        纯 C# 游戏逻辑，不引用 UnityEngine（可直接搬到服务器做校验）
│  │  ├─ Tide/       潮汐节奏判定（蓝潮格挡 / 白潮闪避 / 红潮打断）
│  │  ├─ Combat/     共鸣值、属性、伤害公式
│  │  ├─ Cards/      回响卡、情绪属性、心核盘（六角网格 + 情绪连线）
│  │  ├─ Gacha/      抽卡（硬保底 / 十连保底 SR / 限定大保底 / 回响币）
│  │  ├─ Romance/    好感、心语消息、栖所互动
│  │  ├─ Calendar/   潮汐日历（按日期确定，所有玩家一致，每周一次静潮夜）
│  │  ├─ Economy/    钱包、体力恢复
│  │  ├─ Data/       配置定义 + MVP 默认配置（占位文案）
│  │  └─ State/      存档结构 + GameService（所有改动存档的操作都在这里）
│  ├─ Runtime/     Unity 表现层
│  │  ├─ App/        GameRoot 入口、本地存档
│  │  ├─ UI/         代码生成的 uGUI（主界面 + 各系统面板），虚拟摇杆
│  │  └─ Battle/     战斗：女主 / 男主 AI / 蚀影 / 潮音提示 / HUD
│  └─ Editor/      一键初始化、打包脚本
└─ Tests/EditMode/ Core 单元测试（NUnit，Unity Test Runner 和 dotnet 都能跑）
Tools/CoreTests/   不装 Unity 也能跑 Core 测试：cd Tools/CoreTests && dotnet test
```

**分层原则**：规则写在 Core，表现写在 Runtime。以后接服务器时，Core 的同一份代码可以在服务器上跑，用来校验抽卡和战斗结果。

---

## 原型已实现

| 系统 | 内容 |
|---|---|
| 战斗 | 女主操作 + 男主 AI；4 段连击、闪避（3 次充能，有无敌帧）、按住格挡、技能、协攻、共鸣技；蚀影轻 / 重攻击；**潮音提示**（地面圆盘填满即命中，判定窗口内应对触发「回潮」：子弹时间 + 反击 + 共鸣值）；晚按宽限期；波次、Boss、胜负结算 |
| 男主差异 | 沈砚舟控场眩晕 / 陆曜突进爆发 / 闻溪治疗 / 季夙无视防御；协攻优先打断红潮 |
| 心核盘 | 7 格六角盘，随男主等级解锁；情绪相生连线（喜→念→哀→惧→怒→喜），2 连 / 3 连加成，五色「心潮满溢」 |
| 卡牌 | 升级、突破、觉醒、重复卡升星，属性计算；中心主卡决定共鸣技 |
| 抽卡 | 常驻池 + 限定池，70 抽保底、10 抽必出 SR、限定 50% + 大保底，回响币返还 |
| 恋爱 | 心语（消息 + 选项影响好感）、栖所（每日 5 行动点，心情低时触发安慰事件）、约会（受当日潮汐影响地点） |
| 运营框架 | 潮汐日历、每日 5 点换日（UTC+8）、每周发约会券、记忆组集齐解锁、体力恢复 |
| 调试 | 设置页：加钻石、加材料、回满体力、清档 |

## 尚未实现（下一步）

1. **服务器**：账号、存档、抽卡和关卡结算放到服务端（当前是本地 JSON 存档，只适合原型）。
2. **剧情系统**：对话播放器、分支选项、3D 演出时间轴（Timeline）。
3. **配置表**：策划用 Excel 维护，导出 JSON 替换 `DefaultContent.cs`。
4. **美术接入**：角色模型、动画状态机、卡面、UI 预制体替换代码生成的界面（逻辑层不需要改）。
5. **更多敌人**：远程、自爆、多阶段 Boss。
6. **合规**：实名认证与防沉迷、抽卡概率公示、隐私协议。
7. **SDK**：支付（App Store / Google Play / 国内渠道）、推送、数据埋点。

## 美术接入说明

所有占位物体名称都带「(美术留白)」，在 Hierarchy 里搜索就能找到：
- 角色：`Placeholder.Character()` 生成的胶囊体。替换时把模型挂到同一节点下，删除 Body 即可。
- 中文字体：把可商用字体放到 `Assets/_Project/Resources/Fonts/UIFont.ttf`，界面会自动使用。
- 主色调和占位色都定义在 `UIKit`（UI）和 `DefaultContent`（每位男主的专属色）里。
