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
using System.Drawing.Imaging;

namespace DicomTest
{
    static internal class CheckImageAttributesProgram
    {
        static CheckImageAttributesProgram()
        {
            ImageManager.SetImplementation(WinFormsImageManager.Instance);
        }

        static private void Add(string directory, HashSet<string> photometricInterpretationValues, HashSet<ushort> bitsAllocatedValues, HashSet<ushort> bitsStoredValues, HashSet<ushort> samplesPerPixelValues, ref uint count)
        {
            var filePaths = Directory.GetFiles(directory);
            foreach (var filePath in filePaths)
            {
                DicomFile dicomFile;
                try { dicomFile = DicomFile.Open(filePath); }
                catch { continue; }
                if (dicomFile == null)
                    continue;
                if (!dicomFile.Dataset.TryGetSingleValue<string>(DicomTag.PhotometricInterpretation, out string photometricInterpretationValue))
                    continue;
                photometricInterpretationValues.Add(photometricInterpretationValue);
                bitsAllocatedValues.Add(dicomFile.Dataset.GetSingleValue<ushort>(DicomTag.BitsAllocated));
                bitsStoredValues.Add(dicomFile.Dataset.GetSingleValue<ushort>(DicomTag.BitsStored));
                samplesPerPixelValues.Add(dicomFile.Dataset.GetSingleValue<ushort>(DicomTag.SamplesPerPixel));
                if (!dicomFile.Dataset.Contains(DicomTag.WindowCenter))
                {

                }
                if (!dicomFile.Dataset.Contains(DicomTag.WindowWidth))
                {
                    
                }
                count++;
            }
            var subDirectories = Directory.GetDirectories(directory);
            foreach (var subDirectrory in subDirectories)
                Add(subDirectrory, photometricInterpretationValues, bitsAllocatedValues, bitsStoredValues, samplesPerPixelValues, ref count);
        }
        static internal unsafe void Start(string[] args)
        {
            string sourceDirectory = Environment.GetEnvironmentVariable("SOURCE_DIRECTORY");
            var photometricInterpretationValues = new HashSet<string>();
            var bitsAllocatedValues = new HashSet<ushort>();
            var bitsStoredValues = new HashSet<ushort>();
            var samplesPerPixelValues = new HashSet<ushort>();
            uint count = 0;
            Add(sourceDirectory, photometricInterpretationValues, bitsAllocatedValues, bitsStoredValues, samplesPerPixelValues, ref count);
        }
    }
}