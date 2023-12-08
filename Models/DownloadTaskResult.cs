namespace MSRDownloader.Models;

public class DownloadTaskResult
{
    public DownloadTaskResult(string songCid, string songFileName)
    {
        SongCid = songCid;
        SongFileName = songFileName;
    }
    
    public string SongCid { get; set; }
    public string SongFileName { get; set; }
}