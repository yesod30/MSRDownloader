using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace MSRDownloader.Models;

public class Album : ReactiveObject
{
    public Album(string cid, string name, string coverUrl)
    {
        Cid = cid;
        Name = name;
        CoverUrl = coverUrl;
    }
    
    public string Cid { get; set; }
    
    public string Name { get; set; }
    
    public string CoverUrl { get; set; }
    
    public List<Song> Songs { get; set; } = new();

    private bool? isSelected = false;
    [JsonIgnore]
    public bool? IsSelected
    {
        get => isSelected;
        set => this.RaiseAndSetIfChanged(ref isSelected, value);
    }
}
