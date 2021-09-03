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
    static internal class ComparingDicomFiles
    {
        static internal unsafe void Start(string[] args)
        {
            const string firstDicomFilePath = "/mnt/c/Users/nikol/Desktop/attachment_2_1.871.3.1522049724.50292.18240.95439262.3570939512.1.1.1015.2_CR.dcm";
            const string secondDicomFilePath = "/mnt/c/Users/nikol/Desktop/1000000B";
            // var checkingDicomDataset = DicomFile.Open(severalFramesFilePath).Dataset.GetSequence(DicomTag.SharedFunctionalGroupsSequence).Items[0];
            var checkingDicomDataset = DicomFile.Open(firstDicomFilePath).Dataset;
            var contains = new HashSet<DicomTag>();
            var differentVR = new HashSet<DicomTag>();
            var different = new HashSet<DicomTag>();
            var doesNotContain = new HashSet<DicomTag>();
            var containsStringBuilder = new StringBuilder();
            var differentVRStringBuilder = new StringBuilder();
            var differentStringBuilder = new StringBuilder();
            var doesNotContainStringBuilder = new StringBuilder();
            var comparingDicomDataset = DicomFile.Open(secondDicomFilePath).Dataset;
            int count = 0;
            foreach (var dicomItem in checkingDicomDataset)
            {
                count++;
                if (dicomItem.Tag == DicomTag.TransferSyntaxUID)
                {}
                if (contains.Contains(dicomItem.Tag))
                    continue;
                if (!comparingDicomDataset.Contains(dicomItem.Tag))
                {
                    containsStringBuilder.AppendLine(dicomItem.ToString());
                    contains.Add(dicomItem.Tag);
                    goto Finish;
                }
                if (dicomItem.ValueRepresentation != comparingDicomDataset.GetDicomItem<DicomItem>(dicomItem.Tag).ValueRepresentation)
                {
                    differentVRStringBuilder.AppendLine(dicomItem.ToString());
                    differentVR.Add(dicomItem.Tag);
                    goto Finish;
                }
                if (dicomItem.ValueRepresentation == DicomVR.SQ)
                    goto Finish;
                var checkingBytes = checkingDicomDataset.GetDicomItem<DicomElement>(dicomItem.Tag);
                var comparingBytes = comparingDicomDataset.GetDicomItem<DicomElement>(dicomItem.Tag);
                if (checkingBytes.Length != comparingBytes.Length)
                {
                    differentStringBuilder.AppendLine(dicomItem.ToString());
                    different.Add(dicomItem.Tag);
                    goto Finish;
                }
                byte[] checkingData = checkingBytes.Buffer.Data;
                byte[] comparingData = comparingBytes.Buffer.Data;
                for (int byteIndex = 0; byteIndex != comparingBytes.Length; byteIndex++)
                {
                    if (checkingData[byteIndex] != comparingData[byteIndex])
                    {
                        differentStringBuilder.AppendLine(dicomItem.ToString());
                        different.Add(dicomItem.Tag);
                        goto Finish;
                    }
                }
            Finish:
                {

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
            File.WriteAllText("/mnt/c/Users/nikol/Desktop/contains.txt", containsStringBuilder.ToString());
            File.WriteAllText("/mnt/c/Users/nikol/Desktop/different.txt", differentStringBuilder.ToString());
            File.WriteAllText("/mnt/c/Users/nikol/Desktop/different-vr.txt", differentVRStringBuilder.ToString());
            File.WriteAllText("/mnt/c/Users/nikol/Desktop/does-not-contain.txt", doesNotContainStringBuilder.ToString());
        }
    }
}