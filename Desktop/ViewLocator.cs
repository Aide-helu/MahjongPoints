using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MahjongPoints.ViewModels;

namespace MahjongPoints;

/// <summary>
/// 根据 ViewModel 类型定位对应的 View 类型。
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// 根据传入的 ViewModel 创建对应的视图控件。
    /// </summary>
    /// <param name="param">需要匹配视图的 ViewModel 实例。</param>
    /// <returns>匹配到的视图控件；未匹配时返回错误提示控件。</returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// 判断当前数据对象是否应由此视图定位器处理。
    /// </summary>
    /// <param name="data">待匹配的数据对象。</param>
    /// <returns>如果数据对象是 ViewModel，则返回 <c>true</c>。</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
