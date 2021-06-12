using JwtTest.EF;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace JwtTest.Models
{
    public class BlogModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Заголовок является обязательным")]
        [DisplayName("Заголовок")]
        public string Title { get; set; }
        
                
        [DisplayName("Дата и время")]
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public DateTime Time { get; set; }
                
        [DisplayName("Комментарий")]
        public string Comment { get; set; }
        
        [DisplayName("Изображение")]
        public IFormFile Picture { get; set; }
    }
}
