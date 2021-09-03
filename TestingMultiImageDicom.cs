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
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Threading;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace DicomTest
{
	static internal class TestingMultiImageDicom
	{
		static private void BreakUp()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			const string sourcePath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/several-frames/sources/1.2.392.200036.9116.2.5.1.37.2418295482.1507184833.33568";
			DicomFile dicomFile = DicomFile.Open(sourcePath);
			DicomPixelData pixelData = DicomPixelData.Create(dicomFile.Dataset, false);
			MemoryStream[] frameFiles = new MemoryStream[pixelData.NumberOfFrames];
			MemoryStream zip = new MemoryStream();
			using (ZipArchive archive = new ZipArchive(zip, ZipArchiveMode.Create, true))
			{
				for (int frameIndex = 0; frameIndex != pixelData.NumberOfFrames; frameIndex++)
				{
					DicomDataset frameDataset = new DicomDataset(dicomFile.Dataset.InternalTransferSyntax);
					dicomFile.Dataset.CopyTo(frameDataset);
					DicomPixelData framePixelData = DicomPixelData.Create(frameDataset, true);
					IByteBuffer buffer = pixelData.GetFrame(frameIndex);
					framePixelData.AddFrame(buffer);
					ZipArchiveEntry readmeEntry = archive.CreateEntry(frameIndex.ToString());
					using (Stream stream = readmeEntry.Open())
						new DicomFile(frameDataset).Save(stream);
				}
			}
			stopwatch.Stop();
			Console.WriteLine(stopwatch.ElapsedMilliseconds);
			using (FileStream file = new FileStream("/media/nikolaev_ov/CEFE3C54FE3C36D5/test.zip", FileMode.OpenOrCreate, FileAccess.ReadWrite))
			{
				zip.Position = 0;
				zip.CopyTo(file);
			}
		}
		static private DicomItem GetAttributeValue(DicomDataset dataset, DicomTag attributeTag, DicomVR vr)
		{
			if (dataset.Contains(attributeTag) && dataset.GetDicomItem<DicomItem>(attributeTag).ValueRepresentation == vr)
				return dataset.GetDicomItem<DicomItem>(attributeTag);
			foreach (var item in dataset)
			{
				if (item.ValueRepresentation != DicomVR.SQ)
					continue;
				foreach (var sequenceDataset in dataset.GetSequence(item.Tag))
				{
					var value = GetAttributeValue(sequenceDataset, attributeTag, vr);
					if (value != null)
						return value;
				}
			}
			return null;
		}
		static internal unsafe void Start(string[] args)
		{
			const string severalFramesFilePath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/several-frames/sources/1.2.392.200036.9116.2.5.1.37.2418295482.1507184833.33568";
			string[] comparingFilePaths = new string[]
			{
				"/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/pathology-1/sources/IM0",
				"/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/SE00001/sources/IM00001",
				"/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/3000566.000000-03192/sources/1-001.dcm"
			};
			// var checkingDicomDataset = DicomFile.Open(severalFramesFilePath).Dataset.GetSequence(DicomTag.SharedFunctionalGroupsSequence).Items[0];
			var checkingDicomDataset = DicomFile.Open(severalFramesFilePath).Dataset.GetSequence(DicomTag.PerFrameFunctionalGroupsSequence).Items[0];
			var items = new HashSet<DicomTag>();
			var stringBuilder = new StringBuilder();
			for (var i = 0; i < comparingFilePaths.Length; i++)
			{
				var comparingDicomDataset = DicomFile.Open(comparingFilePaths[i]).Dataset;
				foreach (var dicomItem in comparingDicomDataset)
				{
					if (items.Contains(dicomItem.Tag))
						continue;
					var retrievedItem = GetAttributeValue(checkingDicomDataset, dicomItem.Tag, dicomItem.ValueRepresentation);
					if (retrievedItem != null)
					{
						stringBuilder.AppendLine(dicomItem.ToString());
						items.Add(dicomItem.Tag);
					}
				}
			}
			File.WriteAllText("/media/nikolaev_ov/CEFE3C54FE3C36D5/text.txt", stringBuilder.ToString());
		}
	}
}