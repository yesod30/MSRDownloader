using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MSRDownloader.ApiResponses;
using MSRDownloader.Models;

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

    private static readonly HttpClient Client;

    static ApiHelper()
    {
        Client = new HttpClient();
    }
    
    public static async Task<List<AlbumResponse>> GetAlbumList()
    {
        var json = await Client.GetStringAsync(AlbumListUrl);
        var albumsInfo = JsonSerializer.Deserialize<AlbumsInfoResponse>(json, SerializerOptions);
        var albumList = albumsInfo?.Data ?? new();
        return albumList;
    }

    public static async Task<AlbumDataResponse?> GetAlbumDetail(string albumCid)
    {
        var json = await Client.GetStringAsync(string.Format(AlbumInfoUrl, albumCid));
        var albumDetail = JsonSerializer.Deserialize<AlbumDetailResponse>(json, SerializerOptions);
        return albumDetail?.Data ?? null;
    }

    public static async Task<SongInfoResponse?> GetSongDetail(string songCid)
    {
        var json = await Client.GetStringAsync(string.Format(SongInfoUrl, songCid));
        var songDetail = JsonSerializer.Deserialize<SongDetailResponse>(json, SerializerOptions);
        return songDetail?.Data ?? null;
    }

    public static async Task<DownloadTaskResult> DownloadSong(Song song, string baseDir)
    {
        var response = await Client.GetAsync(song.SourceUrl);
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
            var tempFileName = $"{FileHelper.TransformToValidFileName(song.Name)}{extension}";
            var sanitizedAlbumName = FileHelper.TransformToValidFileName(song.AlbumName);
            var tempFolderPath = Path.Join(baseDir, Constants.TempFolderName, sanitizedAlbumName);
            Directory.CreateDirectory(tempFolderPath);
            await using var fileStream = File.Create(Path.Join(tempFolderPath, tempFileName));
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            stream.Close();
            return new(song.Cid, tempFileName);
        }
        else
        {
            Console.WriteLine("Error downloading song: " + response.ReasonPhrase);
            throw new Exception("Error downloading song: " + response.ReasonPhrase);
        }
    }
}