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
using TextCopy;

namespace DicomTest
{
	static internal class ComparingDicomFiles
	{
		static internal unsafe void Start(string[] args)
		{
			const string firstDicomFilePath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/several-frames/sources/1.2.392.200036.9116.2.5.1.37.2418295482.1507184833.33568";
			const string secondDicomFilePath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/pathology-1/sources/IM0";
			string[] comparingFilePaths = new string[]
			{
				"/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/pathology-1/sources/IM0",
				"/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/SE00001/sources/IM00001",
				"/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/ComputerTomography/Covid/favorite/3000566.000000-03192/sources/1-001.dcm"
			};
			// var checkingDicomDataset = DicomFile.Open(severalFramesFilePath).Dataset.GetSequence(DicomTag.SharedFunctionalGroupsSequence).Items[0];
			var checkingDicomDataset = DicomFile.Open(firstDicomFilePath).Dataset;
			var contains = new HashSet<DicomTag>();
			var doesNotContain = new HashSet<DicomTag>();
			var containsStringBuilder = new StringBuilder();
			var doesNotContainStringBuilder = new StringBuilder();
			var comparingDicomDataset = DicomFile.Open(secondDicomFilePath).Dataset;
			foreach (var dicomItem in checkingDicomDataset)
			{
				if (contains.Contains(dicomItem.Tag))
					continue;
				if (!comparingDicomDataset.Contains(dicomItem.Tag))
				{
					containsStringBuilder.AppendLine(dicomItem.ToString());
					contains.Add(dicomItem.Tag);
				}
			}
			foreach (var dicomItem in comparingDicomDataset)
			{
				if (doesNotContain.Contains(dicomItem.Tag))
					continue;
				if (!checkingDicomDataset.Contains(dicomItem.Tag))
				{
					doesNotContainStringBuilder.AppendLine(dicomItem.ToString());
					doesNotContain.Add(dicomItem.Tag);
				}
			}
			File.WriteAllText("/media/nikolaev_ov/CEFE3C54FE3C36D5/contains.txt", containsStringBuilder.ToString());
			File.WriteAllText("/media/nikolaev_ov/CEFE3C54FE3C36D5/does-not-contain.txt", doesNotContainStringBuilder.ToString());
		}
	}
}