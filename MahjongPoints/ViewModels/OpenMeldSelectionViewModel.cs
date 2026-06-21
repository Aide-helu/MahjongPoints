using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.ViewModels;

/// <summary>
/// 副露面子选择弹窗的视图模型。
/// </summary>
public sealed class OpenMeldSelectionViewModel : ObservableObject
{
    private OpenMeldSplitOption? _selectedSplitOption;

    /// <summary>
    /// 使用可选拆牌结果创建弹窗视图模型。
    /// </summary>
    /// <param name="splits">当前手牌的候选拆牌结果。</param>
    public OpenMeldSelectionViewModel(IEnumerable<MahjongHandSplitResult> splits)
    {
        var index = 1;
        foreach (var split in splits.Where(split => split.Shape == MahjongHandShape.Standard))
        {
            var option = new OpenMeldSplitOption(index, split);
            foreach (var meldOption in option.MeldOptions)
            {
                meldOption.PropertyChanged += MeldOption_OnPropertyChanged;
            }

            SplitOptions.Add(option);
            index++;
        }

        SelectedSplitOption = SplitOptions.FirstOrDefault();
        Message = SplitOptions.Count == 0
            ? "当前手牌没有可用于选择副露面子的标准拆法。"
            : "选择一组拆法，然后勾选其中已经副露的面子。";
    }

    /// <summary>
    /// 当前手牌所有可供选择的标准拆法。
    /// </summary>
    public ObservableCollection<OpenMeldSplitOption> SplitOptions { get; } = [];

    /// <summary>
    /// 弹窗顶部提示文本。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 用户当前选中的拆法。
    /// </summary>
    public OpenMeldSplitOption? SelectedSplitOption
    {
        get => _selectedSplitOption;
        set
        {
            if (SetProperty(ref _selectedSplitOption, value))
            {
                OnPropertyChanged(nameof(CanConfirm));
            }
        }
    }

    /// <summary>
    /// 是否已经选择至少一个副露面子，可以确认弹窗。
    /// </summary>
    public bool CanConfirm => SelectedSplitOption?.MeldOptions.Any(option => option.IsSelected) == true;

    /// <summary>
    /// 创建用户确认后的拆牌结果，勾选的面子会被标记为副露。
    /// </summary>
    /// <returns>带有明暗面子状态的拆牌结果。</returns>
    public MahjongHandSplitResult? CreateSelectedSplit()
    {
        if (SelectedSplitOption is null || !CanConfirm)
        {
            return null;
        }

        var melds = SelectedSplitOption.MeldOptions
            .Select(option => option.IsSelected ? option.Meld with { IsOpen = true } : option.Meld)
            .ToArray();

        return SelectedSplitOption.Split with { Melds = melds };
    }

    private void MeldOption_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OpenMeldOption.IsSelected))
        {
            OnPropertyChanged(nameof(CanConfirm));
        }
    }
}

/// <summary>
/// 弹窗中一组候选拆法。
/// </summary>
public sealed class OpenMeldSplitOption
{
    /// <summary>
    /// 使用拆法创建候选项。
    /// </summary>
    /// <param name="index">拆法序号。</param>
    /// <param name="split">拆牌结果。</param>
    public OpenMeldSplitOption(int index, MahjongHandSplitResult split)
    {
        Split = split;
        DisplayText = $"拆法 {index}: {split.DisplayText}";
        MeldOptions = new ObservableCollection<OpenMeldOption>(
            split.Melds.Select((meld, meldIndex) => new OpenMeldOption(meldIndex + 1, meld)));
    }

    /// <summary>
    /// 原始拆牌结果。
    /// </summary>
    public MahjongHandSplitResult Split { get; }

    /// <summary>
    /// 界面展示文本。
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// 当前拆法中的可选面子。
    /// </summary>
    public ObservableCollection<OpenMeldOption> MeldOptions { get; }
}

/// <summary>
/// 弹窗中一个可勾选的面子。
/// </summary>
public sealed class OpenMeldOption : ObservableObject
{
    private bool _isSelected;

    /// <summary>
    /// 使用面子创建可选项。
    /// </summary>
    /// <param name="index">面子序号。</param>
    /// <param name="meld">面子。</param>
    public OpenMeldOption(int index, MahjongMeld meld)
    {
        Meld = meld;
        DisplayText = $"面子 {index}: {GetMeldTypeName(meld.Type)} {meld.DisplayText}";
    }

    /// <summary>
    /// 原始面子。
    /// </summary>
    public MahjongMeld Meld { get; }

    /// <summary>
    /// 界面展示文本。
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// 用户是否把该面子标记为副露。
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private static string GetMeldTypeName(MahjongMeldType type)
    {
        return type switch
        {
            MahjongMeldType.Sequence => "顺子",
            MahjongMeldType.Triplet => "刻子",
            MahjongMeldType.Quad => "杠子",
            _ => "面子"
        };
    }
}
