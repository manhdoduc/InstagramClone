using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.DTOs.post
{
    public class CursorPaginationRequest
    {
        const int maxPageSize = 50;
        const int defaultPageSize = 10;
        private int _pageSize = defaultPageSize;

        // Tự động chuẩn hóa dữ liệu khi Client truyền vào
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > maxPageSize ? maxPageSize : (value <= 0 ? defaultPageSize : value);
        }

        public DateTime? Cursor { get; set; }
    }
}
