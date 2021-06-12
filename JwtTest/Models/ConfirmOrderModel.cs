using JwtTest.EF;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static JwtTest.EF.ArtOrder;

namespace JwtTest.Models
{
    public class ConfirmOrderModel
    {
        public int? Id { get; set; }
        [Required(ErrorMessage = "Описание заказа является обязательным")]
        [DisplayName("Описание заказа")]
        public string Description { get; set; }
        [DisplayName("Похожие работы")]
        public string Content { get; set; }
        [DisplayName("Цена")]
        public decimal Price { get; set; }
        [DisplayName("Статус")]
        public Status OrderStatus { get; set; }
        [DisplayName("Предлагаемый срок окончания работ")]
		public DateTime DeadLine { get; set; }
        [DisplayName("Статус подтверждения администратором")]
		public bool Confirm { get; set; }
        [DisplayName("Статус принятия условий выполенения заказа")]
        public bool Accepted { get; set; }
    }
}
