using System;

namespace Xenostrap.Models.Attributes;

internal class EnumSortAttribute : Attribute
{
	public int Order { get; set; }
}
