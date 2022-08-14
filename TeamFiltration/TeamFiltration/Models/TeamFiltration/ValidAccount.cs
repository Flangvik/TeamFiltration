using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
