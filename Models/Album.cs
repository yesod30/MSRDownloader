using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace MSRDownloader.Models;

public class Album : ReactiveObject
{
    
    [JsonConstructor]
    public Album(string cid, string name, string coverUrl, ObservableCollection<Song> songs)
    {
        Cid = cid;
        Name = name;
        CoverUrl = coverUrl;
        Songs = songs;
        var isDownloadedObservable = Songs
            .ToObservableChangeSet()
            .AutoRefresh(x => x.IsDownloaded)
            .ToCollection()
            .Select(x => x.All(y => y.IsDownloaded));

        isDownloaded = isDownloadedObservable.ToProperty(this, x => x.IsDownloaded);
    }
    
    public Album(string cid, string name, string coverUrl, List<Song> songs)
    {
        Cid = cid;
        Name = name;
        CoverUrl = coverUrl;
        Songs = new ObservableCollection<Song>(songs);
        var isDownloadedObservable = Songs
            .ToObservableChangeSet()
            .AutoRefresh(x => x.IsDownloaded)
            .ToCollection()
            .Select(x => x.All(y => y.IsDownloaded));

        isDownloaded = isDownloadedObservable.ToProperty(this, x => x.IsDownloaded);
    }
    
    public string Cid { get; set; }
    
    public string Name { get; set; }
    
    public string CoverUrl { get; set; }

    public ObservableCollection<Song> Songs { get; } = new();
    
    private bool? isSelected = false;
    [IgnoreDataMember]
    public bool? IsSelected
    {
        get => isSelected;
        set => this.RaiseAndSetIfChanged(ref isSelected, value);
    }

    private readonly ObservableAsPropertyHelper<bool> isDownloaded;
    [IgnoreDataMember]
    public bool IsDownloaded => isDownloaded.Value;
    
}
