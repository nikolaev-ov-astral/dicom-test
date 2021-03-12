using System.IO;
using System;
using System.Collections.Generic;
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using System.Runtime.InteropServices;
using Dicom.Imaging.Codec;
using Dicom.Imaging.Codec.JpegLossless;
using Dicom.IO;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Threading;

namespace DicomTest
{
	static public class SixLaborsImageExtensions
	{
		static public unsafe byte[] ToPixelData(this SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb48> image)
		{
			const int pixelSize = 6;
			byte[] pixelData = new byte[pixelSize * image.Width * image.Height];
			fixed (byte* p = &pixelData[0])
				for (int y = 0; y < image.Height; y++)
				{
					Span<Rgb48> span = image.GetPixelRowSpan(y);
					fixed (Rgb48* p2 = &span[0])
						Buffer.MemoryCopy(p2, p + y * image.Width * pixelSize, image.Width * pixelSize, image.Width * pixelSize);
				}
			return pixelData;
		}
		static public unsafe byte[] ToPixelData(this SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.L16> image)
		{
			const int pixelSize = 2;
			byte[] pixelData = new byte[pixelSize * image.Width * image.Height];
			fixed (byte* p = &pixelData[0])
				for (int y = 0; y < image.Height; y++)
				{
					Span<L16> span = image.GetPixelRowSpan(y);
					fixed (L16* p2 = &span[0])
						Buffer.MemoryCopy(p2, p + y * image.Width * pixelSize, image.Width * pixelSize, image.Width * pixelSize);
				}
			return pixelData;
		}
	}
}