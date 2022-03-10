using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Gif2Textures
{
	public class GifFrames
	{
		public struct Frame
		{
			public Texture2D texture;

			public float delay;
		}

		private int m_NextIndex;

		public List<GifFrames.Frame> m_Frames;

		private bool m_CacheTextures;

		private Loader m_Loader;

        public Vector2 GetGIFSize()
        {
			return new Vector2(m_Loader.m_pGifFile.SWidth, m_Loader.m_pGifFile.SHeight);
        }

		private Texture2D m_LastTexture;

		public bool Load(Stream stream, bool cacheTextures = true)
		{
			this.m_CacheTextures = cacheTextures;
			bool result;
			if (this.m_CacheTextures)
			{
				Loader loader = new Loader();
				result = loader.Load(stream);
				this.m_Frames = new List<GifFrames.Frame>();
				for (int i = 0; i < loader.GetFrameCount(); i++)
				{
					GifFrames.Frame frame = default(GifFrames.Frame);
					loader.GetNextFrame(out frame.texture, out frame.delay);
					this.m_Frames.Add(frame);
				}
				this.m_NextIndex = 0;
			}
			else
			{
				this.m_Loader = new Loader();
				result = this.m_Loader.Load(stream);
			}
			return result;
		}

        public List<Texture> CacheTextures()
        {
			this.m_Frames = new List<GifFrames.Frame>();
			for (int i = 0; i < m_Loader.GetFrameCount(); i++)
			{
				GifFrames.Frame frame = default(GifFrames.Frame);
				m_Loader.GetNextFrame(out frame.texture, out frame.delay);
				//Debug.Log("Add Frame ");
				this.m_Frames.Add(frame);
			}
			this.m_NextIndex = 0;

			this.m_CacheTextures = true;

			m_Loader = null;
			return Frame2Texture();
		}

        public List<Texture> Frame2Texture()
        {
			List<Texture> texList = new List<Texture>();
			foreach (var f in m_Frames)
			{
				texList.Add(f.texture);
			}
			return texList;
		}

        public List<float> Frame2Delay()
        {
			List<float> delayList = new List<float>();
			foreach (var f in m_Frames)
			{
				delayList.Add(f.delay);
			}
			return delayList;
		}

		public int GetFrameCount()
		{
			if (this.m_CacheTextures)
			{
				return this.m_Frames.Count;
			}
			return this.m_Loader.GetFrameCount();
		}

		public bool GetNextFrame(out Texture2D texture, out float delay)
		{
			bool result;
			if (this.m_CacheTextures)
			{
				if (this.m_Frames != null && this.m_Frames.Count > 0)
				{
					texture = this.m_Frames[this.m_NextIndex].texture;
					delay = this.m_Frames[this.m_NextIndex].delay;
					this.m_NextIndex = (this.m_NextIndex + 1) % this.m_Frames.Count;
					result = true;
				}
				else
				{
					texture = null;
					delay = 3.40282347E+38f;
					result = false;
				}
			}
			else
			{
				UnityEngine.Object.Destroy(this.m_LastTexture);
				result = this.m_Loader.GetNextFrame(out this.m_LastTexture, out delay);
				texture = this.m_LastTexture;
			}
			return result;
		}

		public void Restart()
		{
			if (this.m_CacheTextures)
			{
				this.m_NextIndex = 0;
				return;
			}
			this.m_Loader.Restart();
		}
	}
}
