using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;

namespace IRMAKit.Utils
{
	public class QRCoder : IQRCoder
	{
		private long quality = 80L;
		public long Quality
		{
			get { return this.quality; }

			set {
				if (value > 0 && value != this.quality) {
					this.quality = value;
					this.__eps = null;
				}
			}
		}

		private EncoderParameters __eps = null;
		private EncoderParameters eps {
			get {
				if (this.__eps == null) {
					this.__eps = new EncoderParameters(1);
					this.__eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, this.quality);
				}
				return this.__eps;
			}
		}

		private ImageCodecInfo __jpegEncoder = null;
		private ImageCodecInfo jpegEncoder {
			get {
				if (this.__jpegEncoder == null) {
					ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
					foreach (ImageCodecInfo codec in codecs) {
						if (codec.FormatID == ImageFormat.Jpeg.Guid) {
							this.__jpegEncoder = codec;
							break;
						}
					}
				}
				return this.__jpegEncoder;
			}
		}

		private ImageCodecInfo __pngEncoder = null;
		private ImageCodecInfo pngEncoder {
			get {
				if (this.__pngEncoder == null) {
					ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
					foreach (ImageCodecInfo codec in codecs) {
						if (codec.FormatID == ImageFormat.Png.Guid) {
							this.__pngEncoder = codec;
							break;
						}
					}
				}
				return this.__pngEncoder;
			}
		}

		public QRCoder(long quality=0L)
		{
			this.Quality = quality;
		}

		public byte[] GenQR(string text, int width, int height, QRType type)
		{
			if (string.IsNullOrEmpty(text) || width <= 0 || height <= 0)
				return null;
			Bitmap bmap;
			byte[] bytes;
			try {
				BarcodeWriter barCodeWriter = new BarcodeWriter();
				barCodeWriter.Format = BarcodeFormat.QR_CODE;
				barCodeWriter.Options.Hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");
				barCodeWriter.Options.Hints.Add(EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.H);
				barCodeWriter.Options.Height = height;
				barCodeWriter.Options.Width = width;
				barCodeWriter.Options.Margin = 0;
				ZXing.Common.BitMatrix bm = barCodeWriter.Encode(text);
				bmap = barCodeWriter.Write(bm);
				using (MemoryStream ms = new MemoryStream()) {
					bmap.Save(ms, type == QRType.PNG ? pngEncoder : jpegEncoder, eps);
 					bytes = ms.GetBuffer();
				}
			} catch {
				bytes = null;
			} finally {
				bmap = null;
			}
			return bytes;
		}
	}
}
