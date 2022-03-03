using DDSReader;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;

namespace DFXR3Editor
{
    public class ImGuiFxrTextureHandler
    {
        private Dictionary<int, IntPtr> _loadedTexturesDictionary = new Dictionary<int, IntPtr>();
        public List<int> FfxTexturesIdList { get; } = new List<int>();
        private IEnumerable<BinderFile> _ffxTexturesIEnumerable;
        public ImGuiFxrTextureHandler(BND4 ffxResourcesBin)
        {
            this._ffxTexturesIEnumerable = ffxResourcesBin.Files.Where(item => item.Name.Contains("sfx\\tex"));
            foreach (var binderFileTpf in _ffxTexturesIEnumerable)
            {
                var tpfBytes = binderFileTpf.Bytes;
                var tpfTexturesList = TPF.Read(tpfBytes).Textures;
                if (tpfTexturesList.Any())
                {
                    var tpf = tpfTexturesList.First();
                    if (int.TryParse(tpf.Name.TrimStart('s'), out int textureId))
                    {
                        FfxTexturesIdList.Add(textureId);
                    }
                }
            }
            FfxTexturesIdList.Sort();
            LoadAllFfxTexturesInMemory(_ffxTexturesIEnumerable);
        }
        public (bool TextureExists, IntPtr TextureHandle) GetFfxTextureIntPtr(int textureId)
        {
            if (!_loadedTexturesDictionary.ContainsKey(textureId))
            {
                var a = _ffxTexturesIEnumerable.Where(item => item.Name.Contains($"s{textureId.ToString("00000")}.tpf"));
                if (a.Any())
                {
                    var tpfBytes = a.First().Bytes;
                    var tpfTexturesList = TPF.Read(tpfBytes).Textures;
                    if (tpfTexturesList.Any())
                    {
                        var ddsBytes = tpfTexturesList.First().Bytes;
                        DdsImage ddsImage = new DdsImage(ddsBytes);
                        Image<Rgba32> image = Image.LoadPixelData<Rgba32>(ddsImage.Data, ddsImage.Width, ddsImage.Height);
                        var img = new ImageSharpTexture(image);
                        var veldridTexture = img.CreateDeviceTexture(MainUserInterface.Gd, MainUserInterface.Gd.ResourceFactory);
                        var textureHandle = MainUserInterface.Controller.GetOrCreateImGuiBinding(MainUserInterface.Gd.ResourceFactory, veldridTexture);
                        veldridTexture.Dispose();
                        _loadedTexturesDictionary.Add(textureId, textureHandle);
                        return (true, textureHandle);
                    }
                }
                return (false, IntPtr.Zero);
            }
            else
            {
                return (true, _loadedTexturesDictionary[textureId]);
            }
        }
        public void LoadAllFfxTexturesInMemory(IEnumerable<BinderFile> ffxTexturesIEnumerable)
        {
            foreach (var a in ffxTexturesIEnumerable)
            {
                var tpfBytes = a.Bytes;
                var tpfTexturesList = TPF.Read(tpfBytes).Textures;
                if (tpfTexturesList.Any())
                {
                    var tpf = tpfTexturesList.First();
                    if (int.TryParse(tpf.Name.TrimStart('s'), out int textureId) && !_loadedTexturesDictionary.ContainsKey(textureId))
                    {

                        var ddsBytes = tpf.Bytes;
                        DdsImage ddsImage = new DdsImage(ddsBytes);
                        Image<Rgba32> image = Image.LoadPixelData<Rgba32>(ddsImage.Data, ddsImage.Width, ddsImage.Height);
                        for (int y = 0; y < image.Height; y++)
                        {
                            for (int x = 0; x < image.Width; x++)
                            {
                                var rgba32 = image[x, y];
                                var r = rgba32.R;
                                var g = rgba32.G;
                                var b = rgba32.B;
                                rgba32.R = b;
                                rgba32.G = g;
                                rgba32.B = r;
                                image[x, y] = rgba32; //Set Inverted Pixels
                            }
                        }
                        var img = new ImageSharpTexture(image);
                        var veldridTexture = img.CreateDeviceTexture(MainUserInterface.Gd, MainUserInterface.Gd.ResourceFactory);
                        var textureHandle = MainUserInterface.Controller.GetOrCreateImGuiBinding(MainUserInterface.Gd.ResourceFactory, veldridTexture);
                        veldridTexture.Dispose();
                        _loadedTexturesDictionary.Add(textureId, textureHandle);
                    }
                }
            }
        }
    }
}
