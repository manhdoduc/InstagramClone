using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.DTOs.Chats
{
    public class CreateGroupDto
    {
        public string GroupName { get; set; } = string.Empty;
        public List<string> MemberIds { get; set; } = [];
    }
}
