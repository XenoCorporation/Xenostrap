using System;
using System.Collections.Generic;

namespace Xenostrap.UI.Elements.ContextMenu;

public interface ITextMarkerService
{
	IEnumerable<ITextMarker> TextMarkers { get; }

	ITextMarker Create(int startOffset, int length);

	void RemoveAll(Predicate<ITextMarker> predicate);
}
