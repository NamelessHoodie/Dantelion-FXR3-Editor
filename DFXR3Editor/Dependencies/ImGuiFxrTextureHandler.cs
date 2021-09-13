using DDSReader;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SoulsFormats;
using System;
using System.Collections.Generic;
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
        private Dictionary<int, IntPtr> loadedTexturesDictionary = new Dictionary<int, IntPtr>();
        public List<int> FfxTexturesIdList { get;} = new List<int>();
        private IEnumerable<BinderFile> ffxTexturesIEnumerable;
        public ImGuiFxrTextureHandler(BND4 ffxResourcesBin)
        {
            this.ffxTexturesIEnumerable = ffxResourcesBin.Files.Where(item => item.Name.Contains("sfx\\tex"));
            foreach (var binderFileTpf in ffxTexturesIEnumerable)
            {
                var tpfBytes = binderFileTpf.Bytes;
                var tpfTexturesList = TPF.Read(tpfBytes).Textures;
                if (tpfTexturesList.Any())
                {
                    var tpf = tpfTexturesList.First();
                    if (int.TryParse(tpf.Name.TrimStart('s'), out int textureID))
                    {
                        FfxTexturesIdList.Add(textureID);
                    }
                }
            }
            FfxTexturesIdList.Sort();
            LoadAllFfxTexturesInMemory(ffxTexturesIEnumerable);
        }
        public (bool TextureExists, IntPtr TextureHandle) GetFfxTextureIntPtr(int textureID)
        {
            if (!loadedTexturesDictionary.ContainsKey(textureID))
            {
                var a = ffxTexturesIEnumerable.Where(item => item.Name.Contains($"s{textureID.ToString("00000")}.tpf"));
                if (a.Any())
                {
                    var tpfBytes = a.First().Bytes;
                    var tpfTexturesList = TPF.Read(tpfBytes).Textures;
                    if (tpfTexturesList.Any())
                    {
                        var ddsBytes = tpfTexturesList.First().Bytes;
                        DDSImage ddsImage = new DDSImage(ddsBytes);
                        Image<Rgba32> image = Image.LoadPixelData<Rgba32>(ddsImage.Data, ddsImage.Width, ddsImage.Height);
                        var img = new ImageSharpTexture(image);
                        var veldridTexture = img.CreateDeviceTexture(MainUserInterface._gd, MainUserInterface._gd.ResourceFactory);
                        var textureHandle = MainUserInterface._controller.GetOrCreateImGuiBinding(MainUserInterface._gd.ResourceFactory, veldridTexture);
                        veldridTexture.Dispose();
                        loadedTexturesDictionary.Add(textureID, textureHandle);
                        return (true, textureHandle);
                    }
                }
                return (false, IntPtr.Zero);
            }
            else
            {
                return (true, loadedTexturesDictionary[textureID]);
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
                    if (int.TryParse(tpf.Name.TrimStart('s'), out int textureID) && !loadedTexturesDictionary.ContainsKey(textureID))
                    {
                        var ddsBytes = tpf.Bytes;
                        DDSImage ddsImage = new DDSImage(ddsBytes);
                        Image<Rgba32> image = Image.LoadPixelData<Rgba32>(ddsImage.Data, ddsImage.Width, ddsImage.Height);
                        var img = new ImageSharpTexture(image);
                        var veldridTexture = img.CreateDeviceTexture(MainUserInterface._gd, MainUserInterface._gd.ResourceFactory);
                        var textureHandle = MainUserInterface._controller.GetOrCreateImGuiBinding(MainUserInterface._gd.ResourceFactory, veldridTexture);
                        veldridTexture.Dispose();
                        loadedTexturesDictionary.Add(textureID, textureHandle);
                    }
                }
            }
        }

    }
}
