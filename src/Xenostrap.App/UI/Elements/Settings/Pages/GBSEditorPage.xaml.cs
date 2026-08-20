using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using Xenostrap.UI.ViewModels.Settings;
using Wpf.Ui.Controls;

namespace Xenostrap.UI.Elements.Settings.Pages;

public partial class GBSEditorPage : UiPage{
	private GBSEditorViewModel _viewModel;

	public GBSEditorPage()
	{
		SetupViewModel();
		InitializeComponent();
	}

	private void SetupViewModel()
	{
		_viewModel = new GBSEditorViewModel();
		base.DataContext = _viewModel;
	}

	private void ValidateUInt32(object sender, TextCompositionEventArgs e)
	{
		e.Handled = !uint.TryParse(e.Text, out var _);
	}

	private void ValidateFloat(object sender, TextCompositionEventArgs e)
	{
		e.Handled = !float.TryParse(e.Text, out var _);
	}
}
