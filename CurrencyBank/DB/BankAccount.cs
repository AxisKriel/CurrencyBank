using System.Threading.Tasks;

namespace CurrencyBank.DB
{
	public class BankAccount
	{
		public string AccountName { get; set; }

		public long Balance { get; set; }

		public int ID { get; private set; }

		public BankAccount(int id = 0)
		{
			ID = id;
		}

		public BankAccount(string accountName, long startingMoney = 0)
		{
			Task.Run(async () => ID = await BankMain.Bank.GenID());
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
