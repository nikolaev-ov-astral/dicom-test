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
using System.Text;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace DicomTest
{
	static internal class PresentationState
	{
		public struct DicomSource
		{
			public string StudyInstanceUid;
			public string SeriesInstanceUid;
			public string SopInstanceUid;
			public string SopClassUid;
			public ushort Columns;
			public ushort Rows;
			public double[] PixelSpacings;
			public double[] WindowCenters;
			public double[] WindowWidths;
		}
		public struct DicomPre
		{
			public ushort Columns;
			public ushort Rows;
			public DicomDataset PresentationStateDataset;
			public DicomSequence[][] GraphicObjectSequences;
			public DicomSequence[][] TextObjectSequences;
		}
		public enum GraphicObjectType
		{
			Point,
			Polyline,
			Interpolated,
			Circle,
			Ellipse,
			Text,
		}

		static private void EnsureDirectories(string path)
		{
			if (Directory.Exists(path))
				return;
			EnsureDirectories(Path.GetDirectoryName(path));
			Directory.CreateDirectory(path);
		}
		static private DicomPre CreatePresentationState(string[] sourceFilePaths, bool colorSoftcopy, string seriesDescription, int layerCount = 1)
		{
			DicomPre pre = default;
			DicomSource[] sources = new DicomSource[sourceFilePaths.Length];
			for (int sourceFileIndex = 0; sourceFileIndex != sourceFilePaths.Length; sourceFileIndex++)
			{
				DicomDataset sourceDataset = DicomFile.Open(sourceFilePaths[sourceFileIndex]).Dataset;
				sources[sourceFileIndex].StudyInstanceUid = sourceDataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
				sources[sourceFileIndex].SeriesInstanceUid = sourceDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
				sources[sourceFileIndex].SopInstanceUid = sourceDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
				sources[sourceFileIndex].SopClassUid = sourceDataset.GetSingleValue<string>(DicomTag.SOPClassUID);
				sources[sourceFileIndex].Columns = sourceDataset.GetSingleValue<ushort>(DicomTag.Columns);
				sources[sourceFileIndex].Rows = sourceDataset.GetSingleValue<ushort>(DicomTag.Rows);
				sources[sourceFileIndex].PixelSpacings = sourceDataset.GetValues<double>(DicomTag.PixelSpacing);
				sources[sourceFileIndex].WindowCenters = sourceDataset.GetValues<double>(DicomTag.WindowCenter);
				sources[sourceFileIndex].WindowWidths = sourceDataset.GetValues<double>(DicomTag.WindowWidth);
			}
			pre.PresentationStateDataset = new DicomDataset();
			pre.Columns = sources[0].Columns;
			pre.Rows = sources[0].Rows;

			pre.PresentationStateDataset.Add(DicomTag.StudyInstanceUID, sources[0].StudyInstanceUid);
			pre.PresentationStateDataset.Add(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID().UID);
			pre.PresentationStateDataset.Add(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID().UID);
			pre.PresentationStateDataset.Add(DicomTag.SOPClassUID, colorSoftcopy ? DicomUID.ColorSoftcopyPresentationStateStorage : DicomUID.GrayscaleSoftcopyPresentationStateStorage);
			pre.PresentationStateDataset.Add(DicomTag.Modality, "PR");
			pre.PresentationStateDataset.Add(DicomTag.SpecificCharacterSet, "ISO_IR 192");
			pre.PresentationStateDataset.AddOrUpdate(DicomTag.SeriesDescription, seriesDescription);

			DicomSequence referencedSeriesSequence = new DicomSequence(DicomTag.ReferencedSeriesSequence);
			DicomDataset[] referencedSeriesDatasets = new DicomDataset[sourceFilePaths.Length];

			for (int sourceFileIndex = 0; sourceFileIndex != sourceFilePaths.Length; sourceFileIndex++)
			{
				referencedSeriesDatasets[sourceFileIndex] = new DicomDataset().NotValidated();
				referencedSeriesDatasets[sourceFileIndex].Add(DicomTag.SeriesInstanceUID, sources[0].SeriesInstanceUid);
				DicomSequence referencedImageSequence = new DicomSequence(DicomTag.ReferencedImageSequence);
				DicomDataset referencedImageDataset = new DicomDataset();
				referencedImageDataset.Add(DicomTag.ReferencedSOPClassUID, sources[sourceFileIndex].SopClassUid);
				referencedImageDataset.Add(DicomTag.ReferencedSOPInstanceUID, sources[sourceFileIndex].SopInstanceUid);
				referencedImageSequence.Items.Add(referencedImageDataset);
				referencedSeriesDatasets[sourceFileIndex].Add(referencedImageSequence);
				referencedSeriesSequence.Items.Add(referencedSeriesDatasets[sourceFileIndex]);
			}
			pre.PresentationStateDataset.Add(referencedSeriesSequence);

			DicomSequence graphicLayerSequence = new DicomSequence(DicomTag.GraphicLayerSequence);
			for (int layerIndex = 0; layerIndex != layerCount; layerIndex++)
			{
				DicomDataset graphicLayerDataset = new DicomDataset();
				graphicLayerDataset.Add(DicomTag.GraphicLayer, layerIndex.ToString());
				graphicLayerDataset.Add(DicomTag.GraphicLayerOrder, layerIndex);
				graphicLayerSequence.Items.Add(graphicLayerDataset);
			}
			pre.PresentationStateDataset.Add(graphicLayerSequence);

			DicomSequence graphicAnnotationSequence = new DicomSequence(DicomTag.GraphicAnnotationSequence);

			pre.GraphicObjectSequences = new DicomSequence[sourceFilePaths.Length][];
			pre.TextObjectSequences = new DicomSequence[sourceFilePaths.Length][];
			for (int sourceFileIndex = 0; sourceFileIndex != sourceFilePaths.Length; sourceFileIndex++)
			{
				pre.GraphicObjectSequences[sourceFileIndex] = new DicomSequence[layerCount];
				pre.TextObjectSequences[sourceFileIndex] = new DicomSequence[layerCount];
				for (int layerIndex = 0; layerIndex != layerCount; layerIndex++)
				{
					DicomDataset graphicAnnotationDataset = new DicomDataset();
					graphicAnnotationDataset.Add(referencedSeriesDatasets[sourceFileIndex].GetSequence(DicomTag.ReferencedImageSequence));
					pre.GraphicObjectSequences[sourceFileIndex][layerIndex] = new DicomSequence(DicomTag.GraphicObjectSequence);
					pre.TextObjectSequences[sourceFileIndex][layerIndex] = new DicomSequence(DicomTag.TextObjectSequence);
					graphicAnnotationDataset.Add(pre.GraphicObjectSequences[sourceFileIndex][layerIndex]);
					graphicAnnotationDataset.Add(pre.TextObjectSequences[sourceFileIndex][layerIndex]);
					graphicAnnotationDataset.Add(DicomTag.GraphicLayer, layerIndex.ToString());
					graphicAnnotationSequence.Items.Add(graphicAnnotationDataset);
				}
			}
			pre.PresentationStateDataset.Add(graphicAnnotationSequence);

			DicomSequence displayedAreaSelectionSequence = new DicomSequence(DicomTag.DisplayedAreaSelectionSequence);
			for (int sourceFileIndex = 0; sourceFileIndex != sourceFilePaths.Length; sourceFileIndex++)
			{
				DicomDataset displayedAreaSelectionDataset = new DicomDataset();
				displayedAreaSelectionDataset.Add(referencedSeriesDatasets[sourceFileIndex].GetSequence(DicomTag.ReferencedImageSequence));
				displayedAreaSelectionDataset.Add(DicomTag.DisplayedAreaTopLeftHandCorner, 1, 1);
				displayedAreaSelectionDataset.Add(DicomTag.DisplayedAreaBottomRightHandCorner, (int)pre.Columns, (int)pre.Rows);
				displayedAreaSelectionDataset.Add(DicomTag.PresentationSizeMode, "SCALE TO FIT");
				displayedAreaSelectionDataset.Add(DicomTag.PresentationPixelSpacing, sources[sourceFileIndex].PixelSpacings);
				displayedAreaSelectionSequence.Items.Add(displayedAreaSelectionDataset);
			}
			pre.PresentationStateDataset.Add(displayedAreaSelectionSequence);

			DicomSequence softcopyVoiLutSequence = new DicomSequence(DicomTag.SoftcopyVOILUTSequence);
			DicomDataset softcopyVoiLutDataset = new DicomDataset();
			softcopyVoiLutDataset.Add(DicomTag.WindowCenter, sources[0].WindowCenters);
			softcopyVoiLutDataset.Add(DicomTag.WindowWidth, sources[0].WindowWidths);
			softcopyVoiLutSequence.Items.Add(softcopyVoiLutDataset);
			pre.PresentationStateDataset.Add(softcopyVoiLutSequence);

			return pre;
		}
		static private (ushort l, ushort a, ushort b) GetDicomColor(ColorMine.ColorSpaces.ColorSpace color)
		{
			var lab = color.To<ColorMine.ColorSpaces.Lab>();
			ushort l = (ushort)(lab.L * 0.01 * ushort.MaxValue);
			ushort a = (ushort)((Math.Clamp(((lab.A + 128) / 255), 0.0, 1.0)) * ushort.MaxValue);
			ushort b = (ushort)((Math.Clamp(((lab.B + 128) / 255), 0.0, 1.0)) * ushort.MaxValue);
			return (l, a, b);
		}
		static private void AddLineStyleSequence(DicomDataset graphicObjectDataset, float thickness, ColorMine.ColorSpaces.ColorSpace lineColor)
		{
			DicomSequence lineStyleSequence = new DicomSequence(DicomTag.LineStyleSequence);
			DicomDataset lineStyleDataset = new DicomDataset();
			if (lineColor != null)
			{
				var dicomColor = GetDicomColor(lineColor);
				lineStyleDataset.Add(DicomTag.PatternOnColorCIELabValue, dicomColor.l, dicomColor.a, dicomColor.b);
			}
			lineStyleDataset.Add(DicomTag.LineThickness, thickness);
			lineStyleSequence.Items.Add(lineStyleDataset);
			graphicObjectDataset.Add(lineStyleSequence);
		}
		static private void AddPoint(DicomPre pre, float x, float y, float thickness, ColorMine.ColorSpaces.ColorSpace lineColor, int fileIndex = 0, int layerIndex = 0)
		{
			var graphicObjectItem = new DicomDataset
			{
				{DicomTag.GraphicAnnotationUnits, "PIXEL"},
				{DicomTag.GraphicDimensions, (ushort)2},
				{DicomTag.NumberOfGraphicPoints, (ushort)1},
				{DicomTag.GraphicType, "POINT"},
				{DicomTag.GraphicFilled, "N"},
				{DicomTag.GraphicData, x, y},
			};

			AddLineStyleSequence(graphicObjectItem, thickness, lineColor);

			pre.GraphicObjectSequences[fileIndex][layerIndex].Items.Add(graphicObjectItem);
		}
		static private void AddPolyline(DicomPre pre, float x, float y, float width, float height, float thickness, ColorMine.ColorSpaces.ColorSpace lineColor, int fileIndex = 0, int layerIndex = 0)
		{
			float x1 = x;
			float y1 = y;

			float x2 = x + width;
			float y2 = y;

			float x3 = x + width;
			float y3 = y + height;

			float x4 = x;
			float y4 = y + height;

			var graphicObjectItem = new DicomDataset
			{
				{DicomTag.GraphicAnnotationUnits, "PIXEL"},
				{DicomTag.GraphicDimensions, (ushort) 2},
				{DicomTag.NumberOfGraphicPoints, (ushort) 5},
				{DicomTag.GraphicType, "POLYLINE"},
				{DicomTag.GraphicFilled, "N"},
				{DicomTag.GraphicData, x1, y1, x2, y2, x3, y3, x4, y4, x1, y1}
			};

			AddLineStyleSequence(graphicObjectItem, thickness, lineColor);

			pre.GraphicObjectSequences[fileIndex][layerIndex].Items.Add(graphicObjectItem);
		}
		static private void AddInterpolated(DicomPre pre, float x, float y, float width, float height, float thickness, ColorMine.ColorSpaces.ColorSpace lineColor, int fileIndex = 0, int layerIndex = 0)
		{
			float x1 = x;
			float y1 = y;

			float x2 = x + width;
			float y2 = y + height * 0.5F;

			float x3 = x;
			float y3 = y + height;

			var graphicObjectItem = new DicomDataset
			{
				{DicomTag.GraphicAnnotationUnits, "PIXEL"},
				{DicomTag.GraphicDimensions, (ushort) 2},
				{DicomTag.NumberOfGraphicPoints, (ushort) 3},
				{DicomTag.GraphicType, "INTERPOLATED"},
				{DicomTag.GraphicFilled, "N"},
				{DicomTag.GraphicData, x1, y1, x2, y2, x3, y3},
			};

			AddLineStyleSequence(graphicObjectItem, thickness, lineColor);
			pre.GraphicObjectSequences[fileIndex][layerIndex].Items.Add(graphicObjectItem);
		}
		static private void AddCircle(DicomPre pre, float x, float y, float diameter, float thickness, ColorMine.ColorSpaces.ColorSpace lineColor, int fileIndex = 0, int layerIndex = 0)
		{
			float x1 = x + diameter * 0.5F;
			float y1 = y + diameter * 0.5F;

			float x2 = x;
			float y2 = y;

			var graphicObjectItem = new DicomDataset
			{
				{DicomTag.GraphicAnnotationUnits, "PIXEL"},
				{DicomTag.GraphicDimensions, (ushort) 2},
				{DicomTag.NumberOfGraphicPoints, (ushort) 2},
				{DicomTag.GraphicType, "CIRCLE"},
				{DicomTag.GraphicFilled, "N"},
				{DicomTag.GraphicData, x1, y1, x2, y2},
			};

			AddLineStyleSequence(graphicObjectItem, thickness, lineColor);
			pre.GraphicObjectSequences[fileIndex][layerIndex].Items.Add(graphicObjectItem);
		}
		static private void AddEllipse(DicomPre pre, float x, float y, float width, float height, float thickness, ColorMine.ColorSpaces.ColorSpace lineColor, int fileIndex = 0, int layerIndex = 0)
		{
			float x1 = x + width * 0.5F;
			float y1 = y;

			float x2 = x + width * 0.5F;
			float y2 = y + height;

			float x3 = x;
			float y3 = y + height * 0.5F;

			float x4 = x + width;
			float y4 = y + height * 0.5F;

			var graphicObjectItem = new DicomDataset
			{
				{DicomTag.GraphicAnnotationUnits, "PIXEL"},
				{DicomTag.GraphicDimensions, (ushort) 2},
				{DicomTag.NumberOfGraphicPoints, (ushort) 4},
				{DicomTag.GraphicType, "ELLIPSE"},
				{DicomTag.GraphicFilled, "N"},
				{DicomTag.GraphicData, x1, y1, x2, y2, x3, y3, x4, y4},
			};

			AddLineStyleSequence(graphicObjectItem, thickness, lineColor);
			pre.GraphicObjectSequences[fileIndex][layerIndex].Items.Add(graphicObjectItem);
		}
		static private void AddText(DicomPre pre, string text, float x, float y, float width, float height, int horizontalAlignment, int verticalAlignment, bool useBoundingBox, bool showAnchor, ColorMine.ColorSpaces.ColorSpace color, int fileIndex = 0, int layerIndex = 0)
		{
			float x1 = x;
			float y1 = y;
			float x2 = x + width;
			float y2 = y + height;

			var textObjectItem = new DicomDataset();
			if (useBoundingBox)
			{
				textObjectItem.Add(DicomTag.BoundingBoxAnnotationUnits, "PIXEL");
				textObjectItem.Add(DicomTag.BoundingBoxTopLeftHandCorner, x1, y1);
				textObjectItem.Add(DicomTag.BoundingBoxBottomRightHandCorner, x2, y2);
				textObjectItem.Add(DicomTag.BoundingBoxTextHorizontalJustification, horizontalAlignment == 0 ? "LEFT" : horizontalAlignment == 1 ? "RIGHT" : "CENTER");
			}
			else
			{
				textObjectItem.Add(DicomTag.AnchorPointAnnotationUnits, "PIXEL");
				textObjectItem.Add(DicomTag.AnchorPointVisibility, showAnchor ? "Y" : "N");
				textObjectItem.Add(DicomTag.AnchorPoint, x1, y1);
				textObjectItem.Add(DicomTag.BoundingBoxTextHorizontalJustification, horizontalAlignment == 0 ? "LEFT" : horizontalAlignment == 1 ? "RIGHT" : "CENTER");
			}
			textObjectItem.Add(DicomTag.UnformattedTextValue, Encoding.UTF8, text);

			var textStyleSequence = new DicomSequence(DicomTag.TextStyleSequence);
			var textStyleSequenceItem = new DicomDataset();
			textStyleSequenceItem.Add(DicomTag.HorizontalAlignment, horizontalAlignment == 0 ? "LEFT" : horizontalAlignment == 1 ? "RIGHT" : "CENTER");
			textStyleSequenceItem.Add(DicomTag.VerticalAlignment, verticalAlignment == 0 ? "BOTTOM" : verticalAlignment == 1 ? "TOP" : "CENTER");
			if (color != null)
			{
				var dicomColor = GetDicomColor(color);
				textStyleSequenceItem.Add(DicomTag.TextColorCIELabValue, dicomColor.l, dicomColor.a, dicomColor.b);
			}
			textStyleSequence.Items.Add(textStyleSequenceItem);
			textObjectItem.Add(textStyleSequence);

			pre.TextObjectSequences[fileIndex][layerIndex].Items.Add(textObjectItem);
		}
		static private void GraphicObjectTest(string studyDirectoryPath, string outputFileName, bool colorSoftcopy, string seriesDescription, GraphicObjectType figureType, string text = "", bool useBoundingBox = false, bool showAnchor = false)
		{
			string[] files = Directory.GetFiles(studyDirectoryPath);

			string sourceFilePath = files.Single(a => DicomFile.Open(a).Dataset.GetSingleValue<string>(DicomTag.Modality) != "PR");

			var pre = CreatePresentationState(new[] { sourceFilePath }, colorSoftcopy, seriesDescription);
			const float minThickness = 1F;
			const float maxThickness = 8F;
			for (float y = maxThickness; y + y * 0.25F + maxThickness * 0.5F <= pre.Rows; y += y * 0.25F + maxThickness * 2F)
			{
				int count = 0;
				for (float x = maxThickness; x + x * 0.25F + maxThickness * 0.5F <= pre.Columns; x += x * 0.25F + maxThickness * 2F, count++)
				{
					float height = y * 0.25F;
					float width = x * 0.25F;
					float diameter = Math.Min(width, height) * 0.5F;
					float thickness = minThickness + (maxThickness - minThickness) * (x / pre.Columns);
					ColorMine.ColorSpaces.Hsv color = new ColorMine.ColorSpaces.Hsv { H = ((x + width) / pre.Columns) * 360, S = ((1F - (y) / pre.Rows)), V = (1F - ((y) / pre.Rows)) };
					switch (figureType)
					{
						case GraphicObjectType.Point:
							AddPoint(pre, x, y, thickness, colorSoftcopy ? color : null);
							break;
						case GraphicObjectType.Polyline:
							AddPolyline(pre, x, y, width, height, thickness, colorSoftcopy ? color : null);
							break;
						case GraphicObjectType.Interpolated:
							AddInterpolated(pre, x, y, width, height, thickness, colorSoftcopy ? color : null);
							break;
						case GraphicObjectType.Circle:
							AddCircle(pre, x, y, diameter, thickness, colorSoftcopy ? color : null);
							break;
						case GraphicObjectType.Ellipse:
							AddEllipse(pre, x, y, width, height, thickness, colorSoftcopy ? color : null);
							break;
						case GraphicObjectType.Text:
							AddPolyline(pre, x, y, width, height, minThickness, null);
							AddText(pre, text, x, y, width, height, count % 3, count % 4, useBoundingBox, showAnchor, colorSoftcopy ? color : null);
							break;
					}
				}
			}
			if (figureType == GraphicObjectType.Text && !useBoundingBox)
			{
				AddText(pre, text, pre.Columns * 0.5F, pre.Rows, 0, 0, 0, 0, false, showAnchor, colorSoftcopy ? new ColorMine.ColorSpaces.Rgb { R = 255, G = 255, B = 255 } : null);
				AddText(pre, text, pre.Columns, pre.Rows * 0.5F, 0, 0, 0, 0, false, showAnchor, colorSoftcopy ? new ColorMine.ColorSpaces.Rgb { R = 255, G = 255, B = 255 } : null);
			}
			string destFilePath = Path.Combine(studyDirectoryPath, Path.GetFileNameWithoutExtension(outputFileName) + ".dcm");
			EnsureDirectories(Path.GetDirectoryName(destFilePath));
			new DicomFile(pre.PresentationStateDataset).Save(destFilePath);
		}
		static private void GraphicObjectTest()
		{
			const string studyDirectoryPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/Presentation State/GraphicObjectTest";
			const string testText = @"ТЕКСТ ТЕКСТ
текст текст";
			GraphicObjectTest(studyDirectoryPath, "gsps-point.dcm", false, "gsps-point", GraphicObjectType.Point);
			GraphicObjectTest(studyDirectoryPath, "gsps-polyline.dcm", false, "gsps-polyline", GraphicObjectType.Polyline);
			GraphicObjectTest(studyDirectoryPath, "gsps-interpolated.dcm", false, "gsps-interpolated", GraphicObjectType.Interpolated);
			GraphicObjectTest(studyDirectoryPath, "gsps-circle.dcm", false, "gsps-circle", GraphicObjectType.Circle);
			GraphicObjectTest(studyDirectoryPath, "gsps-ellipse.dcm", false, "gsps-ellipse", GraphicObjectType.Ellipse);
			GraphicObjectTest(studyDirectoryPath, "gsps-text-bounding-box.dcm", false, "gsps-text-bounding-box", GraphicObjectType.Text, testText, true);
			GraphicObjectTest(studyDirectoryPath, "gsps-text-anchor-invisible.dcm", false, "gsps-text-anchor-invisible", GraphicObjectType.Text, testText, false, false);
			GraphicObjectTest(studyDirectoryPath, "gsps-text-anchor-visible.dcm", false, "gsps-text-anchor-visible", GraphicObjectType.Text, testText, false, true);

			GraphicObjectTest(studyDirectoryPath, "csps-point.dcm", true, "csps-point", GraphicObjectType.Point);
			GraphicObjectTest(studyDirectoryPath, "csps-polyline.dcm", true, "csps-polyline", GraphicObjectType.Polyline);
			GraphicObjectTest(studyDirectoryPath, "csps-interpolated.dcm", true, "csps-interpolated", GraphicObjectType.Interpolated);
			GraphicObjectTest(studyDirectoryPath, "csps-circle.dcm", true, "csps-circle", GraphicObjectType.Circle);
			GraphicObjectTest(studyDirectoryPath, "csps-ellipse.dcm", true, "csps-ellipse", GraphicObjectType.Ellipse);
			GraphicObjectTest(studyDirectoryPath, "csps-text-bounding-box.dcm", true, "csps-text-bounding-box", GraphicObjectType.Text, testText, true);
			GraphicObjectTest(studyDirectoryPath, "csps-text-anchor-invisible.dcm", true, "csps-text-anchor-invisible", GraphicObjectType.Text, testText, false, false);
			GraphicObjectTest(studyDirectoryPath, "csps-text-anchor-visible.dcm", true, "csps-text-anchor-visible", GraphicObjectType.Text, testText, false, true);
		}
		static private void LayerTest(string studyDirectoryPath, string outputFileName, string seriesDescription)
		{
			string[] files = Directory.GetFiles(studyDirectoryPath);

			string sourceFilePath = files.Single(a => DicomFile.Open(a).Dataset.GetSingleValue<string>(DicomTag.Modality) != "PR");

			const int layerCount = 25;
			var pre = CreatePresentationState(new[] { sourceFilePath }, true, seriesDescription, layerCount);
			const float thickness = 5F;
			float size = MathF.Min(pre.Columns, pre.Rows) - thickness * (layerCount + 2);
			List<int> layerIndices = new List<int>();
			for (int layerIndex = 0; layerIndex != layerCount; layerIndex++)
				layerIndices.Add(layerIndex);
			for (int figureIndex = 0; figureIndex != layerCount; figureIndex++)
			{
				ColorMine.ColorSpaces.Hsv color = new ColorMine.ColorSpaces.Hsv { H = ((float)figureIndex / layerCount) * 360, S = 1F, V = 1F };
				float offset = thickness * (figureIndex + 1);
				int layerIndex = figureIndex < layerCount / 2 ? 0 : layerIndices.Count - 1;
				AddPolyline(pre, offset, offset, size, size, thickness, color, 0, layerIndices[layerIndex]);
				layerIndices.RemoveAt(layerIndex);
			}

			string destFilePath = Path.Combine(studyDirectoryPath, Path.GetFileNameWithoutExtension(outputFileName) + ".dcm");
			EnsureDirectories(Path.GetDirectoryName(destFilePath));
			new DicomFile(pre.PresentationStateDataset).Save(destFilePath);
		}
		static private void LayerTest()
		{
			const string studyDirectoryPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/Presentation State/LayerTest";
			LayerTest(studyDirectoryPath, "csps-layer.dcm", "csps-layer");
		}
		static private void OnePresentationStateForSeriesTest(string studyDirectoryPath, string outputFileName, string seriesDescription, int totalFigureCount)
		{
			string[] files = Directory.GetFiles(studyDirectoryPath);

			string[] sourceFilePaths = files.Where(a => DicomFile.Open(a).Dataset.GetSingleValue<string>(DicomTag.Modality) != "PR").OrderBy(a => DicomFile.Open(a).Dataset.GetSingleValue<int>(DicomTag.InstanceNumber)).ToArray();

			var pre = CreatePresentationState(sourceFilePaths, true, seriesDescription);
			const float thickness = 2F;
			int figurePerFrame = totalFigureCount / sourceFilePaths.Length + 1;
			totalFigureCount = figurePerFrame * sourceFilePaths.Length;
			float minImageSize = MathF.Min(pre.Columns, pre.Rows);
			float size = minImageSize / 4;
			int totalFigureIndex = 0;
			for (int sourceFileIndex = 0; sourceFileIndex != sourceFilePaths.Length; sourceFileIndex++)
			{
				for (int figureIndex = 0; figureIndex != figurePerFrame; figureIndex++, totalFigureIndex++)
				{
					ColorMine.ColorSpaces.Hsv color = new ColorMine.ColorSpaces.Hsv { H = ((float)totalFigureIndex / totalFigureCount) * 360, S = 1F, V = 1F };
					float offset = thickness + ((float)totalFigureIndex / totalFigureCount) * (minImageSize - size);
					AddPolyline(pre, offset, offset, size - thickness * 2, size - thickness * 2, thickness, color, sourceFileIndex, 0);
				}
			}

			string destFilePath = Path.Combine(studyDirectoryPath, Path.GetFileNameWithoutExtension(outputFileName) + ".dcm");
			EnsureDirectories(Path.GetDirectoryName(destFilePath));
			new DicomFile(pre.PresentationStateDataset).Save(destFilePath);
		}
		static private void PresentationStatePerSeriesTest()
		{
			const string studyDirectoryPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/Presentation State/PresentationStatePerSeriesTest";
			OnePresentationStateForSeriesTest(studyDirectoryPath, "csps-per-series-500.dcm", "csps-per-series-500", 500);
			OnePresentationStateForSeriesTest(studyDirectoryPath, "csps-per-series-1500.dcm", "csps-per-series-1500", 1500);
			OnePresentationStateForSeriesTest(studyDirectoryPath, "csps-per-series-5000.dcm", "csps-per-series-5000", 5000);
		}
		static private void PresentationStatePerInstanceTest(string studyDirectoryPath, string outputFileName, string seriesDescription, int totalFigureCount)
		{
			string[] files = Directory.GetFiles(studyDirectoryPath);

			string[] sourceFilePaths = files.Where(a => DicomFile.Open(a).Dataset.GetSingleValue<string>(DicomTag.Modality) != "PR").OrderBy(a => DicomFile.Open(a).Dataset.GetSingleValue<int>(DicomTag.InstanceNumber)).ToArray();

			const float thickness = 2F;
			int figurePerFrame = totalFigureCount / sourceFilePaths.Length + 1;
			totalFigureCount = figurePerFrame * sourceFilePaths.Length;
			int totalFigureIndex = 0;
			for (int sourceFileIndex = 0; sourceFileIndex != sourceFilePaths.Length; sourceFileIndex++)
			{
				var pre = CreatePresentationState(new string[] { sourceFilePaths[sourceFileIndex] }, true, seriesDescription);
				float minImageSize = MathF.Min(pre.Columns, pre.Rows);
				float size = minImageSize / 4;
				for (int figureIndex = 0; figureIndex != figurePerFrame; figureIndex++, totalFigureIndex++)
				{
					ColorMine.ColorSpaces.Hsv color = new ColorMine.ColorSpaces.Hsv { H = ((float)totalFigureIndex / totalFigureCount) * 360, S = 1F, V = 1F };
					float offset = thickness + ((float)totalFigureIndex / totalFigureCount) * (minImageSize - size);
					AddPolyline(pre, offset, offset, size - thickness * 2, size - thickness * 2, thickness, color, 0, 0);
				}

				string destFilePath = Path.Combine(studyDirectoryPath, Path.GetFileNameWithoutExtension(outputFileName) + $"-{sourceFileIndex}.dcm");
				EnsureDirectories(Path.GetDirectoryName(destFilePath));
				new DicomFile(pre.PresentationStateDataset).Save(destFilePath);
			}
		}
		static private void PresentationStatePerInstanceTest()
		{
			const string studyDirectoryPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/Presentation State/PresentationStatePerInstanceTest";
			PresentationStatePerInstanceTest(studyDirectoryPath, "csps-per-instance-500.dcm", "csps-per-instance-500", 500);
			PresentationStatePerInstanceTest(studyDirectoryPath, "csps-per-instance-1500.dcm", "csps-per-instance-1500", 1500);
			PresentationStatePerInstanceTest(studyDirectoryPath, "csps-per-instance-5000.dcm", "csps-per-instance-5000", 5000);
		}
		static private void ScTextGspsShapesTest(string studyDirectoryPath, string scFileName, string outputFileName, string seriesDescription)
		{
			string[] files = Directory.GetFiles(studyDirectoryPath);

			string sourceFilePath = files.Single(a => DicomFile.Open(a).Dataset.GetSingleValue<string>(DicomTag.Modality) != "PR");

			var dataset = DicomFile.Open(sourceFilePath).Dataset;
			var image = dataset.ReadL16Image();
			var newDataset = new DicomDataset();
			dataset.CopyTo(newDataset);

			FontCollection fontCollection = new FontCollection();
			ushort windowCenter = dataset.GetValue<ushort>(DicomTag.WindowCenter, 0);
			ushort windowWidth = dataset.GetValue<ushort>(DicomTag.WindowWidth, 0);
			ushort colorValue = (ushort)(windowCenter + windowWidth);
			Color color = new Color(new Rgba64(colorValue, colorValue, colorValue, colorValue));
			FontFamily fontFamily = fontCollection.Install("/media/nikolaev_ov/CEFE3C54FE3C36D5/fonts/TimesNewRoman/TimesNewRomanRegular/TimesNewRomanRegular.ttf");
			Font font = new Font(fontFamily, 36, FontStyle.Regular);
			var objectCoord = (1000F, 1000F);
			var objectSize = (400F, 200f);
			image.Mutate(a =>
			{
				a.DrawText("1: (Информация о находке)", font, color, new PointF(100, 100));
				a.DrawText(new TextGraphicsOptions(new GraphicsOptions(), new TextOptions() { HorizontalAlignment = HorizontalAlignment.Center }), "1", font, color, new PointF(objectCoord.Item1 + objectSize.Item1 * 0.5F, objectCoord.Item2 + objectSize.Item2 + 5));
			});

			var pixelData = image.ToPixelData();

			var dicomPixelData = DicomPixelData.Create(newDataset, true);
			dicomPixelData.AddFrame(new MemoryByteBuffer(pixelData));
			dataset = newDataset.Compress(DicomTransferSyntax.JPEGLSLossless, new DicomJpegLsLosslessCodec(), new DicomJpegParams());

			string destFilePath = Path.Combine(studyDirectoryPath, Path.GetFileNameWithoutExtension(scFileName) + ".dcm");
			EnsureDirectories(Path.GetDirectoryName(destFilePath));
			new DicomFile(dataset).Save(destFilePath);

			var pre = CreatePresentationState(new[] { sourceFilePath }, true, seriesDescription, 1);
			const float thickness = 2F;
			ColorMine.ColorSpaces.Rgb rgb = new ColorMine.ColorSpaces.Rgb { G = 255 };
			AddPolyline(pre, objectCoord.Item1 + thickness * 0.5F, objectCoord.Item2 + thickness * 0.5F, objectSize.Item1 - thickness, objectSize.Item2 - thickness, thickness, rgb);

			destFilePath = Path.Combine(studyDirectoryPath, Path.GetFileNameWithoutExtension(outputFileName) + ".dcm");
			EnsureDirectories(Path.GetDirectoryName(destFilePath));
			new DicomFile(pre.PresentationStateDataset).Save(destFilePath);
		}
		static private void ScTextGspsShapesTest()
		{
			const string studyDirectoryPath = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM/Presentation State/ScTextGspsShapesTest";
			ScTextGspsShapesTest(studyDirectoryPath, "sc.dcm", "csps.dcm", "csps");
		}
		static internal unsafe void Start(string[] args)
		{
			GraphicObjectTest();
			// LayerTest();
			// PresentationStatePerSeriesTest();
			// PresentationStatePerInstanceTest();
			// ScTextGspsShapesTest();
		}
	}
}