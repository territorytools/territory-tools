using WebUI.Areas.UrlShortener.Models;

namespace UrlShortener.Services
{
    public interface IShortUrlService
    {
        ShortUrl GetById(int id);

        ShortUrl GetByPath(string path, string ip);

        ShortUrl GetByOriginalUrl(string originalUrl);

        int Save(ShortUrl shortUrl);
    }
}
