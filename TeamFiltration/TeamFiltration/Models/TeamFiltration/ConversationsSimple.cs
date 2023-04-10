using Microsoft.VisualBasic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeamFiltration.Models.Teams;

namespace TeamFiltration.Models.TeamFiltration
{
    public class ConversationsSimple
    {
        public string Title { get; set; }
        public string id { get; set; }
        public List<MessagesSimple> Messages { get; set; }

        public static explicit operator ConversationsSimple((Conversations conversation, WorkingWithResp workingWithResp) inputParams)
        {


            //Remove the ones that are Teams action , contains :orgid:
            var buffMessages = inputParams.conversation.chatMessagesArray.Select(chatmsg => (MessagesSimple)(chatmsg, inputParams.workingWithResp));
            return new ConversationsSimple()
            {
                Title = inputParams.conversation.Title,
                id = Helpers.Generic.StringToGUID(inputParams.conversation.Id).ToString(),
                Messages = buffMessages.Where(x => 
                !x.Content.Contains(":orgid:") 
                && !string.IsNullOrEmpty(x.Content)
                && !x.Content.Equals("<partlist alt =\"\"></partlist>")).OrderBy(x => x.Sent).ToList()

                

            };
        }
    }
    public class MessagesSimple
    {
        public string FromDisplayName { get; set; }
        public string FromEmail { get; set; }
        public string FromId { get; set; }
        public DateTime Sent { get; set; }
        public string Content { get; set; }
    
        public List<string> Attachments { get; set; }

        public static explicit operator MessagesSimple((ChatMessages chatMessage, WorkingWithResp workingWithResp) inputParams)
        {

            var fromId = "";
            var fromEmailBuff = "";
            var conversationAttachemnts = new List<string>() { };
            try
            {
                fromId = inputParams.chatMessage.FromURL.Split(':')[3];
                fromEmailBuff = inputParams.workingWithResp.value.Where(x => x.objectId.Equals(fromId)).Select(x => x.email).FirstOrDefault();
            }
            catch (Exception ex)
            {

            }

            try
            {
                if (inputParams.chatMessage.FileObject != null)
                    conversationAttachemnts = inputParams.chatMessage.FileObject.Select(x => x.fileName).ToList();
            }
            catch (Exception)
            {

                throw;
            }

            return new MessagesSimple()
            {
                FromDisplayName = inputParams.chatMessage.FromDisplayName,
                FromEmail = fromEmailBuff,
                FromId = fromId,
                Sent = inputParams.chatMessage.ArrivalTime,
                Content = inputParams.chatMessage.Content,
                Attachments = conversationAttachemnts,

            };
        }
    }
}
