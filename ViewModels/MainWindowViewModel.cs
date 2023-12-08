using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DynamicData;
using MSRDownloader.Helpers;
using MSRDownloader.Models;
using ReactiveUI;
using System.Reactive.Concurrency;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Metadata;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace MSRDownloader.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        RxApp.MainThreadScheduler.Schedule(LoadAlbumData);
    }
    
    public ObservableCollection<Album> AlbumsList { get; } = new();

    public ObservableCollection<string> DownloadedSongs { get; } = new();
    
    private bool isWorking;
    public bool IsWorking
    {
        get => isWorking;
        set => this.RaiseAndSetIfChanged(ref isWorking, value);
    }
    
    private bool isLoadingData;
    public bool IsLoadingData
    {
        get => isLoadingData;
        set => this.RaiseAndSetIfChanged(ref isLoadingData, value);
    }
    
    private string progressText = string.Empty;
    public string ProgressText
    {
        get => progressText;
        set => this.RaiseAndSetIfChanged(ref progressText, value);
    }
    
    private int progressBarMaximum = 1;
    public int ProgressBarMaximum
    {
        get => progressBarMaximum;
        set => this.RaiseAndSetIfChanged(ref progressBarMaximum, value);
    }
    
    private int progressBarValue;
    public int ProgressBarValue
    {
        get => progressBarValue;
        set => this.RaiseAndSetIfChanged(ref progressBarValue, value);
    }
    
    private string artistOverride = string.Empty;
    public string ArtistOverride
    {
        get => artistOverride;
        set => this.RaiseAndSetIfChanged(ref artistOverride, value);
    }
    
    private string albumOverride = string.Empty;
    public string AlbumOverride
    {
        get => albumOverride;
        set => this.RaiseAndSetIfChanged(ref albumOverride, value);
    }
    
    private FileType outputFileType = FileType.Mp3;
    public FileType OutputFileType
    {
        get => outputFileType;
        set => this.RaiseAndSetIfChanged(ref outputFileType, value);
    }
    
    private async void LoadAlbumData()
    {
        IsLoadingData = true;
        var loadedAlbums = await FileHelper.ReadAlbumData();
        AlbumsList.AddRange(loadedAlbums);
        if (!AlbumsList.Any())
        {
            await UpdateData();
        }
        await LoadDownloadedSongs();
        foreach (var album in AlbumsList)
        {
            foreach (var albumSong in album.Songs)
            {
                albumSong.IsDownloaded = DownloadedSongs.Contains(albumSong.Cid);
            }
        }
        IsLoadingData = false;
    }
    
    private async Task LoadDownloadedSongs()
    {
        var downloadedSongsCid = await FileHelper.ReadDownloadedSongs();
        DownloadedSongs.AddRange(downloadedSongsCid);
    }

    public void ClearData()
    {
        isLoadingData = true;
        AlbumsList.Clear();
        isLoadingData = false;
    }
    
    public async Task UpdateData()
    {
        IsLoadingData = true;
        ProgressBarValue = 0;
        ProgressText = "Fetching album list";
        var albums = await ApiHelper.GetAlbumList();
        albums = albums.Where(x => AlbumsList.All(y => y.Cid != x.Cid)).ToList();

        if (!albums.Any())
        {
            ProgressText = "No new album found";
            return;
        }
        
        ProgressBarMaximum = albums.Count;
        
        await Parallel.ForEachAsync(albums,   async (albumResponse, cancellationToken) =>   
        {
            var albumDetail = await ApiHelper.GetAlbumDetail(albumResponse.Cid);
            if (albumDetail is not null)
            {
                var album = new Album(albumDetail.Cid, albumDetail.Name, albumDetail.CoverUrl);
                foreach (var albumSong in albumDetail.Songs)
                {
                    var songDetail = await ApiHelper.GetSongDetail(albumSong.Cid);
                    if (songDetail is not null)
                    {
                        var song = new Song(songDetail.Cid, songDetail.Name, album.Name, album.CoverUrl, songDetail.SourceUrl);
                        song.ArtistName.AddRange(songDetail.Artists);
                        album.Songs.Add(song);
                    }
                }
                AlbumsList.Add(album);
                ProgressBarValue++;
                ProgressText = $"Fetched data for {ProgressBarValue}/{ProgressBarMaximum} albums";
            }
        });
        ProgressText = "Saving data to file";
        await FileHelper.WriteAlbumData(AlbumsList.ToList());
        ProgressText = "Done";
        IsLoadingData = false;
    }

    public void HandleSelectSong(object song)
    {
        if (song is Song selectedSong)
        {
            var songAlbum = AlbumsList.First(x => x.Songs.Contains(selectedSong));
            if (songAlbum.Songs.All(x => x.IsSelected))
            {
                songAlbum.IsSelected = true;
            }
            else if (songAlbum.Songs.Any(x => x.IsSelected))
            {
                songAlbum.IsSelected = null;
            }
            else
            {
                songAlbum.IsSelected = false;
            }
        }
    }
    
    public void HandleSelectAlbum(object album)
    {
        if (album is Album selectedAlbum)
        {
            foreach (var song in selectedAlbum.Songs)
            {
                song.IsSelected = selectedAlbum.IsSelected ?? false;
            }
        }
    }

    public async void DownloadSongs()
    {
        ProgressBarValue = 0;
        var selectedSongs = AlbumsList.SelectMany(x => x.Songs.Where(y => y.IsSelected)).ToList();
        if (selectedSongs.Any())
        {
            ProgressBarMaximum = selectedSongs.Count;
            ProgressText = $"Downloading songs: {ProgressBarValue}/{ProgressBarMaximum}";
            var currentExe = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
            string currentFolder = Path.GetDirectoryName(currentExe)!;
            var fileExtension = OutputFileType == FileType.Mp3 ? ".mp3" : ".flac";
            try
            {
                List<DownloadTaskResult> downloadTaskResults = new();
                await Parallel.ForEachAsync(selectedSongs, async (song, cancellationToken) =>
                {
                    var result = await ApiHelper.DownloadSong(song, currentFolder);
                    song.IsDownloaded = true;
                    ProgressBarValue++;
                    ProgressText = $"Downloading songs: {ProgressBarValue}/{ProgressBarMaximum}";
                    DownloadedSongs.Add(result.SongCid);
                    downloadTaskResults.Add(result);
                });

                ProgressBarValue = 0;
                ProgressText = $"Converting songs: {ProgressBarValue}/{ProgressBarMaximum}";

                await Parallel.ForEachAsync(selectedSongs, async (song, cancellationToken) =>
                {
                    var tempFileName = downloadTaskResults.First(x => x.SongCid.Equals(song.Cid)).SongFileName;
                    await ConversionHelper.ConvertAndApplyTags(song, currentFolder, tempFileName, fileExtension, ArtistOverride, AlbumOverride);
                    ProgressBarValue++;
                    ProgressText = $"Converting songs: {ProgressBarValue}/{ProgressBarMaximum}";
                });
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (e.Message.Contains("Forbidden"))
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cached data may be stale. Clear and update the data.",
                        ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
                    await box.ShowAsync();
                }
                else
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Unknown error: {e.Message}",
                        ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
                    await box.ShowAsync();
                }
            }
            finally
            {
                await FileHelper.WriteDownloadedSongs(DownloadedSongs.ToList());
                Directory.Delete(Path.Join(currentFolder, Constants.TempFolderName), true);
                ProgressText ="Done";
            }
        }
    }
}