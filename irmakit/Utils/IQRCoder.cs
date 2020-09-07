namespace IRMAKit.Utils
{
	public enum QRType
	{
		PNG = 0,
		JPEG
	}

	public interface IQRCoder
	{
		/// <summary>
		/// Quality
		/// 图像生成质量(压缩比)，参考System.Drawing.Imaging.Encoder.Quality
		/// 取值：1-100，值越小压缩比越大，理论上生成耗时越长、size越小、与原图质量偏差越大
		/// </summary>
		long Quality { get; set; }

		/// <summary>
		/// Generate QR code
		/// </summary>
		byte[] GenQR(string text, int width, int height, QRType type);
	}
}
