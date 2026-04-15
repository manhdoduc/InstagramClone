using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.DTOs.Chats
{
    public class CursorPaginationRequestMessage
    {
        const int maxChatSize = 50;
        const int defaultChatSize = 20;
        private int _chatSize = defaultChatSize;

        // Tự động chuẩn hóa dữ liệu khi Client truyền vào
        public int ChatSize
        {
            get => _chatSize;
            set => _chatSize = value > maxChatSize ? maxChatSize : (value <= 0 ? defaultChatSize : value);
        }

        public DateTime? Cursor { get; set; }
    }
}
