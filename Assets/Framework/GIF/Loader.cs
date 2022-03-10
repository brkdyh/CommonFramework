using System;
using System.IO;
using UnityEngine;

internal class Loader
{
	private const int LZ_MAX_CODE = 4095;

	public GifFileType m_pGifFile = new GifFileType();

	private bool m_Loaded;

	private int m_NextIndex;

	private Color32[] m_Colors;

	private Color32[] m_ColorsBak;

	public bool Load(Stream gifStream)
	{
		this.m_Loaded = this.m_pGifFile.Load(gifStream);
		if (this.m_Loaded)
		{
			this.m_Colors = new Color32[this.m_pGifFile.SWidth * this.m_pGifFile.SHeight];
			//MonoBehaviour.print("m_Colors = " + m_Colors.Length);
			this.m_NextIndex = 0;
		}
		return this.m_Loaded;
	}

	public int GetFrameCount()
	{
		if (this.m_Loaded)
		{
			return this.m_pGifFile.SavedImages.Count;
		}
		return 0;
	}

	int frameCount = 0;

	public bool GetNextFrame(out Texture2D texture, out float delay)
	{
		texture = null;
		delay = 3.40282347E+38f;
		if (!this.m_Loaded)
		{
			return false;
		}
		Color32 color = new Color32(this.m_pGifFile.SColorMap[this.m_pGifFile.SBackGroundColor * 3], this.m_pGifFile.SColorMap[this.m_pGifFile.SBackGroundColor * 3 + 1], this.m_pGifFile.SColorMap[this.m_pGifFile.SBackGroundColor * 3 + 2], 0);
		if (this.m_NextIndex == 0)
		{
			for (int i = 0; i < this.m_Colors.Length; i++)
			{
				this.m_Colors[i] = color;
			}
		}
		GifImageDesc gifImageDesc = this.m_pGifFile.SavedImages[this.m_NextIndex];
		if (gifImageDesc.Dispose == DisposalMethod.RESTORE)
		{
			if (this.m_ColorsBak == null)
			{
				this.m_ColorsBak = new Color32[this.m_Colors.Length];
			}
			Array.Copy(this.m_Colors, this.m_ColorsBak, this.m_Colors.Length);
		}
		byte[] imageData = gifImageDesc.ImageData;
		byte[] pColorTable = (gifImageDesc.ColorMap == null) ? this.m_pGifFile.SColorMap : gifImageDesc.ColorMap;
		int num = 0;
		int j = 0;
		while (j < gifImageDesc.Height)
		{
			this.CopyGIF(this.m_Colors, (this.m_pGifFile.SHeight - 1 - (gifImageDesc.Top + j)) * this.m_pGifFile.SWidth + gifImageDesc.Left, imageData, num, gifImageDesc.Width, gifImageDesc.Transparent, pColorTable);
			j++;
			num += gifImageDesc.Width;
		}
		//Debug.Log(this.m_pGifFile.SWidth + "," + this.m_pGifFile.SHeight);

		texture = new Texture2D(this.m_pGifFile.SWidth, this.m_pGifFile.SHeight, TextureFormat.RGBA32, false);
		frameCount++;

		//Debug.Log(texture.width + "," + texture.height + " mip map count = " + texture.mipmapCount + "frameCount = " + frameCount);
		texture.SetPixels32(this.m_Colors);
        //texture.Compress(true);
        texture.Apply();

		//MonoBehaviour.print(texture.width + "," + texture.height);
		for (int k = 0; k < gifImageDesc.Height; k++)
		{
			for (int l = 0; l < gifImageDesc.Width; l++)
			{
				int num2 = (this.m_pGifFile.SHeight - 1 - (gifImageDesc.Top + k)) * this.m_pGifFile.SWidth + gifImageDesc.Left + l;
				switch (gifImageDesc.Dispose)
				{
				case DisposalMethod.BACKGND:
					this.m_Colors[num2] = color;
					break;
				case DisposalMethod.RESTORE:
					this.m_Colors[num2] = this.m_ColorsBak[num2];
					break;
				}
			}
		}
		if (this.m_pGifFile.SavedImages.Count == 1)
		{
			delay = 3.40282347E+38f;
		}
		else if (gifImageDesc.Delay <= 0)
		{
			delay = 0.1f;
		}
		else
		{
			delay = (float)gifImageDesc.Delay / 100f;
		}
		this.m_NextIndex = (this.m_NextIndex + 1) % this.m_pGifFile.SavedImages.Count;
		return true;
	}

	public void Restart()
	{
		this.m_NextIndex = 0;
	}

	private void CopyGIF(Color32[] pDst, int offset, byte[] pSrc, int offestSrc, int width, int transparent, byte[] pColorTable)
	{
		if (width != 0)
		{
			do
			{
				byte b = pSrc[offestSrc++];
				if ((int)b != transparent)
				{
					pDst[offset].r = pColorTable[(int)(b * 3)];
					pDst[offset].g = pColorTable[(int)(b * 3 + 1)];
					pDst[offset].b = pColorTable[(int)(b * 3 + 2)];
					pDst[offset].a = 255;
				}
				offset++;
			}
			while (--width != 0);
		}
	}

	private void LZWDecompress(int codeSize, byte[] input, byte[] output)
	{
		int num = 0;
		byte[] array = new byte[4095];
		byte[] array2 = new byte[4096];
		uint[] array3 = new uint[4096];
		int num2 = 1 << codeSize;
		int num3 = num2 + 1;
		int num4 = num3 + 1;
		int num5 = codeSize + 1;
		int num6 = 1 << num5;
		int num7 = 0;
		int num8 = 4098;
		int i = 0;
		uint num9 = 0u;
		int j;
		for (j = 0; j <= 4095; j++)
		{
			array3[j] = 4098u;
		}
		int num10 = 0;
		j = 0;
		while (j < output.Length)
		{
			ushort[] array4 = new ushort[]
			{
				0,
				1,
				3,
				7,
				15,
				31,
				63,
				127,
				255,
				511,
				1023,
				2047,
				4095
			};
			if (num5 > 12)
			{
				Debug.Break();
			}
			while (i < num5)
			{
				byte b = input[num++];
				num9 |= (uint)((uint)b << i);
				i += 8;
			}
			int num11 = (int)(num9 & (uint)array4[num5]);
			num9 >>= num5;
			i -= num5;
			if (num4 < 4097 && ++num4 > num6 && num5 < 12)
			{
				num6 <<= 1;
				num5++;
			}
			if (num11 == num3)
			{
				j++;
			}
			else if (num11 == num2)
			{
				for (int k = 0; k <= 4095; k++)
				{
					array3[k] = 4098u;
				}
				num4 = num3 + 1;
				num5 = codeSize + 1;
				num6 = 1 << num5;
				num8 = 4098;
			}
			else
			{
				if (num11 < num2)
				{
					output[j++] = (byte)num11;
				}
				else
				{
					if (array3[num11] == 4098u)
					{
						if (num11 == num4 - 2)
						{
							num10 = num8;
							array2[num4 - 2] = (array[num7++] = (byte)Loader.DGifGetPrefixChar(array3, num8, num2));
						}
						else
						{
							Debug.Break();
						}
					}
					else
					{
						num10 = num11;
					}
					int k = 0;
					while (k++ <= 4095 && num10 > num2 && num10 <= 4095)
					{
						array[num7++] = array2[num10];
						num10 = (int)array3[num10];
					}
					if (k >= 4095 || num10 > 4095)
					{
						Debug.Break();
					}
					array[num7++] = (byte)num10;
					while (num7 != 0 && j < output.Length)
					{
						output[j++] = array[--num7];
					}
				}
				if (num8 != 4098)
				{
					array3[num4 - 2] = (uint)num8;
					if (num11 == num4 - 2)
					{
						array2[num4 - 2] = (byte)Loader.DGifGetPrefixChar(array3, num8, num2);
					}
					else
					{
						array2[num4 - 2] = (byte)Loader.DGifGetPrefixChar(array3, num11, num2);
					}
				}
				num8 = num11;
			}
		}
	}

	private static int DGifGetPrefixChar(uint[] Prefix, int Code, int ClearCode)
	{
		int num = 0;
		while (Code > ClearCode && num++ <= 4095)
		{
			Code = (int)Prefix[Code];
		}
		return Code;
	}
}
