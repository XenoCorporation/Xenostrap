using System.Collections.Generic;
using Xenostrap.Models.Persistable;

namespace Xenostrap.AppData;

internal interface IAppData
{
	string ProductName { get; }

	string BinaryType { get; }

	string RegistryName { get; }

	string ExecutableName { get; }

	string VersionsRoot { get; }

	string Directory { get; }

	string ExecutablePath { get; }

	AppState State { get; }

	IReadOnlyDictionary<string, string> PackageDirectoryMap { get; set; }

	IReadOnlyList<string> CandidateCriticalFiles { get; }
}
