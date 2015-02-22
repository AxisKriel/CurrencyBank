using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyBank.DB
{
	public class BankAccount
	{
		public string AccountName { get; set; }

		public long Balance { get; set; }

		private int _id;
		public int ID
		{
			get { return _id; }
		}

		public BankAccount(int id = 0)
		{
			_id = id;
		}

		public BankAccount(string accountName, long startingMoney = 0)
		{
			_id = BankMain.Bank.GenID();
			AccountName = accountName;
			Balance = startingMoney;
		}

		// The Server Account, currently used for... notices
		public static BankAccount Server = new BankAccount()
		{
			AccountName = "Server",
		};
	}
}
