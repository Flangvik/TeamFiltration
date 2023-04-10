using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeamFiltration.Models.Teams;

namespace TeamFiltration.Models.TeamFiltration
{
    public class Conversations
    {

        public Conversations(ChatLogResp chatsLogs, Conversation conversation)
        {


            this.Id = conversation.id;
            this.Title = conversation.threadProperties.topic;
            this.chatMessagesArray = chatsLogs.messages.Select(x => (ChatMessages)x).ToList();

        }

        public string Id { get; set; }
        public string Title { get; set; }

        public List<ChatMessages> chatMessagesArray { get; set; }
    }

 

    public class FileData
    {
        public string type { get; set; }
        public int version { get; set; }
        public string id { get; set; }
        public string baseUrl { get; set; }

        public string title { get; set; }
        public string state { get; set; }
        public string objectUrl { get; set; }
    
        public string itemid { get; set; }
        public string fileName { get; set; }
        public string fileType { get; set; }
        public Fileinfo fileInfo { get; set; }
        public Botfileproperties botFileProperties { get; set; }
        public int sourceOfFile { get; set; }
        public Filepreview filePreview { get; set; }
        public Filechicletstate fileChicletState { get; set; }
    }

 

    public class Fileinfo
    {
        public string fileUrl { get; set; }
        public string siteUrl { get; set; }
        public string serverRelativeUrl { get; set; }
    }

    public class Botfileproperties
    {
    }

    public class Filepreview
    {
        public string previewUrl { get; set; }
    }

    public class Filechicletstate
    {
        public string serviceName { get; set; }
        public string state { get; set; }
    }


    public class ChatMessages
    {
        public string Id { get; set; }
        public string sequenceId { get; set; }
        public string FromURL { get; set; }
        public string FromDisplayName { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public string Title { get; set; }
        public PropertiesSub Properties { get; set; }
        public List<FileData> FileObject { get; set; }

        public static explicit operator ChatMessages(Message v)
        {

            return new ChatMessages()
            {
                Id = v.id,
                FromDisplayName = v.imdisplayname,
                ArrivalTime = v.originalarrivaltime,
                FromURL = v.from,
                sequenceId = v.sequenceId,
                Type = v.type,
                Content = v.content,
                ContentType = v.contenttype,
                FileObject = (v.properties?.files != null) ? JsonConvert.DeserializeObject<List<FileData>>(v.properties?.files) : null,
                Properties = v.properties,
               

            };
        }
    }
}
