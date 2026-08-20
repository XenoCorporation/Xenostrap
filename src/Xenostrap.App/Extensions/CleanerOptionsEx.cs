using System.Collections.Generic;
using Xenostrap.Enums;

namespace Xenostrap.Extensions;

internal static class CleanerOptionsEx
{
	public static IReadOnlyCollection<CleanerOptions> Selections { get; } = new CleanerOptions[9]
	{
		CleanerOptions.Never,
		CleanerOptions.AfterLaunch,
		CleanerOptions.OneDay,
		CleanerOptions.OneWeek,
		CleanerOptions.TwoWeeks,
		CleanerOptions.ThreeWeeks,
		CleanerOptions.OneMonth,
		CleanerOptions.TwoMonths,
		CleanerOptions.ThreeMonths
	};
}
