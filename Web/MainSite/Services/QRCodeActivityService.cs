using System;
using System.Collections.Generic;
using System.Linq;
using TerritoryTools.Entities;
using TerritoryTools.Web.Data;

namespace TerritoryTools.Web.MainSite.Services
{
    public interface IQRCodeActivityService
    {
        List<QRCodeHit> QRCodeHitsForUser(string userName);
    }

    public class QRCodeActivityService : IQRCodeActivityService
    {
        readonly MainDbContext context;

        public QRCodeActivityService(MainDbContext context)
        {
            this.context = context;
        }

        public List<QRCodeHit> QRCodeHitsForUser(string userName)
        {
            var urls = context
                .ShortUrls
                .Where(url => 
                    url.UserName != null
                    && userName != null
                    && url.UserName.ToLower() == userName.ToLower())
                .ToList();

            var hits = new List<QRCodeHit>();
            foreach(var url in urls)
            {
                var activity = context
                    .ShortUrlActivity
                    .Where(a => a.ShortUrlId == url.Id)
                    .OrderBy(a => a.TimeStamp);

                string lastIPAddress = activity.LastOrDefault()?.IPAddress;
                DateTime? lastHit = null;
                if (activity.Count() > 0)
                {
                    lastHit = activity.Max(a => a.TimeStamp);
                }

                hits.Add(
                    new QRCodeHit
                    {
                        Id = url.Id,
                        ShortUrl = AlphaNumberId.ToAlphaNumberId(url.Id),
                        OriginalUrl = url.OriginalUrl,
                        Created = url.Created,
                        HitCount = context.ShortUrlActivity.Count(a => a.ShortUrlId == url.Id),
                        LastIPAddress = lastIPAddress,
                        LastTimeStamp = lastHit,
                        UserName = url.UserName,
                        Subject = url.Subject,
                        Note = url.Note
                    });

            }

            return hits;
        }
    }

    public class QRCodeHit
    {
        public int Id { get; set; }
        public string ShortUrl { get; set; }
        public string OriginalUrl { get; set; }
        public DateTime Created { get; set; }
        public string UserName { get; set; }
        public string Subject { get; set; }
        public string Note { get; set; }
        public int HitCount { get; set; }
        public string LastIPAddress { get; set; }
        public DateTime? LastTimeStamp { get; set; }
    }
}
