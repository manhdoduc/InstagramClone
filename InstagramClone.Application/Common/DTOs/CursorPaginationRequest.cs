using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Common.DTOs;
    public class CursorPaginationRequest
    {
        const int maxPageSize = 50;
        const int defaultPageSize = 10;
        private int _pageSize = defaultPageSize;

        // Tu dong chuan hoa du lieu khi Client truyen vao
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > maxPageSize ? maxPageSize : (value <= 0 ? defaultPageSize : value);
        }

        public DateTime? Cursor { get; set; }
    }
