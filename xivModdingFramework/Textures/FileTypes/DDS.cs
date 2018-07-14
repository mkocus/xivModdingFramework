﻿// xivModdingFramework
// Copyright © 2018 Rafael Gonzalez - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using xivModdingFramework.Textures.DataContainers;
using xivModdingFramework.Textures.Enums;

namespace xivModdingFramework.Textures.FileTypes
{
    /// <summary>
    /// This class deals with dds file types
    /// </summary>
    public class DDS
    {
        /// <summary>
        /// Creates a DDS file for the given Texture
        /// </summary>
        /// <param name="saveDirectory">The directory to save the dds file to</param>
        /// <param name="xivTex">The Texture information</param>
        public void MakeDDS(DirectoryInfo saveDirectory, XivTex xivTex)
        {
            var savePath = Path.Combine(saveDirectory.FullName,
                Path.GetFileNameWithoutExtension(xivTex.TextureTypeAndPath.Path) + ".dds");

            var DDS = new List<byte>();
            switch (xivTex.TextureTypeAndPath.Type)
            {
                case XivTexType.ColorSet:
                    DDS.AddRange(CreateColorDDSHeader());
                    DDS.AddRange(xivTex.TexData);
                    break;
                case XivTexType.Vfx:
                case XivTexType.Diffuse:
                case XivTexType.Specular:
                case XivTexType.Normal:
                case XivTexType.Multi:
                case XivTexType.Mask:
                case XivTexType.Skin:
                case XivTexType.Map:
                case XivTexType.Icon:
                default:
                    DDS.AddRange(CreateDDSHeader(xivTex));
                    DDS.AddRange(xivTex.TexData);
                    break;
            }

            File.WriteAllBytes(savePath, DDS.ToArray());
        }

        /// <summary>
        /// Creates the DDS header for given texture data.
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/bb943982(v=vs.85).aspx"/>
        /// </summary>
        /// <returns>Byte array containing DDS header</returns>
        private byte[] CreateDDSHeader(XivTex xivTex)
        {
            uint dwPitchOrLinearSize, pfFlags, dwFourCC;
            var header = new List<byte>();

            // DDS header magic number
            const uint dwMagic = 0x20534444;
            header.AddRange(BitConverter.GetBytes(dwMagic));

            // Size of structure. This member must be set to 124.
            const uint dwSize = 124;
            header.AddRange(BitConverter.GetBytes(dwSize));

            // Flags to indicate which members contain valid data.
            const uint dwFlags = 528391;
            header.AddRange(BitConverter.GetBytes(dwFlags));

            // Surface height (in pixels).
            var dwHeight = (uint)xivTex.Heigth;
            header.AddRange(BitConverter.GetBytes(dwHeight));

            // Surface width (in pixels).
            var dwWidth = (uint)xivTex.Width;
            header.AddRange(BitConverter.GetBytes(dwWidth));

            // The pitch or number of bytes per scan line in an uncompressed texture; the total number of bytes in the top level texture for a compressed texture.
            if (xivTex.TextureFormat == XivTexFormat.A16B16G16R16F)
            {
                dwPitchOrLinearSize = 512;
            }
            else if (xivTex.TextureFormat == XivTexFormat.A8R8G8B8)
            {
                dwPitchOrLinearSize = (dwHeight * dwWidth) * 4;
            }
            else if (xivTex.TextureFormat == XivTexFormat.DXT1)
            {
                dwPitchOrLinearSize = (dwHeight * dwWidth) / 2;
            }
            else if (xivTex.TextureFormat == XivTexFormat.A4R4G4B4 || xivTex.TextureFormat == XivTexFormat.A1R5G5B5)
            {
                dwPitchOrLinearSize = (dwHeight * dwWidth) * 2;
            }
            else
            {
                dwPitchOrLinearSize = dwHeight * dwWidth;
            }
            header.AddRange(BitConverter.GetBytes(dwPitchOrLinearSize));


            // Depth of a volume texture (in pixels), otherwise unused.
            const uint dwDepth = 0;
            header.AddRange(BitConverter.GetBytes(dwDepth));

            // Number of mipmap levels, otherwise unused.
            var dwMipMapCount = (uint)xivTex.MipMapCount;
            header.AddRange(BitConverter.GetBytes(dwMipMapCount));

            // Unused.
            var dwReserved1 = new byte[44];
            Array.Clear(dwReserved1, 0, 44);
            header.AddRange(dwReserved1);

            // DDS_PIXELFORMAT start

            // Structure size; set to 32 (bytes).
            const uint pfSize = 32;
            header.AddRange(BitConverter.GetBytes(pfSize));

            switch (xivTex.TextureFormat)
            {
                // Values which indicate what type of data is in the surface.
                case XivTexFormat.A8R8G8B8:
                case XivTexFormat.A4R4G4B4:
                case XivTexFormat.A1R5G5B5:
                    pfFlags = 65;
                    break;
                case XivTexFormat.A8:
                    pfFlags = 2;
                    break;
                default:
                    pfFlags = 4;
                    break;
            }
            header.AddRange(BitConverter.GetBytes(pfFlags));

            switch (xivTex.TextureFormat)
            {
                // Four-character codes for specifying compressed or custom formats.
                case XivTexFormat.DXT1:
                    dwFourCC = 0x31545844;
                    break;
                case XivTexFormat.DXT5:
                    dwFourCC = 0x35545844;
                    break;
                case XivTexFormat.DXT3:
                    dwFourCC = 0x33545844;
                    break;
                case XivTexFormat.A16B16G16R16F:
                    dwFourCC = 0x71;
                    break;
                case XivTexFormat.A8R8G8B8:
                case XivTexFormat.A8:
                case XivTexFormat.A4R4G4B4:
                case XivTexFormat.A1R5G5B5:
                    dwFourCC = 0;
                    break;
                default:
                    return null;
            }
            header.AddRange(BitConverter.GetBytes(dwFourCC));

            switch (xivTex.TextureFormat)
            {
                case XivTexFormat.A8R8G8B8:
                    {
                        // Number of bits in an RGB (possibly including alpha) format.
                        const uint dwRGBBitCount = 32;
                        header.AddRange(BitConverter.GetBytes(dwRGBBitCount));

                        // Red (or lumiannce or Y) mask for reading color data. 
                        const uint dwRBitMask = 16711680;
                        header.AddRange(BitConverter.GetBytes(dwRBitMask));

                        // Green (or U) mask for reading color data.
                        const uint dwGBitMask = 65280;
                        header.AddRange(BitConverter.GetBytes(dwGBitMask));

                        // Blue (or V) mask for reading color data.
                        const uint dwBBitMask = 255;
                        header.AddRange(BitConverter.GetBytes(dwBBitMask));

                        // Alpha mask for reading alpha data.
                        const uint dwABitMask = 4278190080;
                        header.AddRange(BitConverter.GetBytes(dwABitMask));

                        // DDS_PIXELFORMAT End

                        // Specifies the complexity of the surfaces stored.
                        const uint dwCaps = 4096;
                        header.AddRange(BitConverter.GetBytes(dwCaps));

                        // dwCaps2, dwCaps3, dwCaps4, dwReserved2.
                        // Unused.
                        var blank1 = new byte[16];
                        header.AddRange(blank1);

                        break;
                    }
                case XivTexFormat.A8:
                    {
                        // Number of bits in an RGB (possibly including alpha) format.
                        const uint dwRGBBitCount = 8;
                        header.AddRange(BitConverter.GetBytes(dwRGBBitCount));

                        // Red (or lumiannce or Y) mask for reading color data. 
                        const uint dwRBitMask = 0;
                        header.AddRange(BitConverter.GetBytes(dwRBitMask));

                        // Green (or U) mask for reading color data.
                        const uint dwGBitMask = 0;
                        header.AddRange(BitConverter.GetBytes(dwGBitMask));

                        // Blue (or V) mask for reading color data.
                        const uint dwBBitMask = 0;
                        header.AddRange(BitConverter.GetBytes(dwBBitMask));

                        // Alpha mask for reading alpha data.
                        const uint dwABitMask = 255;
                        header.AddRange(BitConverter.GetBytes(dwABitMask));

                        // DDS_PIXELFORMAT End

                        // Specifies the complexity of the surfaces stored.
                        const uint dwCaps = 4096;
                        header.AddRange(BitConverter.GetBytes(dwCaps));

                        // dwCaps2, dwCaps3, dwCaps4, dwReserved2.
                        // Unused.
                        var blank1 = new byte[16];
                        header.AddRange(blank1);
                        break;
                    }
                case XivTexFormat.A1R5G5B5:
                    {
                        // Number of bits in an RGB (possibly including alpha) format.
                        const uint dwRGBBitCount = 16;
                        header.AddRange(BitConverter.GetBytes(dwRGBBitCount));

                        // Red (or lumiannce or Y) mask for reading color data. 
                        const uint dwRBitMask = 31744;
                        header.AddRange(BitConverter.GetBytes(dwRBitMask));

                        // Green (or U) mask for reading color data.
                        const uint dwGBitMask = 992;
                        header.AddRange(BitConverter.GetBytes(dwGBitMask));

                        // Blue (or V) mask for reading color data.
                        const uint dwBBitMask = 31;
                        header.AddRange(BitConverter.GetBytes(dwBBitMask));

                        // Alpha mask for reading alpha data.
                        const uint dwABitMask = 32768;
                        header.AddRange(BitConverter.GetBytes(dwABitMask));

                        // DDS_PIXELFORMAT End

                        // Specifies the complexity of the surfaces stored.
                        const uint dwCaps = 4096;
                        header.AddRange(BitConverter.GetBytes(dwCaps));

                        // dwCaps2, dwCaps3, dwCaps4, dwReserved2.
                        // Unused.
                        var blank1 = new byte[16];
                        header.AddRange(blank1);
                        break;
                    }
                case XivTexFormat.A4R4G4B4:
                    {
                        // Number of bits in an RGB (possibly including alpha) format.
                        const uint dwRGBBitCount = 16;
                        header.AddRange(BitConverter.GetBytes(dwRGBBitCount));

                        // Red (or lumiannce or Y) mask for reading color data. 
                        const uint dwRBitMask = 3840;
                        header.AddRange(BitConverter.GetBytes(dwRBitMask));

                        // Green (or U) mask for reading color data.
                        const uint dwGBitMask = 240;
                        header.AddRange(BitConverter.GetBytes(dwGBitMask));

                        // Blue (or V) mask for reading color data.
                        const uint dwBBitMask = 15;
                        header.AddRange(BitConverter.GetBytes(dwBBitMask));

                        // Alpha mask for reading alpha data.
                        const uint dwABitMask = 61440;
                        header.AddRange(BitConverter.GetBytes(dwABitMask));

                        // DDS_PIXELFORMAT End

                        // Specifies the complexity of the surfaces stored.
                        const uint dwCaps = 4096;
                        header.AddRange(BitConverter.GetBytes(dwCaps));

                        // dwCaps2, dwCaps3, dwCaps4, dwReserved2.
                        // Unused.
                        var blank1 = new byte[16];
                        header.AddRange(blank1);
                        break;
                    }
                default:
                    {
                        // dwRGBBitCount, dwRBitMask, dwGBitMask, dwBBitMask, dwABitMask, dwCaps, dwCaps2, dwCaps3, dwCaps4, dwReserved2.
                        // Unused.
                        var blank1 = new byte[40];
                        header.AddRange(blank1);
                        break;
                    }
            }

            return header.ToArray();
        }

        /// <summary>
        /// Creates the DDS header for given texture data.
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/bb943982(v=vs.85).aspx"/>
        /// </summary>
        /// <returns>Byte array containing DDS header</returns>
        private byte[] CreateColorDDSHeader()
        {
            var header = new List<byte>();

            // DDS header magic number
            const uint dwMagic = 0x20534444;
            header.AddRange(BitConverter.GetBytes(dwMagic));

            // Size of structure. This member must be set to 124.
            const uint dwSize = 124;
            header.AddRange(BitConverter.GetBytes(dwSize));

            // Flags to indicate which members contain valid data.
            const uint dwFlags = 528399;
            header.AddRange(BitConverter.GetBytes(dwFlags));

            // Surface height (in pixels).
            const uint dwHeight = 16;
            header.AddRange(BitConverter.GetBytes(dwHeight));

            // Surface width (in pixels).
            const uint dwWidth = 4;
            header.AddRange(BitConverter.GetBytes(dwWidth));

            // The pitch or number of bytes per scan line in an uncompressed texture; the total number of bytes in the top level texture for a compressed texture.
            const uint dwPitchOrLinearSize = 512;
            header.AddRange(BitConverter.GetBytes(dwPitchOrLinearSize));

            // Depth of a volume texture (in pixels), otherwise unused.
            const uint dwDepth = 0;
            header.AddRange(BitConverter.GetBytes(dwDepth));

            // Number of mipmap levels, otherwise unused.
            const uint dwMipMapCount = 0;
            header.AddRange(BitConverter.GetBytes(dwMipMapCount));

            // Unused.
            var dwReserved1 = new byte[44];
            header.AddRange(dwReserved1);

            // DDS_PIXELFORMAT start

            // Structure size; set to 32 (bytes).
            const uint pfSize = 32;
            header.AddRange(BitConverter.GetBytes(pfSize));

            // Values which indicate what type of data is in the surface.
            const uint pfFlags = 4;
            header.AddRange(BitConverter.GetBytes(pfFlags));

            // Four-character codes for specifying compressed or custom formats.
            const uint dwFourCC = 0x71;
            header.AddRange(BitConverter.GetBytes(dwFourCC));

            // dwRGBBitCount, dwRBitMask, dwGBitMask, dwBBitMask, dwABitMask, dwCaps, dwCaps2, dwCaps3, dwCaps4, dwReserved2.
            // Unused.
            var blank1 = new byte[40];
            header.AddRange(blank1);

            return header.ToArray();
        }
    }
}