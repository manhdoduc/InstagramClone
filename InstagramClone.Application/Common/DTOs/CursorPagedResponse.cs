using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Common.DTOs{
    public class CursorPagedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();

        // di?m neo cho request ti?p theo 
        public DateTime? NextCursor { get; set; }

        // C? báo hi?u cho FE bi?t d? hi?n/?n nút "T?i thêm" (ho?c loading spinner)
        public bool HasNextPage { get; set; }
    }
}
