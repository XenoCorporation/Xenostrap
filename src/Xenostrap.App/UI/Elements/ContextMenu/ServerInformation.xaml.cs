using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using Xenostrap.UI.Elements.Base;
using Xenostrap.UI.ViewModels.ContextMenu;
using Wpf.Ui.Controls;

namespace Xenostrap.UI.Elements.ContextMenu;

public partial class ServerInformation : WpfUiWindow{
	private readonly ServerInformationViewModel _viewModel;

	public ServerInformation(Watcher watcher)
	{
		_viewModel = new ServerInformationViewModel(watcher);
		base.DataContext = _viewModel;
		InitializeComponent();
		base.Closed += ServerInformation_Closed;
	}

	private void ServerInformation_Closed(object? sender, EventArgs e)
	{
		base.Closed -= ServerInformation_Closed;
		_viewModel.Dispose();
		base.DataContext = null;
	}
}
