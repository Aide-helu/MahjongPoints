using System;

namespace MahjongPoints.Models;

/// <summary>
/// 标记 <see cref="Services.Scoring.MahjongScoringContext"/> 中可作为界面选项显示的布尔属性。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MahjongScoringOptionAttribute : Attribute
{
    /// <summary>
    /// 创建算点选项标记。
    /// </summary>
    /// <param name="displayName">界面显示名称。</param>
    /// <param name="order">界面排序值。</param>
    public MahjongScoringOptionAttribute(string displayName, int order)
    {
        DisplayName = displayName;
        Order = order;
    }

    /// <summary>
    /// 界面显示名称。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 界面排序值。
    /// </summary>
    public int Order { get; }
}
