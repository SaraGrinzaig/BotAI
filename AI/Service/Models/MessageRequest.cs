using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    public class MessageRequest
    {
        public string Message { get; set; }
        public UserDetails UserDetails { get; set; }
        public bool IncludeOrderInfo { get; set; } // true to include order info, false otherwise
        public string ConversationHistory { get; set; } // Add this field

    }
}
