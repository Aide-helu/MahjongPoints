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

    private MahjongScoringOptionItem(
        MahjongScoringContext context,
        PropertyInfo property,
        MahjongScoringOptionAttribute option)
    {
        _context = context;
        _property = property;
        Key = property.Name;
        DisplayName = option.DisplayName;

        _context.PropertyChanged += Context_OnPropertyChanged;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public bool IsChecked
    {
        get => (bool)(_property.GetValue(_context) ?? false);
        set
        {
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
        if (string.Equals(e.PropertyName, Key, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(IsChecked));
        }
    }
}
