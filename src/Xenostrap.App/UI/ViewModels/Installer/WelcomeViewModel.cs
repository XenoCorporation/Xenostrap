using Xenostrap.Resources;

namespace Xenostrap.UI.ViewModels.Installer;

public class WelcomeViewModel : NotifyPropertyChangedViewModel
{
	public string MainText => string.Format(Strings.Installer_Welcome_MainText, "Thank you for downloading Xenostrap. This installation process will be quick and simple, and you will be able to configure any of Xenostrap's settings after installation.");

	public bool CanContinue { get; set; }
}
