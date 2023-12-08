using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using MSRDownloader.Models;
using TagLib;
using TagLib.Id3v2;

namespace MSRDownloader.Helpers;

public static class ConversionHelper
{
    
    private static readonly HttpClient Client;

    static ConversionHelper()
    {
        Client = new HttpClient();
    }
    
    public static async Task ConvertAndApplyTags(Song song, string baseDir, string tempFileName, string finalExtension, string artistOverride, string albumOverride)
    {
        var sanitizedAlbumName = FileHelper.TransformToValidFileName(song.AlbumName);
        var tempFolderPath = Path.Join(baseDir, Constants.TempFolderName, sanitizedAlbumName);
        
        var finalFileName = $"{FileHelper.TransformToValidFileName(song.Name)}{finalExtension}";
        var finalFolderPath = Path.Join(baseDir, Constants.OutputFolderName, sanitizedAlbumName);
        Directory.CreateDirectory(finalFolderPath);
        
        FFMpegArguments
            .FromFileInput(Path.Join(tempFolderPath, tempFileName))
            .OutputToFile(Path.Join(finalFolderPath, finalFileName), true, options => options.WithAudioBitrate(AudioQuality.Ultra))
            .ProcessSynchronously();
        
        var tfile = TagLib.File.Create(Path.Join(finalFolderPath, finalFileName));
        tfile.Tag.Title = song.Name;
        tfile.Tag.Album = string.IsNullOrWhiteSpace(albumOverride) ? song.AlbumName : albumOverride;
        tfile.Tag.Performers = string.IsNullOrWhiteSpace(artistOverride) ? song.ArtistName.ToArray() : new[] { artistOverride };

        var imageBytes = await Client.GetByteArrayAsync(song.CoverUrl);
        
        AttachmentFrame cover = new AttachmentFrame
        {
            Type = PictureType.FrontCover,
            Description = "Cover",
            MimeType = MediaTypeNames.Image.Jpeg,
            Data = imageBytes,
            TextEncoding = StringType.Latin1,
        };
        
        tfile.Tag.Pictures = new IPicture[] { cover };
        tfile.Save();
    }
}