using System;
using System.Collections.Generic;
using System.Text;

namespace JwtTest.EF
{
	public class ArtOrder
	{
		public int Id { get; set; }

		public string Description { get; set; }
		public string Content { get; set; }
		public decimal Price { get; set; }
		public Status OrderStatus { get; set; }
		public DateTime DeadLine { get; set; }
		public string PathToFile { get; set; }
		public virtual Person Customer { get; set; }

		public bool Confirm { get; set; }
		public bool Accepted { get; set; }

		public enum Status
		{
			registred,
			accepted,
			rejected,
			inProgress,
			done
		}

		public ArtOrder()
		{
			this.OrderStatus = Status.registred;
			Confirm = false;
			Accepted = false;
		}
	}
}
