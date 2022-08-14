using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.TeamFiltration
{
    public class SprayAttemptPretty
    {
        public int Id { get; set; }

        public DateTime DateTime { get; set; }
        public bool Disqualified { get; set; }
        public bool Valid { get; set; }
        public bool ConditionalAccess { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

  

        public static explicit operator SprayAttemptPretty(SprayAttempt v)
        {
            return new SprayAttemptPretty()
            {
                Id = v.Id,
                DateTime = v.DateTime,
                Disqualified = v.Disqualified,
                Valid = v.Valid,
                ConditionalAccess = v.ConditionalAccess,
                Username = v.Username,
                Password = v.Password,
            };
        }
    }

    public class SprayAttempt
    {
        public int Id { get; set; }
        public bool Disqualified { get; set; }
        public bool Valid { get; set; }
        public bool AADSSO { get; set; }
        public bool ADFS { get; set; }
        public bool ConditionalAccess { get; set; }
        public bool CanRefresh { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ResponseCode { get; set; }
        public string FireProxURL { get; set; }
        public string FireProxRegion { get; set; }
        public string ResourceUri { get; set; }
        public string ResourceClientId { get; set; }
        public string ResponseData { get; set; }
        public string ComboHash { get; set; }
        public DateTime DateTime { get; set; }
    }
}
