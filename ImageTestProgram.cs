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
	static internal class ImageTestProgram
	{
		static ImageTestProgram()
		{
			ImageManager.SetImplementation(WinFormsImageManager.Instance);
		}

		static internal unsafe void Start(string[] args)
		{
			DicomFile file = new DicomFile();
			var z = file.Dataset.InternalTransferSyntax;
			string sourceDirectory = Environment.GetEnvironmentVariable("SOURCE_DIRECTORY");
			string fontDirectory = Environment.GetEnvironmentVariable("FONT_DIRECTORY");


			System.Drawing.Bitmap w = new System.Drawing.Bitmap(100, 100, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			w.Save(System.IO.Path.Combine(sourceDirectory, "image.png"));
			var d = System.Drawing.Image.FromFile(System.IO.Path.Combine(sourceDirectory, "image.png"), false);
			// string filePath = System.IO.Path.Combine(sourceDirectory, "DICOM/SikorskayaN.M/1.2.840.113681.3232266507.1433317480.4724.262");
			// string filePath = System.IO.Path.Combine(sourceDirectory, "DICOM/Topina V A/1.2.840.113681.169869557.1534493190.4808.47144");
			// string filePath = System.IO.Path.Combine(sourceDirectory, "DICOM/!Test/ИС Флюорография/ФЛЮ для показа/Пациент001/Пациент001.dcm");
			// string filePath = System.IO.Path.Combine(sourceDirectory, "1");
			// string filePath = System.IO.Path.Combine(sourceDirectory, "DICOM/Fluorography/Emias Compressed/1.871.3.2050448135.34720.20335.72851334.2248937210.1.1.1");
			// string filePath = System.IO.Path.Combine(sourceDirectory, "DICOM/Fluorography/Emias Compressed/1.871.3.1778880733.53108.19741.3386264761.3888045999.1.1.1");
			// string filePath = System.IO.Path.Combine(sourceDirectory, "DICOM/Fluorography/Emias Compressed/1.871.3.3958735481.62656.17963.4096047274.351950500.1.1.1");
			string filePath = System.IO.Path.Combine(sourceDirectory, "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/SE0  (3)/IM0");
			// string filePath = System.IO.Path.Combine(sourceDirectory, "SE000001/IM000000");

			// string filePath = System.IO.Path.Combine(sourceDirectory, "SC_R_CC.dcm");
			// string[] files = Directory.GetFiles("/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/3000566.000000-03192/");
			DicomFile dicomFile = DicomFile.Open(filePath);
			DicomDataset dataset = dicomFile.Dataset;
			// var t = dataset.InternalTransferSyntax;
			// dataset = Compress(dataset, DicomTransferSyntax.JPEGProcess1, new DicomJpegProcess1Codec());
			// dicomFile = new DicomFile(dataset);
			// dicomFile.Save(System.IO.Path.Combine(sourceDirectory, "1"));

			// dataset.AddOrUpdate(DicomTag.WindowCenter, 32767.0);
			// dataset.AddOrUpdate(DicomTag.WindowWidth, 65534.0);
			// dicomFile.Save(System.IO.Path.Combine(sourceDirectory, "1"));

			// PullDicom(dataset);
			// GetAverageValue(dataset, out double average, out double dispersion, out ushort min, out ushort max);
			// GetAverageValue(dataset, new DicomJpegLsLosslessCodec(), out double average, out double dispersion, out ushort min, out ushort max);
			// GetAverageValue(dataset, new JpegLosslessDecoderWrapperProcess14SV1(), out double average, out double dispersion, out ushort min, out ushort max);
			var bitsAllocated = dataset.GetSingleValue<string>(DicomTag.BitsAllocated);
			var BitsStored = dataset.GetSingleValue<string>(DicomTag.BitsStored);
			var samplesPerPixel = dataset.GetSingleValue<string>(DicomTag.SamplesPerPixel);
			var photometricInterpretation = dataset.GetSingleValue<string>(DicomTag.PhotometricInterpretation);
			var PixelRepresentation = dataset.GetSingleValue<string>(DicomTag.PixelRepresentation);
			double windowCenter = dataset.GetValue<double>(DicomTag.WindowCenter, 0);
			double windowWidth = dataset.GetValue<double>(DicomTag.WindowWidth, 0);
			byte[] pixelData;
			using (FileStream readStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				pixelData = DicomDatasetExtensions.DecodeImage(readStream, out _).PixelData;
			// using FileStream stream1 = new FileStream("/media/nikolaev_ov/CEFE3C54FE3C36D5/pixelData.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);
			// pixelData = new byte[stream1.Length];
			// stream1.Read(pixelData, 0, pixelData.Length);
			// GetAverageValue(dataset, pixelData, pixelData.Length, out double average, out double dispersion, out ushort min, out ushort max);

			DicomImage image = new DicomImage(dataset);
			image.RenderImage().AsSharedBitmap().Save(System.IO.Path.Combine(sourceDirectory, "image.png"));
			DicomPixelData sdsd = DicomPixelData.Create(dataset);
			// pixelData = sdsd.GetFrame(0).Data;
			fixed (byte* p = &pixelData[0])
			{
				//                 var bitmap2 = new System.Drawing.Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

				// stream1.SetLength(0);
				//                 for (int x = 0; x != bitmap2.Width; x++)
				//                     for (int y = 0; y != bitmap2.Height; y++)
				//                     {
				//                         byte value = pixelData[y * bitmap2.Width + x];
				//                         bitmap2.SetPixel(x, y, System.Drawing.Color.FromArgb(value));
				//                         stream1.WriteByte(value);
				//                     }
				//                     stream1.Close();
				//                 bitmap2.Save("/media/nikolaev_ov/CEFE3C54FE3C36D5/image.png");
				//                 var img = new SixLabors.ImageSharp.Image<L8>(image.Width, image.Height);
				//                 for (int y = 0; y < image.Height; y++)
				//                 {
				//                     Span<L8> span = img.GetPixelRowSpan(y);
				//                     for (int x = 0; x < image.Width; x++)
				//                         span[x] = new L8(pixelData[y * image.Width + x]);
				//                 }
				// var img = Image.LoadPixelData<L8>(new ReadOnlySpan<L8>(p, pixelData.Length), image.Width, image.Height);
				var img = Image.LoadPixelData<L16>(new ReadOnlySpan<L16>(p, pixelData.Length / 2), image.Width, image.Height);
				img.Mutate(a =>
				{
					// Color whiteColor = new Color(new Rgba64(ushort.MaxValue & 0x0FFF, ushort.MaxValue & 0x0FFF, ushort.MaxValue & 0x0FFF, ushort.MaxValue & 0x0FFF));
					Color whiteColor = new Color(new Rgba64(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue));
					Color blackColor = new Color(new Rgba64(ushort.MinValue, ushort.MinValue, ushort.MinValue, ushort.MinValue));
					a.Draw(Pens.Solid(whiteColor, 10), new EllipsePolygon(new PointF(image.Width * 0.5F, image.Height * 0.5F), 200F));
					FontCollection fontCollection = new FontCollection();
					fontCollection.Install(System.IO.Path.Combine(fontDirectory, "fonts/TIMES.TTF"));
					string text = "ТОЛЬКО ДЛЯ ИССЛЕДОВАТЕЛЬСКИХ ЦЕЛЕЙ";
					Font font = new Font(fontCollection.Families.First(), 64, FontStyle.Regular);
					FontRectangle fontRectangle = TextMeasurer.Measure(text, new RendererOptions(font));
					PointF textPoint = new PointF(image.Width * 0.5F - fontRectangle.Width * 0.5F, image.Height * 0.5F - 200F - fontRectangle.Height);
					a.DrawText(text, font, blackColor, new PointF(textPoint.X + 5, textPoint.Y + 5));
					a.DrawText(text, font, whiteColor, textPoint);
				});
				using FileStream stream = new FileStream(System.IO.Path.Combine(sourceDirectory, "image.jpg"), FileMode.OpenOrCreate, FileAccess.Write);
				img.Save(stream, new JpegEncoder() { Quality = 100, Subsample = JpegSubsample.Ratio444 });
				for (int y = 0; y < image.Height; y++)
				{
					Span<L16> span = img.GetPixelRowSpan(y);
					fixed (L16* p2 = &span[0])
						Buffer.MemoryCopy(p2, p + y * image.Width * 2, image.Width * 2, image.Width * 2);
				}
			}
			DicomDataset dataset2 = new DicomDataset();
			dataset.CopyTo(dataset2);
			dataset = dataset2;
			dicomFile = new DicomFile(dataset);
			DicomPixelData dicomPixelData = DicomPixelData.Create(dataset, true);
			dicomPixelData.AddFrame(new MemoryByteBuffer(pixelData));
			// dicomFile.Save(Path.Combine(sourceDirectory, "1");
			// GetAverageValue(dataset, out average, out dispersion, out min, out max);

			windowCenter = dataset.GetSingleValue<double>(DicomTag.WindowCenter);
			windowWidth = dataset.GetSingleValue<double>(DicomTag.WindowWidth);
			// string VOILUTFunction = dataset.GetSingleValue<string>(DicomTag.VOILUTFunction);
			// dataset = GenerateUncompressedDicomDataset(dataset);
			dataset = dataset.Compress(DicomTransferSyntax.JPEGLSLossless, new DicomJpegLsLosslessCodec(), new DicomJpegParams { });
			// GetAverageValue(dataset, new DicomJpegLsLosslessCodec(), out average, out dispersion, out min, out max);
			// dataset = Uncompress(dataset, new DicomJpegLsLosslessCodec());
			// GetAverageValue(dataset, out average, out dispersion, out min, out max);
			dicomFile = new DicomFile(dataset);

			// dataset.AddOrUpdate(DicomTag.WindowCenter, 255.0 * 0.5);
			// dataset.AddOrUpdate(DicomTag.WindowWidth, 255.0);
			dataset.Remove(DicomTag.WindowCenter);
			dataset.Remove(DicomTag.WindowWidth);
			// CheckDicomTransferSyntax(dataset);
			// PullDicom(dataset);

			dicomFile.Save(System.IO.Path.Combine(sourceDirectory, "1"));
		}
	}
}