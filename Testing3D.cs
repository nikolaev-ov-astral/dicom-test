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

namespace DicomTest
{
	static internal class Testing3D
	{
		public struct AttributeValue
		{
			public DicomTag Tag;
			public string[] Values;
		}

		static private readonly DicomTag[] _notCopyingTags;

		static Testing3D()
		{
			_notCopyingTags = new DicomTag[]
			{
				DicomTag.StudyInstanceUID,
				DicomTag.SeriesInstanceUID,
				DicomTag.SOPInstanceUID,
				DicomTag.InstanceNumber,
				DicomTag.BitsAllocated,
				DicomTag.BitsStored,
				DicomTag.HighBit,
				DicomTag.PixelRepresentation,
				DicomTag.SamplesPerPixel,
				DicomTag.PhotometricInterpretation,
				DicomTag.Rows,
				DicomTag.Columns,
				DicomTag.RescaleType,
				DicomTag.RescaleIntercept,
				DicomTag.RescaleSlope,
			};
		}

		internal static AttributeValue[] GetNotExistingAttributes(DicomDataset source, DicomDataset result)
		{
			List<AttributeValue> notExistingAttributes = new List<AttributeValue>();
			foreach (DicomItem item in source)
			{
				if (!item.ValueRepresentation.IsString)
					continue;
				if (_notCopyingTags.Contains(item.Tag))
					continue;
				string[] sourceValues = source.GetValues<string>(item.Tag);
				if (result.Contains(item.Tag))
				{
					string[] resultValues = result.GetValues<string>(item.Tag);
					if (resultValues.Length == sourceValues.Length)
					{
						bool ok = true;
						for (int valueIndex = 0; valueIndex != sourceValues.Length; valueIndex++)
						{
							if (resultValues[valueIndex] == sourceValues[valueIndex])
								continue;
							ok = false;
							break;
						}
						if (ok)
							continue;
					}
				}
				notExistingAttributes.Add(new AttributeValue { Tag = item.Tag, Values = sourceValues });
				// if (item.ValueRepresentation.Code == "SQ")
				//     foreach (DicomDataset subDataset in buffer.GetSequence(item.Tag))
				//         subDataset.Rebuild();
				// dataset.Add(item);
			}
			return notExistingAttributes.ToArray();
		}
		static internal unsafe void Start(string[] args)
		{
			const string sourcesPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/1.2.392.200036.9116.2.5.1.37.2426554992.1575423591.939426/163e1bae-fb4d-4109-8a0f-b4e7038f0e77";
			const string resultsPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/1.2.392.200036.9116.2.5.1.37.2426554992.1575423591.939426/c7e15bee-0113-4531-9466-c7ae8de12de3";
			const string fixedResultsPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/1.2.392.200036.9116.2.5.1.37.2426554992.1575423591.939426/fixed-results";
			string[] sourceFilePaths = Directory.GetFiles(sourcesPath);
			string[] resultFilePaths = Directory.GetFiles(resultsPath);
			DicomDataset[] sourceDicomFiles = new DicomDataset[sourceFilePaths.Length];
			DicomDataset[] resultDicomFiles = new DicomDataset[resultFilePaths.Length];
			for (int sourceFileIndex = 0; sourceFileIndex != sourceFilePaths.Length; sourceFileIndex++)
				sourceDicomFiles[sourceFileIndex] = DicomFile.Open(sourceFilePaths[sourceFileIndex]).Dataset;
			for (int resultFileIndex = 0; resultFileIndex != resultFilePaths.Length; resultFileIndex++)
				resultDicomFiles[resultFileIndex] = DicomFile.Open(resultFilePaths[resultFileIndex]).Dataset;
			if (!Directory.Exists(fixedResultsPath))
				Directory.CreateDirectory(fixedResultsPath);

			// for (int resultFileIndex = 0; resultFileIndex != resultFilePaths.Length; resultFileIndex++)
			// {
			// 	DicomDataset result = resultDicomFiles[resultFileIndex];
			// 	int instanceNumber = result.GetSingleValue<int>(DicomTag.InstanceNumber);
			// 	DicomDataset source = sourceDicomFiles.Single(a=>a.GetSingleValue<int>(DicomTag.InstanceNumber) == instanceNumber);
			// 	AttributeValue[] values = GetNotExistingAttributes(source, result);
			// 	string fixedResultPath = Path.Combine(fixedResultsPath, Path.GetFileName(resultFilePaths[resultFileIndex]));
			// 	result.NotValidated();
			// 	foreach (AttributeValue value in values)
			// 		result.AddOrUpdate(value.Tag, value.Values);
			// 	new DicomFile(result).Save(fixedResultPath);
			// }

			// for (int resultFileIndex = 0; resultFileIndex != resultFilePaths.Length; resultFileIndex++)
			// {
			// 	DicomDataset result = resultDicomFiles[resultFileIndex];
			// 	int instanceNumber = result.GetSingleValue<int>(DicomTag.InstanceNumber);
			// 	DicomDataset source = sourceDicomFiles.Single(a => a.GetSingleValue<int>(DicomTag.InstanceNumber) == instanceNumber);
			// 	DicomDataset newSource = new DicomDataset(result.InternalTransferSyntax);
			// 	source.CopyTo(newSource);
			// 	source = newSource;
			// 	source.AddOrUpdate(DicomTag.BitsAllocated, result.GetSingleValue<ushort>(DicomTag.BitsAllocated));
			// 	source.AddOrUpdate(DicomTag.BitsStored, result.GetSingleValue<ushort>(DicomTag.BitsStored));
			// 	source.AddOrUpdate(DicomTag.HighBit, result.GetSingleValue<ushort>(DicomTag.HighBit));
			// 	source.AddOrUpdate(DicomTag.PixelRepresentation, result.GetSingleValue<ushort>(DicomTag.PixelRepresentation));
			// 	source.AddOrUpdate(DicomTag.SamplesPerPixel, result.GetSingleValue<ushort>(DicomTag.SamplesPerPixel));
			// 	source.AddOrUpdate(DicomTag.PhotometricInterpretation, result.GetSingleValue<string>(DicomTag.PhotometricInterpretation));
			// 	source.AddOrUpdate(DicomTag.PlanarConfiguration, result.GetSingleValue<ushort>(DicomTag.PlanarConfiguration));
			// 	source.AddOrUpdate(DicomTag.Rows, result.GetSingleValue<ushort>(DicomTag.Rows));
			// 	source.AddOrUpdate(DicomTag.Columns, result.GetSingleValue<ushort>(DicomTag.Columns));

			// 	DicomPixelData resultPixelData = DicomPixelData.Create(result, false);
			// 	DicomPixelData sourcePixelData = DicomPixelData.Create(source, true);

			// 	sourcePixelData.AddFrame(new MemoryByteBuffer(resultPixelData.GetFrame(0).Data));

			// 	string fixedResultPath = Path.Combine(fixedResultsPath, Path.GetFileName(resultFilePaths[resultFileIndex]));
			// 	new DicomFile(source).Save(fixedResultPath);
			// }

			// for (int resultFileIndex = 0; resultFileIndex != resultFilePaths.Length; resultFileIndex++)
			// {
			// 	DicomDataset result = resultDicomFiles[resultFileIndex];
			// 	int instanceNumber = result.GetSingleValue<int>(DicomTag.InstanceNumber);
			// 	DicomDataset source = sourceDicomFiles.Single(a => a.GetSingleValue<int>(DicomTag.InstanceNumber) == instanceNumber);
			// 	DicomDataset newSource = new DicomDataset(result.InternalTransferSyntax);
			// 	source.CopyTo(newSource);
			// 	source = newSource;
			// 	source.AddOrUpdate(DicomTag.BitsAllocated, (ushort)16);
			// 	source.AddOrUpdate(DicomTag.BitsStored, (ushort)16);
			// 	source.AddOrUpdate(DicomTag.HighBit, (ushort)15);
			// 	source.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)1);
			// 	source.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)1);
			// 	source.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME2");
			// 	source.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);
			// 	source.AddOrUpdate(DicomTag.Rows, result.GetSingleValue<ushort>(DicomTag.Rows));
			// 	source.AddOrUpdate(DicomTag.Columns, result.GetSingleValue<ushort>(DicomTag.Columns));

			// 	DicomPixelData resultPixelData = DicomPixelData.Create(result, false);
			// 	DicomPixelData sourcePixelData = DicomPixelData.Create(source, true);

			// 	byte[] resultData = resultPixelData.GetFrame(0).Data;
			// 	int pixelCount = resultData.Length / 3;
			// 	byte[] sourceData = new byte[pixelCount * 2];
			// 	for (int pixelIndex = 0; pixelIndex != pixelCount; pixelIndex++)
			// 	{
			// 		byte byteValue = Math.Max(Math.Max(resultData[pixelIndex * 3], resultData[pixelIndex * 3 + 1]), resultData[pixelIndex * 3 + 2]);
			// 		ushort ushortValue = (ushort)((((ushort)(byteValue / 255.0 * 400.0) - 200 + 40)));
			// 		sourceData[pixelIndex * 2] = (byte)ushortValue;
			// 		sourceData[pixelIndex * 2 + 1] = (byte)(ushortValue >> 8);
			// 		// sourceData[pixelIndex * 2] = resultData[pixelIndex * 3];
			// 	}
			// 	Console.WriteLine(resultFileIndex);
			// 	sourcePixelData.AddFrame(new MemoryByteBuffer(sourceData));
			// 	string fixedResultPath = Path.Combine(fixedResultsPath, Path.GetFileName(resultFilePaths[resultFileIndex]));
			// 	new DicomFile(source).Save(fixedResultPath);
			// }

			for (int resultFileIndex = 0; resultFileIndex != resultFilePaths.Length; resultFileIndex++)
			{
				DicomDataset result = resultDicomFiles[resultFileIndex];

				DicomPixelData oldPixelData = DicomPixelData.Create(result, false);
				byte[] oldData = oldPixelData.GetFrame(0).Data;
				result.AddOrUpdate(DicomTag.BitsAllocated, (ushort)16);
				result.AddOrUpdate(DicomTag.BitsStored, (ushort)16);
				result.AddOrUpdate(DicomTag.HighBit, (ushort)15);
				result.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)1);
				result.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)1);
				result.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME2");
				result.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);
				result.AddOrUpdate(DicomTag.Rows, result.GetSingleValue<ushort>(DicomTag.Rows));
				result.AddOrUpdate(DicomTag.Columns, result.GetSingleValue<ushort>(DicomTag.Columns));
				DicomPixelData newPixelData = DicomPixelData.Create(result, true);

				int pixelCount = oldData.Length / 3;
				byte[] sourceData = new byte[pixelCount * 2];
				for (int pixelIndex = 0; pixelIndex != pixelCount; pixelIndex++)
				{
					byte byteValue = Math.Max(Math.Max(oldData[pixelIndex * 3], oldData[pixelIndex * 3 + 1]), oldData[pixelIndex * 3 + 2]);
					ushort ushortValue = (ushort)((((ushort)(byteValue / 255.0 * 400.0) - 200 + 40)));
					sourceData[pixelIndex * 2] = (byte)ushortValue;
					sourceData[pixelIndex * 2 + 1] = (byte)(ushortValue >> 8);
					// sourceData[pixelIndex * 2] = resultData[pixelIndex * 3];
				}
				Console.WriteLine(resultFileIndex);
				newPixelData.AddFrame(new MemoryByteBuffer(sourceData));
				string fixedResultPath = Path.Combine(fixedResultsPath, Path.GetFileName(resultFilePaths[resultFileIndex]));
				new DicomFile(result).Save(fixedResultPath);
			}
		}
	}
}