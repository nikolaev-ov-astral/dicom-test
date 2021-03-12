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
	static internal class TestDicom48bpp
	{
		static private readonly Dictionary<DicomTransferSyntax, (IDicomCodec codec, string fileName)> _transferSyntaxes;

		static TestDicom48bpp()
		{
			_transferSyntaxes = new Dictionary<DicomTransferSyntax, (IDicomCodec codec, string fileName)>();
			_transferSyntaxes.Add(DicomTransferSyntax.JPEGLSLossless, (new DicomJpegLsLosslessCodec(), "JPEGLSLossless.dcm"));
			_transferSyntaxes.Add(DicomTransferSyntax.JPEGLSNearLossless, (new DicomJpegLsNearLosslessCodec(), "JPEGLSNearLossless.dcm"));
			ImageManager.SetImplementation(WinFormsImageManager.Instance);
		}

		static internal unsafe void Start(string[] args)
		{
			string sourceDirectory = Environment.GetEnvironmentVariable("SOURCE_DIRECTORY");
			string fontDirectory = Environment.GetEnvironmentVariable("FONT_DIRECTORY");
			string filePath = System.IO.Path.Combine(sourceDirectory, "DICOM/Fluorography/favorite/jpeg-process14-1/sources/1.871.3.2050448135.34720.20335.72851334.2248937210.1.1.1");

			DicomFile dicomFile = DicomFile.Open(filePath);
			DicomDataset dataset = dicomFile.Dataset;

			var decodedImage = dataset.DecodeImage();
			var sourcePixelData = decodedImage.PixelData;
			ushort width = dataset.GetSingleValue<ushort>(DicomTag.Columns);
			ushort height = dataset.GetSingleValue<ushort>(DicomTag.Rows);

			// {
			// 	var resultDataset2 = new DicomDataset();
			// 	dataset.CopyTo(resultDataset2);
			// 	var dicomPixelData2 = DicomPixelData.Create(resultDataset2, true);
			// 	dicomPixelData2.AddFrame(new MemoryByteBuffer(sourcePixelData));
			// 	resultDataset2.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
			// 	resultDataset2.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
			// 	var resultCompressedDataset = resultDataset2.Compress(DicomTransferSyntax.JPEGLSLossless, new DicomJpegLsLosslessCodec(), new DicomJpegParams { });
			// 	dicomFile = new DicomFile(resultCompressedDataset);
			// 	dicomFile.Save(System.IO.Path.Combine(sourceDirectory, "JPEGLSLossless.dcm"));
			// 	return;
			// }

			var img = dataset.ReadRgb48Image();
			img.Mutate(a =>
			{
				ushort value = 8000;
				Color whiteColor = new Color(new Rgba64(value, value, value, value));
				Color redColor = new Color(new Rgba64(value, ushort.MinValue, ushort.MinValue, value));
				Color greenColor = new Color(new Rgba64(ushort.MinValue, value, ushort.MinValue, value));
				Color blueColor = new Color(new Rgba64(ushort.MinValue, ushort.MinValue, value, value));
				a.Draw(Pens.Solid(whiteColor, 10), new EllipsePolygon(new PointF(width * 0.25F, height * 0.25F), 200F));
				a.Draw(Pens.Solid(redColor, 10), new EllipsePolygon(new PointF(width * 0.25F, height * 0.75F), 200F));
				a.Draw(Pens.Solid(greenColor, 10), new EllipsePolygon(new PointF(width * 0.75F, height * 0.75F), 200F));
				a.Draw(Pens.Solid(blueColor, 10), new EllipsePolygon(new PointF(width * 0.75F, height * 0.25F), 200F));
				FontCollection fontCollection = new FontCollection();
				FontFamily fontFamily = fontCollection.Install(System.IO.Path.Combine(fontDirectory, "fonts/TimesNewRoman/TimesNewRomanRegular/TimesNewRomanRegular.ttf"));
				Font font = new Font(fontFamily, 124, FontStyle.Regular);
				a.Fill(Brushes.Solid(redColor), new Rectangle(0, 0, width, height));
				a.DrawText("sdfd", font, whiteColor, new PointF(width * 0.5F, height * 0.5F));
			});
			using FileStream stream = new FileStream(System.IO.Path.Combine(sourceDirectory, "image.jpg"), FileMode.OpenOrCreate, FileAccess.Write);
			img.Save(stream, new JpegEncoder() { Quality = 100, Subsample = JpegSubsample.Ratio444 });
			var pixelData = img.ToPixelData();


			DicomDataset resultDataset = new DicomDataset();
			dataset.CopyTo(resultDataset);

			resultDataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)16);
			resultDataset.AddOrUpdate(DicomTag.BitsStored, (ushort)16);
			resultDataset.AddOrUpdate(DicomTag.HighBit, (ushort)15);
			resultDataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
			resultDataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)3);
			resultDataset.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);
			resultDataset.AddOrUpdate(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);
			resultDataset.AddOrUpdate(DicomTag.Rows, (ushort)height);
			resultDataset.AddOrUpdate(DicomTag.Columns, (ushort)width);
			var dicomPixelData = DicomPixelData.Create(resultDataset, true);
			dicomPixelData.AddFrame(new MemoryByteBuffer(pixelData));

			resultDataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
			resultDataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
			dicomFile = new DicomFile(resultDataset);
			dicomFile.Save(System.IO.Path.Combine(sourceDirectory, "uncompressed.dcm"));

			foreach (var transferSyntax in _transferSyntaxes)
			{
				var resultCompressedDataset = resultDataset.Compress(transferSyntax.Key, transferSyntax.Value.codec, new DicomJpegParams { });
				resultCompressedDataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
				resultCompressedDataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
				dicomFile = new DicomFile(resultCompressedDataset);
				dicomFile.Save(System.IO.Path.Combine(sourceDirectory, transferSyntax.Value.fileName));
			}
		}
	}
}