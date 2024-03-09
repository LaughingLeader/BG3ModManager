using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DivinityModManager.Controls.Util;

/// <summary>
/// Source: https://stackoverflow.com/a/45627524
/// </summary>
/// <param name="textContainer"></param>
/// <param name="uiScope"></param>
/// <param name="isUndoEnabled"></param>
public class TextEditorWrapper(object textContainer, FrameworkElement uiScope, bool isUndoEnabled)
{
	private readonly object _editor = Activator.CreateInstance(TextEditorType, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance,
			null, [textContainer, uiScope, isUndoEnabled], null);

	private static readonly Type TextEditorType = Type.GetType("System.Windows.Documents.TextEditor, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
	private static readonly PropertyInfo IsReadOnlyProp = TextEditorType.GetProperty("IsReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly PropertyInfo TextViewProp = TextEditorType.GetProperty("TextView", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly MethodInfo RegisterMethod = TextEditorType.GetMethod("RegisterCommandHandlers",
		BindingFlags.Static | BindingFlags.NonPublic, null, [typeof(Type), typeof(bool), typeof(bool), typeof(bool)], null);

	private static readonly Type TextContainerType = Type.GetType("System.Windows.Documents.ITextContainer, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
	private static readonly PropertyInfo TextContainerTextViewProp = TextContainerType.GetProperty("TextView");

	private static readonly PropertyInfo TextContainerProp = typeof(TextBlock).GetProperty("TextContainer", BindingFlags.Instance | BindingFlags.NonPublic);

	public static void RegisterCommandHandlers(Type controlType, bool acceptsRichContent, bool readOnly, bool registerEventListeners)
	{
		RegisterMethod.Invoke(null, [controlType, acceptsRichContent, readOnly, registerEventListeners]);
	}

	public static TextEditorWrapper CreateFor(TextBlock tb)
	{
		var textContainer = TextContainerProp.GetValue(tb);

		var editor = new TextEditorWrapper(textContainer, tb, false);
		IsReadOnlyProp.SetValue(editor._editor, true);
		TextViewProp.SetValue(editor._editor, TextContainerTextViewProp.GetValue(textContainer));

		return editor;
	}
}
