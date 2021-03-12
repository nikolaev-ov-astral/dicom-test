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
	static internal class TestingWindowPresets
	{
		public struct AttributeValue
		{
			public DicomTag Tag;
			public string[] Values;
		}

		static private readonly DicomTag[] _notCopyingTags;

		static TestingWindowPresets()
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

		internal static AttributeValue[] GetNotExistingOrDifferentAttributes(DicomDataset source, DicomDataset result)
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
		internal static AttributeValue[] GetDifferentAttributes(DicomDataset source, DicomDataset result)
		{
			List<AttributeValue> notExistingAttributes = new List<AttributeValue>();
			foreach (DicomItem item in source)
			{
				if (!item.ValueRepresentation.IsString)
					continue;
				if (_notCopyingTags.Contains(item.Tag))
					continue;
				string[] sourceValues = source.GetValues<string>(item.Tag);
				if (!result.Contains(item.Tag))
					continue;
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
			const string sourcesPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/sikorskaya/sources";
			const string resultsPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/sikorskaya/results";
			const string fixedResultsPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/sikorskaya/fixed-results";
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

			for (int resultFileIndex = 0; resultFileIndex != resultFilePaths.Length; resultFileIndex++)
			{
				DicomDataset result = resultDicomFiles[resultFileIndex];
				int instanceNumber = result.GetSingleValue<int>(DicomTag.InstanceNumber);
				DicomDataset source = sourceDicomFiles.Single(a => a.GetSingleValue<int>(DicomTag.InstanceNumber) == instanceNumber);
				AttributeValue[] values = GetDifferentAttributes(source, result);
				string fixedResultPath = Path.Combine(fixedResultsPath, Path.GetFileName(resultFilePaths[resultFileIndex]));
				result.NotValidated();
				foreach (AttributeValue value in values)
				{
					if (value.Tag == DicomTag.Modality)
						result.AddOrUpdate(value.Tag, value.Values);
				}
				new DicomFile(result).Save(fixedResultPath);
			}
		}
	}
}