using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;

namespace DicomTest
{
	static internal class Program
	{
		static private void Main(string[] args)
		{
			// RemovePixelDataProgram.Start(args);
			// ItkSimpleTestProgram.Start(args);
			// ImageTestProgram.Start(args);
			// CheckImageAttributesProgram.Start(args);
			// TestDicom48bpp.Start(args);
			// Testing3D.Start(args);
			// TestingWindowPresets.Start(args);
			// TestingMultiImageDicom.Start(args);
			// ComparingDicomFiles.Start(args);
			PresentationState.Start(args);
			// PresentationStateAnalyzer.Start(args);
		}
	}
}