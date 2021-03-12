using System.IO;
using Dicom;
using Dicom.Imaging;

namespace DicomTest
{
	static internal class RemovePixelDataProgram
	{
		static private void SaveDicomFile(DicomFile file, string directory, string fileName)
		{
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			var destFilePath = Path.Combine(directory, fileName);
			if (File.Exists(destFilePath))
				File.Delete(destFilePath);
			file.Save(destFilePath);
		}

		static internal unsafe void Start(string[] args)
		{
			const string rootDirectory = "/media/nikolaev_ov/CEFE3C54FE3C36D5/DICOM";
			const string targetDirectionDirectory = "favorite";

			foreach (var direction in new[] { "Mammography", "Fluorography", "ComputerTomography/Covid", "ComputerTomography/Cancer" })
				foreach (var directory in Directory.GetDirectories(Path.Combine(rootDirectory, direction, targetDirectionDirectory)))
				{
					var sourceDirectory = Path.Combine(directory, "sources");
					var noPixelDataDirectory = Path.Combine(directory, "no-pixel-data");
					var emptyPixelDataDirectory = Path.Combine(directory, "empty-pixel-data");
					var filePaths = Directory.GetFiles(sourceDirectory);
					foreach (var sourceFilePath in filePaths)
					{
						string fileName = Path.GetFileName(sourceFilePath);

						DicomFile file;
						try { file = DicomFile.Open(sourceFilePath); }
						catch { continue; }
						if (file == null)
							continue;

						file.Dataset.Remove(DicomTag.PixelData);
						SaveDicomFile(file, noPixelDataDirectory, fileName);

						var pixelData = DicomPixelData.Create(file.Dataset, true);
						SaveDicomFile(file, emptyPixelDataDirectory, fileName);
					}
				}
		}
	}
}