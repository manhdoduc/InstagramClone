using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.DTOs.post
{
    public class CreateCommentDto
    {
        [Required]
        [MaxLength(1000, ErrorMessage = "Comment content cannot exceed 1000 characters.")]
        public string Content { get; set; } = string.Empty;
    }
}
