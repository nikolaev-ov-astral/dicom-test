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
	static internal class ItkSimpleTestProgram
	{
		static private void ProcessDicomFile(string sourceDirectory)
		{

			var file = DicomFile.Open("/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/Fluorography/favorite/invalid-window/1.2.826.0.1.3680043.2.634.0.8683.2020113.101951.99.dcm");

			var all = file.Dataset.ToArray();
			var leftTags = new HashSet<DicomTag>(new DicomTag[]
			{
						DicomTag.TransferSyntaxUID,
						DicomTag.StudyInstanceUID,
						DicomTag.SeriesInstanceUID,
						DicomTag.SOPInstanceUID,
						DicomTag.SOPClassUID,
						DicomTag.Modality,
						DicomTag.PixelData,
						DicomTag.BitsAllocated,
						DicomTag.BitsStored,
						DicomTag.HighBit,
						DicomTag.PixelRepresentation,
						DicomTag.SamplesPerPixel,
						DicomTag.PhotometricInterpretation,
						DicomTag.Rows,
						DicomTag.Columns
			});

			foreach (var one in all)
			{
				if (leftTags.Contains(one.Tag))
					continue;
				file.Dataset.Remove(one.Tag);
			}

			file.Dataset.AddOrUpdate(DicomTag.PresentationLUTShape, "IDENTITY");
			file.Dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME1");
			file.Save(System.IO.Path.Combine(sourceDirectory, "IDENTITY&MONOCHROME1.dcm"));
			file.Dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME2");
			file.Save(System.IO.Path.Combine(sourceDirectory, "IDENTITY&MONOCHROME2.dcm"));

			file.Dataset.AddOrUpdate(DicomTag.PresentationLUTShape, "INVERSE");
			file.Dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME1");
			file.Save(System.IO.Path.Combine(sourceDirectory, "INVERSE&MONOCHROME1.dcm"));
			file.Dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME2");
			file.Save(System.IO.Path.Combine(sourceDirectory, "INVERSE&MONOCHROME2.dcm"));

			file.Dataset.Remove(DicomTag.PresentationLUTShape);
			file.Dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME1");
			file.Save(System.IO.Path.Combine(sourceDirectory, "REMOVED&MONOCHROME1.dcm"));
			file.Dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME2");
			file.Save(System.IO.Path.Combine(sourceDirectory, "REMOVED&MONOCHROME2.dcm"));

			file.Dataset.Remove(DicomTag.PhotometricInterpretation);
			file.Dataset.AddOrUpdate(DicomTag.PresentationLUTShape, "IDENTITY");
			file.Save(System.IO.Path.Combine(sourceDirectory, "IDENTITY&REMOVED.dcm"));
			file.Dataset.AddOrUpdate(DicomTag.PresentationLUTShape, "INVERSE");
			file.Save(System.IO.Path.Combine(sourceDirectory, "INVERSE&REMOVED.dcm"));

			file.Dataset.Remove(DicomTag.PresentationLUTShape);
			file.Dataset.Remove(DicomTag.PhotometricInterpretation);
			file.Save(System.IO.Path.Combine(sourceDirectory, "REMOVED&REMOVED.dcm"));
		}


		static internal unsafe void Start(string[] args)
		{
			string sourceDirectory = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/Fluorography/favorite/invalid-window";
			ProcessDicomFile(sourceDirectory);
			// using var readStream = new FileStream("/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/Mammography/favorite/presentation-lut-shape-inverse/sources/MG_1.2.276.0.7230010.3.1.4.50143260.1368.1571935817.381.dcm", FileMode.Open, FileAccess.ReadWrite);
			return;
			foreach (var fileName in new[] { "IDENTITY&MONOCHROME1.dcm", "IDENTITY&MONOCHROME2.dcm", "INVERSE&MONOCHROME1.dcm", "INVERSE&MONOCHROME2.dcm", "REMOVED&MONOCHROME1.dcm", "REMOVED&MONOCHROME2.dcm", "IDENTITY&REMOVED.dcm", "INVERSE&REMOVED.dcm", "REMOVED&REMOVED.dcm" })
			{
				using var readStream = new FileStream(System.IO.Path.Combine(sourceDirectory, fileName), FileMode.Open, FileAccess.ReadWrite);
				var decodedDicomImage = DicomDatasetExtensions.DecodeImage(readStream, out _);

				// var decodedDicomImage = file.Dataset.ReadPixelData();

				// var image = DicomPixelData.Create(file.Dataset, false);

				// var decodedDicomImage = new DecodedDicomImageModel()
				// {
				// 	PixelData = image.GetFrame(0).Data,
				// 	ChannelSize = (uint)(file.Dataset.GetSingleValue<ushort>(DicomTag.BitsAllocated) >> 3),
				// 	Signed = file.Dataset.GetSingleValue<ushort>(DicomTag.PixelRepresentation) == 1,
				// 	ChannelCount = file.Dataset.GetSingleValue<ushort>(DicomTag.SamplesPerPixel),
				// 	Width = file.Dataset.GetSingleValue<ushort>(DicomTag.Columns),
				// 	Height = file.Dataset.GetSingleValue<ushort>(DicomTag.Rows),
				// };
				// decodedDicomImage.PixelCount = decodedDicomImage.Width * decodedDicomImage.Height;
				fixed (byte* p = &decodedDicomImage.PixelData[0])
				{
					var pixelData = new Span<L16>(p, (int)(decodedDicomImage.PixelData.Length / decodedDicomImage.ChannelSize));
					var min = ushort.MaxValue;
					var max = ushort.MinValue;
					foreach (var pixel in pixelData)
					{
						if (pixel.PackedValue < min)
							min = pixel.PackedValue;
						if (pixel.PackedValue > max)
							max = pixel.PackedValue;
					}
					for (int pixelIndex = 0; pixelIndex != pixelData.Length; pixelIndex++)
						pixelData[pixelIndex] = new L16(checked((ushort)(pixelData[pixelIndex].PackedValue * 64)));
					var img = Image.LoadPixelData<L16>(pixelData, (int)decodedDicomImage.Width, (int)decodedDicomImage.Height);
					// img.Mutate(a =>
					// {
					// 	// Color whiteColor = new Color(new Rgba64(ushort.MaxValue & 0x0FFF, ushort.MaxValue & 0x0FFF, ushort.MaxValue & 0x0FFF, ushort.MaxValue & 0x0FFF));
					// 	Color whiteColor = new Color(new Rgba64(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue));
					// 	Color blackColor = new Color(new Rgba64(ushort.MinValue, ushort.MinValue, ushort.MinValue, ushort.MinValue));
					// 	a.Draw(Pens.Solid(whiteColor, 10), new EllipsePolygon(new PointF(image.Width * 0.5F, image.Height * 0.5F), 200F));
					// 	FontCollection fontCollection = new FontCollection();
					// 	fontCollection.Install(System.IO.Path.Combine(fontDirectory, "fonts/TIMES.TTF"));
					// 	string text = "ТОЛЬКО ДЛЯ ИССЛЕДОВАТЕЛЬСКИХ ЦЕЛЕЙ";
					// 	Font font = new Font(fontCollection.Families.First(), 64, FontStyle.Regular);
					// 	FontRectangle fontRectangle = TextMeasurer.Measure(text, new RendererOptions(font));
					// 	PointF textPoint = new PointF(image.Width * 0.5F - fontRectangle.Width * 0.5F, image.Height * 0.5F - 200F - fontRectangle.Height);
					// 	a.DrawText(text, font, blackColor, new PointF(textPoint.X + 5, textPoint.Y + 5));
					// 	a.DrawText(text, font, whiteColor, textPoint);
					// });
					using var writeStream = new FileStream(System.IO.Path.Combine(sourceDirectory, System.IO.Path.GetFileNameWithoutExtension(fileName) + ".jpg"), FileMode.OpenOrCreate, FileAccess.Write);
					img.Save(writeStream, new JpegEncoder() { Quality = 100, Subsample = JpegSubsample.Ratio444 });
					for (int y = 0; y < img.Height; y++)
					{
						Span<L16> span = img.GetPixelRowSpan(y);
						fixed (L16* p2 = &span[0])
							Buffer.MemoryCopy(p2, p + y * img.Width * decodedDicomImage.ChannelSize, img.Width * decodedDicomImage.ChannelSize, img.Width * decodedDicomImage.ChannelSize);
					}
				}
			}
		}
	}
}