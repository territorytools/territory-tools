﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TerritoryTools.Alba.Controllers.Models;
using TerritoryTools.Entities;
using TerritoryTools.Web.Data;
using TerritoryTools.Web.MainSite.Models;
using TerritoryTools.Web.MainSite.Services;

namespace TerritoryTools.Web.MainSite.Controllers
{
    [Authorize]
    public class TerritoryUserController : AuthorizedController
    {
        public TerritoryUserController(
            MainDbContext database,
            IStringLocalizer<AuthorizedController> localizer,
            IAlbaCredentials credentials,
            Services.IAuthorizationService authorizationService,
            IAlbaCredentialService albaCredentialService,
            IOptions<WebUIOptions> optionsAccessor) 
            : base(
                database,
                localizer,
                credentials,
                authorizationService,
                albaCredentialService,
                optionsAccessor)
        {
        }

        public IActionResult Index()
        {
            if (!IsUser())
            {
                return Forbid();
            }

            return View();
        }

        public IActionResult Invitation()
        {
            try
            {
                if (!IsUser())
                {
                    return Forbid();
                }

                return View();
            }
            catch(AlbaCredentialException e)
            {
                return Redirect($"~/Home/LoginError?message={WebUtility.UrlEncode(e.Message)}");
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }
        public class UserInvitation
        {
            public bool Selected { get; set; }
            public string Email { get; set; }
            public string Name { get; set; }
        }

        [HttpPost]
        public IActionResult AddUsers([FromBody] List<UserInvitation> invitations)
        {
            try
            {
                var now = DateTime.Now;
                Console.WriteLine($"Adding Users...");
                Console.WriteLine($"    Adding User: {now.ToString("HH:mm:ss")}");
                Console.WriteLine($"    User Count: {invitations.Count}");

                if (!IsUser())
                {
                    return Forbid();
                }

                Guid albaAccountId = albaCredentialService.GetAlbaAccountIdFor(User.Identity.Name);

                foreach(var invitation in invitations)
                {
                    Console.WriteLine($"    Adding User: {now.ToString("HH:mm:ss")}: {invitation.Email} {invitation.Name}");
                    
                    if(!invitation.Selected || string.IsNullOrWhiteSpace(invitation.Email))
                    {
                        continue;
                    }

                    if (database.TerritoryUser.Any(u => BasicStrings.StringsEqual(u.Email, invitation.Email)))
                    {
                        continue; // Move to the next invite
                    }

                    var user = new TerritoryUser
                    {
                        Id = Guid.NewGuid(),
                        Email = invitation.Email,
                        GivenName = invitation.Name,
                        Created = now,
                        Updated = now,
                        Role = "Added"
                    };

                    database.TerritoryUser.Add(user);
                    database
                        .TerritoryUserAlbaAccountLink
                        .Add(
                            new TerritoryUserAlbaAccountLink()
                            {
                                TerritoryUserId = user.Id,
                                AlbaAccountId = albaAccountId,
                                Role = "Added",
                                Created = now,
                                Updated = now
                            });
                }

                database.SaveChanges();

                return Ok();
            }
            catch(AlbaCredentialException e)
            {
                return Redirect($"~/Home/LoginError?message={WebUtility.UrlEncode(e.Message)}");
            }
            catch(Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public IActionResult Invitation(TerritoryUserInvitation invitation)
        {
            try
                {
                if (!IsUser())
                {
                    return Forbid();
                }

                if (database.TerritoryUser.Count(u => BasicStrings.StringsEqual(u.Email, invitation.Email)) > 0)
                {
                    return RedirectToAction(nameof(AlreadyInvited), invitation);
                }

                var now = DateTime.Now;

                var user = new TerritoryUser
                {
                    Id = Guid.NewGuid(),
                    Email = invitation.Email,
                    Surname = invitation.Surname,
                    GivenName = invitation.GivenName,
                    Created = now,
                    Updated = now,
                    Role = "Invited"
                };

                database.TerritoryUser.Add(user);

                if(string.Equals(invitation.AlbaAccount, "this-account"))
                {
                    Guid albaAccountId = albaCredentialService.GetAlbaAccountIdFor(User.Identity.Name);

                    database
                        .TerritoryUserAlbaAccountLink
                        .Add(
                            new TerritoryUserAlbaAccountLink()
                            {
                                TerritoryUserId = user.Id,
                                AlbaAccountId = albaAccountId,
                                Role = "Invited",
                                Created = DateTime.Now,
                                Updated = DateTime.Now
                            });

                }

                database.SaveChanges();

                return Ok();
            }
            catch(AlbaCredentialException e)
            {
                return Redirect($"~/Home/LoginError?message={WebUtility.UrlEncode(e.Message)}");
            }
            catch(Exception)
            {
                throw;
            }
        }

        public IActionResult AlreadyInvited(TerritoryUserInvitation invitation)
        {
            try
            {
                if (!IsUser())
                {
                    return Forbid();
                }

                return View(invitation);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        public IActionResult LinkAlbaAccount()
        {
            try
            {
                if (!IsUser())
                {
                    return Forbid();
                }

                return View();
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPost]
        public IActionResult LinkAlbaAccount(LinkAlbaAccount link)
        {
            if (!IsUser())
            {
                return Forbid();
            }

            var now = DateTime.Now;

            var user = database
                .TerritoryUser
                .FirstOrDefault(u => u.Email != null
                    && link.TerritoryUserEmail != null
                    && u.Email.ToUpper() == link.TerritoryUserEmail.ToUpper());
                
            if (user == null)
            {
                user = new TerritoryUser
                {
                    Id = Guid.NewGuid(),
                    Email = link.TerritoryUserEmail,
                    Created = now,
                    Updated = now,
                };

                database.TerritoryUser.Add(user);
                database.SaveChanges();
            }

            var accountName = database
                .AlbaAccounts
                .FirstOrDefault(a => a.AccountName != null
                    && link.AccountName != null
                    && a.AccountName.ToUpper() == link.AccountName.ToUpper());

            if (accountName != null)
            {
                throw new Exception($"Alba account '{link.AccountName}' already exists!");
            }

            // TODO: Check if credentials work

            var account = new AlbaAccount
            {
                Id = Guid.NewGuid(),
                HostName = link.Host,
                AccountName = link.AccountName,
                Created = now,
                Updated = now,
                LongName = link.AccountName
            };

            database.AlbaAccounts.Add(account);
            database.SaveChanges();

            var credentials = new Credentials(
                account: link.AccountName,
                user: link.UserName,
                password: link.Password)
            {
                AlbaAccountId = account.Id
            };

            albaCredentialService.SaveCredentials(credentials);

            var userLink = new TerritoryUserAlbaAccountLink
            {
                AlbaAccountId = account.Id,
                TerritoryUserId = user.Id,
                Created = now,
                Updated = now
            };

            database.TerritoryUserAlbaAccountLink.Add(userLink);
            database.SaveChanges();

            return Ok();
        }
    }
}
