using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MSRDownloader.Models;

namespace MSRDownloader.Helpers;

public static class FileHelper
{
    private static readonly string AppDataDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MSRDownloader");
    
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = DataContractResolver.Default,
    };

    private const string AlbumTestData = """[{"Cid":"0242","Name":"Fleeting Wish (Monster Siren Records)","CoverUrl":"https://web.hycdn.cn/siren/pic/20231125/1b0bb90c2b93d7e1e3f5485fa749d502.jpg","Songs":[{"Cid":"514546","Name":"Fleeting Wish (Monster Siren Records)","SourceUrl":"https://res01.hycdn.cn/411a9584b12514f2ff1d61889dc76c71/6571FF3E/siren/audio/20231125/539c3b86fd6ea5bc62ca81cce9f02b5e.wav","ArtistName":[]}]},{"Cid":"7774","Name":"Fleeting Wish","CoverUrl":"https://web.hycdn.cn/siren/pic/20231125/2ad07c338377f33a5c6e6b851d3a06f5.jpg","Songs":[{"Cid":"048703","Name":"Fleeting Wish","SourceUrl":"https://res01.hycdn.cn/9bc3ddaf4626447a2356db057c54f24c/6571FF3E/siren/audio/20231125/08b35c431316a1fb52236114cf7285ae.wav","ArtistName":[]}]},{"Cid":"2457","Name":"Blade Catcher","CoverUrl":"https://web.hycdn.cn/siren/pic/20231204/cd5cda30d228a82535bb2d2f577df16d.jpg","Songs":[{"Cid":"461144","Name":"Blade Catcher","SourceUrl":"https://res01.hycdn.cn/c900600c8988891998a576a942707340/6571FF3E/siren/audio/20231204/d41d8cd98f00b204e9800998ecf8427e.wav","ArtistName":[]},{"Cid":"125067","Name":"Blade Catcher","SourceUrl":"https://res01.hycdn.cn/f2aa1fd3e5f00c39d9d3889ca27fb9c6/6571FF3F/siren/audio/20231204/d41d8cd98f00b204e9800998ecf8427e.wav","ArtistName":[]}]},{"Cid":"8935","Name":"Lessing","CoverUrl":"https://web.hycdn.cn/siren/pic/20231103/3a71f223e97342b11b6406ccd908b4a8.jpg","Songs":[{"Cid":"953989","Name":"Lessing","SourceUrl":"https://res01.hycdn.cn/a058cac2eb96cad3ece310b3178b5003/6571FF41/siren/audio/20231103/3ad7bac9e97f19e3ff34192c3470a8f6.wav","ArtistName":[]},{"Cid":"697624","Name":"Lessing (Instrumental)","SourceUrl":"https://res01.hycdn.cn/8f18d82c3a401f654673938312876f83/6571FF42/siren/audio/20231103/cfdba53337b4e7c0ddf76867da01b60d.wav","ArtistName":[]}]},{"Cid":"5109","Name":"\u5371\u673A\u5408\u7EA6\u6D4A\u71C3\u4F5C\u6218OST","CoverUrl":"https://web.hycdn.cn/siren/pic/20231122/6facde2752d54d80573e24b384e9afea.jpg","Songs":[{"Cid":"048704","Name":"Battleplan Pyrolysis","SourceUrl":"https://res01.hycdn.cn/e63ff1c8747863c3d0df2d12544e2967/6571FF40/siren/audio/20231122/1a6c8eb38ecb9eeec023e318d85d8364.wav","ArtistName":[]},{"Cid":"125077","Name":"Battleplan Pyrolysis (Instrumental)","SourceUrl":"https://res01.hycdn.cn/f90580e60c41b6688365e4110394f320/6571FF41/siren/audio/20231122/3c770df2e0cb63edd64e57ea4f37cae6.wav","ArtistName":[]},{"Cid":"953988","Name":"Wavering Flame","SourceUrl":"https://res01.hycdn.cn/1710bf1bcf73dc1434b103a902b2e228/6571FF42/siren/audio/20231122/1e2c05d977346b41e749d832a89a388e.wav","ArtistName":[]}]},{"Cid":"1021","Name":"Illuminate","CoverUrl":"https://web.hycdn.cn/siren/pic/20231102/ad7ce259346f8dd14346092c4853c8e4.jpg","Songs":[{"Cid":"461152","Name":"Illuminate","SourceUrl":"https://res01.hycdn.cn/1dbb643b1a1cde4796d7c55cfb47f47b/6571FF44/siren/audio/20231102/40e258dd60f65c19f788eacf06f48775.wav","ArtistName":[]},{"Cid":"232260","Name":"Illuminate (Instrumental)","SourceUrl":"https://res01.hycdn.cn/3f6c6869eb5186db097d07c737e72f5e/6571FF44/siren/audio/20231102/3f20f7f21addd385435db51e37d1f617.wav","ArtistName":[]}]},{"Cid":"2458","Name":"Revealing","CoverUrl":"https://web.hycdn.cn/siren/pic/20231031/fab3582201f380755ad28c2cee95562f.png","Songs":[{"Cid":"306813","Name":"Intro: Resonance","SourceUrl":"https://res01.hycdn.cn/f2b15c55a0ddfc37b51a4a58e9a0fbcb/6571FF45/siren/audio/20231031/75c3c2d3f5180cc2a41f8269ff08837d.wav","ArtistName":[]},{"Cid":"048705","Name":"Revealing","SourceUrl":"https://res01.hycdn.cn/6874961595c4a268e0c3c38fc17f3437/6571FF46/siren/audio/20231031/3f1c4db913594aa1c1f36664e827ce71.wav","ArtistName":[]},{"Cid":"880331","Name":"Revealing (Instrumental)","SourceUrl":"https://res01.hycdn.cn/2eb0917a75b86d8b8642627ca47c26ec/6571FF46/siren/audio/20231031/aea046d53dc376552e028bbcb79e6ef1.wav","ArtistName":[]}]},{"Cid":"3890","Name":"\u5D14\u6797\u7279\u5C14\u6885\u4E4B\u91D1OST","CoverUrl":"https://web.hycdn.cn/siren/pic/20231106/f7d3960e08eabfbf042c6f5570ce6c04.jpg","Songs":[{"Cid":"514547","Name":"Visage","SourceUrl":"https://res01.hycdn.cn/f912bd545f245c77e05f517c844a8136/6571FF40/siren/audio/20231106/41002af272522b3d6dcba250c1d5017c.wav","ArtistName":[]},{"Cid":"125078","Name":"Underneath the Spires","SourceUrl":"https://res01.hycdn.cn/ad3bf4b3f77468be931641d255f7bc0e/6571FF41/siren/audio/20231106/2c7343329015a4b454be3f15c9d9e82d.wav","ArtistName":[]},{"Cid":"779496","Name":"Der Hexenk\u00F6nig","SourceUrl":"https://res01.hycdn.cn/5c9af415c49f4fca153f3e4ea03cb8e5/6571FF42/siren/audio/20231106/185930fd2b1872ca24694c1f2458bc7f.wav","ArtistName":[]},{"Cid":"697623","Name":"Die S\u00FCnden des Herkunftshorns","SourceUrl":"https://res01.hycdn.cn/98b28b6293acc7e54bce6695a00e22b4/6571FF43/siren/audio/20231106/f29e62f61a45274e82a87299376edcea.wav","ArtistName":[]},{"Cid":"779495","Name":"The Theme (Imperial)","SourceUrl":"https://res01.hycdn.cn/bc2f52b8f25aca152319deb592d0067f/6571FF44/siren/audio/20231106/e7e3f38dfaae1f77e8a5f0aca73da4b7.wav","ArtistName":[]},{"Cid":"461151","Name":"The Theme (Variant)","SourceUrl":"https://res01.hycdn.cn/cbfe130f7ba4648b53cb5dab393e2ba5/6571FF44/siren/audio/20231106/a9590e8c04bab76014ff95479e5e3a35.wav","ArtistName":[]},{"Cid":"306812","Name":"Pavillon, My Last Creation","SourceUrl":"https://res01.hycdn.cn/59789ac3c89b956ff3d63230bf36f34a/6571FF45/siren/audio/20231106/5344d64aec842d6f17d4918418971130.wav","ArtistName":[]},{"Cid":"880330","Name":"Scordatura","SourceUrl":"https://res01.hycdn.cn/e6f73d8354e6337f117a9d93884ab344/6571FF46/siren/audio/20231106/7429cbdf80965c2daca5633c4440700c.wav","ArtistName":[]},{"Cid":"232269","Name":"Before the Cessation","SourceUrl":"https://res01.hycdn.cn/662e8e55684624b499ee71a35f1f009d/6571FF47/siren/audio/20231106/86aad3b0dd780fdb11bc59314d2b60fc.wav","ArtistName":[]}]},{"Cid":"4516","Name":"\u51AC\u9690\u5F52\u8DEFOST","CoverUrl":"https://web.hycdn.cn/siren/pic/20231125/35bfa1df0f1f6d5bf24ad64845d9de33.png","Songs":[{"Cid":"514545","Name":"\u51AC\u9690\u5F52\u8DEF","SourceUrl":"https://res01.hycdn.cn/20a7a02a8888e0886e89dca63ba0c743/6571FF3E/siren/audio/20231125/d998871b1eda1fdb3b4ad6a7eb19c571.wav","ArtistName":[]},{"Cid":"306811","Name":"\u6574\u5408\u8FD0\u52A8","SourceUrl":"https://res01.hycdn.cn/81da9daaa6de95531eff735e617021e0/6571FF3F/siren/audio/20231125/7566e7878ff2b66a1c1e71c17e9b69c6.wav","ArtistName":[]},{"Cid":"697622","Name":"\u56DE\u60F3","SourceUrl":"https://res01.hycdn.cn/f31bd244e6a52c02cb2a85312e523e07/6571FF40/siren/audio/20231125/438ea8b32d1a4792b707ca140b9122e3.wav","ArtistName":[]},{"Cid":"461150","Name":"\u7231\u56FD\u8005","SourceUrl":"https://res01.hycdn.cn/23ce511c7f100bbbc2d0661f90dd2c9f/6571FF41/siren/audio/20231125/ef45b854a82718c7adbe19469680dfef.wav","ArtistName":[]},{"Cid":"880339","Name":"\u51AC\u901D","SourceUrl":"https://res01.hycdn.cn/c6b6b70693064b9ae7062be36d60941f/6571FF42/siren/audio/20231125/9fbba9b9d263b2c15a48c1b8c85371dc.wav","ArtistName":[]},{"Cid":"779494","Name":"\u6E17\u900F","SourceUrl":"https://res01.hycdn.cn/d6baedb9645f021333b4abaa3aa41755/6571FF43/siren/audio/20231125/a4bb8781cee43dc24c76540978702bac.wav","ArtistName":[]},{"Cid":"953987","Name":"\u601D\u5FC6","SourceUrl":"https://res01.hycdn.cn/bd307c2d8014c463a03a1facc96502b0/6571FF43/siren/audio/20231125/c2531ae67352cf664a8b6cf3f8fad0e1.wav","ArtistName":[]},{"Cid":"232268","Name":"\u971C\u51BD","SourceUrl":"https://res01.hycdn.cn/5d816baf40963b5c41d519a45f62d53a/6571FF44/siren/audio/20231125/57de2ee5ca4de0b5d78691155a3dde6a.wav","ArtistName":[]},{"Cid":"125076","Name":"\u714C","SourceUrl":"https://res01.hycdn.cn/6ef03f6abf2c71862f3f0ecf32789877/6571FF45/siren/audio/20231125/71f2352f2c8bde6416b23aa2a0384636.wav","ArtistName":[]},{"Cid":"048793","Name":"\u7267\u7FA4","SourceUrl":"https://res01.hycdn.cn/e0e745b79e320a6ca064fbfd41352a26/6571FF46/siren/audio/20231125/9fd65adeb8ecf903f0fe16875b638ce4.wav","ArtistName":[]},{"Cid":"953975","Name":"\u9B4F\u5F66\u543E","SourceUrl":"https://res01.hycdn.cn/2d94fab4e2835151136c2b9d2e5798c5/6571FF47/siren/audio/20231125/4d34e6881a735337a1e6ef9fb67cb3e8.wav","ArtistName":[]},{"Cid":"880321","Name":"\u51B0\u5C01\u65E0\u57A0","SourceUrl":"https://res01.hycdn.cn/0772069e1da043a73430df628aa54452/6571FF48/siren/audio/20231125/aa6edb082f2cc1880ffe0f31c981e0d0.wav","ArtistName":[]},{"Cid":"306802","Name":"\u51EF\u5C14\u5E0C","SourceUrl":"https://res01.hycdn.cn/ad9c0053d1133a5da0cd99c757e5714f/6571FF49/siren/audio/20231125/0dcf9ee2fdaf6df16df14060572298ae.wav","ArtistName":[]},{"Cid":"697610","Name":"\u8E2A\u8FF9","SourceUrl":"https://res01.hycdn.cn/0ce40c6f0ee0544c1bb53464f952a377/6571FF4A/siren/audio/20231125/65b6a6cb96a65206fff23308528ed042.wav","ArtistName":[]},{"Cid":"232259","Name":"\u51B3\u610F","SourceUrl":"https://res01.hycdn.cn/00257e1e99c7d656095a66ae6101f293/6571FF4B/siren/audio/20231125/53917e0c93e7657dc8cc905f014b1a08.wav","ArtistName":[]}]},{"Cid":"6663","Name":"ACHE in PULSE","CoverUrl":"https://web.hycdn.cn/siren/pic/20231023/a497f5681fc53a07beb56b88ea05f7b5.jpg","Songs":[{"Cid":"048706","Name":"ACHE in PULSE","SourceUrl":"https://res01.hycdn.cn/bcbc671ee8e8632d57741a6620c51187/6571FF4B/siren/audio/20231023/73ea0f69541c6d81689a7d8997052bb7.wav","ArtistName":[]}]}]""";

    public static async Task WriteAlbumData(List<Album> albums)
    {
        if (!Directory.Exists(AppDataDirectory))
        {
            Directory.CreateDirectory(AppDataDirectory);
        }
        var content = JsonSerializer.Serialize(albums, SerializerOptions);
        var albumFile = Path.Join(AppDataDirectory, Constants.AlbumFileJson);
        await File.WriteAllTextAsync(albumFile, content);
    }
    
    public static async Task<List<Album>> ReadAlbumData()
    {
        if (!Directory.Exists(AppDataDirectory))
        {
            Directory.CreateDirectory(AppDataDirectory);
        }
        var albumFile = Path.Join(AppDataDirectory, Constants.AlbumFileJson);
        if (File.Exists(albumFile))
        {
            var content = await File.ReadAllTextAsync(albumFile);
            return JsonSerializer.Deserialize<List<Album>>(content, SerializerOptions) ?? new List<Album>();
        }
        return new List<Album>();
    }
    
    public static async Task WriteDownloadedSongs(List<string> songCids)
    {
        if (!Directory.Exists(AppDataDirectory))
        {
            Directory.CreateDirectory(AppDataDirectory);
        }
        var content = JsonSerializer.Serialize(songCids);
        var downloadedFile = Path.Join(AppDataDirectory, Constants.DownloadedFileJson);
        await File.WriteAllTextAsync(downloadedFile, content);
    }
    
    public static async Task<List<string>> ReadDownloadedSongs()
    {
        if (!Directory.Exists(AppDataDirectory))
        {
            Directory.CreateDirectory(AppDataDirectory);
        }
        var downloadedFile = Path.Join(AppDataDirectory, Constants.DownloadedFileJson);
        if (File.Exists(downloadedFile))
        {
            var content = await File.ReadAllTextAsync(downloadedFile);
            return JsonSerializer.Deserialize<List<string>>(content) ?? new();
        }
        return new();
    }
    
    public static string TransformToValidFileName(string input)
    {
        // Remove any invalid characters from the input string
        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        string sanitizedInput = Regex.Replace(input, "[" + Regex.Escape(invalidChars) + "]", "_");
        return sanitizedInput;
    }
}