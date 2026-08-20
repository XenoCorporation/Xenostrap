using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Xenostrap.Enums;
using Xenostrap.Extensions;
using Xenostrap.Models.SettingTasks.Base;
using Xenostrap.Resources;
using Xenostrap.Utility;

namespace Xenostrap.Models.SettingTasks;

public class ExtractIconsTask : BoolBaseTask
{
	private static readonly IReadOnlyDictionary<string, BootstrapperIcon> AllowedIcons = new Dictionary<string, BootstrapperIcon>
	{
		["Icon2008.ico"] = BootstrapperIcon.Icon2008,
		["Icon2011.ico"] = BootstrapperIcon.Icon2011,
		["Icon2017.ico"] = BootstrapperIcon.Icon2017,
		["Icon2019.ico"] = BootstrapperIcon.Icon2019,
		["Icon2022.ico"] = BootstrapperIcon.Icon2022,
		["IconXenostrap.ico"] = BootstrapperIcon.IconXenostrap,
		["IconEarly2015.ico"] = BootstrapperIcon.IconEarly2015,
		["IconLate2015.ico"] = BootstrapperIcon.IconLate2015
	};

	private static string IconsPath => Path.Combine(Paths.Base, Strings.Paths_Icons);

	private string _path => IconsPath;

	public ExtractIconsTask()
		: base("ExtractIcons")
	{
		OriginalState = Xenostrap.Utility.Platform.IsWindows && Directory.Exists(_path);
	}

	public static void ExtractAll(bool overwrite)
	{
		if (!Xenostrap.Utility.Platform.IsWindows)
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(IconsPath);
			foreach (KeyValuePair<string, BootstrapperIcon> entry in AllowedIcons)
			{
				string destination = Path.Combine(IconsPath, entry.Key);
				if (!overwrite && File.Exists(destination))
				{
					continue;
				}
				Filesystem.AssertReadOnly(destination);
				using FileStream stream = File.Create(destination);
				entry.Value.GetIcon().Save(stream);
			}
		}
		catch (Exception ex)
		{
			App.Logger?.WriteException("ExtractIconsTask::ExtractAll", ex);
		}
	}

	public override void Execute()
	{
		if (!Xenostrap.Utility.Platform.IsWindows)
		{
			NewState = false;
			OriginalState = false;
			return;
		}
		ExtractAll(overwrite: true);
		NewState = Directory.Exists(_path);
		OriginalState = NewState;
	}
}
