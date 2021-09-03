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
	static public class DicomDatasetExtensions
	{
		static public DicomDataset Compress(this DicomDataset original, DicomTransferSyntax dicomTransferSyntax, IDicomCodec codec, DicomCodecParams jpegParams)
		{
			DicomDataset dataset = new DicomDataset(dicomTransferSyntax);
			original.CopyTo(dataset);

			DicomPixelData pixelData = DicomPixelData.Create(dataset, true);

			DicomPixelData pixelDataSource = DicomPixelData.Create(original, false);
			codec.Encode(pixelDataSource, pixelData, jpegParams);

			return dataset;
		}
		static public DicomDataset Uncompress(this DicomDataset original, IDicomCodec codec)
		{
			DicomDataset dataset = new DicomDataset();
			original.CopyTo(dataset);

			DicomPixelData pixelData = DicomPixelData.Create(dataset, true);

			DicomPixelData pixelDataSource = DicomPixelData.Create(original, false);
			codec.Decode(pixelDataSource, pixelData, new DicomJpeg2000Params { });

			return dataset;
		}
		static public unsafe void GetAverageValue(this DicomDataset original, byte[] bytes, int size, out double average, out double dispersion, out ushort min, out ushort max)
		{
			DicomDataset dataset = new DicomDataset();
			original.CopyTo(dataset);

			DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
			if (pixelData.BytesAllocated != 2 || pixelData.SamplesPerPixel != 1)
				throw new Exception();
			double sum = 0.0;
			min = ushort.MaxValue;
			max = ushort.MinValue;
			fixed (byte* floorPointer = &bytes[0])
			{
				byte* roofPointer = floorPointer + size;
				for (ushort* pointer = (ushort*)floorPointer; pointer != roofPointer; pointer++)
				{
					ushort value = *pointer;
					sum += value;
					if (value < min)
						min = value;
					if (value > max)
						max = value;
				}
			}
			average = sum / (pixelData.Width * pixelData.Height);
			sum = 0.0;
			fixed (byte* floorPointer = &bytes[0])
			{
				byte* roofPointer = floorPointer + size;
				for (ushort* pointer = (ushort*)floorPointer; pointer != roofPointer; pointer++)
				{
					double temp = *pointer - average;
					sum += temp * temp;
				}
			}
			dispersion = Math.Sqrt(sum / (pixelData.Width * pixelData.Height - 1));
		}
		static public unsafe void GetAverageValue(this DicomDataset original, out double average, out double dispersion, out ushort min, out ushort max)
		{
			DicomPixelData pixelDataSource = DicomPixelData.Create(original, false);
			if (pixelDataSource.BytesAllocated != 2 || pixelDataSource.SamplesPerPixel != 1)
				throw new Exception();
			IByteBuffer buffer = pixelDataSource.GetFrame(0);
			byte[] bytes = buffer.Data;
			int size = (int)buffer.Size;
			GetAverageValue(original, bytes, size, out average, out dispersion, out min, out max);
		}
		static public unsafe void GetAverageValue(this DicomDataset original, IDicomCodec codec, out double average, out double dispersion, out ushort min, out ushort max)
		{
			DicomDataset dataset = new DicomDataset();
			original.CopyTo(dataset);

			DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
			DicomPixelData pixelDataSource = DicomPixelData.Create(original, false);
			codec.Decode(pixelDataSource, pixelData, new DicomJpegParams { });
			GetAverageValue(dataset, out average, out dispersion, out min, out max);
		}

		static public unsafe DecodedDicomImageModel DecodeImage(Stream stream, out Dictionary<string, string> meta)
		{
			stream.Seek(0, SeekOrigin.Begin);
			string filePath = System.IO.Path.Combine("memory", Thread.CurrentThread.ManagedThreadId.ToString());

			itk.simple.Image image;
			var tempStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
			try
			{
				using (tempStream)
					stream.CopyTo(tempStream);

				var reader = new itk.simple.ImageFileReader();
				reader.SetFileName(filePath);
				image = reader.Execute();
				var writer = new itk.simple.ImageFileWriter();
				writer.SetFileName("/mnt/c/Users/nikol/Desktop/CR.1.871.3.1522049724.50292.18240.95439262.3570939512.1.1.dcm");
				writer.KeepOriginalImageUIDOff();
				writer.Execute(image);
				reader.Dispose();
			}
			finally { File.Delete(filePath); }

			var result = new DecodedDicomImageModel();

			IntPtr intPtr;
			var pixelId = image.GetPixelID();
			if (pixelId == itk.simple.PixelIDValueEnum.sitkUInt8)
			{
				intPtr = image.GetBufferAsUInt8();
				result.ChannelSize = 1;
				result.Signed = false;
			}
			else if (pixelId == itk.simple.PixelIDValueEnum.sitkInt8)
			{
				intPtr = image.GetBufferAsInt8();
				result.ChannelSize = 1;
				result.Signed = true;
			}
			else if (pixelId == itk.simple.PixelIDValueEnum.sitkUInt16)
			{
				intPtr = image.GetBufferAsUInt16();
				result.ChannelSize = 2;
				result.Signed = false;
			}
			else if (pixelId == itk.simple.PixelIDValueEnum.sitkInt16)
			{
				intPtr = image.GetBufferAsInt16();
				result.ChannelSize = 2;
				result.Signed = true;
			}
			else if (pixelId == itk.simple.PixelIDValueEnum.sitkUInt32)
			{
				intPtr = image.GetBufferAsUInt32();
				result.ChannelSize = 4;
				result.Signed = false;
			}
			else if (pixelId == itk.simple.PixelIDValueEnum.sitkInt32)
			{
				intPtr = image.GetBufferAsInt32();
				result.ChannelSize = 4;
				result.Signed = true;
			}
			else
				throw new Exception($"Неподдерживаемый формат изображения {image.GetPixelIDTypeAsString()}.");

			result.ChannelCount = image.GetNumberOfComponentsPerPixel();
			result.Width = (ushort)image.GetWidth();
			result.Height = (ushort)image.GetHeight();
			result.PixelCount = image.GetNumberOfPixels();

			result.PixelData = new byte[result.ChannelCount * result.ChannelSize * result.PixelCount];
			Marshal.Copy(intPtr, result.PixelData, 0, result.PixelData.Length);

			meta = new Dictionary<string, string>();
			var keys  = image.GetMetaDataKeys();
			foreach (var key in keys)
			{
				meta.Add(key, image.GetMetaData(key));
			}

			image.Dispose();

			return result;
		}
		static public DecodedDicomImageModel DecodeImage(this DicomDataset dataset, out Dictionary<string, string> meta)
		{
			var dicomFile = new DicomFile(dataset);
			var stream = new MemoryStream();
			dicomFile.Save(stream);
			return DecodeImage(stream, out meta);
		}
		static public unsafe SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.L16> ReadL16Image(this DicomDataset dataset)
		{
			const int pixelSize = 2;
			var decodedImage = dataset.DecodeImage(out _);
			var pixelData = decodedImage.PixelData;
			fixed (byte* p = &pixelData[0])
				return Image.LoadPixelData<L16>(new ReadOnlySpan<L16>(p, pixelData.Length / pixelSize), decodedImage.Width, decodedImage.Height);
		}
		static public unsafe SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb48> ReadRgb48Image(this DicomDataset dataset)
		{
			const int pixelSize = 6;
			var decodedImage = dataset.DecodeImage(out _);
			var grayscalePixelData = decodedImage.PixelData;

			int pixelCount = decodedImage.Width * decodedImage.Height;
			byte[] colorPixelData = new byte[pixelSize * pixelCount];
			for (int pixelIndex = 0; pixelIndex != pixelCount; pixelIndex++)
			{
				colorPixelData[pixelIndex * pixelSize] = grayscalePixelData[pixelIndex * 2];
				colorPixelData[pixelIndex * pixelSize + 2] = grayscalePixelData[pixelIndex * 2];
				colorPixelData[pixelIndex * pixelSize + 4] = grayscalePixelData[pixelIndex * 2];
				colorPixelData[pixelIndex * pixelSize + 1] = grayscalePixelData[pixelIndex * 2 + 1];
				colorPixelData[pixelIndex * pixelSize + 3] = grayscalePixelData[pixelIndex * 2 + 1];
				colorPixelData[pixelIndex * pixelSize + 5] = grayscalePixelData[pixelIndex * 2 + 1];
			}
			fixed (byte* p = &colorPixelData[0])
				return Image.LoadPixelData<Rgb48>(new ReadOnlySpan<Rgb48>(p, colorPixelData.Length / pixelSize), decodedImage.Width, decodedImage.Height);
		}
	}
}