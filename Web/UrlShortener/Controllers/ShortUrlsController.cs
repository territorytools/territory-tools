using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using TerritoryTools.Web.Data.Services;

namespace UrlShortener.Controllers
{
    public class ShortUrlsController : Controller
    {
        private readonly IShortUrlService _service;
        private readonly IActionContextAccessor _accessor;

        public ShortUrlsController(
            IShortUrlService service, 
            IActionContextAccessor accessor)
        {
            _service = service;
            _accessor = accessor;
        }

        [HttpGet("/{path:required}")]
        public IActionResult RedirectTo(string path)
        {
            if (path == null) 
            {
                return NotFound();
            }

            string host = Request.Host.Host;
            string ip = HttpContext.Connection.RemoteIpAddress.ToString();

            var shortUrl = _service.GetByPath(path, ip, host);
            if (shortUrl == null) 
            {
                return NotFound();
            }

            return Redirect(shortUrl.OriginalUrl);
        }
    }
}
