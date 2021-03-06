using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerritoryTools.Alba.Controllers.UseCases;
using Controllers.UseCases;

namespace TerritoryTools.Web.MainSite.Models
{
    public class NeverCompletedReport
    {
        public List<User> Publishers { get; set; } 
            = new List<User>();

        public List<AlbaAssignmentValues> Assignments { get; set; } 
            = new List<AlbaAssignmentValues>();
    }
}