using System;

internal struct GifImageDesc
{
	public int Delay;

	public DisposalMethod Dispose;

	public int Transparent;

	public int Left;

	public int Top;

	public int Width;

	public int Height;

	public byte[] ColorMap;

	public byte[] ImageData;
}
