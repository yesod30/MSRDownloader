using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace MSRDownloader.Models;

public class Song : ReactiveObject
{
    public Song(string cid, string name, string albumName, string coverUrl, string sourceUrl)
    {
        Cid = cid;
        Name = name;
        AlbumName = albumName;
        SourceUrl = sourceUrl;
        CoverUrl = coverUrl;
    }
    
    public string Cid { get; set; } 
    
    public string Name { get; set; } 
    
    public string SourceUrl { get; set; }
    
    public string AlbumName { get; set; }
    
    public string CoverUrl { get; set; }
    
    public List<string> ArtistName { get; set; } = new();
    
    private bool isSelected;
    [JsonIgnore]
    public bool IsSelected
    {
        get => isSelected;
        set => this.RaiseAndSetIfChanged(ref isSelected, value);
    }
    
    private bool isDownloaded;
    [JsonIgnore]
    public bool IsDownloaded
    {
        get => isDownloaded;
        set => this.RaiseAndSetIfChanged(ref isDownloaded, value);
    }
}