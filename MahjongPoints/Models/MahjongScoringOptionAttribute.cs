using System;

namespace MahjongPoints.Models;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MahjongScoringOptionAttribute : Attribute
{
    public MahjongScoringOptionAttribute(string displayName, int order)
    {
        DisplayName = displayName;
        Order = order;
    }

    public string DisplayName { get; }

    public int Order { get; }
}
