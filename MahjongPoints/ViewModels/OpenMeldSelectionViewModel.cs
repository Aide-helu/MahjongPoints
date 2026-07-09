using System;
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
    /// <summary>
    /// 使用可选拆牌结果创建弹窗视图模型。
    /// </summary>
    /// <param name="splits">当前手牌的候选拆牌结果。</param>
    public OpenMeldSelectionViewModel(IEnumerable<MahjongHandSplitResult> splits)
    {
        var melds = splits
            .Where(split => split.Shape == MahjongHandShape.Standard)
            .SelectMany(split => split.Melds)
            .GroupBy(meld => meld.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(meld => meld.Type)
            .ThenBy(meld => meld.Key, StringComparer.Ordinal)
            .ToArray();

        var index = 1;
        foreach (var meld in melds)
        {
            var option = new OpenMeldOption(index, meld);
            option.PropertyChanged += MeldOption_OnPropertyChanged;
            MeldOptions.Add(option);
            index++;
        }

        Message = MeldOptions.Count == 0
            ? "当前手牌没有可选择的副露面子。"
            : "勾选已经副露的面子。算点时只保留包含这些面子的拆法。";
    }

    /// <summary>
    /// 当前手牌所有可供标记为副露的面子。
    /// </summary>
    public ObservableCollection<OpenMeldOption> MeldOptions { get; } = [];

    /// <summary>
    /// 弹窗顶部提示文本。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 是否已经选择至少一个副露面子，可以确认弹窗。
    /// </summary>
    public bool CanConfirm => MeldOptions.Any(option => option.IsSelected);

    /// <summary>
    /// 创建用户确认后的副露面子列表。
    /// </summary>
    /// <returns>带有副露标记的面子列表。</returns>
    public IReadOnlyList<MahjongMeld> CreateSelectedOpenMelds()
    {
        return MeldOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Meld with { IsOpen = true })
            .ToArray();
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
