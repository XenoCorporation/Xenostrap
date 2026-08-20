using System;

namespace Xenostrap.Models.Attributes;

internal class EnumNameAttribute : Attribute
{
	public string? StaticName { get; set; }

	public string? FromTranslation { get; set; }
}
