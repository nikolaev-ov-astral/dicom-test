namespace DicomTest
{
	/// <summary>
	/// Представляет собой модель данных декодированного изображения из dicom-файла.
	/// </summary>
	public class DecodedDicomImageModel
	{
		/// <summary>
		/// Получает или задает декодированные данные изображения.
		/// </summary>
		public byte[] PixelData { get; set; }
		
		/// <summary>
		/// Получает или задает значение, указывающее на количество байт на канал в декодированном изображении.
		/// </summary>
		public uint ChannelSize { get; set; }

		/// <summary>
		/// Получает или задает значение, указывающее на то представлены ли значение каналов в декодированном изображении знаковым числом или нет.
		/// </summary>
		public bool Signed { get; set; }

		/// <summary>
		/// Получает или задает значение, указывающее на количество каналов в пикселе в декодированном изображении.
		/// </summary>
		public uint ChannelCount { get; set; }

		/// <summary>
		/// Получает или задает количество пикселей в декодированном изображении по горизонтали.
		/// </summary>
		public ushort Width { get; set; }

		/// <summary>
		/// Получает или задает количество пикселей в декодированном изображении по вертикали.
		/// </summary>
		public ushort Height { get; set; }

		/// <summary>
		/// Получает или задает количество пикселей в декодированном изображении.
		/// </summary>
		public uint PixelCount { get; set; }
	}
}