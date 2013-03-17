using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ConvertPhotosynthToCubeTexture
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var synthRoot = "/Users/nick/Desktop/Photosynth panoramas from iPhone/panorama/2E649D64-7743-4E79-8086-F6F5B61F19A7";
			var synthOutputCubemapPath = "/Users/nick/Desktop/cubemap.png";
			var deepzoomroot = Path.Combine (synthRoot, "deepzoom");
			const int faceSize = 256;
			// TODO: combine images into single .png
			var bmp = new Bitmap (faceSize * 4, faceSize * 4);
			using (var graphics = Graphics.FromImage(bmp)) {
				ProcessFace (graphics, 
			            Path.Combine (deepzoomroot, "left_files"), 
			            new Rectangle (0, faceSize, faceSize, faceSize));
				ProcessFace (graphics, 
				             Path.Combine (deepzoomroot, "front_files"), 
				             new Rectangle (faceSize, faceSize, faceSize, faceSize));
				ProcessFace (graphics, 
				             Path.Combine (deepzoomroot, "right_files"), 
				             new Rectangle (2*faceSize, faceSize, faceSize, faceSize));
				ProcessFace (graphics, 
				             Path.Combine (deepzoomroot, "back_files"), 
				             new Rectangle (3*faceSize, faceSize, faceSize, faceSize));
				ProcessFace (graphics, 
				             Path.Combine (deepzoomroot, "top_files"), 
				             new Rectangle (faceSize, 0, faceSize, faceSize));
				ProcessFace (graphics, 
				             Path.Combine (deepzoomroot, "bottom_files"), 
				             new Rectangle (faceSize, 2*faceSize, faceSize, faceSize));
			}
			bmp.Save (synthOutputCubemapPath, ImageFormat.Png);
		}

		static void ProcessFace(Graphics graphics, string faceDirectory, Rectangle faceRectangle) {
			// TODO: don't use just the "level 8" image. Also use the higher detail ones.
			var imagePath = 
				Path.Combine (Path.Combine (faceDirectory, "8"), "0_0.jpg");
			if (File.Exists (imagePath)) {
				using (var faceImage = new Bitmap(imagePath)) {
					graphics.DrawImage (faceImage, faceRectangle);
				}
			}
		}
	}
}
