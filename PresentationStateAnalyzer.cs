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
	static internal class PresentationStateAnalyzer
	{
		static internal unsafe void Start(string[] args)
		{
			const string sourceFilePath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/gsps.pre";
			const string destFilePath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/gsps2.pre";
			DicomDataset sourceDataset = DicomFile.Open(sourceFilePath).Dataset;
			HashSet<DicomTag> notRemovingTags = new HashSet<DicomTag>
			{
				DicomTag.StudyInstanceUID,
				DicomTag.SeriesInstanceUID,
				DicomTag.SOPInstanceUID,
				DicomTag.SOPClassUID,
				DicomTag.Modality,
				DicomTag.SpecificCharacterSet,

				
				DicomTag.ReferencedSeriesSequence,
				DicomTag.GraphicAnnotationSequence,
				DicomTag.GraphicLayerSequence,
				DicomTag.DisplayedAreaSelectionSequence,
				DicomTag.SoftcopyVOILUTSequence,
			};
			string StudyInstanceUID = sourceDataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);//1.3.6.1.4.1.14519.5.2.1.6279.6001.298806137288633453246975630178
			string SeriesInstanceUID = sourceDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);//1.3.6.1.4.1.14519.5.2.1.6279.6001.298806137288633453246.1.2
			string SOPInstanceUID = sourceDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);//1.3.6.1.4.1.14519.5.2.1.6279.6001.179049373636438705059.1.2
			string SOPClassUID = sourceDataset.GetSingleValue<string>(DicomTag.SOPClassUID);//1.2.840.10008.5.1.4.1.1.11.1
			string Modality = sourceDataset.GetSingleValue<string>(DicomTag.Modality);//1.2.840.10008.5.1.4.1.1.11.1
			List<DicomTag> removingTags = new List<DicomTag>();
			foreach (DicomItem item in sourceDataset)
				if (!notRemovingTags.Contains(item.Tag))
					removingTags.Add(item.Tag);
			sourceDataset.Remove(removingTags.ToArray());
			
			sourceDataset.AddOrUpdate(DicomTag.StudyInstanceUID, new DicomUID
			(
				"1.3.6.1.4.1.14519.5.2.1.6279.6001.298806137288633453246975630178",
				"Study Instance UID",
				DicomUidType.SOPInstance
			));
			sourceDataset.AddOrUpdate(DicomTag.SeriesInstanceUID, new DicomUID
			(
				"1.3.6.1.4.1.14519.5.2.1.6279.6001.298806137288633453246.1.2",
				"Series Instance UID",
				DicomUidType.SOPInstance
			));
			sourceDataset.AddOrUpdate(DicomTag.SOPInstanceUID, new DicomUID
			(
				"1.3.6.1.4.1.14519.5.2.1.6279.6001.179049373636438705059.1.2",
				"SOP Instance UID",
				DicomUidType.SOPInstance
			));
			sourceDataset.AddOrUpdate(DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.11.1");
			sourceDataset.AddOrUpdate(DicomTag.Modality, "PR");
			sourceDataset.AddOrUpdate(DicomTag.SpecificCharacterSet, "ISO_IR 192");
			new DicomFile(sourceDataset).Save(destFilePath);
		}
	}
}