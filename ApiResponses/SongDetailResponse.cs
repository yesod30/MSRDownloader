using System.Collections.Generic;

namespace MSRDownloader.ApiResponses;

public class SongDetailResponse
{
    public int Code { get; set; }
    public string Msg { get; set; } = string.Empty;
    public SongInfoResponse? Data { get; set; }
}

public class SongInfoResponse
{
    public string Cid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AlbumCid { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string LyricUrl { get; set; } = string.Empty;
    public string MvUrl { get; set; } = string.Empty;
    public string MvCoverUrl { get; set; } = string.Empty;
    public List<string> Artists { get; set; } = new();
}

