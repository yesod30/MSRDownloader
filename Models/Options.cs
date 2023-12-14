namespace MSRDownloader.Models;

public class Options
{
    public Options()
    {
    }
    
    public Options(string artistOverride, string albumOverride, FileType fileType)
    {
        ArtistOverride = artistOverride;
        AlbumOverride = albumOverride;
        OutputFileType = fileType;
    }
    public string ArtistOverride { get; set; } = string.Empty;
    public string AlbumOverride { get; set; } = string.Empty;
    public FileType OutputFileType { get; set; } = FileType.Mp3;
}