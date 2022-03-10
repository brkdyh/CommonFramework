using System;
using System.Collections.Generic;
using System.IO;

internal class GifFileType
{
	private const int MAX_LZW_BITS = 12;

	public int SWidth;

	public int SHeight;

	public int SBackGroundColor;

	public byte[] SColorMap;

	public List<GifImageDesc> SavedImages = new List<GifImageDesc>();

	private static bool zeroDataBlock = false;

	private static byte[] buf = new byte[280];

	private static int curbit;

	private static int lastbit;

	private static int last_byte;

	private static bool done;

	private static bool fresh = false;

	private static int codeSize;

	private static int set_codeSize;

	private static int max_code;

	private static int max_codeSize;

	private static int firstcode;

	private static int oldcode;

	private static int clear_code;

	private static int end_code;

	private static int[,] table = new int[2, 4096];

	private static int[] stack = new int[8192];

	private static int sp;

	public bool Load(Stream stream)
	{
		this.SColorMap = null;
		this.SavedImages.Clear();
		byte[] array = new byte[6];
		if (stream.Read(array, 0, 6) != 6)
		{
			return false;
		}
		if (array[0] != 71 || array[1] != 73 || array[2] != 70)
		{
			return false;
		}
		this.SWidth = GifFileType.ReadUInt16(stream);
		this.SHeight = GifFileType.ReadUInt16(stream);
		//UnityEngine.MonoBehaviour.print(this.SWidth + "," + this.SHeight);
		int num = stream.ReadByte();
		this.SBackGroundColor = stream.ReadByte();
		stream.ReadByte();
		if ((num & 128) != 0)
		{
			this.SColorMap = new byte[(1 << (num & 7) + 1) * 3];
			if (stream.Read(this.SColorMap, 0, this.SColorMap.Length) != this.SColorMap.Length)
			{
				return false;
			}
		}
		else
		{
			this.SColorMap = null;
		}
		GifImageDesc gifImageDesc = default(GifImageDesc);
		while (true)
		{
			int num2 = stream.ReadByte();
			int num3 = num2;
			if (num3 != 33)
			{
				if (num3 == 44)
				{
					gifImageDesc.Left = GifFileType.ReadUInt16(stream);
					gifImageDesc.Top = GifFileType.ReadUInt16(stream);
					gifImageDesc.Width = GifFileType.ReadUInt16(stream);
					gifImageDesc.Height = GifFileType.ReadUInt16(stream);
					int num4 = stream.ReadByte();
					if (num4 == -1)
					{
						break;
					}
					bool interlace = (num4 & 64) != 0;
					if ((num4 & 128) != 0)
					{
						gifImageDesc.ColorMap = new byte[(1 << (num4 & 7) + 1) * 3];
						if (stream.Read(gifImageDesc.ColorMap, 0, gifImageDesc.ColorMap.Length) != gifImageDesc.ColorMap.Length)
						{
							return false;
						}
					}
					else
					{
						gifImageDesc.ColorMap = null;
					}
					gifImageDesc.ImageData = new byte[gifImageDesc.Width * gifImageDesc.Height];
					GifFileType.readImageData(stream, gifImageDesc.ImageData, gifImageDesc.Width, gifImageDesc.Height, interlace);
					this.SavedImages.Add(gifImageDesc);
					//GameManager.Instance.pbStirng += "\n" + gifImageDesc.Width + " X " + gifImageDesc.Height + gifImageDesc.ImageData.Length;
					//UnityEngine.MonoBehaviour.print("Add gif Picture " + SavedImages.Count);
					gifImageDesc = default(GifImageDesc);
				}
			}
			else
			{
				int num5 = stream.ReadByte();
				if (num5 == -1)
				{
					return false;
				}
				int num6 = num5;
				if (num6 == 249)
				{
					stream.ReadByte();
					int num7 = stream.ReadByte();
					gifImageDesc.Dispose = (DisposalMethod)(num7 >> 2 & 15);
					gifImageDesc.Delay = GifFileType.ReadUInt16(stream);
					gifImageDesc.Transparent = stream.ReadByte();
					if ((num7 & 1) == 0)
					{
						gifImageDesc.Transparent = -1;
					}
					stream.ReadByte();
				}
				else
				{
					int num8;
					while ((num8 = stream.ReadByte()) > 0)
					{
						stream.Seek((long)num8, (SeekOrigin)1);
					}
				}
			}
			if (num2 == 59)
			{
				return true;
			}
		}
		return false;
	}

	private static int ReadUInt16(Stream stream)
	{
		int num = stream.ReadByte();
		int num2 = stream.ReadByte();
		return num2 << 8 | num;
	}

	private static void getDataBlock(Stream ifP, byte[] buf, int offset, out bool eofP, out int lengthP)
	{
		int num = ifP.ReadByte();
		if (num == -1)
		{
			eofP = true;
			lengthP = 0;
			return;
		}
		eofP = false;
		lengthP = num;
		if (num == 0)
		{
			GifFileType.zeroDataBlock = true;
			return;
		}
		GifFileType.zeroDataBlock = false;
		ifP.Read(buf, offset, num);
	}

	private static int getCode(Stream ifP, int codeSize, bool first)
	{
		int num;
		if (first)
		{
			GifFileType.buf[0] = 0;
			GifFileType.buf[1] = 0;
			GifFileType.last_byte = 2;
			GifFileType.curbit = 16;
			GifFileType.lastbit = 16;
			GifFileType.done = false;
			num = 0;
		}
		else
		{
			if (GifFileType.curbit + codeSize >= GifFileType.lastbit)
			{
				if (GifFileType.done)
				{
					return -1;
				}
				GifFileType.buf[0] = GifFileType.buf[GifFileType.last_byte - 2];
				GifFileType.buf[1] = GifFileType.buf[GifFileType.last_byte - 1];
				bool flag;
				int num2;
				GifFileType.getDataBlock(ifP, GifFileType.buf, 2, out flag, out num2);
				int num3;
				if (flag)
				{
					num3 = 0;
				}
				else
				{
					num3 = num2;
				}
				if (num3 == 0)
				{
					GifFileType.done = true;
				}
				GifFileType.last_byte = 2 + num3;
				GifFileType.curbit = GifFileType.curbit - GifFileType.lastbit + 16;
				GifFileType.lastbit = (2 + num3) * 8;
			}
			num = 0;
			int num4 = GifFileType.curbit;
			for (int i = 0; i < codeSize; i++)
			{
				num |= ((((int)GifFileType.buf[num4 / 8] & 1 << num4 % 8) != 0) ? 1 : 0) << i;
				num4++;
			}
			GifFileType.curbit += codeSize;
		}
		return num;
	}

	private static int lzwReadByte(Stream ifP, bool first, int input_codeSize)
	{
		if (first)
		{
			GifFileType.set_codeSize = input_codeSize;
			GifFileType.codeSize = GifFileType.set_codeSize + 1;
			GifFileType.clear_code = 1 << GifFileType.set_codeSize;
			GifFileType.end_code = GifFileType.clear_code + 1;
			GifFileType.max_codeSize = 2 * GifFileType.clear_code;
			GifFileType.max_code = GifFileType.clear_code + 2;
			GifFileType.getCode(ifP, 0, true);
			GifFileType.fresh = true;
			int i;
			for (i = 0; i < GifFileType.clear_code; i++)
			{
				GifFileType.table[0, i] = 0;
				GifFileType.table[1, i] = i;
			}
			while (i < 4096)
			{
				GifFileType.table[0, i] = (GifFileType.table[1, i] = 0);
				i++;
			}
			GifFileType.sp = 0;
			return 0;
		}
		if (GifFileType.fresh)
		{
			GifFileType.fresh = false;
			do
			{
				GifFileType.firstcode = (GifFileType.oldcode = GifFileType.getCode(ifP, GifFileType.codeSize, false));
			}
			while (GifFileType.firstcode == GifFileType.clear_code);
			return GifFileType.firstcode;
		}
		if (GifFileType.sp > 0)
		{
			return GifFileType.stack[--GifFileType.sp];
		}
		int j;
		while ((j = GifFileType.getCode(ifP, GifFileType.codeSize, false)) >= 0)
		{
			if (j == GifFileType.clear_code)
			{
				int k;
				for (k = 0; k < GifFileType.clear_code; k++)
				{
					GifFileType.table[0, k] = 0;
					GifFileType.table[1, k] = k;
				}
				while (k < 4096)
				{
					GifFileType.table[0, k] = (GifFileType.table[1, k] = 0);
					k++;
				}
				GifFileType.codeSize = GifFileType.set_codeSize + 1;
				GifFileType.max_codeSize = 2 * GifFileType.clear_code;
				GifFileType.max_code = GifFileType.clear_code + 2;
				GifFileType.sp = 0;
				GifFileType.firstcode = (GifFileType.oldcode = GifFileType.getCode(ifP, GifFileType.codeSize, false));
				return GifFileType.firstcode;
			}
			if (j == GifFileType.end_code)
			{
				if (GifFileType.zeroDataBlock)
				{
					return -2;
				}
				return -2;
			}
			else
			{
				int num = j;
				if (j >= GifFileType.max_code)
				{
					GifFileType.stack[GifFileType.sp++] = GifFileType.firstcode;
					j = GifFileType.oldcode;
				}
				while (j >= GifFileType.clear_code)
				{
					GifFileType.stack[GifFileType.sp++] = GifFileType.table[1, j];
					j = GifFileType.table[0, j];
				}
				GifFileType.stack[GifFileType.sp++] = (GifFileType.firstcode = GifFileType.table[1, j]);
				if ((j = GifFileType.max_code) < 4096)
				{
					GifFileType.table[0, j] = GifFileType.oldcode;
					GifFileType.table[1, j] = GifFileType.firstcode;
					GifFileType.max_code++;
					if (GifFileType.max_code >= GifFileType.max_codeSize && GifFileType.max_codeSize < 4096)
					{
						GifFileType.max_codeSize *= 2;
						GifFileType.codeSize++;
					}
				}
				GifFileType.oldcode = num;
				if (GifFileType.sp > 0)
				{
					return GifFileType.stack[--GifFileType.sp];
				}
			}
		}
		return j;
	}

	private static void readImageData(Stream ifP, byte[] xels, int width, int height, bool interlace)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int input_codeSize = ifP.ReadByte();
		if (GifFileType.lzwReadByte(ifP, true, input_codeSize) < 0)
		{
		}
		int num4;
		while ((num4 = GifFileType.lzwReadByte(ifP, false, input_codeSize)) >= 0)
		{
			xels[num3 * width + num2] = (byte)num4;
			num2++;
			if (num2 == width)
			{
				num2 = 0;
				if (interlace)
				{
					switch (num)
					{
					case 0:
					case 1:
						num3 += 8;
						break;
					case 2:
						num3 += 4;
						break;
					case 3:
						num3 += 2;
						break;
					}
					if (num3 >= height)
					{
						num++;
						if (num > 3)
						{
							return;
						}
						switch (num)
						{
						case 1:
							num3 = 4;
							break;
						case 2:
							num3 = 2;
							break;
						case 3:
							num3 = 1;
							break;
						}
					}
				}
				else
				{
					num3++;
				}
			}
			if (num3 >= height)
			{
				return;
			}
		}
	}
}
