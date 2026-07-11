using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Platform;
using MahjongPoints;
using MahjongPoints.Models;
using MahjongPoints.Services;
using MahjongPoints.Services.Scoring;
using MahjongPoints.ViewModels;
using MahjongPoints.Views;

// <summary>
// 创建测试用识别牌，显示名固定等于牌编码。
// </summary>
// <param name="code">要创建的牌编码。</param>
// <returns>测试用识别牌。</returns>
static RecognizedMahjongTile T(string code) => new(code, code, 1);

// <summary>
// 将一组牌编码转换为识别牌数组。
// </summary>
// <param name="codes">要转换的牌编码列表。</param>
// <returns>识别牌数组。</returns>
static RecognizedMahjongTile[] Tiles(params string[] codes) => codes.Select(T).ToArray();

// <summary>
// 从 ViewModel 中展开所有打牌后可胡的候选项。
// </summary>
// <param name="vm">包含打牌候选项的 ViewModel。</param>
// <returns>所有可点击的打牌胡牌候选项。</returns>
static TenpaiDiscardWinningOption[] DiscardWinningOptions(MainWindowViewModel vm) =>
    vm.TenpaiDiscardOptions.SelectMany(option => option.WinningOptions).ToArray();

// <summary>
// 创建测试用杠面子。
// </summary>
// <param name="code">杠子的牌编码。</param>
// <param name="isOpen">是否为明杠。</param>
// <returns>杠面子。</returns>
static MahjongMeld Kan(string code, bool isOpen) =>
    new(MahjongMeldType.Quad, [T(code), T(code), T(code), T(code)], isOpen);

// <summary>
// 当测试条件不成立时抛出异常。
// </summary>
// <param name="condition">期望为真的条件。</param>
// <param name="message">失败时显示的消息。</param>
static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

var avaloniaInitialized = false;

// <summary>
// 为界面绑定测试初始化一次 Avalonia。
// </summary>
void EnsureAvaloniaInitialized()
{
    if (avaloniaInitialized)
    {
        return;
    }

    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .SetupWithoutStarting();
    avaloniaInitialized = true;
}

// <summary>
// 验证只有牌型、没有役的听牌候选不会被算作有效和牌。
// </summary>
// <returns>异步测试任务。</returns>
static async Task TenpaiTilesIncludeNoYakuShape()
{
    var service = new MahjongHandScoringService();
    var hand = Tiles("1m", "1m", "1m", "2p", "3p", "4p", "3s", "4s", "5s", "6s", "7s", "8s", "9m");
    var candidates = service.FindTenpaiTiles(hand);

    Assert(candidates.Any(tile => tile.Code == "9m"), "Expected 9m as a shape-valid tenpai candidate.");

    var context = new MahjongScoringContext { WinningTile = candidates.First(tile => tile.Code == "9m") };
    var result = await service.CalculateAsync(hand, context);

    Assert(!result.IsWinningHand, "Shape-valid no-yaku hand must not score as a valid win.");
    Assert(result.ScoreSummary.Contains("No yaku", StringComparison.OrdinalIgnoreCase), "No-yaku scoring should say No yaku.");
}

// <summary>
// 验证宝牌只增加番数，不增加符数。
// </summary>
static void DoraAddsFanButNotFu()
{
    var calculator = new DefaultScoreCalculator();
    var yaku = new YakuDetectionResult([new MahjongYaku("test", "Test Yaku", 1, "test")], null);
    var fu = new FuCalculationResult(30, [], null);

    var withoutDora = calculator.Calculate(yaku, fu, new MahjongScoringContext());
    var withDora = calculator.Calculate(yaku, fu, new MahjongScoringContext { DoraCount = 2 });

    Assert(withDora.TotalFan == withoutDora.TotalFan + 2, "Dora should add fan.");
    Assert(withDora.Fu == withoutDora.Fu, "Dora must not change fu.");
    Assert(withDora.Items.Any(item => item.Name.Contains("Dora", StringComparison.OrdinalIgnoreCase) || item.Name.Contains("宝牌", StringComparison.Ordinal)), "Dora should be shown as a score item.");
}

// <summary>
// 验证只有宝牌不会形成有效役。
// </summary>
static void DoraDoesNotCreateYaku()
{
    var calculator = new DefaultScoreCalculator();
    var result = calculator.Calculate(
        new YakuDetectionResult([], null),
        new FuCalculationResult(30, [], null),
        new MahjongScoringContext { DoraCount = 3 });

    Assert(result.TotalFan == 0, "Dora must not score without yaku.");
    Assert(result.TotalPoints == 0, "Dora-only hand must not have points.");
}

// <summary>
// 验证算点服务会选择得点最高的拆牌结果。
// </summary>
// <returns>异步测试任务。</returns>
static async Task HighestScoringShapeWins()
{
    var lowSplit = new MahjongHandSplitResult([], T("1m"));
    var highSplit = new MahjongHandSplitResult([], T("2m"));
    var service = new MahjongHandScoringService(
        new FakeSplitter([lowSplit, highSplit]),
        new FakeYakuDetector(lowSplit, highSplit),
        new FakeFuCalculator(),
        new FakeScoreCalculator());

    var result = await service.CalculateAsync(
        Tiles("1m", "1m", "1m", "2m", "2m", "2m", "3m", "3m", "3m", "4m", "4m", "4m", "5m", "5m"),
        new MahjongScoringContext { WinningTile = T("5m") });

    Assert(result.ScoreSummary == "High", "Expected service to choose the highest-scoring split.");
    Assert(result.WinningShape == highSplit.DisplayText, "Expected selected shape to match the highest-scoring split.");
}

// <summary>
// 验证 14 张牌的弃牌候选复用 13 张听牌搜索。
// </summary>
static void FourteenTilesFindDiscardOptionsByReusingTenpaiSearch()
{
    var service = new MahjongHandScoringService();
    var options = service.FindTenpaiDiscardOptions(Tiles(
        "1m", "1m", "1m",
        "2p", "3p", "4p",
        "3s", "4s", "5s",
        "6s", "7s", "8s",
        "9m", "1z"));

    var discard1z = options.FirstOrDefault(option => option.DiscardTile.Code == "1z");

    Assert(discard1z is not null, "Discarding 1z should leave a 13-tile tenpai shape.");
    Assert(discard1z!.WinningTiles.Any(tile => tile.Code == "9m"), "Discarding 1z should show 9m as a wait.");
    Assert(discard1z.RemainingTiles.Count == 13, "Discard option should keep the 13 tiles left after discarding.");
    Assert(!discard1z.RemainingTiles.Any(tile => tile.Code == "1z"), "Discard option remaining tiles should remove one discarded tile.");
}

// <summary>
// 验证明杠会把 15 张物理牌归一化后再算点。
// </summary>
// <returns>异步测试任务。</returns>
static async Task OpenKanNormalizesFifteenPhysicalTiles()
{
    var service = new MahjongHandScoringService();
    var context = new MahjongScoringContext
    {
        WinningTile = T("1z"),
        DeclaredKans = [Kan("2m", isOpen: true)],
        IsOpenHand = true
    };

    var result = await service.CalculateAsync(Tiles(
        "2m", "2m", "2m", "2m",
        "3p", "3p", "3p",
        "4s", "4s", "4s",
        "5z", "5z", "5z",
        "1z", "1z"), context);

    Assert(result.IsWinningHand, "Declared open kan should normalize 15 physical tiles into a winning hand.");
    Assert(result.WinningShape.Contains("2m 2m 2m 2m", StringComparison.Ordinal), "Declared open kan should be kept as a quad in the winning shape.");
    Assert(context.IsMenzen == false, "Declared open kan should break menzen.");
}

// <summary>
// 验证暗杠会把两张可见同牌补成杠面子。
// </summary>
// <returns>异步测试任务。</returns>
static async Task ConcealedKanPadsTwoVisibleTiles()
{
    var service = new MahjongHandScoringService();
    var context = new MahjongScoringContext
    {
        WinningTile = T("1z"),
        DeclaredKans = [Kan("2m", isOpen: false)],
        IsTsumo = true
    };

    var result = await service.CalculateAsync(Tiles(
        "2m", "2m",
        "3p", "3p", "3p",
        "4s", "4s", "4s",
        "5z", "5z", "5z",
        "1z", "1z"), context);

    Assert(result.IsWinningHand, "Declared concealed kan should pad two visible tiles into an effective quad meld.");
    Assert(result.WinningShape.Contains("2m 2m 2m 2m", StringComparison.Ordinal), "Declared concealed kan should be kept as a quad in the winning shape.");
    Assert(context.IsMenzen, "Declared concealed kan should not break menzen.");
}

// <summary>
// 验证 ViewModel 会为 14 张未和牌显示弃牌听牌候选。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelShowsDiscardOptionsForFourteenNonWinningTiles()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "1m", "1m", "1m",
        "2p", "3p", "4p",
        "3s", "4s", "5s",
        "6s", "7s", "8s",
        "9m", "1z");
    var vm = new MainWindowViewModel(
        new FakeRecognizer(tiles),
        new FakeHandScoringService(
            new MahjongScoringResult(T("unknown"), false, "No valid hand split.", "No valid hand split.", 0, 0, 0),
            [new TenpaiDiscardOption(T("1z"), [T("9m")], tiles.Where(tile => tile.Code != "1z").ToArray())]));

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");

    Assert(!vm.IsWinningTileMode, "A 14-tile non-winning hand should not stay in winning-tile selection mode.");
    Assert(
        vm.IsDiscardTenpaiMode,
        $"A 14-tile non-winning hand with useful discards should show discard-tenpai options. Count={vm.TenpaiDiscardOptions.Count}, Recognized={vm.RecognizedTiles.Count}, Status={vm.StatusMessage}");
    Assert(vm.TenpaiDiscardOptions.Count == 1, "Expected one discard-tenpai option in the selectable area.");
    Assert(DiscardWinningOptions(vm).Length == 1, "Expected one selectable discard plus winning tile option.");
    Assert(vm.TenpaiDiscardOptions[0].DisplayText.Contains("1z", StringComparison.Ordinal), "Discard option should name the discarded tile.");
    Assert(vm.TenpaiDiscardOptions[0].DisplayText.Contains("9m", StringComparison.Ordinal), "Discard option should list the wait tiles.");
}

// <summary>
// 验证弃牌听牌模式会等待用户选择胡牌张。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelWaitsForDiscardWinningSelection()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "1m", "1m", "1m",
        "2p", "3p", "4p",
        "3s", "4s", "5s",
        "6s", "7s", "8s",
        "9m", "1z");
    var vm = new MainWindowViewModel(
        new FakeRecognizer(tiles),
        new FakeHandScoringService(
            new MahjongScoringResult(T("unknown"), false, "NULL", "NULL", 0, 0, 0),
            [new TenpaiDiscardOption(T("1z"), [T("9m")], tiles.Where(tile => tile.Code != "1z").ToArray())]));

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");

    Assert(vm.IsDiscardTenpaiMode, "Discard-tenpai mode should be active.");
    Assert(vm.ScoreSummary != "NULL", "Discard-tenpai mode should wait for a winning tile instead of showing the initial failed calculation.");
    Assert(!vm.WinningShape.Contains("NULL", StringComparison.Ordinal), "Discard-tenpai mode should not show the initial failed split result.");
}

// <summary>
// 验证选择弃牌胡牌候选后会用所选胡牌张重新算点。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelCalculatesSelectedDiscardWinningTile()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "1m", "1m", "1m",
        "2p", "3p", "4p",
        "3s", "4s", "5s",
        "6s", "7s", "8s",
        "9m", "1z");
    var remainingTiles = tiles.Where(tile => tile.Code != "1z").ToArray();
    var calculationCount = 0;
    var scoringService = new FakeHandScoringService(
        new MahjongScoringResult(T("unknown"), false, "No valid hand split.", "No valid hand split.", 0, 0, 0),
        [new TenpaiDiscardOption(T("1z"), [T("9m")], remainingTiles)],
        (calculationTiles, context) =>
        {
            calculationCount++;
            return calculationCount == 1
                ? new MahjongScoringResult(T("unknown"), false, "No valid hand split.", "No valid hand split.", 0, 0, 0)
                : new MahjongScoringResult(context.WinningTile, true, "selected", "Selected score", 1, 30, 1000);
        });
    var vm = new MainWindowViewModel(new FakeRecognizer(tiles), scoringService);

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");
    var options = DiscardWinningOptions(vm);
    await vm.SelectTenpaiDiscardWinningOptionCommand.ExecuteAsync(options[0]);

    Assert(scoringService.LastCalculationTiles.Select(tile => tile.Code).SequenceEqual(remainingTiles.Select(tile => tile.Code).Concat(["9m"])), "Selected discard wait should calculate with the 14-tile hand after adding the selected winning tile.");
    Assert(scoringService.LastWinningTileCode == "9m", "Selected discard wait should calculate with the selected winning tile.");
    Assert(vm.ScoreSummary == "Selected score", "Selecting a discard wait should update the score summary.");
}

// <summary>
// 验证第一次选择弃牌胡牌候选时使用完整 14 张牌算点。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelFirstDiscardWinningSelectionUsesCompleteHand()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "1m", "1m", "1m",
        "2p", "3p", "4p",
        "3s", "4s", "5s",
        "6s", "7s", "8s",
        "9m", "1z");
    var vm = new MainWindowViewModel(new FakeRecognizer(tiles), new MahjongHandScoringService());

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");
    var option = DiscardWinningOptions(vm).First(option => option.DiscardTile.Code == "1z" && option.WinningTile.Code == "9m");

    await vm.SelectTenpaiDiscardWinningOptionCommand.ExecuteAsync(option);

    Assert(!vm.ScoreSummary.Contains("No valid hand split", StringComparison.OrdinalIgnoreCase), "The first discard-winning selection should calculate from the 14-tile winning hand.");
    Assert(!vm.WinningShape.Contains("No valid 4 melds", StringComparison.OrdinalIgnoreCase), "The first discard-winning selection should not calculate the 13-tile remaining hand by itself.");
}

// <summary>
// 验证所有弃牌胡牌候选中只会保留一个选中项。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelKeepsOneSelectedDiscardWinningTile()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "1m", "1m", "1m",
        "2p", "3p", "4p",
        "3s", "4s", "5s",
        "6s", "7s", "8s",
        "9m", "1z");
    var discard1z = new TenpaiDiscardOption(T("1z"), [T("9m")], tiles.Where(tile => tile.Code != "1z").ToArray());
    var discard9m = new TenpaiDiscardOption(T("9m"), [T("1z")], tiles.Where(tile => tile.Code != "9m").ToArray());
    var scoringService = new FakeHandScoringService(
        new MahjongScoringResult(T("unknown"), false, "No valid hand split.", "No valid hand split.", 0, 0, 0),
        [discard1z, discard9m]);
    var vm = new MainWindowViewModel(new FakeRecognizer(tiles), scoringService);

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");
    var options = DiscardWinningOptions(vm);
    await vm.SelectTenpaiDiscardWinningOptionCommand.ExecuteAsync(options[0]);
    await vm.SelectTenpaiDiscardWinningOptionCommand.ExecuteAsync(options[1]);

    Assert(DiscardWinningOptions(vm).Count(option => option.IsSelected) == 1, "Only one discard-winning tile should be selected across all discard groups.");
    Assert(ReferenceEquals(vm.SelectedTenpaiDiscardWinningOption, options[1]), "The latest clicked discard-winning tile should be the selected option.");
}

// <summary>
// 验证只有一个明杠候选时会直接添加，不弹选择窗。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelAutoAddsSingleOpenKanCandidate()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "2m", "2m", "2m", "2m",
        "3p", "3p", "3p",
        "4s", "4s", "4s",
        "5z", "5z", "5z",
        "1z", "1z");
    var vm = new MainWindowViewModel(new FakeRecognizer(tiles), new FakeHandScoringService());

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");
    await vm.AddOpenKanCommand.ExecuteAsync(null);

    Assert(vm.DeclaredKans.Count == 1, "A single open-kan candidate should be added automatically.");
    Assert(vm.ScoringContext.DeclaredKans.Single().Tiles[0].Code == "2m", "The declared open kan should use the only four-of-a-kind tile.");
    Assert(vm.ScoringContext.IsOpenHand, "Declared open kan should mark the hand as open.");
    Assert(!vm.ScoringContext.IsMenzen, "Declared open kan should break menzen.");
}

// <summary>
// 验证声明明杠时会请求普通副露选择。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelOpenKanRequestsOpenMeldSelection()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "2m", "2m", "2m", "2m",
        "3p", "3p", "3p",
        "4s", "4s", "4s",
        "5z", "5z", "5z",
        "1z", "1z");
    var vm = new MainWindowViewModel(new FakeRecognizer(tiles), new FakeHandScoringService());
    var openMeldRequests = 0;
    vm.OpenMeldSelectionRequested += (_, _) => openMeldRequests++;

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");
    await vm.AddOpenKanCommand.ExecuteAsync(null);

    Assert(openMeldRequests == 1, "Declaring an open kan should request normal open-meld selection.");
}

// <summary>
// 验证从多个候选中选择明杠后会请求普通副露选择。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelSelectedOpenKanRequestsOpenMeldSelection()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "2m", "2m", "2m", "2m",
        "3p", "3p", "3p", "3p",
        "4s", "4s", "4s",
        "5z", "5z", "5z",
        "1z");
    var vm = new MainWindowViewModel(new FakeRecognizer(tiles), new FakeHandScoringService());
    KanSelectionRequestedEventArgs? requested = null;
    var openMeldRequests = 0;
    vm.KanSelectionRequested += (_, args) => requested = args;
    vm.OpenMeldSelectionRequested += (_, _) => openMeldRequests++;

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");
    await vm.AddOpenKanCommand.ExecuteAsync(null);
    await vm.ApplyKanSelectionAsync(DeclaredKanKind.Open, requested!.Candidates[0]);

    Assert(requested is not null, "Multiple open-kan candidates should request kan selection.");
    Assert(vm.ScoringContext.IsOpenHand, "Selected open kan should mark the hand as open.");
    Assert(openMeldRequests == 1, "Selecting an open kan should request normal open-meld selection.");
}

// <summary>
// 验证只有一个暗杠候选时会直接添加，不弹选择窗。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelAutoAddsSingleConcealedKanCandidate()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "2m", "2m",
        "3p", "3p", "3p",
        "4s", "4s", "4s",
        "5z", "5z", "5z",
        "6m", "7m", "8m");
    var vm = new MainWindowViewModel(new FakeRecognizer(tiles), new FakeHandScoringService());

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");
    await vm.AddConcealedKanCommand.ExecuteAsync(null);

    Assert(vm.DeclaredKans.Count == 1, "A single concealed-kan candidate should be added automatically.");
    Assert(vm.ScoringContext.DeclaredKans.Single().Tiles[0].Code == "2m", "The declared concealed kan should use the only pair tile.");
    Assert(!vm.ScoringContext.IsOpenHand, "Declared concealed kan should not mark the hand as open.");
    Assert(vm.ScoringContext.IsMenzen, "Declared concealed kan should keep menzen.");
}

// <summary>
// 验证多个杠候选会请求用户选择。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelRequestsKanSelectionForMultipleCandidates()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "2m", "2m",
        "3p", "3p",
        "4s", "4s", "4s",
        "5z", "5z", "5z",
        "1z", "1z");
    var vm = new MainWindowViewModel(new FakeRecognizer(tiles), new FakeHandScoringService());
    KanSelectionRequestedEventArgs? requested = null;
    vm.KanSelectionRequested += (_, args) => requested = args;

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");
    await vm.AddConcealedKanCommand.ExecuteAsync(null);

    Assert(requested is not null, "Multiple concealed-kan candidates should request user selection.");
    Assert(requested!.Candidates.Count == 3, "All pair tiles should be offered as concealed-kan candidates.");
    Assert(vm.DeclaredKans.Count == 0, "Multiple candidates should not auto-add a kan before selection.");
}

// <summary>
// 验证杠候选窗口可以正常初始化命名控件。
// </summary>
void KanSelectionWindowInitializesNamedControls()
{
    EnsureAvaloniaInitialized();

    var window = new KanSelectionWindow(DeclaredKanKind.Concealed, [T("2m"), T("3p")]);

    Assert(window.FindControl<TextBlock>("MessageText") is not null, "Kan selection message text should be initialized.");
    Assert(window.FindControl<ListBox>("CandidateList") is not null, "Kan selection candidate list should be initialized.");
}

// <summary>
// 验证同一种牌不能重复声明明杠。
// </summary>
// <returns>异步测试任务。</returns>
async Task ViewModelDoesNotDeclareSameOpenKanTileTwice()
{
    EnsureAvaloniaInitialized();

    var tiles = Tiles(
        "2m", "2m", "2m", "2m",
        "3p", "3p", "3p",
        "4s", "4s", "4s",
        "5z", "5z", "5z",
        "1z", "1z");
    var vm = new MainWindowViewModel(new FakeRecognizer(tiles), new FakeHandScoringService());

    await vm.LoadAndRecognizeAsync(@"MahjongPoints\Images\tupai\z_5.png");
    await vm.AddOpenKanCommand.ExecuteAsync(null);
    await vm.AddOpenKanCommand.ExecuteAsync(null);

    Assert(vm.DeclaredKans.Count == 1, "The same tile code should not be declared as more than one kan.");
}

// <summary>
// 验证弃牌胡牌候选的选中状态会刷新边框。
// </summary>
static void DiscardWinningOptionSelectionChangesBorder()
{
    var option = new TenpaiDiscardWinningOption(T("1z"), T("9m"), []);

    Assert(option.SelectionBorderThickness.Left == 0, "Unselected discard-winning tile should not have a visible border.");

    option.IsSelected = true;

    Assert(option.SelectionBorderThickness.Left == 4, "Selected discard-winning tile should have a visible border.");
    Assert(ReferenceEquals(option.SelectionBorderBrush, Brushes.DodgerBlue), "Selected discard-winning tile should use the blue selection brush.");
}

// <summary>
// 验证宝牌数量命令会把下限限制在零。
// </summary>
static void DoraCommandsClampAtZero()
{
    var vm = new MainWindowViewModel(new FakeRecognizer(), new FakeHandScoringService());
    vm.DecrementDoraCountCommand.Execute(null);
    Assert(vm.ScoringContext.DoraCount == 0, "Dora count should clamp at zero.");
    vm.IncrementDoraCountCommand.Execute(null);
    Assert(vm.ScoringContext.DoraCount == 1, "Increment command should add one dora.");
}

// <summary>
// 验证算点选项显示名会省略“是否”前缀。
// </summary>
static void ScoringOptionLabelsOmitQuestionPrefix()
{
    var labels = MahjongScoringOptionItem.CreateItems(new MahjongScoringContext())
        .Select(option => option.DisplayName)
        .ToArray();

    Assert(labels.Contains("立直"), "Expected option label to omit 是否 prefix.");
    Assert(!labels.Any(label => label.StartsWith("是否", StringComparison.Ordinal)), "Option labels should not start with 是否.");
}

// <summary>
// 验证立直快捷命令会设置精确的算点选项组合。
// </summary>
static void RiichiShortcutCommandsSetExactCombination()
{
    var vm = new MainWindowViewModel(new FakeRecognizer(), new FakeHandScoringService());
    vm.ScoringContext.IsOpenHand = true;
    vm.ScoringContext.IsDoubleRiichi = true;

    vm.SelectRiichiIppatsuTsumoCommand.Execute(null);
    Assert(vm.ScoringContext.IsRiichi, "立一自 should select riichi.");
    Assert(vm.ScoringContext.IsIppatsu, "立一自 should select ippatsu.");
    Assert(vm.ScoringContext.IsTsumo, "立一自 should select tsumo.");
    Assert(!vm.ScoringContext.IsOpenHand, "Riichi shortcuts should clear open hand.");
    Assert(!vm.ScoringContext.IsDoubleRiichi, "Riichi shortcuts should clear double riichi.");

    vm.SelectRiichiIppatsuCommand.Execute(null);
    Assert(vm.ScoringContext.IsRiichi, "立一 should select riichi.");
    Assert(vm.ScoringContext.IsIppatsu, "立一 should select ippatsu.");
    Assert(!vm.ScoringContext.IsTsumo, "立一 should clear tsumo.");

    vm.SelectRiichiTsumoCommand.Execute(null);
    Assert(vm.ScoringContext.IsRiichi, "立自 should select riichi.");
    Assert(!vm.ScoringContext.IsIppatsu, "立自 should clear ippatsu.");
    Assert(vm.ScoringContext.IsTsumo, "立自 should select tsumo.");
}

// <summary>
// 验证副露快捷命令会设置精确的算点选项组合。
// </summary>
static void OpenHandShortcutCommandsSetExactCombination()
{
    var vm = new MainWindowViewModel(new FakeRecognizer(), new FakeHandScoringService());
    vm.ScoringContext.IsRiichi = true;
    vm.ScoringContext.IsDoubleRiichi = true;
    vm.ScoringContext.IsIppatsu = true;

    vm.SelectOpenTsumoCommand.Execute(null);
    Assert(vm.ScoringContext.IsOpenHand, "副自 should select open hand.");
    Assert(vm.ScoringContext.IsTsumo, "副自 should select tsumo.");
    Assert(!vm.ScoringContext.IsRiichi, "副自 should clear riichi.");
    Assert(!vm.ScoringContext.IsDoubleRiichi, "副自 should clear double riichi.");
    Assert(!vm.ScoringContext.IsIppatsu, "副自 should clear ippatsu.");

    vm.SelectOpenRonCommand.Execute(null);
    Assert(vm.ScoringContext.IsOpenHand, "副荣 should select open hand.");
    Assert(!vm.ScoringContext.IsTsumo, "副荣 should clear tsumo.");
    Assert(!vm.ScoringContext.IsRiichi, "副荣 should keep riichi cleared.");
    Assert(!vm.ScoringContext.IsDoubleRiichi, "副荣 should keep double riichi cleared.");
    Assert(!vm.ScoringContext.IsIppatsu, "副荣 should keep ippatsu cleared.");
}

// <summary>
// 验证牌图路径匹配内置资源文件名。
// </summary>
static void TileImagePathsFollowAssetNames()
{
    Assert(T("1m").ImagePath == "avares://MahjongPoints/Images/manzu/m_1.png", "1m should map to manzu image.");
    Assert(T("9p").ImagePath == "avares://MahjongPoints/Images/pinzu/p_9.png", "9p should map to pinzu image.");
    Assert(T("5s").ImagePath == "avares://MahjongPoints/Images/sozu/s_5.png", "5s should map to sozu image.");
    Assert(T("7z").ImagePath == "avares://MahjongPoints/Images/tupai/z_7.png", "7z should map to honor image.");
    Assert(T("unknown").ImagePath == "avares://MahjongPoints/Images/tupai/z_5.png", "Unknown tile should use blank tile image.");
}

// <summary>
// 验证牌图资源文件存在。
// </summary>
void TileImageAssetsExist()
{
    EnsureAvaloniaInitialized();

    Assert(AssetLoader.Exists(new Uri(T("1m").ImagePath)), "1m image asset should exist.");
    Assert(AssetLoader.Exists(new Uri(T("7z").ImagePath)), "7z image asset should exist.");
}

// <summary>
// 验证牌图绑定可以加载图片源。
// </summary>
void TileImageBindingLoadsSource()
{
    EnsureAvaloniaInitialized();

    var image = new Image { DataContext = T("1m") };
    image.Bind(Image.SourceProperty, new Binding(nameof(RecognizedMahjongTile.TileImage)));

    Assert(image.Source is not null, "Image.Source should load from bound TileImage.");
}

await TenpaiTilesIncludeNoYakuShape();
DoraAddsFanButNotFu();
DoraDoesNotCreateYaku();
await HighestScoringShapeWins();
FourteenTilesFindDiscardOptionsByReusingTenpaiSearch();
await OpenKanNormalizesFifteenPhysicalTiles();
await ConcealedKanPadsTwoVisibleTiles();
await ViewModelShowsDiscardOptionsForFourteenNonWinningTiles();
await ViewModelWaitsForDiscardWinningSelection();
await ViewModelCalculatesSelectedDiscardWinningTile();
await ViewModelFirstDiscardWinningSelectionUsesCompleteHand();
await ViewModelKeepsOneSelectedDiscardWinningTile();
await ViewModelAutoAddsSingleOpenKanCandidate();
await ViewModelOpenKanRequestsOpenMeldSelection();
await ViewModelSelectedOpenKanRequestsOpenMeldSelection();
await ViewModelAutoAddsSingleConcealedKanCandidate();
await ViewModelRequestsKanSelectionForMultipleCandidates();
KanSelectionWindowInitializesNamedControls();
await ViewModelDoesNotDeclareSameOpenKanTileTwice();
DiscardWinningOptionSelectionChangesBorder();
DoraCommandsClampAtZero();
ScoringOptionLabelsOmitQuestionPrefix();
RiichiShortcutCommandsSetExactCombination();
OpenHandShortcutCommandsSetExactCombination();
TileImagePathsFollowAssetNames();
TileImageAssetsExist();
TileImageBindingLoadsSource();

Console.WriteLine("MahjongPoints core tests passed.");

sealed class FakeSplitter(IReadOnlyList<MahjongHandSplitResult> splits) : IHandSplitter
{
    /// <summary>
    /// 返回预设的假拆牌结果。
    /// </summary>
    /// <param name="tiles">调用方传入的手牌。</param>
    /// <returns>预设拆牌结果。</returns>
    public IReadOnlyList<MahjongHandSplitResult> Split(IReadOnlyList<RecognizedMahjongTile> tiles) => splits;
}

sealed class FakeYakuDetector(MahjongHandSplitResult lowSplit, MahjongHandSplitResult highSplit) : IYakuDetector
{
    public IReadOnlyList<YakuDetectionResult> Detect(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        IReadOnlyList<MahjongHandSplitResult> splits,
        MahjongScoringContext context) =>
        [
            new([new MahjongYaku("low", "Low", 1, "")], lowSplit),
            new([new MahjongYaku("high", "High", 1, "")], highSplit)
        ];
}

sealed class FakeFuCalculator : IFuCalculator
{
    public FuCalculationResult Calculate(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        YakuDetectionResult yakuResult,
        MahjongScoringContext context) =>
        new(30, [], yakuResult.SelectedSplit);
}

sealed class FakeScoreCalculator : IScoreCalculator
{
    public PointCalculationResult Calculate(
        YakuDetectionResult yakuResult,
        FuCalculationResult fuResult,
        MahjongScoringContext context)
    {
        var isHigh = yakuResult.Yakus.Any(yaku => yaku.Id == "high");
        return new(
            1,
            30,
            isHigh ? 2000 : 1000,
            isHigh ? "High" : "Low",
            [],
            []);
    }
}

sealed class FakeRecognizer(IReadOnlyList<RecognizedMahjongTile>? tiles = null) : IHandImageRecognizer
{
    /// <summary>
    /// 返回预设的假识别结果。
    /// </summary>
    /// <param name="imagePath">调用方传入的图片路径。</param>
    /// <param name="cancellationToken">调用方传入的取消令牌。</param>
    /// <returns>假识别结果。</returns>
    public Task<MahjongHandRecognitionResult> RecognizeAsync(string imagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MahjongHandRecognitionResult(tiles ?? [], "fake", "fake", "fake"));
}

sealed class FakeHandScoringService(
    MahjongScoringResult? result = null,
    IReadOnlyList<TenpaiDiscardOption>? discardOptions = null,
    Func<IReadOnlyList<RecognizedMahjongTile>, MahjongScoringContext, MahjongScoringResult>? calculate = null) : IHandScoringService
{
    public IReadOnlyList<RecognizedMahjongTile> LastCalculationTiles { get; private set; } = [];

    public string LastWinningTileCode { get; private set; } = "";

    /// <summary>
    /// 为 ViewModel 测试返回固定听牌候选。
    /// </summary>
    /// <param name="recognizedTiles">调用方传入的识别牌。</param>
    /// <returns>固定听牌候选列表。</returns>
    public IReadOnlyList<RecognizedMahjongTile> FindTenpaiTiles(IReadOnlyList<RecognizedMahjongTile> recognizedTiles) => [new("4s", "4s", 1)];

    /// <summary>
    /// 返回预设的弃牌听牌候选。
    /// </summary>
    /// <param name="recognizedTiles">调用方传入的识别牌。</param>
    /// <returns>预设弃牌听牌候选。</returns>
    public IReadOnlyList<TenpaiDiscardOption> FindTenpaiDiscardOptions(IReadOnlyList<RecognizedMahjongTile> recognizedTiles) =>
        discardOptions ?? [];

    public Task<MahjongScoringResult> CalculateAsync(
        IReadOnlyList<RecognizedMahjongTile> recognizedTiles,
        MahjongScoringContext context,
        CancellationToken cancellationToken = default)
    {
        LastCalculationTiles = recognizedTiles.ToArray();
        LastWinningTileCode = context.WinningTile.Code;
        return Task.FromResult(calculate?.Invoke(recognizedTiles, context) ?? result ?? new MahjongScoringResult(context.WinningTile, false, "shape", "No yaku", 0, 0, 0));
    }
}
