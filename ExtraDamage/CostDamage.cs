using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraDamage
{
	class CostDamage
	{
		public string Name { get; }
		public int Cost { get; }
		public int Damage { get; }
		public bool isPicked { get;set; }

		public CostDamage(string name, int cost, int damage)
		{
			Name = name;
			Cost = cost;
			Damage = damage;
			isPicked = false;
		}

	}
}
