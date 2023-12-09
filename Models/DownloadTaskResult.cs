namespace MSRDownloader.Models;

public class DownloadTaskResult
{
    public DownloadTaskResult(Song song, string songFileName)
    {
        Song = song;
        SongFileName = songFileName;
    }
    
    public Song Song { get; set; }
    public string SongFileName { get; set; }
}