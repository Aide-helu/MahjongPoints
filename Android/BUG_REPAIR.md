# BUG 修理文档

每次处理 BUG、构建失败、运行异常或不符合预期的行为前，先阅读本文件。

## 修理流程

1. 先确认问题
   - 阅读完整错误信息。
   - 记录触发问题的命令、IDE 操作、文件、目标或运行路径。
   - 能用命令复现时，先用最小命令复现。

2. 再定位原因
   - 查找失败值、配置项或调用链的来源。
   - 先和项目里能正常工作的写法对比。
   - 不只修表面报错，要修导致问题的根因。

3. 最小修复
   - 只改解决根因所需的最少文件。
   - 不做无关重构。
   - 不为小问题新增依赖。

4. 验证结果
   - 运行能证明修复有效的最小构建、测试或手动验证命令。
   - 读完输出后再说明结果。
   - 如果验证失败，回到原因定位，不叠加猜测式修复。

5. 询问确认
   - 修复并验证后，先询问用户：“这个 BUG 修好了吗？”
   - 只有用户回复修好了，才能写入或更新下面的修理记录。

## 修理记录模板

### 问题是什么

写清楚用户看到的错误、异常表现或失败命令。

### 问题原因

写清楚根因，不只写表面报错。

### 解决方案

写清楚改了什么、为什么这样改、用什么命令验证通过。

## 修理记录

### 2026-07-12：缺少 aapt2.exe 导致 Android 构建失败

### 问题是什么

Rider 构建 `MahjongPoints.Android` 时失败：

```text
Microsoft.Android.Sdk.Tooling.targets(35,5): Error XA5205 : 找不到“aapt2.exe”。请使用 `D:\Android\Emulator\Sdk\tools\android.bat` 程序安装 Android SDK 生成工具包。
```

### 问题原因

Rider 和构建环境已经指向 `D:\Android\Emulator\Sdk`，但这个 SDK 当时只安装了模拟器相关组件，没有安装 Android SDK Platform 和 Build-Tools。`aapt2.exe` 位于 Build-Tools 目录中，因此构建找不到该工具。

### 解决方案

向同一个 D 盘 SDK 补装构建所需组件：

```powershell
D:\Android\Emulator\Sdk\cmdline-tools\latest\bin\sdkmanager.bat --sdk_root=D:\Android\Emulator\Sdk "platforms;android-36" "build-tools;36.0.0"
```

验证结果：

```text
D:\Android\Emulator\Sdk\build-tools\36.0.0\aapt2.exe 存在
D:\Android\Emulator\Sdk\platforms\android-36\android.jar 存在
dotnet build .\MahjongPoints.Android.csproj -p:AndroidSdkDirectory=D:\Android\Emulator\Sdk 成功
0 个警告，0 个错误
```

### 2026-07-12：Rider 中 Pixel 5 API 36 显示 unavailable

### 问题是什么

Rider 中选择 `Pixel 5 API 36` 时显示 `unavailable`，无法作为 Android 调试目标使用。

### 问题原因

D 盘 Android AVD 目录中已创建 `Pixel_5_API_36` 后，命令行可以启动模拟器，但 Rider 仍显示 `unavailable`。根因有两个：

1. 普通用户的 Android 环境变量没有真正写入当前用户的 `HKCU\Environment`，Rider 不是从 Codex shell 启动时读不到 `D:\Android\Emulator` 下的 SDK/AVD。
2. Rider 项目的部署目标配置仍锁定到 `D:\Android\Emulator\Avd\Pixel_6_API_36.avd`，不是用户要运行的 `Pixel_5_API_36`。

### 解决方案

使用 D 盘 SDK 创建对应 AVD，并修正 Rider 可见的用户环境变量和部署目标：

```powershell
D:\Android\Emulator\Sdk\cmdline-tools\latest\bin\avdmanager.bat create avd --force -n Pixel_5_API_36 -k "system-images;android-36;google_apis;x86_64" --device "pixel_5"
```

普通用户环境变量设置为：

```text
ANDROID_HOME=D:\Android\Emulator\Sdk
ANDROID_SDK_ROOT=D:\Android\Emulator\Sdk
ANDROID_AVD_HOME=D:\Android\Emulator\Avd
ANDROID_USER_HOME=D:\Android\Emulator
```

同时将 `.idea/.idea.MahjongPoints.Android.dir/.idea/deploymentTargetSelector.xml` 中的部署目标改为：

```text
D:\Android\Emulator\Avd\Pixel_5_API_36.avd
```

验证结果：

```text
avdmanager list avd 能看到 Pixel_5_API_36
adb devices 能看到 emulator-5554 device
Rider 重启后运行目标不再显示 unavailable
```

### 2026-07-12：Rider 运行时报 XA0031 需要 Java SDK 11+

### 问题是什么

点击运行 `MahjongPoints.Android` 时，Rider 报错：

```text
Microsoft.Android.Sdk.Tooling.targets(22,5): Error XA0031 : 使用 .NET 6 或更高版本时，需要 Java SDK 11.0 或更高版本。
```

### 问题原因

电脑已安装 JDK 17，命令行 `java -version` 和 `javac -version` 都显示 `17.0.16`，并且命令行构建可以通过。根因不是 Java 版本过低，而是 Rider 的 Java 路径选择没有指向可用的 JDK 17，导致 Rider 启动的 MSBuild 没拿到正确 Java SDK。

### 解决方案

在 Rider 设置中把 Java/JDK 路径改为当前电脑已有的 JDK 17：

```text
D:\java_jdk\graalvm-jdk-17.0.16+12.1
```

不要把 `JavaSdkDirectory` 写死到 `.csproj`，避免项目绑定本机绝对路径。

验证结果：

```text
java -version 显示 17.0.16
javac -version 显示 17.0.16
dotnet build .\MahjongPoints.Android.csproj -p:AndroidSdkDirectory=D:\Android\Emulator\Sdk 成功
Rider 修改 Java 路径后点击运行不再报 XA0031
```
