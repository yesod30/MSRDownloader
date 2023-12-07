using System;
using System.Collections.Generic;

namespace MSRDownloader.ApiResponses;

public class AlbumResponse
{
    public string Cid { get; set; } = String.Empty;
    public string Name { get; set; } = String.Empty;
    public string CoverUrl { get; set; } = String.Empty;
    public List<string> Artistes { get; set; } = new();
}

