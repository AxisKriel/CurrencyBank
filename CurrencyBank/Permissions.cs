using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyBank
{
	public class Permissions
	{
		public static readonly string Create = "cbank.admin.create";
		public static readonly string Delete = "cbank.admin.delete";
		public static readonly string Give = "cbank.admin.give";
		public static readonly string Take = "cbank.admin.take";
		public static readonly string Pay = "cbank.common.pay";
		public static readonly string Permit = "cbank.common.permit";
		public static readonly string Convert = "cbank.seconomy.convert";
	}
}
