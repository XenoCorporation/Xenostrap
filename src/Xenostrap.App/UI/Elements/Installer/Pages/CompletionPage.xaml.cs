using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using Xenostrap.Enums;
using Xenostrap.Resources;
using Xenostrap.UI.ViewModels.Installer;
using Wpf.Ui.Controls;

namespace Xenostrap.UI.Elements.Installer.Pages;

public partial class CompletionPage : UiPage{
	private readonly CompletionViewModel _viewModel = new CompletionViewModel();

	public CompletionPage()
	{
		_viewModel.CloseWindowRequest += OnCloseWindowRequest;
		base.DataContext = _viewModel;
		InitializeComponent();
		Unloaded += OnPageUnloaded;
	}

	private void OnCloseWindowRequest(object? sender, NextAction closeAction)
	{
		if (Window.GetWindow((DependencyObject)(object)this) is MainWindow mainWindow)
		{
			mainWindow.CloseAction = closeAction;
			mainWindow.Close();
		}
	}

	private void OnPageUnloaded(object sender, RoutedEventArgs e)
	{
		Unloaded -= OnPageUnloaded;
		_viewModel.CloseWindowRequest -= OnCloseWindowRequest;
		_viewModel.Dispose();
		DataContext = null;
	}

	private void UiPage_Loaded(object sender, RoutedEventArgs e)
	{
		if (Window.GetWindow((DependencyObject)(object)this) is MainWindow mainWindow)
		{
			mainWindow.SetNextButtonText(Strings.Common_Navigation_Next);
			mainWindow.SetButtonEnabled("back", state: false);
		}
	}
}
