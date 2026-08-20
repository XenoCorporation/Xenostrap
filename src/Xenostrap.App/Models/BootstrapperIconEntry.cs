using System.Windows.Media;
using Xenostrap.Enums;
using Xenostrap.Extensions;

namespace Xenostrap.Models;

public class BootstrapperIconEntry
{
	public BootstrapperIcon IconType { get; set; }

	public ImageSource ImageSource => IconType.GetIcon().GetImageSource();
}
