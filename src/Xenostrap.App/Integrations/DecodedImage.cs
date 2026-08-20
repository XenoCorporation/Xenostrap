using System;

namespace Xenostrap.Integrations;

public sealed class DecodedImage
{
	public int Width { get; init; }

	public int Height { get; init; }

	public byte[] Bgra { get; init; } = Array.Empty<byte>();
}
