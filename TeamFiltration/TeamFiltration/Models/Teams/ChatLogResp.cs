using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Teams
{


    public class ChatLogResp
    {
        public Message[] messages { get; set; }
        public Metadata _metadata { get; set; }
    }

    public class Metadata
    {
        public string syncState { get; set; }
        public string lastCompleteSegmentStartTime { get; set; }
        public string lastCompleteSegmentEndTime { get; set; }
    }

    public class Message
    {
        public string id { get; set; }
        public string sequenceId { get; set; }
        public string clientmessageid { get; set; }
        public string version { get; set; }
        public string conversationid { get; set; }
        public string conversationLink { get; set; }
        public string type { get; set; }
        public string messagetype { get; set; }
        public string contenttype { get; set; }
        public string content { get; set; }
        public string[] amsreferences { get; set; }
        public string from { get; set; }
        public string imdisplayname { get; set; }
        public DateTime composetime { get; set; }
        public DateTime originalarrivaltime { get; set; }
        public PropertiesSub properties { get; set; }
        public string origincontextid { get; set; }
    }

    public class PropertiesSub
    {
        public string importance { get; set; }
        public string files { get; set; }
    }


}
