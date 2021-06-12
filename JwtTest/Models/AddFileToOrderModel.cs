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
    public class AddFileToOrderModel
    {
		public int? Id { get; set; }
		[DisplayName("Описание")]
		public string Description { get; set; }
		[DisplayName("Похожие работы")]
		public string Content { get; set; }
		[DisplayName("Заказчик")]
		public Person Customer { get; set; }
		[DisplayName("Цена")]
		public decimal Price { get; set; }
		[DisplayName("Статус")]
		public Status OrderStatus { get; set; }
		[DisplayName("Предлагаемый срок окончания работ")]
		public DateTime DeadLine { get; set; }
		public string ConfirmPassword { get; set; }
		[DisplayName("Статус подтверждения администратором")]
		public bool Confirm { get; set; }
		[DisplayName("Статус принятия условий выполенения заказа")]
		public bool Accepted { get; set; }
		[DisplayName("Загрузите Аватар")]
        [DataType(DataType.Upload)]
        public IFormFile File { get; set; }
    }
}
