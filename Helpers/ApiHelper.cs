using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using MSRDownloader.ApiResponses;
using MSRDownloader.Models;
using TagLib;
using TagLib.Id3v2;
using File = System.IO.File;

namespace MSRDownloader.Helpers;

public static class ApiHelper
{
    private const string AlbumListUrl = "https://monster-siren.hypergryph.com/api/albums";
    private const string AlbumInfoUrl = "https://monster-siren.hypergryph.com/api/album/{0}/detail";
    private const string SongInfoUrl = "https://monster-siren.hypergryph.com/api/song/{0}";
    
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };
    
    public static async Task<List<AlbumResponse>> GetAlbumList()
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var json = await client.GetStringAsync(AlbumListUrl);
        var albumsInfo = JsonSerializer.Deserialize<AlbumsInfoResponse>(json, SerializerOptions);
        var albumList = albumsInfo?.Data ?? new();
        return albumList;
    }

    public static async Task<AlbumDataResponse?> GetAlbumDetail(string albumCid)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var json = await client.GetStringAsync(string.Format(AlbumInfoUrl, albumCid));
        var albumDetail = JsonSerializer.Deserialize<AlbumDetailResponse>(json, SerializerOptions);
        return albumDetail?.Data ?? null;
    }

    public static async Task<SongInfoResponse?> GetSongDetail(string songCid)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var json = await client.GetStringAsync(string.Format(SongInfoUrl, songCid));
        var songDetail = JsonSerializer.Deserialize<SongDetailResponse>(json, SerializerOptions);
        return songDetail?.Data ?? null;
    }

    public static async Task DownloadSong(Song song, string baseDir, string finalExtension)
    {
        using HttpClient client = new();
        var response = await client.GetAsync(song.SourceUrl);
        if (response.IsSuccessStatusCode)
        {
            string extension;
            switch (response.Content.Headers.ContentType)
            {
                case { MediaType: "audio/mpeg" }:
                    // mp3 file
                    extension = ".mp3";
                    break;
                case { MediaType: "audio/wav" }:
                    // wav file
                    extension = ".wav";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(null, "Unsupported file type");
            }

            var stream = await response.Content.ReadAsStreamAsync();
            var tempFileName = $"{TransformToValidFileName(song.Name)}{extension}";
            var finalFileName = $"{TransformToValidFileName(song.Name)}{finalExtension}";
            var sanitizedAlbumName = TransformToValidFileName(song.AlbumName);
            var tempFolderPath = Path.Join(baseDir, "Temp", sanitizedAlbumName);
            var finalFolderPath = Path.Join(baseDir, "Songs", sanitizedAlbumName);
            Console.WriteLine(tempFolderPath);
            Directory.CreateDirectory(tempFolderPath);
            Directory.CreateDirectory(finalFolderPath);
            await using var fileStream = File.Create(Path.Join(tempFolderPath, tempFileName));
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            stream.Close();
        
            FFMpegArguments
                .FromFileInput(Path.Join(tempFolderPath, tempFileName))
                .OutputToFile(Path.Join(finalFolderPath, finalFileName))
                .ProcessSynchronously();
        
            var tfile = TagLib.File.Create(Path.Join(finalFolderPath, finalFileName));
            tfile.Tag.Title = song.Name;
            tfile.Tag.Album = song.AlbumName;
            tfile.Tag.Performers = song.ArtistName.ToArray();

            var imageBytes = await client.GetByteArrayAsync(song.CoverUrl);
        
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
        else
        {
            Console.WriteLine("Error downloading song: " + response.ReasonPhrase);
        }
        
    }

    private static string TransformToValidFileName(string input)
    {
        // Remove any invalid characters from the input string
        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        string sanitizedInput = Regex.Replace(input, "[" + Regex.Escape(invalidChars) + "]", "_");
        return sanitizedInput;
    }
}