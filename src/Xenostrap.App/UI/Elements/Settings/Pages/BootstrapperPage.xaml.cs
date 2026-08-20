using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Xenostrap.UI.Elements.Controls;
using Xenostrap.UI.ViewModels.Settings;
using Wpf.Ui.Controls;

namespace Xenostrap.UI.Elements.Settings.Pages;

public partial class BehaviourPage : UiPage{

	public BehaviourPage()
	{
		base.DataContext = new BehaviourViewModel();
		InitializeComponent();
	}

	private void ResetDatacenters_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is BehaviourViewModel behaviourViewModel)
		{
			behaviourViewModel.ResetDatacenters();
		}
	}
}
