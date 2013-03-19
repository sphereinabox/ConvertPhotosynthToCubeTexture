using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace ConvertPhotosynthToCubeTexture
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var panoramasFolder =
                @"C:\Users\nwint_000\Desktop\Photosynth panoramas from iPhone\panorama";

            // Process all panoramas:
            foreach (string panoramaGuidDirectory in Directory.GetDirectories(panoramasFolder))
            {
                var directoryNameOnly = Path.GetFileName(panoramaGuidDirectory);
                // Folder names must be GUIDs
                if (
                    !Regex.IsMatch(directoryNameOnly,
                                   "[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}"))
                {
                    continue;
                }

                Console.WriteLine(panoramaGuidDirectory);
                var deepzoomPath = Path.Combine(panoramasFolder, panoramaGuidDirectory, "deepzoom");
                var cubeManifestPath = Path.Combine(deepzoomPath, "CubeManifest.txt");
                var cubeManifest = ParseCubeManifest(cubeManifestPath);
                Console.WriteLine(cubeManifest.LargestFaceSize);
                CompositeCubemapIntoSingleImage(cubeManifest);
            }
        }

        private static void CompositeCubemapIntoSingleImage(CubeManifest cubeManifest)
        {
            const int faceSize = 1024;

            string deepzoomroot = Path.GetDirectoryName(cubeManifest.CubeManifestPath);
            var guidDirectoryPath = Path.GetDirectoryName(deepzoomroot);
            var guid = Path.GetFileName(guidDirectoryPath);
            // Note: does not typically exist. I created it for testing.
            var outputFolder = Path.Combine(
                Path.GetDirectoryName(Path.GetDirectoryName(guidDirectoryPath)),
                "composite");
            var synthOutputCubemapPath = Path.Combine(outputFolder, guid + ".png");
            // combine images into single .png
            using (var bmp = new Bitmap(faceSize * 4, faceSize * 4))
            {
                using (var graphics = Graphics.FromImage(bmp))
                {
                    ProcessFace(graphics,
                                Path.Combine(deepzoomroot, "left_files"),
                                new Rectangle(0, faceSize, faceSize, faceSize),
                                cubeManifest.LargestFaceSize, cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "front_files"),
                            new Rectangle(faceSize, faceSize, faceSize, faceSize),
                            cubeManifest.LargestFaceSize, cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "right_files"),
                            new Rectangle(2 * faceSize, faceSize, faceSize, faceSize),
                            cubeManifest.LargestFaceSize, cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "back_files"),
                            new Rectangle(3 * faceSize, faceSize, faceSize, faceSize),
                            cubeManifest.LargestFaceSize, cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "top_files"),
                            new Rectangle(faceSize, 0, faceSize, faceSize),
                            cubeManifest.LargestFaceSize, cubeManifest.LargestFaceSize);
                    ProcessFace(graphics,
                            Path.Combine(deepzoomroot, "bottom_files"),
                            new Rectangle(faceSize, 2 * faceSize, faceSize, faceSize),
                            cubeManifest.LargestFaceSize, cubeManifest.LargestFaceSize);
                }
                bmp.Save(synthOutputCubemapPath, ImageFormat.Png);
            }
        }

        private static CubeManifest ParseCubeManifest(string cubeManifestPath)
        {
            var manifest = new CubeManifest();
            manifest.CubeManifestPath = cubeManifestPath;
            var cubeManifestLines = File.ReadAllLines(cubeManifestPath);
            // Expected format:
            // comment                  Sample File
            // Unknown                  19
            // Largest Size             1040
            // Angular Bounds           -180,180,-58.8321,40.5767
            // Image coords per face    0,front,0,0,0,934,1040,934,1040,0
            //                          1,right,0,0,0,934,1040,934,1040,0
            //                          2,back,0,0,0,1040,1040,1040,1040,0
            //                          3,left,0,0,0,1040,1040,1040,1040,0
            //                          4,top,0,0,0,1040,1040,1040,1040,0
            //                          5,bottom,0,832,0,1040,199,1040,199,832
            // Note that not all faces of the cube are always included.

            if (cubeManifestLines.Length < 4)
            {
                throw new ArgumentException(
                    string.Format(
                        "Expected CubeManifest file of at least 4 lines (one cube face), got {0} lines for file '{1}'",
                        cubeManifestLines.Length, cubeManifestPath));
            }
            var line0 = cubeManifestLines[0];
            var maxSizeLine = cubeManifestLines[1];
            if (!int.TryParse(maxSizeLine, out manifest.LargestFaceSize))
            {
                throw new ArgumentException(
                    string.Format(
                        "Unable to parse line as integer line in file '{0}'({1}), got '{2}'",
                        cubeManifestPath, 2, maxSizeLine));
            }
            var panoramaAngularBounds = cubeManifestPath[2];
            // We don't need the information stored for each face, don't bother parsing it.
            //for (int lineIndex = 3; lineIndex < cubeManifestLines.Length; ++lineIndex)
            //{
            //    var faceLine = cubeManifestLines[lineIndex];
            //    var faceLineParts = faceLine.Split(',');
            //    if (faceLineParts.Length != 10)
            //    {
            //        throw new ArgumentException(
            //            string.Format(
            //                "Invalid face line in file '{0}'({1}), expected 10 parts when split by comma, got {2}. Line Looks like '{3}'",
            //                cubeManifestPath, lineIndex + 1, faceLineParts.Length, faceLine));
            //    }
            //}

            return manifest;
        }

        class CubeManifest
        {
            public string CubeManifestPath;
            public int LargestFaceSize;
        }

        private static void ProcessFace(Graphics graphics, string faceDirectory, Rectangle faceRectangle, int levelWidth,
                                        int levelHeight)
        {
            if (!Directory.Exists(faceDirectory))
            {
                // No imges on this face of the cube.
                return;
            }

            // When images get larger they are split so the resulting image is 256px wide
            // but 1px on each side is a copy of the adjacent image so that texture filtering works.
            const int maxImageSize = 256-2;

            // Find the corresponding level for the image size
            // the level is the power of two. Level 8 is up to (including) 256x256 pixels of real image data (does not count the duplicate pixels for edges)
            // level 9 is up to 512x512 and so-on
            int faceLevel = -999;
            for (int tempFaceLevel = 1, tempFaceLevelSize = 2;
                tempFaceLevel < 20;
                tempFaceLevel += 1, tempFaceLevelSize *= 2)
            {
                if (tempFaceLevelSize / 2 < levelWidth && levelWidth <= tempFaceLevelSize)
                {
                    faceLevel = tempFaceLevel;
                    break;
                }
            }

            var numImagesPerRow = (int)Math.Ceiling((double)levelWidth / (maxImageSize-2));

            var faceLevelDirectory = Path.Combine(faceDirectory, faceLevel.ToString(CultureInfo.InvariantCulture));
            if (!Directory.Exists(faceLevelDirectory))
            {
                return;
            }

            // Copy images to bitmap:
            for (int row = 0; row < numImagesPerRow; row++)
            {
                for (int col = 0; col < numImagesPerRow; col++)
                {
                    var imagePath = Path.Combine(
                        faceLevelDirectory,
                        string.Format("{0}_{1}.jpg", col, row));
                    if (File.Exists(imagePath))
                    {
                        using (var faceImage = new Bitmap(imagePath))
                        {
                            var destRectangle =
                                new Rectangle(
                                    faceRectangle.Left + faceRectangle.Width * (maxImageSize * col) / levelWidth,
                                    faceRectangle.Top + faceRectangle.Height * (maxImageSize * row) / levelHeight,
                                    faceRectangle.Width * faceImage.Width / levelWidth,
                                    faceRectangle.Height * faceImage.Height / levelHeight);
                            graphics.DrawImage(
                                faceImage,
                                destRectangle,
                                // Crop off the outer 1px, it is a copy of the adjacent images so that scaled image has no visible seams.
                                1, 1, faceImage.Width - 2, faceImage.Height - 2,
                                GraphicsUnit.Pixel);
                        }
                    }
                }
            }
        }
    }
}
