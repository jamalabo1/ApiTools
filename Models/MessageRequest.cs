using System.Collections.Generic;

namespace ApiTools.Models
{
    public class MessageRequest
    {
        public string To { get; set; }
        public MessageBody Body { get; set; }
    }
    public class MessageBody
    {
        public string MessageId { get; set; }
        public Dictionary<string, object> Values { get; set; }
    }
} 