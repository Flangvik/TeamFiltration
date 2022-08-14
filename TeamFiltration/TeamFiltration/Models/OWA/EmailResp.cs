using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TeamFiltration.Models.OWA
{

    public class EmailRespBody
    {
        [JsonIgnore]
        public string odatacontext { get; set; }
        public List<EmailObjectBody> value { get; set; }
        [JsonIgnore]
        public string odatanextLink { get; set; }
    }

    public class EmailObjectBody
    {
        [JsonIgnore]
        public string odataid { get; set; }
        [JsonIgnore]
        public string odataetag { get; set; }
        public string Id { get; set; }
        public Body Body { get; set; }
        public string odatatype { get; set; }
    }

    public class Body
    {
        public string ContentType { get; set; }
        public string Content { get; set; }
    }


    public class EmailResp
    {
        [JsonIgnore]
        public string odatacontext { get; set; }
        [JsonIgnore]
        public string odataid { get; set; }
        [JsonIgnore]
        public string odataetag { get; set; }
        [JsonIgnore]
        public string Id { get; set; }
        public string CreatedDateTime { get; set; }
        public string LastModifiedDateTime { get; set; }
        public string ChangeKey { get; set; }
        public object[] Categories { get; set; }
        public DateTime? ReceivedDateTime { get; set; }
        public string SentDateTime { get; set; }
        public bool HasAttachments { get; set; }
        [JsonIgnore]
        public string InternetMessageId { get; set; }
        public string Subject { get; set; }
        public string BodyPreview { get; set; }
        public string Importance { get; set; }
        [JsonIgnore]
        public string ParentFolderId { get; set; }
        [JsonIgnore]
        public string ConversationId { get; set; }
        [JsonIgnore]
        public string ConversationIndex { get; set; }
        public object IsDeliveryReceiptRequested { get; set; }
        public bool IsReadReceiptRequested { get; set; }
        public bool IsRead { get; set; }
        public bool IsDraft { get; set; }
        [JsonIgnore]
        public string WebLink { get; set; }
        public string InferenceClassification { get; set; }
        public MailBody Body { get; set; }
        public MailSender Sender { get; set; }
        public MailFrom From { get; set; }
        public MailTorecipient[] ToRecipients { get; set; }
        public object[] CcRecipients { get; set; }
        public object[] BccRecipients { get; set; }
        public object[] ReplyTo { get; set; }
        public MailFlag Flag { get; set; }
    }

    public class MailBody
    {
        public string ContentType { get; set; }
        public string Content { get; set; }
    }

    public class MailSender
    {
        public MailEmailaddress EmailAddress { get; set; }
    }

    public class MailEmailaddress
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class MailFrom
    {
        public MailEmailaddress1 EmailAddress { get; set; }
    }

    public class MailEmailaddress1
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class MailFlag
    {
        public string FlagStatus { get; set; }
    }

    public class MailTorecipient
    {
        public MailEmailaddress2 EmailAddress { get; set; }
    }

    public class MailEmailaddress2
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }


}
