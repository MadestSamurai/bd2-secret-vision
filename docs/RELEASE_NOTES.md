# BD2 Secret Vision v0.1.8

## 简体中文

### 更新内容

- 修复结算回执缺失后组件长期无法切换的问题；恢复时按真实网络状态继续，不虚报通关。
- 改善本地设置文件短暂占用时的保存，并同步统一宿主连接和语言支持。

### 下载

适用于 Windows x64。两版功能相同，均为单 EXE，内置简体中文／English。

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| **Portable** | 内置 .NET 运行时 | 首次使用推荐，下载即用 |
| **Lite** | 需安装 .NET Desktop Runtime 8 x64 | 已安装运行时，下载更小 |

下载一种版本即可。ZIP 附中英文说明和许可证；SHA256SUMS.txt 可用于核对下载文件。

### 升级

暂停并关闭旧工具，再打开新版连接；本机设置保留。当前组件支持在游戏保持运行时更新和交接；如游戏本身仍显示断线或登录提示，请先恢复游戏连接。

[使用说明与风险声明](https://github.com/MadestSamurai/bd2-secret-vision/blob/main/README.md)

## English

### Changes

- Fixed missing settlement responses blocking component switching indefinitely; recovery follows the real network state without inventing a completed stage.
- Improved settings saves during brief file locks, shared-host connections and language integration.

### Downloads

For Windows x64. Both editions have the same features, run as a single EXE and include Simplified Chinese / English.

| Edition | Runtime requirement | Recommended for |
| --- | --- | --- |
| **Portable** | .NET included | Most users; download and run |
| **Lite** | .NET Desktop Runtime 8 x64 required | Smaller download if the runtime is installed |

Download one edition. ZIPs include both READMEs and licenses. Use SHA256SUMS.txt to verify downloaded files.

### Upgrade

Pause and close the old assistant, then connect with the new version. Local settings are retained. Current components support updates and handoff while the game stays open. If the game itself is disconnected or asking you to log in, restore its connection first.

[Usage and risk disclaimer](https://github.com/MadestSamurai/bd2-secret-vision/blob/main/README.en.md)

---

# BD2 Secret Vision v0.1.6

## 简体中文

### 更新内容

- 首次独立开源发布，支持选关、80%／100%目标、失败自动重试和实时盘面显示。
- 优先接近外边界、沿外围推进再闭合；加速道具按绕行成本与剩余收益选择。
- 根据实际攻击状态安排出发，处理残缺边线和逼近的闪电，减少无效往返与空等。
- 成功结算保留 8 秒，再切换关卡或返回首页；期间可立即停止，失败重试速度不变。
- 内置简体中文／English，保留本地设置；主窗口提供免费来源署名与说明。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| **Portable** | 自带 .NET，无需另装运行库 | 大多数用户 |
| **Lite** | 需要 [.NET Desktop Runtime 8 x64](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) | 已安装桌面运行库、希望减小下载体积 |

适用于 Windows x64。两版功能相同，EXE 可独立使用；ZIP 附带双语说明与许可证。用 `SHA256SUMS.txt` 核对下载。无需 Python、开发 SDK 或其他 BD2 工具。

### 升级

正常启动游戏并进入 SECRET VISION 首页，打开工具连接，选好关卡后开始。从本地 0.1.4／0.1.5 升级，只需停止并关闭旧助手，再打开新版；已有连接和设置可以继续使用。更早版本需要正常重启游戏一次。

作者发布版免费。GitHub **MadestSamurai**／B站 **MadSamurai**。第三方收费不代表作者参与、背书或提供服务。[使用说明与风险提示](https://github.com/MadestSamurai/bd2-secret-vision/blob/main/README.md)。

## English

### Changes

- First standalone open-source release, with stage selection, 80% / 100% targets, automatic failed-attempt retries and a live board view.
- Prioritizes reaching and extending the outer rim before closing it. Optional speed pickups account for detour cost and remaining benefit.
- Uses current attack state for departures and handles damaged edges and approaching lightning to reduce wasted movement and waiting.
- Keeps successful results visible for 8 seconds before advancing or returning home. Stop remains immediate; failed-attempt retry timing is unchanged.
- Includes Simplified Chinese / English, persistent preferences and free-release attribution in the main window.

### Downloads

| Build | Runtime | Recommended for |
| --- | --- | --- |
| **Portable** | Includes .NET; no separate runtime needed | Most users |
| **Lite** | Requires [.NET Desktop Runtime 8 x64](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) | Smaller download when the desktop runtime is installed |

For Windows x64. Both builds have identical features. EXEs run independently; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`. No Python, development SDK or other BD2 tools are required.

### Upgrade

Start the game normally and open the SECRET VISION home screen. Connect the assistant, select stages and start. To upgrade from local 0.1.4 / 0.1.5 builds, stop and close the old assistant, then open this version. Existing connections and preferences are retained. Earlier builds require one normal game restart.

Official releases are free. GitHub **MadestSamurai** / Bilibili **MadSamurai**. Third-party fees do not imply the author's involvement, endorsement or support. [Usage and risk notice](https://github.com/MadestSamurai/bd2-secret-vision/blob/main/README.en.md).
