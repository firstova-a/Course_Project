using JwtTest.EF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static JwtTest.EF.ArtOrder;

namespace JwtTest.Models
{
	public class OrderModel
	{
		public int? Id { get; set; }
		[Required(ErrorMessage = "Описание является обязательным")]
        [DisplayName("Описание")]
		public string Description { get; set; }
		[DisplayName("Похожие работы")]
		public string Content { get; set; }
		[DisplayName("Заказчик")]
		public Person Customer { get; set; }
		[DisplayName("Цена")]
		public int Price { get; set; }
		[DisplayName("Статус")]
		public Status OrderStatus { get; set; }
		[DisplayName("Предлагаемый срок окончания работ")]
		public DateTime DeadLine { get; set; }
	}
}
