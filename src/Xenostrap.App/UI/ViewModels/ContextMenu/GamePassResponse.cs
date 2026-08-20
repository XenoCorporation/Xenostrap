using System.Collections.Generic;

namespace Xenostrap.UI.ViewModels.ContextMenu;

internal class GamePassResponse
{
	public List<GamePassData> GamePasses { get; set; } = new List<GamePassData>();
}
