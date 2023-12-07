using System.Collections.Generic;

namespace MSRDownloader.ApiResponses;

public class SongResponse
{
    public string Cid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Artistes { get; set; } = new();
}

