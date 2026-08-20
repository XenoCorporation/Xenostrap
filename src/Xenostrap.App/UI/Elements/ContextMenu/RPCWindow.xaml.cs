using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using Xenostrap.UI.Elements.Base;
using Xenostrap.UI.ViewModels.ContextMenu;

namespace Xenostrap.UI.Elements.ContextMenu;

public partial class RPCWindow : WpfUiWindow{

	public RPCWindow()
	{
		InitializeComponent();
		base.DataContext = RPCCustomizerViewModel.Shared;
	}
}
