using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Teams
{


    public class UserConversationsResp
    {
        public Conversation[] conversations { get; set; }
        public _Metadata _metadata { get; set; }
    }

    public class _Metadata
    {
        public int totalCount { get; set; }
        public string forwardLink { get; set; }
        public string syncState { get; set; }
    }

    public class Conversation
    {
    

        public string id { get; set; }
        public string type { get; set; }
        public long version { get; set; }
        public Properties properties { get; set; }
        public Threadproperties threadProperties { get; set; }
        public Memberproperties memberProperties { get; set; }
        public Lastmessage lastMessage { get; set; }
        public string messages { get; set; }
        public long lastUpdatedMessageId { get; set; }
        public long lastUpdatedMessageVersion { get; set; }
        public string targetLink { get; set; }

     
    }

    public class Properties
    {
        public DateTime lastimreceivedtime { get; set; }
        public string consumptionhorizon { get; set; }
        public string isemptyconversation { get; set; }
        public string topic { get; set; }
    }

    public class Threadproperties
    {
        public bool isCreator { get; set; }
        public string gapDetectionEnabled { get; set; }
        public string lastjoinat { get; set; }
        public string lastSequenceId { get; set; }
        public string version { get; set; }
        public string threadType { get; set; }
        public long rosterVersion { get; set; }
        public string topic { get; set; }
    }

    public class Memberproperties
    {
        public string role { get; set; }
        public bool isReader { get; set; }
        public string memberExpirationTime { get; set; }
    }

    public class Lastmessage
    {
        public string id { get; set; }
        public string sequenceId { get; set; }
        public string clientmessageid { get; set; }
        public string version { get; set; }
        public string conversationid { get; set; }
        public string conversationLink { get; set; }
        public string type { get; set; }
        public string messagetype { get; set; }
        public string content { get; set; }
        public string from { get; set; }
        public string imdisplayname { get; set; }
        public DateTime composetime { get; set; }
        public DateTime originalarrivaltime { get; set; }
    }


}
