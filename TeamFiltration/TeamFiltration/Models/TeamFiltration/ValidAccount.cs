using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using TeamFiltration.Models.Teams;

namespace TeamFiltration.Models.TeamFiltration
{
    public class ValidAccount
    {
        public ValidAccount()
        {
           
        }

        [BsonId]
        public string Id { get; set; }
        public string Username { get; set; }
        public string objectId { get; set; }
        public string DisplayName { get; set; }
        public string OutOfOfficeMessage { get; set; }
    }
}
