using System.IO;

namespace UrlBlockListWpfApp;

public record BlockListOptions(
    string ChromePath,
    string EdgePath
)
{
    private static string CurrentDirectory => Directory.GetCurrentDirectory();
    public  BlockListOptions():this(
         ChromePath : $@"{CurrentDirectory}\Registration Files\Chrome-Full-AllowList.reg",
         EdgePath : $@"{CurrentDirectory}\Registration Files\Edge-Full-AllowList.reg"
    )
    { }
}