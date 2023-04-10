using System;
using System.Collections.Generic;
using System.Text;
using TeamFiltration.Models.TeamFiltration;

namespace TeamFiltration.Helpers
{
    public class HTMLChat
    {
        private string outTemplate { get; set; }
        private string inTemplate { get; set; }
        private string pageTemplate { get; set; }
        private string fromUser { get; set; }

        public HTMLChat(string fromUser, string title)
        {

            outTemplate = @"<li class=""out"">
                            <div class=""chat-img"">
                            </div>
                            <div class=""chat-body"">
                                <div class=""chat-message"">
                                    <h5>#DISPLAYNAME# - #TIMESTAMP#</h5>
                                    #MESSAGE#
                                </div>
                            </div>
                        </li>";


            inTemplate = @"<li class=""in"">
                            <div class=""chat-body"">
                                <div class=""chat-message"">
                                    <h5>#DISPLAYNAME# - #TIMESTAMP#</h5>
                                    #MESSAGE#
                                </div>
                            </div>
                        </li>";
            pageTemplate = @"
<html>
<head>
<style>
body{
    background:#F5F5F5;   
}
.chat-list {
    padding: 0;
    font-size: .8rem;
}

.chat-list li {
    margin-bottom: 10px;
    overflow: auto;
    color: #242424;
}

.chat-list .chat-img {
    float: left;
    width: 48px;
}

.chat-list .chat-img img {
    -webkit-border-radius: 5px;
    -moz-border-radius: 5px;
    border-radius: 5px;
    width: 100%;
}

.chat-list .chat-message {
    -webkit-border-radius: 5px;
    -moz-border-radius: 5px;
    border-radius: 5px;
    background: #FFFFFF;
    display: inline-block;
    padding: 10px 20px;
    position: relative;
}

.chat-list .chat-message:before {
    position: absolute;
    top: 15px;
    width: 0;
    height: 0;
}

.chat-list .chat-message h5 {
    margin: 0 0 5px 0;
    font-weight: 600;
    line-height: 100%;
    font-size: .9rem;
}

.chat-list .chat-message p {
    line-height: 18px;
    margin: 0;
    padding: 0;
}

.chat-list .chat-body {
    margin-left: 20px;
    float: left;
    width: 70%;
}

.chat-list .in .chat-message:before {
    left: -12px;
    border-bottom: 20px solid transparent;
    border-right: 20px solid #5a99ee;
}

.chat-list .out .chat-img {
    float: right;
}

.chat-list .out .chat-body {
    float: right;
    margin-right: 20px;
    text-align: right;
}

.chat-list .out .chat-message {
    background: #E8EBFA;
}

.chat-list .out .chat-message:before {
    right: -12px;
    border-bottom: 20px solid transparent;
    border-left: 20px solid #E8EBFA;
}

.card .card-header:first-child {
    -webkit-border-radius: 0.3rem 0.3rem 0 0;
    -moz-border-radius: 0.3rem 0.3rem 0 0;
    border-radius: 0.3rem 0.3rem 0 0;
}
.card .card-header {
    background: #17202b;
    border: 0;
    font-size: 1rem;
    padding: .65rem 1rem;
    position: relative;
    font-weight: 600;
    color: #ffffff;
}

.content{
    margin-top:40px;    
}
</style>

</head>
<body>
<div class=""container content"">
    <div class=""row"">
        <div class=""col-xl-6 col-lg-6 col-md-6 col-sm-12 col-12"">
        	<div class=""card"">
        		<div class=""card-header"">#TITLE#</div>
        		<div class=""card-body height3"">
        			<ul class=""chat-list"">
        			#PAGE#
        				
        			</ul>
        		</div>
        	</div>
        </div>
</div>
</body>
</html>".Replace("#TITLE#",title);
            this.fromUser = fromUser;
        }


        public string GenerateChat(ConversationsSimple inputConverastion)
        {
            StringBuilder message = new StringBuilder();

            foreach (var item in inputConverastion.Messages)
            {
                if (item.FromDisplayName == fromUser)
                    message.AppendLine(
                        outTemplate
                        .Replace("#DISPLAYNAME#", item.FromDisplayName)
                        .Replace("#MESSAGE#", item.Content)
                        .Replace("#TIMESTAMP#", item.Sent.ToString())
                        );
                else
                    message.AppendLine(
                       inTemplate
                       .Replace("#DISPLAYNAME#", item.FromDisplayName)
                       .Replace("#MESSAGE#", item.Content)
                       .Replace("#TIMESTAMP#", item.Sent.ToString())
                       );
            }

            return pageTemplate.Replace("#PAGE#", message.ToString());
        }
    }
}
