using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using TerritoryTools.Web.MainSite.Services;
using TerritoryTools.Web.Data;
using TerritoryTools.Entities;
using TerritoryTools.Web.MainSite.Models;

namespace TerritoryTools.Web.MainSite.Controllers
{
    [Authorize]
    public class ReportController : AuthorizedController
    {
        public const string DATE_FORMAT = "yyyy-MM-dd";

        IAccountLists accountLists;
        public ReportController(
            MainDbContext database,
            IAccountLists accountLists,
            IStringLocalizer<AuthorizedController> localizer,
            IAlbaCredentials credentials,
            Services.IAuthorizationService authorizationService,
            IAlbaCredentialService albaCredentialService,
            IOptions<WebUIOptions> optionsAccessor) : base(
                database,
                localizer,
                credentials,
                authorizationService,
                albaCredentialService,
                optionsAccessor)
        {
            this.accountLists = accountLists;
        }

        [Authorize]
        public IActionResult Index()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            ViewData["CompletionMapUrl"] = options.CompletionMapUrl;

            return View();
        }

        [Authorize]
        public IActionResult ByPublisher()
        {
            try
            {
                if (!IsAdmin())
                {
                    return Forbid();
                }

                var groups = GetAllAssignments()
                    .Where(a => !string.IsNullOrWhiteSpace(a.SignedOutTo))
                    .GroupBy(a => a.SignedOutTo)
                    .ToList();

                var publishers = new List<Publisher>();
                foreach (var group in groups.OrderBy(g => g.Key))
                {
                    var pub = new Publisher() { Name = group.Key };
                    var ordered = group.OrderByDescending(a => a.SignedOut);
                    foreach (var item in ordered)
                    {
                        pub.Territories.Add(item);
                    }

                    publishers.Add(pub);
                }

                return View(publishers);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        public IActionResult YearlyCompletionSummary()
        {
            try
            {
                if (!IsAdmin())
                {
                    return Forbid();
                }

                var allAssignments = GetAllAssignments()
                    .ToList();

                var now = DateTime.Now;
                int thisYear = now.Month >= 9 ? now.Year : (now.Year - 1);

                var report = new SummarizeCompletedReport();

                for (int i = 0; i <= 10; i++)
                {
                    int year = thisYear - i;
                    report.SummaryItems.Add(
                        new SummaryItem
                        {
                            Name = $"{year}-{year + 1}",
                            Value = allAssignments
                                .Where(a => a.LastCompleted >= new DateTime(year, 9, 1)
                                && a.LastCompleted < new DateTime(year + 1, 9, 1))
                                .ToList()
                                .Count
                                .ToString()
                        });
                }

                return View(report);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public class Summary
        {
            public string Period { get; set; }
            public string Group { get; set; }
            public string Area { get; set; }
            public int Count { get; set; }
            public int Addresses { get; internal set; }
        }

        [Authorize]
        public IActionResult SummarizeCompleted()
        {
            try
            {
                if (!IsAdmin())
                {
                    return Forbid();
                }

                var report = GeographicSummaryReportFrom(ServiceYearFrom);

                return View(report);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        public IActionResult SummarizeCompletedFromNow()
        {
            try
            {
                if (!IsAdmin())
                {
                    return Forbid();
                }

                var report = GeographicSummaryReportFrom(ThisYearFrom);

                return View(report);
            }
            catch (Exception)
            {
                throw;
            }
        }


        public GeographicSummaryReport GeographicSummaryReportFrom(Func<DateTime, DateTime, DateTime> periodYearFrom)
        {
            try
            {
                var completed = GetAllAssignments()
                    .Where(ast => ast.LastCompleted != null)
                    .ToList();

                var now = DateTime.Now;
                var summaries = new List<Summary>();
                foreach (var a in completed)
                {
                    DateTime periodStart = periodYearFrom((DateTime)a.LastCompleted, now)
                        .Date;

                    string prefix = a.Description?.Substring(0, 3);

                    string areaName = prefix;
                    string groupName = "?";
                    if (accountLists.Areas.TryGetValue(prefix, out Area area))
                    {
                        areaName = string.IsNullOrWhiteSpace(area.Name) ? prefix : area.Name;
                        groupName = string.IsNullOrWhiteSpace(area.Group) ? "?" : area.Group;
                    }

                    summaries.Add(new Summary
                    {
                        Period = $"{periodStart.ToString(DATE_FORMAT)}---{periodStart.AddYears(1).AddDays(-1).ToString(DATE_FORMAT)}",
                        Area = areaName,
                        Group = groupName,
                        Addresses = a.Addresses
                    });
                }

                var periods =
                   from p in summaries
                   orderby p.Period descending
                   group p by p.Period into period
                   from zones in
                       (from z in period
                        orderby z.Group
                        group z by z.Group into zone
                        from areas in
                            (from a in zone
                             orderby a.Area
                             group a by a.Area)
                        group areas by zone.Key)
                   group zones by period.Key;


                var report = new GeographicSummaryReport();

                foreach (var period in periods)
                {
                    var periodView = new GeographicSummaryPeriod
                    {
                        Period = period.Key,
                        Completed = period.Sum(p => p.Sum(z => z.Count())),
                        Addresses = period.Sum(p => p.Sum(z => z.Sum(a => a.Addresses)))
                    };

                    report.Periods.Add(periodView);

                    foreach (var zone in period)
                    {
                        var zoneView = new GeographicSummaryZone
                        {
                            Zone = zone.Key,
                            Completed = zone.Sum(z => z.Count()),
                            Addresses = zone.Sum(z => z.Sum(a => a.Addresses))
                        };

                        periodView.Zones.Add(zoneView);

                        foreach (var area in zone)
                        {
                            var areaView = new GeographicSummaryArea
                            {
                                Area = area.Key,
                                Completed = area.Count(),
                                Addresses = area.Sum(a => a.Addresses)
                            };

                            zoneView.Areas.Add(areaView);
                        }
                    }
                }

                return report;
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        public IActionResult NeverCompleted()
        {
            try
            {
                if (!IsAdmin())
                {
                    return Forbid();
                }

                var publishers = GetUsers(account, user, password)
                    .OrderBy(u => u.Name)
                    .ToList();

                var assignments = GetAllAssignments()
                    // Territories never worked
                    .Where(a => a.LastCompleted == null && a.SignedOut == null)
                    .OrderBy(a => a.Description)
                    .ThenBy(a => a.Number)
                    .ToList();

                var report = new NeverCompletedReport()
                {
                    Publishers = publishers,
                    Assignments = assignments
                };

                return View(report);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        public IActionResult Available()
        {
            try
            {
                if (!IsAdmin())
                {
                    return Forbid();
                }

                var users = GetUsers(account, user, password)
                    .OrderBy(u => u.Name)
                    .ToList();

                var assignments = GetAllAssignments()
                    .Where(a => string.Equals(
                        a.Status,
                        "Available",
                        StringComparison.OrdinalIgnoreCase))
                    .OrderBy(a => a.LastCompleted)
                    .ThenBy(a => a.Number)
                    .ToList();

                var report = new NeverCompletedReport()
                {
                    // TODO: Rename Publishers to Users
                    Publishers = users,
                    Assignments = assignments
                };

                return View(report);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        public IActionResult AlbaUsers()
        {
            try
            {
                if (!IsAdmin())
                {
                    return Forbid();
                }

                Guid albaAccountId = albaCredentialService
                    .GetAlbaAccountIdFor(User.Identity.Name);

                var userListView = new AlbaUserListView();
                userListView.Users = GetAlbaUsers(albaAccountId)
                    .OrderBy(u => u.Name)
                    .ToList();

                return View(userListView);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Authorize]
        public IActionResult AssignmentHistory()
        {
            try
            {
                if (!IsAdmin())
                {
                    return Forbid();
                }

                var assignments = database
                    .TerritoryAssignments
                    .ToList();

                var report = new AssignmentHistoryReport();

                foreach(var assignment in assignments)
                {
                    string territoryNumber = assignment.TerritoryNumber;
                    if(int.TryParse(assignment.TerritoryNumber, out int number))
                    {
                        territoryNumber = number.ToString("0000");
                    }

                    report.Records.Add(
                        new AssignmentRecord
                        {
                            TerritoryNumber = territoryNumber,
                            Date = assignment.Date,
                            PublisherName = assignment.PublisherName,
                            CheckedIn = assignment.CheckedIn?.Year == 1900
                                ? ""
                                : assignment.CheckedIn?.ToString(DATE_FORMAT),
                            CheckedOut = assignment.CheckedOut?.Year == 1900
                                ? ""
                                : assignment.CheckedOut?.ToString(DATE_FORMAT),
                            Note = assignment.Note
                        });
                }

                report.Records = report
                    .Records
                    .OrderBy(r => r.TerritoryNumber)
                    .ThenBy(r => r.Date)
                    .ToList();

                return View(report);
            }
            catch (Exception)
            {
                throw;
            }
        }

        DateTime ServiceYearFrom(DateTime date, DateTime now)
        {
            return date.Month >= 9
                ? new DateTime(date.Year, 9, 1)
                : new DateTime(date.Year - 1, 9, 1);
        }

        DateTime ThisYearFrom(DateTime date, DateTime now)
        {
            for (int i = 0; i < 99; i++)
            {
                if (date >= now.AddYears(-(i + 1)) && date <= now.AddYears(-i))
                {
                    return now.AddYears(-(i + 1));
                }
            }

            return DateTime.MinValue;
        }
    }
}
