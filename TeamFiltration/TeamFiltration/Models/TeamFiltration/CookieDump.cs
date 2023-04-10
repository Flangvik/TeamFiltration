using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.TeamFiltration
{


 
    public class SharpChromeCookieObject
    {
        public string domain { get; set; }
        public bool hostOnly { get; set; }
        public bool httpOnly { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public string sameSite { get; set; }
        public bool secure { get; set; }
        public bool session { get; set; }
        public object storeId { get; set; }
        public string value { get; set; }
        public float expirationDate { get; set; }

        
    }

    public class CookieQuickManagerObject
    {
        [JsonProperty(PropertyName = "Host raw")]
        public string Hostraw { get; set; }
        [JsonProperty(PropertyName = "Name raw")]
        public string Nameraw { get; set; }
        public string Pathraw { get; set; }
        [JsonProperty(PropertyName = "Content raw")]
        public string Contentraw { get; set; }
        public string Expires { get; set; }
        public string Expiresraw { get; set; }
        public string Sendfor { get; set; }
        public string Sendforraw { get; set; }
        public string HTTPonlyraw { get; set; }
        public string SameSiteraw { get; set; }
        public string Thisdomainonly { get; set; }
        public string Thisdomainonlyraw { get; set; }
        public string Storeraw { get; set; }
        public string FirstPartyDomain { get; set; }
    }

    public class CookieObject
    {
        public string domain { get; set; }
        public float expirationDate { get; set; }
        public bool hostOnly { get; set; }
        public bool httpOnly { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public string sameSite { get; set; }
        public bool secure { get; set; }
        public bool session { get; set; }
        public object storeId { get; set; }
        public string value { get; set; }
        public static explicit operator CookieObject(SharpChromeCookieObject v)
        {
            return new CookieObject()
            {
                domain = v.domain,
                value = v.value,
                name = v.name
            };
        }
        public static explicit operator CookieObject(CookieQuickManagerObject v)
        {
            return new CookieObject()
            {
                domain = v.Hostraw,
                value = v.Contentraw,
                name = v.Nameraw
            };
        }
    }




}
