using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using Xenostrap.UI.Elements.Base;
using Xenostrap.UI.ViewModels.Dialogs;

namespace Xenostrap.UI.Elements.Dialogs;

public partial class UninstallerDialog : WpfUiWindow{
	private readonly UninstallerViewModel _viewModel;

	public bool Confirmed { get; private set; }

	public bool KeepData { get; private set; } = true;

	public UninstallerDialog()
	{
		_viewModel = new UninstallerViewModel();
		_viewModel.ConfirmUninstallRequest += OnConfirmUninstallRequest;
		base.DataContext = _viewModel;
		InitializeComponent();
		base.Closed += OnClosed;
	}

	private void OnConfirmUninstallRequest(object? sender, EventArgs e)
	{
		Confirmed = true;
		KeepData = _viewModel.KeepData;
		Close();
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		_viewModel.ConfirmUninstallRequest -= OnConfirmUninstallRequest;
		base.Closed -= OnClosed;
	}
}
