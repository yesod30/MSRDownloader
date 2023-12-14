using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using FFMpegCore.Exceptions;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MSRDownloader.Helpers;
using MSRDownloader.Models;
using ReactiveUI;

namespace MSRDownloader.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly Window? window;
    public MainWindowViewModel(Options options)
    {
        window = (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow;
        OutputFileType = options.OutputFileType;
        ArtistOverride = options.ArtistOverride;
        AlbumOverride = options.AlbumOverride;
        RxApp.MainThreadScheduler.Schedule(LoadAlbumData);
    }

    public ObservableCollection<Album> AlbumsList { get; } = new();

    public ObservableCollection<string> DownloadedSongs { get; } = new();
    
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

        try
        {
            var loadedAlbums = await FileHelper.ReadAlbumData();
            AlbumsList.AddRange(loadedAlbums.OrderBy(x => x.Order));
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
        }
        catch (Exception e)
        {
            LogHelper.WriteError("Error while loading album data", e);
            LogHelper.WriteException(e);
            var box = MessageBoxManager.GetMessageBoxStandard("Error",
                "Error while loading data. Clearing data is reccomended.",
                ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(window);
        }

        IsLoadingData = false;
    }

    private async Task LoadDownloadedSongs()
    {
        var downloadedSongsCid = await FileHelper.ReadDownloadedSongs();
        DownloadedSongs.AddRange(downloadedSongsCid);
    }

    public async Task ClearAndUpdateData()
    {
        isLoadingData = true;
        AlbumsList.Clear();
        await FileHelper.WriteAlbumData(AlbumsList.ToList());
        await UpdateData();
        isLoadingData = false;
    }

    public async Task ClearDownloadedData()
    {
        isLoadingData = true;
        DownloadedSongs.Clear();
        await FileHelper.WriteDownloadedSongs(DownloadedSongs.ToList());
        isLoadingData = false;
    }

    public void SelectAllNotDownload()
    {
        isLoadingData = true;
        foreach (var album in AlbumsList)
        {
            var songsNumber = album.Songs.Count;
            var count = 0;
            foreach (var albumSong in album.Songs)
            {
                if (!albumSong.IsDownloaded)
                {
                    albumSong.IsSelected = true;
                    count++;
                }
            }

            if (count == songsNumber)
            {
                album.IsSelected = true;
            }
            else if (count < songsNumber && count != 0)
            {
                album.IsSelected = null;
            }
            else if (count == 0)
            {
                album.IsSelected = false;
            }
        }

        isLoadingData = false;
    }

    public void SelectAll()
    {
        isLoadingData = true;
        foreach (var album in AlbumsList)
        {
            foreach (var albumSong in album.Songs)
            {
                albumSong.IsSelected = true;
            }

            album.IsSelected = true;
        }

        isLoadingData = false;
    }

    public void DeselectAll()
    {
        isLoadingData = true;
        foreach (var album in AlbumsList)
        {
            foreach (var albumSong in album.Songs)
            {
                albumSong.IsSelected = false;
            }

            album.IsSelected = false;
        }

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
        
        ProgressBarMaximum = albums.Count - 1;
        ConcurrentQueue<Album> list = new();
        try
        {
            await Parallel.ForEachAsync(albums, async (albumResponse, cancellationToken) =>
            {
                var albumDetail = await ApiHelper.GetAlbumDetail(albumResponse.Cid);
                if (albumDetail is not null)
                {
                    var albumName = albumDetail.Name;
                    var albumCoverUrl = albumDetail.CoverUrl;
                    List<Song> songs = new();
                    foreach ( var albumSong in albumDetail.Songs)
                    {
                        var songDetail = await ApiHelper.GetSongDetail(albumSong.Cid);
                        if (songDetail is not null)
                        {
                            var song = new Song(songDetail.Cid, songDetail.Name, albumName, albumCoverUrl,
                                songDetail.SourceUrl, albumDetail.Songs.IndexOf(albumSong));
                            song.ArtistName.AddRange(songDetail.Artists);
                            song.IsDownloaded = DownloadedSongs.Contains(song.Cid);
                            songs.Add(song);
                        }
                    }

                    songs = songs.OrderBy(x => x.Order).ToList();
                    var album = new Album(albumDetail.Cid, albumName, albumCoverUrl, albums.IndexOf(albumResponse), songs);
                    list.Enqueue(album);
                    ProgressBarValue++;
                    ProgressText = $"Fetched data for {ProgressBarValue}/{ProgressBarMaximum} albums";
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            LogHelper.WriteError("Error while updating album list", e);
            var box = MessageBoxManager.GetMessageBoxStandard("Error",
                "Error while updating album list. Try to update again after the current update finish.",
                ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(window);
        }

        //deal with fixing order when adding new albums
        if (AlbumsList.Any())
        {
            var newAlbums = list.Count;
            foreach (var album in AlbumsList)
            {
                album.Order += newAlbums;
            }
        }

        var temp = list.OrderBy(x => x.Order).Concat(AlbumsList).ToList();
        AlbumsList.Clear();
        AlbumsList.AddRange(temp);
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
        IsLoadingData = true;
        ProgressBarValue = 0;
        await FileHelper.WriteOptions(ArtistOverride, AlbumOverride, OutputFileType);
        var selectedSongs = AlbumsList.SelectMany(x => x.Songs.Where(y => y.IsSelected)).ToList();
        if (selectedSongs.Any())
        {
            ProgressBarMaximum = selectedSongs.Count;
            ProgressText = $"Downloading songs: {ProgressBarValue}/{ProgressBarMaximum}";
            var currentFolder = AppContext.BaseDirectory;
            var fileExtension = OutputFileType == FileType.Mp3 ? ".mp3" : ".flac";
            ParallelOptions downloadOptions = new()
            {
                MaxDegreeOfParallelism = 5,
            };

            try
            {
                List<DownloadTaskResult> downloadTaskResults = new();
                await Parallel.ForEachAsync(selectedSongs, downloadOptions, async (song, cancellationToken) =>
                {
                    var result = await ApiHelper.DownloadSong(song, currentFolder);
                    song.IsDownloaded = true;
                    ProgressBarValue++;
                    ProgressText = $"Downloading songs: {ProgressBarValue}/{ProgressBarMaximum}";
                    downloadTaskResults.Add(result);
                });

                ProgressBarValue = 0;
                ProgressText = $"Converting songs: {ProgressBarValue}/{ProgressBarMaximum}";

                await Parallel.ForEachAsync(downloadTaskResults, async (downloadResult, cancellationToken) =>
                {
                    await ConversionHelper.ConvertAndApplyTags(downloadResult.Song, currentFolder,
                        downloadResult.SongFileName, fileExtension, ArtistOverride, AlbumOverride);
                    downloadResult.Song.IsDownloaded = true;
                    DownloadedSongs.Add(downloadResult.Song.Cid);
                    ProgressBarValue++;
                    ProgressText = $"Converting songs: {ProgressBarValue}/{ProgressBarMaximum}";
                });
            }
            catch (HttpRequestException e)
            {
                if (e.Message.Contains("Forbidden"))
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Error",
                        "Cached data may be stale. Clear and update the data.",
                        ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
                    await box.ShowWindowDialogAsync(window);
                }
                else
                {
                    LogHelper.WriteError("Connection error", e);
                    var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Connection error: {e.Message}",
                        ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
                    await box.ShowWindowDialogAsync(window);
                }
            }
            catch (FFMpegException e)
            {
                LogHelper.WriteError("Error while converting", e);
                var box = MessageBoxManager.GetMessageBoxStandard("Error",
                    "Error during conversion of one or more files. Some downloaded files may be corrupted",
                    ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
                await box.ShowWindowDialogAsync(window);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                LogHelper.WriteError("Unknown error while downloading or converting", e);
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Unknown error: {e.Message}",
                    ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner);
                await box.ShowWindowDialogAsync(window);
            }
            finally
            {
                await FileHelper.WriteDownloadedSongs(DownloadedSongs.ToList());
                var tempFolder = Path.Join(currentFolder, Constants.TempFolderName);
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }

                IsLoadingData = false;
                ProgressText = "Done";
                
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Arguments = Path.Join(currentFolder, Constants.OutputFolderName),
                    FileName = "explorer.exe",
                };
                Process.Start(startInfo);
            }
        }
    }
}