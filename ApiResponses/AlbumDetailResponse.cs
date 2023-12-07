using System.Collections.Generic;

namespace MSRDownloader.ApiResponses;

public class AlbumDetailResponse
{
    public int Code { get; set; }
    public string Msg { get; set; } = string.Empty;
    public AlbumDataResponse? Data { get; set; }
}

public class AlbumDataResponse
{
    public string Cid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public string Belong { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string CoverDeUrl { get; set; } = string.Empty;
    public List<SongResponse> Songs { get; set; } = new();
}
