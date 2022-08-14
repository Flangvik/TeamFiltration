using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeamFiltration.Models.Teams;

namespace TeamFiltration.Models.TeamFiltration
{
    public class ConversationsSimple
    {
        public string id { get; set; }
        public List<MessagesSimple> Messages { get; set; }

        public static explicit operator ConversationsSimple((Conversations conversation, WorkingWithResp workingWithResp) inputParams)
        {

            return new ConversationsSimple()
            {
                id = Helpers.Generic.StringToGUID(inputParams.conversation.Id).ToString(),
                Messages = inputParams.conversation.chatMessagesArray.Select(chatmsg => (MessagesSimple)(chatmsg, inputParams.workingWithResp)).ToList()

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

        public static explicit operator MessagesSimple((ChatMessages chatMessage, WorkingWithResp workingWithResp) inputParams)
        {
            var fromId = "";
            var fromEmailBuff = "";
            try
            {
                fromId = inputParams.chatMessage.FromURL.Split(':')[3];
                fromEmailBuff = inputParams.workingWithResp.value.Where(x => x.objectId.Equals(fromId)).Select(x => x.email).FirstOrDefault();
            }
            catch (Exception ex)
            {

            }
            return new MessagesSimple()
            {
                FromDisplayName = inputParams.chatMessage.FromDisplayName,
                FromEmail = fromEmailBuff,
                FromId = fromId,
                Sent = inputParams.chatMessage.ArrivalTime,
                Content = inputParams.chatMessage.Content

            };
        }
    }
}
