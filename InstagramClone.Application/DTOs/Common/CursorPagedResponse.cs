using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.DTOs.Common
{
    public class CursorPagedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();

        // điểm neo cho request tiếp theo 
        public DateTime? NextCursor { get; set; }

        // Cờ báo hiệu cho FE biết để hiện/ẩn nút "Tải thêm" (hoặc loading spinner)
        public bool HasNextPage { get; set; }
    }
}
