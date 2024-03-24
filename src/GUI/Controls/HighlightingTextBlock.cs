/*
MIT License

Copyright (c) 2018 Dean Chalk

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. 
*/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace ModManager.Controls;

/// <summary>
/// A TextBlock that highlights text, adapted from:
/// Source: https://github.com/deanchalk/SearchMatchTextblock
/// </summary>
[TemplatePart(Name = HighlightTextBlockName, Type = typeof(TextBlock))]
public class HighlightingTextBlock : Control
{
    private const string HighlightTextBlockName = "PART_HighlightTextblock";

    private static readonly DependencyProperty HighlightStartProperty = DependencyProperty.Register("HighlightStart", typeof(int), 
        typeof(HighlightingTextBlock), new PropertyMetadata(0, OnHighlightStartPropertyChanged));
    private static readonly DependencyProperty HighlightEndProperty = DependencyProperty.Register("HighlightEnd", typeof(int), 
        typeof(HighlightingTextBlock), new PropertyMetadata(0, OnHighlightEndPropertyChanged));

    public static readonly DependencyProperty TextProperty =
        TextBlock.TextProperty.AddOwner(
            typeof(HighlightingTextBlock),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public static readonly DependencyProperty TextWrappingProperty = TextBlock.TextWrappingProperty.AddOwner(
        typeof(HighlightingTextBlock),
        new PropertyMetadata(TextWrapping.NoWrap));

    public static readonly DependencyProperty TextTrimmingProperty = TextBlock.TextTrimmingProperty.AddOwner(
        typeof(HighlightingTextBlock),
        new PropertyMetadata(TextTrimming.None));

    public static readonly DependencyProperty HighlightForegroundProperty =
        DependencyProperty.Register("HighlightForeground", typeof(Brush),
            typeof(HighlightingTextBlock),
            new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty HighlightBackgroundProperty =
        DependencyProperty.Register("HighlightBackground", typeof(Brush),
            typeof(HighlightingTextBlock),
            new PropertyMetadata(Brushes.Blue));

    private TextBlock highlightTextBlock;

    static HighlightingTextBlock()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HighlightingTextBlock),
            new FrameworkPropertyMetadata(typeof(HighlightingTextBlock)));
    }

    public Brush HighlightBackground
    {
        get => (Brush)GetValue(HighlightBackgroundProperty);
        set => SetValue(HighlightBackgroundProperty, value);
    }

    public Brush HighlightForeground
    {
        get => (Brush)GetValue(HighlightForegroundProperty);
        set => SetValue(HighlightForegroundProperty, value);
    }

    public int HighlightStart
    {
        get => (int)GetValue(HighlightStartProperty);
        set => SetValue(HighlightStartProperty, value);
    }

    public int HighlightEnd
    {
        get => (int)GetValue(HighlightEndProperty);
        set => SetValue(HighlightEndProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => (TextTrimming)GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    private static void OnHighlightStartPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textblock = (HighlightingTextBlock)d;
        textblock.ProcessTextChanged(textblock.Text, (int)e.NewValue, textblock.HighlightEnd);
    }

    private static void OnHighlightEndPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textblock = (HighlightingTextBlock)d;
        textblock.ProcessTextChanged(textblock.Text, textblock.HighlightStart, (int)e.NewValue);
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textblock = (HighlightingTextBlock)d;
        textblock.ProcessTextChanged(e.NewValue as string, textblock.HighlightStart, textblock.HighlightEnd);
    }

    private void ProcessTextChanged(string mainText, int highlightStart, int highlightEnd)
    {
        if (highlightTextBlock == null)
            return;
        highlightTextBlock.Inlines.Clear();
        if (highlightTextBlock == null || string.IsNullOrWhiteSpace(mainText)) return;

        var start = Math.Max(0, highlightStart);
        var end = Math.Min(mainText.Length, highlightEnd);
        var highlightLength = Math.Min(mainText.Length, end - start);

        if (start == end || highlightLength <= 0)
        {
            var completeRun = new Run(mainText);
            highlightTextBlock.Inlines.Add(completeRun);
            return;
        }

        var highlightedText = mainText.Substring(start, highlightLength);

        if (start > 0) highlightTextBlock.Inlines.Add(GetRunForText(mainText.Substring(0, start), false));
        highlightTextBlock.Inlines.Add(GetRunForText(highlightedText, true));
        if (end < mainText.Length)
        {
            highlightTextBlock.Inlines.Add(GetRunForText(mainText.Substring(end), false));
        }
    }

    private Run GetRunForText(string text, bool isHighlighted)
    {
        var textRun = new Run(text)
        {
            Foreground = isHighlighted ? HighlightForeground : Foreground,
            Background = isHighlighted ? HighlightBackground : Background
        };
        if(isHighlighted)
        {
            textRun.SetBinding(ForegroundProperty, new Binding(nameof(HighlightForeground)) { Source = this });
            textRun.SetBinding(BackgroundProperty, new Binding(nameof(HighlightBackground)) { Source = this });
        }
        return textRun;
    }

    public override void OnApplyTemplate()
    {
        highlightTextBlock = GetTemplateChild(HighlightTextBlockName) as TextBlock;
        if (highlightTextBlock == null)
            return;
        ProcessTextChanged(Text, HighlightStart, HighlightEnd);
    }
}
