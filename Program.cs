using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using Dicom.Imaging.NativeCodec;

namespace DicomTest
{
    static internal class Program
    {
        // static private void StartGdcm()
        // {
        //     var s = new gdcm.Reader();
        //     s.SetFileName("/mnt/c/Users/nikol/Desktop/CR.1.871.3.1522049724.50292.18240.95439262.3570939512.1.1.1");
        //     bool b = s.CanRead();
        //     bool d = s.Read();
        // }

        static private unsafe void Main(string[] args)
        {
            // ComparingDicomFiles.Start(args);
            // return;
            // StartGdcm();

            Dicom.Imaging.Codec.TranscoderManager.SetImplementation(new NativeTranscoderManager());
            ImageManager.SetImplementation(WinFormsImageManager.Instance);
            const string filePath = "/mnt/d/DICOM/Fluorography/favorite/jpeg-process14-1/sources/1.871.3.2050448135.34720.20335.72851334.2248937210.1.1.1";
            DicomFile dicomFile = DicomFile.Open(filePath);
            DicomDataset dataset = dicomFile.Dataset;
            DicomImage dicomImage = new DicomImage(dataset, 0);
            var image = dicomImage.RenderImage(0);
            var bitmap = image.AsSharedBitmap();
            bitmap.Save("/mnt/c/Users/nikol/Desktop/image-new.png");

            byte[] pixelData;
            Dictionary<string, string> meta;
            using (FileStream readStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                pixelData = DicomDatasetExtensions.DecodeImage(readStream, out meta).PixelData;
            // using (FileStream readStream = new FileStream(sc, FileMode.Open, FileAccess.Read))
            //     pixelData = DicomDatasetExtensions.DecodeImage(readStream, out meta).PixelData;
            ushort width = dataset.GetValue<ushort>(DicomTag.Columns, 0);
            ushort height = dataset.GetValue<ushort>(DicomTag.Rows, 0);
            byte[] pixelData2 = new byte[pixelData.Length / 2 * 3];
            double windowCenter = dataset.GetValue<double>(DicomTag.WindowCenter, 0);
            double windowWidth = dataset.GetValue<double>(DicomTag.WindowWidth, 0);
            fixed (byte* pPixelData = &pixelData[0])
                for (int pixelIndex = 0; pixelIndex != pixelData.Length / 2; pixelIndex++)
                {
                    int rowIndex = pixelIndex / width;
                    pixelData2[pixelIndex * 3 + (int)(rowIndex / (double)height * 3)] = (byte)(Math.Clamp((*(((ushort*)pPixelData) + pixelIndex) - windowCenter + windowWidth * 0.5), 0.0, windowWidth) / windowWidth * 255.0);
                }
            meta = new Dictionary<string, string>();
            foreach (var item in dataset)
            {
                try
                {
                    string value = dataset.GetValue<string>(item.Tag, 0);
                    int count = dataset.GetValueCount(item.Tag);
                    for (int valueIndex = 1; valueIndex != count; valueIndex++)
                        value += $"\\{dataset.GetValue<string>(item.Tag, valueIndex)}";
                    meta.Add(item.Tag.ToString().Replace("(", string.Empty).Replace(")", string.Empty).Replace(',', '|'), value);
                }
                catch
                {

                }
            }
            fixed (byte* p = &pixelData2[0])
            {
                var im = itk.simple.SimpleITK.ImportAsUInt8(new IntPtr(p), new itk.simple.VectorUInt32(new uint[] { width, height }), new itk.simple.VectorDouble(new double[] { 1, 1 }), new itk.simple.VectorDouble(new double[] { 0, 0 }), new itk.simple.VectorDouble(new double[] { 1, 0, 0, 1 }), 3);
                itk.simple.ImageFileWriter writer = new itk.simple.ImageFileWriter();
                writer.SetFileName("/mnt/c/Users/nikol/Desktop/rewritten.dcm");
                writer.KeepOriginalImageUIDOff();
                foreach (var pair in meta)
                    im.SetMetaData(pair.Key, pair.Value);
                writer.SetImageIO("GDCMImageIO");
                writer.UseCompressionOff();
                writer.Execute(im);
                writer.Dispose();
                im.Dispose();
            }
            // dicomFile = DicomFile.Open("/mnt/c/Users/nikol/Desktop/CR.1.871.3.1522049724.50292.18240.95439262.3570939512.1.1.dcm");
            // dicomFile.Dataset.AddOrUpdate<string>(DicomTag.StudyInstanceUID, dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            // dicomFile.Save("/mnt/c/Users/nikol/Desktop/CR.1.871.3.1522049724.50292.18240.95439262.3570939512.1.2.dcm");
            DicomDataset dataset2 = new DicomDataset();
            dataset.CopyTo(dataset2);
            dataset = dataset2;
            DicomPixelData dicomPixelData = DicomPixelData.Create(dataset, true);
            dicomPixelData.AddFrame(new MemoryByteBuffer(pixelData2));
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
            dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
            dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
            dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
            dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)3);
            dataset.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);
            dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);
            dataset.AddOrUpdate(DicomTag.WindowCenter, 127.5);
            dataset.AddOrUpdate(DicomTag.WindowWidth, 255.0);

            dicomFile = new DicomFile(dataset);
            dicomFile.Save("/mnt/c/Users/nikol/Desktop/efferent-no-compression.dcm");

            var j2000lossy = dataset.Compress(DicomTransferSyntax.JPEGLSLossless, new DicomJpegLsLosslessCodec(), new Dicom.Imaging.Codec.DicomJpegLsParams());
            j2000lossy.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            j2000lossy.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            j2000lossy.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dicomFile = new DicomFile(j2000lossy);
            dicomFile.Save("/mnt/c/Users/nikol/Desktop/efferent-ls-lossless.dcm");

            var j2000lossless = dataset.Compress(DicomTransferSyntax.JPEGLSNearLossless, new DicomJpegLsNearLosslessCodec(), new Dicom.Imaging.Codec.DicomJpegLsParams());
            j2000lossless.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            j2000lossless.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            j2000lossless.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dicomFile = new DicomFile(j2000lossless);
            dicomFile.Save("/mnt/c/Users/nikol/Desktop/efferent-ls-near-lossless.dcm");

            // RemovePixelDataProgram.Start(args);
            // ItkSimpleTestProgram.Start(args);
            // ImageTestProgram.Start(args);
            // CheckImageAttributesProgram.Start(args);
            // TestDicom48bpp.Start(args);
            // Testing3D.Start(args);
            // TestingWindowPresets.Start(args);
            // TestingMultiImageDicom.Start(args);
            // ComparingDicomFiles.Start(args);
            // PresentationState.Start(args);
            // PresentationStateAnalyzer.Start(args);
        }
    }
}