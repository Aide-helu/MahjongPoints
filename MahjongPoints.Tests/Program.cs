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

static RecognizedMahjongTile T(string code) => new(code, code, 1);
static RecognizedMahjongTile[] Tiles(params string[] codes) => codes.Select(T).ToArray();
static TenpaiDiscardWinningOption[] DiscardWinningOptions(MainWindowViewModel vm) =>
    vm.TenpaiDiscardOptions.SelectMany(option => option.WinningOptions).ToArray();

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

var avaloniaInitialized = false;

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

    Assert(scoringService.LastCalculationTiles.Select(tile => tile.Code).SequenceEqual(remainingTiles.Select(tile => tile.Code)), "Selected discard wait should calculate with the 13 tiles left after discarding.");
    Assert(scoringService.LastWinningTileCode == "9m", "Selected discard wait should calculate with the selected winning tile.");
    Assert(vm.ScoreSummary == "Selected score", "Selecting a discard wait should update the score summary.");
}

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

static void DiscardWinningOptionSelectionChangesBorder()
{
    var option = new TenpaiDiscardWinningOption(T("1z"), T("9m"), []);

    Assert(option.SelectionBorderThickness.Left == 0, "Unselected discard-winning tile should not have a visible border.");

    option.IsSelected = true;

    Assert(option.SelectionBorderThickness.Left == 4, "Selected discard-winning tile should have a visible border.");
    Assert(ReferenceEquals(option.SelectionBorderBrush, Brushes.DodgerBlue), "Selected discard-winning tile should use the blue selection brush.");
}

static void DoraCommandsClampAtZero()
{
    var vm = new MainWindowViewModel(new FakeRecognizer(), new FakeHandScoringService());
    vm.DecrementDoraCountCommand.Execute(null);
    Assert(vm.ScoringContext.DoraCount == 0, "Dora count should clamp at zero.");
    vm.IncrementDoraCountCommand.Execute(null);
    Assert(vm.ScoringContext.DoraCount == 1, "Increment command should add one dora.");
}

static void ScoringOptionLabelsOmitQuestionPrefix()
{
    var labels = MahjongScoringOptionItem.CreateItems(new MahjongScoringContext())
        .Select(option => option.DisplayName)
        .ToArray();

    Assert(labels.Contains("立直"), "Expected option label to omit 是否 prefix.");
    Assert(!labels.Any(label => label.StartsWith("是否", StringComparison.Ordinal)), "Option labels should not start with 是否.");
}

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

static void TileImagePathsFollowAssetNames()
{
    Assert(T("1m").ImagePath == "avares://MahjongPoints/Images/manzu/m_1.png", "1m should map to manzu image.");
    Assert(T("9p").ImagePath == "avares://MahjongPoints/Images/pinzu/p_9.png", "9p should map to pinzu image.");
    Assert(T("5s").ImagePath == "avares://MahjongPoints/Images/sozu/s_5.png", "5s should map to sozu image.");
    Assert(T("7z").ImagePath == "avares://MahjongPoints/Images/tupai/z_7.png", "7z should map to honor image.");
    Assert(T("unknown").ImagePath == "avares://MahjongPoints/Images/tupai/z_5.png", "Unknown tile should use blank tile image.");
}

void TileImageAssetsExist()
{
    EnsureAvaloniaInitialized();

    Assert(AssetLoader.Exists(new Uri(T("1m").ImagePath)), "1m image asset should exist.");
    Assert(AssetLoader.Exists(new Uri(T("7z").ImagePath)), "7z image asset should exist.");
}

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
await ViewModelShowsDiscardOptionsForFourteenNonWinningTiles();
await ViewModelCalculatesSelectedDiscardWinningTile();
await ViewModelKeepsOneSelectedDiscardWinningTile();
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

    public IReadOnlyList<RecognizedMahjongTile> FindTenpaiTiles(IReadOnlyList<RecognizedMahjongTile> recognizedTiles) => [new("4s", "4s", 1)];

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
