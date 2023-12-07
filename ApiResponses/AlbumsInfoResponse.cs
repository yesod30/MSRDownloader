using System.Collections.Generic;

namespace MSRDownloader.ApiResponses;

public class AlbumsInfoResponse
{
    public int Code { get; set; }
    public string Msg { get; set; } = string.Empty;
    public List<AlbumResponse> Data { get; set; } = new();
}



