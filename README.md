# Convert Photosynth to Cube Map

Utility to convert a Microsoft Photosynth for iOS panorama to a single unfolded cube image.

## How to use

**1)** Copy the entire "panorama" folder from your iOS device's Photosynth\Documents folder to your computer. This folder should be filled with folders named like GUIDs (2E66C9F7-2CD3-45D1-82C0-049656F0DEA3 and so-on).
You will need an application to copy the files from your device or pull them from your iPhone backup.

**2)** Run this application following the examples below to export a PNG file for the desired panorama or all panoramas.

## Examples

**Output a single panorama as a 2048x2048 PNG file named output.png**

	ConvertPhotosynthToCubeTexture.exe -file "panorama\003F255D-7DB2-4B73-A079-9D89533D901C\deepzoom\CubeManifest.txt" -out output.png -size 2048

**Output all panoramas as 1024x1024 PNG files to directory named "outfiles"**

	ConvertPhotosynthToCubeTexture.exe -dir "panorama" -out "outfiles" -size 1024

## Author
**Nick Winters**

- [Twitter](https://twitter.com/sphereinabox)
- [Github](https://github.com/sphereinabox)
- [Personal page](http://sphereinabox.wordpress.com/)  

## License

Copyright (c) 2013 Nick Winters

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to 
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all 
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
SOFTWARE.
