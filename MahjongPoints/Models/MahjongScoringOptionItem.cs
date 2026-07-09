using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.Models;

public sealed class MahjongScoringOptionItem : ObservableObject
{
    private readonly MahjongScoringContext _context;
    private readonly PropertyInfo _property;
    private bool _isUpdatingDisabledValue;

    private MahjongScoringOptionItem(
        MahjongScoringContext context,
        PropertyInfo property,
        MahjongScoringOptionAttribute option)
    {
        _context = context;
        _property = property;
        Key = property.Name;
        DisplayName = option.DisplayName.StartsWith("是否", StringComparison.Ordinal)
            ? option.DisplayName[2..]
            : option.DisplayName;

        _context.PropertyChanged += Context_OnPropertyChanged;
    }

    public string Key { get; }

    public string DisplayName { get; }

    /// <summary>
    /// 当前选项是否允许用户勾选。
    /// </summary>
    public bool IsEnabled => CanEnableOption(Key, _context);

    public bool IsChecked
    {
        get => (bool)(_property.GetValue(_context) ?? false);
        set
        {
            if (value && !IsEnabled)
            {
                return;
            }

            if (IsChecked == value)
            {
                return;
            }

            _property.SetValue(_context, value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 从 <see cref="MahjongScoringContext"/> 类型中提取所有带有 <see cref="MahjongScoringOptionAttribute"/> 特性的布尔属性，
    /// 并按特性中指定的顺序生成可绑定的选项项集合。
    /// </summary>
    /// <param name="context">麻将计分上下文实例，用于绑定选项的实际值。</param>
    /// <returns>按 Order 排序后的只读选项列表，每个选项可绑定到 UI 控件。</returns>
    public static IReadOnlyList<MahjongScoringOptionItem> CreateItems(MahjongScoringContext context)
    {
        // 获取 MahjongScoringContext 类型的所有公开实例属性
        return typeof(MahjongScoringContext)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            // 为每个属性提取其对应的 MahjongScoringOptionAttribute 特性
            .Select(property => new
            {
                Property = property,
                Option = property.GetCustomAttribute<MahjongScoringOptionAttribute>()
            })
            // 筛选出满足条件的项：
            // 1. 特性存在
            // 2. 属性类型为 bool
            // 3. 属性可读且可写（支持双向绑定）
            .Where(item =>
                item.Option is not null &&
                item.Property.PropertyType == typeof(bool) &&
                item.Property.CanRead &&
                item.Property.CanWrite)
            // 按特性中指定的 Order 值升序排列，决定 UI 上的显示顺序
            .OrderBy(item => item.Option!.Order)
            // 将每个有效属性包装为 MahjongScoringOptionItem 实例
            // 该包装类负责：
            // - 提供 DisplayName（来自特性）
            // - 实现与 context 中实际布尔属性的双向绑定
            // - 支持属性变更通知（INotifyPropertyChanged）
            .Select(item => new MahjongScoringOptionItem(context, item.Property, item.Option!))
            .ToArray(); // 返回数组，但以 IReadOnlyList<T> 接口形式暴露
    }

    private void Context_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshAvailability();

        if (string.Equals(e.PropertyName, Key, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(IsChecked));
        }
    }

    /// <summary>
    /// 根据当前上下文刷新选项可用状态；如果选项变为不可用，会自动取消勾选。
    /// </summary>
    private void RefreshAvailability()
    {
        OnPropertyChanged(nameof(IsEnabled));

        if (IsEnabled || !IsChecked || _isUpdatingDisabledValue)
        {
            return;
        }

        try
        {
            _isUpdatingDisabledValue = true;
            _property.SetValue(_context, false);
            OnPropertyChanged(nameof(IsChecked));
        }
        finally
        {
            _isUpdatingDisabledValue = false;
        }
    }

    /// <summary>
    /// 判断指定选项在当前算点上下文下是否可用。
    /// </summary>
    /// <param name="key">选项对应的上下文属性名。</param>
    /// <param name="context">当前算点上下文。</param>
    /// <returns>如果允许用户勾选该选项，则返回 <c>true</c>。</returns>
    private static bool CanEnableOption(string key, MahjongScoringContext context)
    {
        return key switch
        {
            // 立直、双立直、一发都要求门前清；副露时不可选择。
            nameof(MahjongScoringContext.IsRiichi) => context.IsMenzen && !context.IsDoubleRiichi,
            nameof(MahjongScoringContext.IsDoubleRiichi) => context.IsMenzen && !context.IsRiichi,
            nameof(MahjongScoringContext.IsIppatsu) => context.IsMenzen && (context.IsRiichi || context.IsDoubleRiichi),

            // 已经选择立直相关状态后，副露不再可选。
            nameof(MahjongScoringContext.IsOpenHand) =>
                !context.IsRiichi &&
                !context.IsDoubleRiichi &&
                !context.IsIppatsu,

            // 抢杠、河底是荣和语境；已经选择它们时，自摸不再可选。
            nameof(MahjongScoringContext.IsTsumo) =>
                !context.IsRobKong &&
                !context.IsHeDi,

            // 抢杠、河底是荣和语境；自摸时不可选择。
            nameof(MahjongScoringContext.IsRobKong) =>
                !context.IsTsumo &&
                !context.IsRidgeBlossom &&
                !context.IsHaiDi &&
                !context.IsHeDi,
            nameof(MahjongScoringContext.IsHeDi) =>
                !context.IsTsumo &&
                !context.IsRobKong &&
                !context.IsRidgeBlossom &&
                !context.IsHaiDi,

            // 岭上开花和海底捞月是自摸语境，且与抢杠、河底互斥。
            nameof(MahjongScoringContext.IsRidgeBlossom) =>
                context.IsTsumo &&
                !context.IsRobKong &&
                !context.IsHeDi &&
                !context.IsHaiDi,
            nameof(MahjongScoringContext.IsHaiDi) =>
                context.IsTsumo &&
                !context.IsRobKong &&
                !context.IsHeDi &&
                !context.IsRidgeBlossom,

            _ => true
        };
    }
}
