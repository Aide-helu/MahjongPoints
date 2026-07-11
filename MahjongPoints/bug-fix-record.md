# Bug Fix Record

## 14 张非胡牌时不能提示打哪张进入听牌

**问题**

识别出 14 张牌时，程序只按“已胡牌”流程处理。若这 14 张本身不是胡牌，但打出某张后可以进入听牌，界面不会提示可打出的牌以及对应可听牌。

**错误原因**

原有逻辑只支持 13 张手牌调用 `FindTenpaiTiles` 查可听牌；14 张手牌直接进入选择胡牌张和算点流程，没有逐张模拟弃牌后复用 13 张听牌判断。

**修复结果**

新增 `FindTenpaiDiscardOptions`：对 14 张手牌逐张移除一张，得到 13 张后复用原有 `FindTenpaiTiles`。界面在 14 张非胡牌时显示“打出 X 后可听”的候选区域。

## 打出后可听只能看，不能继续选择胡牌算点

**问题**

界面能显示“打出某张后可听哪些牌”，但不能在这些可胡牌中继续选择一张来计算点数。

**错误原因**

弃牌听牌结果只保存了弃牌和可听牌列表，没有保存“弃牌后的 13 张手牌”。因此用户选中某张胡牌时，算点服务不知道应该用哪 13 张基础手牌重新计算。

**修复结果**

`TenpaiDiscardOption` 增加 `RemainingTiles`，并生成 `TenpaiDiscardWinningOption`。用户点击“打出 X，胡 Y”后，程序用 `RemainingTiles + WinningTile` 复用现有算点流程。

## 打出后可听的展示太分散

**问题**

一开始每个候选项显示成“打出牌图片 -> 胡牌图片”，可打出的牌多时界面分散，不容易按弃牌查看。

**错误原因**

UI 按“单个弃牌 + 单个胡牌”扁平展示，没有以“打出哪张牌”为分组。

**修复结果**

界面改为按弃牌分组：外层文字显示“打出 X”，组内用牌图网格显示这张弃牌后可以胡的牌，并允许点击胡牌图继续算点。

## 可选牌区域挤占图片预览区域

**问题**

当“打出后可听”的分组很多时，可选牌区域会把上方图片预览区域挤小，甚至看起来图片消失。

**错误原因**

左侧布局使用 `RowDefinitions="*,Auto"`，底部可选牌区域高度由内容撑开。候选项越多，底部区域越高，上方预览区域就被压缩。

**修复结果**

左侧布局改为图片预览固定高度，可选牌区域占满剩余空间。可选牌区域外层加 `ScrollViewer`，内容超出时在该区域内部滚动，不再挤占图片预览。

## `Image MaxHeight` 设置后看起来没有效果

**问题**

给预览图 `<Image Source="{Binding PreviewImage}" Stretch="Uniform"/>` 设置 `MaxHeight` 后，视觉上没有达到预期限制高度的效果。

**错误原因**

`Image` 位于 `Grid` 的 `*` 行和外层 `Border` 内。`MaxHeight` 只限制图片自身测量，外层容器和行高仍按剩余空间布局，所以看起来像没有控制住区域高度。

**修复结果**

不再只限制 `Image`，而是把预览区域所在行和外层 `Border` 的高度固定为 `100`，让预览区域高度由布局容器直接决定。

## 不同弃牌分组里会出现多张牌同时选中

**问题**

在“打出后可听”区域中，选择不同弃牌分组内的胡牌时，多个分组里的牌会同时显示蓝色选中边框。

**错误原因**

内层每个弃牌分组都使用了独立 `ListBox`。每个 `ListBox` 都维护自己的 `SelectedItem`，跨分组选中时，其他分组的本地选中视觉不会自动清除。

**修复结果**

内层候选改为普通 `ItemsControl`，不再使用多个 `ListBox` 管理选择。ViewModel 只保留一个全局 `SelectedTenpaiDiscardWinningOption`，所有候选牌根据同一个全局状态刷新选中状态。

## 改成全局选中后蓝色边框不显示

**问题**

修复多选后，点击可胡牌时可以触发算点，但蓝色选中边框没有显示。

**错误原因**

最初使用 `Classes.selected-wait="{Binding IsSelected}"` 加样式选择器控制边框。这个方式依赖 Avalonia 类绑定和样式匹配，实际没有稳定刷新到边框视觉上。

**修复结果**

改为直接属性绑定：`TenpaiDiscardWinningOption` 根据 `IsSelected` 暴露 `SelectionBorderBrush` 和 `SelectionBorderThickness`。XAML 直接绑定 `BorderBrush` 和 `BorderThickness`，不再依赖类样式刷新。

## 打出后可听第一次点击显示 No valid hand split

**问题**

在“打出后可听”区域中，第一次点击可胡牌时，右侧等待算点区域会显示 `No valid hand split`；再次点击同一张或其它可胡牌后才显示正常算点结果。

**错误原因**

点击候选牌时，ViewModel 只把“打出后的 13 张牌”传给算点服务，依赖 `ScoringContext.WinningTile` 在服务内部补成 14 张。第一次点击时这个上下文状态依赖不稳定，可能按 13 张直接拆牌，导致拆牌失败并显示 `No valid hand split`。

**修复结果**

点击“打出后可听”的候选胡牌时，ViewModel 直接构造 `RemainingTiles + WinningTile` 的完整 14 张算点牌再调用算点服务；后续修改算点选项重新计算时也复用同一构造逻辑。新增回归测试验证第一次点击就使用完整 14 张牌，不再显示 `No valid hand split`。

## 点击暗杠候选选择窗口时程序崩溃

**问题**

点击“暗杠”按钮后，如果存在多个暗杠候选并需要弹出选择窗口，程序抛出 `NullReferenceException` 并退出。

**错误原因**

`KanSelectionWindow` 加载 XAML 后直接访问 `MessageText` 和 `CandidateList` 命名字段，但这些字段在运行时没有被初始化，导致构造函数里访问空对象。

**修复结果**

窗口构造函数改为通过 `FindControl<TextBlock>("MessageText")` 和 `FindControl<ListBox>("CandidateList")` 获取控件，并在找不到控件时抛出明确异常。新增回归测试验证暗杠候选窗口可以正常初始化命名控件。
