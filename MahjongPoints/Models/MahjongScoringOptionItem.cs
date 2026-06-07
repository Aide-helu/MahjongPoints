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

    public static IReadOnlyList<MahjongScoringOptionItem> CreateItems(MahjongScoringContext context)
    {
        return typeof(MahjongScoringContext)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => new
            {
                Property = property,
                Option = property.GetCustomAttribute<MahjongScoringOptionAttribute>()
            })
            .Where(item =>
                item.Option is not null &&
                item.Property.PropertyType == typeof(bool) &&
                item.Property.CanRead &&
                item.Property.CanWrite)
            .OrderBy(item => item.Option!.Order)
            .Select(item => new MahjongScoringOptionItem(context, item.Property, item.Option!))
            .ToArray();
    }

    private void Context_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, Key, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(IsChecked));
        }
    }
}
