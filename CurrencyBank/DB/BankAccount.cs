using System.Threading.Tasks;
using TShockAPI;

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

		// The Server Account, currently used for... notices
		public static BankAccount Server = new BankAccount()
		{
			AccountName = "Server",
		};

		public static async Task<BankAccount> Create(string accountName, long startingMoney = 0)
		{
			int id = await BankMain.Bank.GenID();

			if (id == -1)
			{
				// This means the max amount of accounts has been reached
				return null;
			}
			else if (id == -2)
			{
				// This means maxtries was reached while trying to find an unused ID. Shouldn't happen, but should be logged if it does.
				TShock.Log.ConsoleInfo($"currencybank: maxtries was reached while trying to create bank account '{accountName}'.");
				return null;
			}
			
			return new BankAccount(id)
			{
				AccountName = accountName,
				Balance = startingMoney
			};
		}
	}
}
