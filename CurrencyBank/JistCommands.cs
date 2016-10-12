using System;
using System.Threading.Tasks;
using CurrencyBank.DB;
using Wolfje.Plugins.Jist.Framework;

namespace CurrencyBank
{
	[JavascriptProvidesAttribute("currencybank")]
	public class JistCommands
	{
		/// <summary>
		/// Determines whether a bank account by a given name exists.
		/// </summary>
		/// <param name="accountName">The bank account name.</param>
		/// <returns>True if an account by the given <paramref name="accountName"/> exists.</returns>
		[JavascriptFunction("currencybank_account_exists")]
		public static bool AccountExists(object accountName)
		{
			if (accountName == null || !(accountName is string))
				return false;

			return BankMain.Bank.Get(accountName as string) != null;
		}

		/// <summary>
		/// Retrieves a bank account's balance.
		/// </summary>
		/// <param name="accountName">The bank account name.</param>
		/// <returns>The account's balance, or -1 if it couldn't be retrieved.</returns>
		[JavascriptFunction("currencybank_get_balance")]
		public static long GetBalance(object accountName)
		{
			if (accountName == null || !(accountName is string))
				return -1;

			BankAccount account = BankMain.Bank.Get(accountName as string);
			if (account == null)
				return -1;

			return account.Balance;
		}

		/// <summary>
		/// Increments target bank account's balance by a given amount.
		/// </summary>
		/// <param name="accountName">The bank account name.</param>
		/// <br/>
		/// <param name="value">The value to be added.</param>
		/// <br/>
		/// <returns>
		/// True if the transaction completed successfully.
		/// False is returned in one of the following situations:
		/// * <paramref	name="accountName"/> is null or not a string;
		/// * <paramref name="value"/> is null, negative or may not be parsed to an Int64;
		/// * A bank account could not be found by the given name.
		/// </returns>
		[JavascriptFunction("currencybank_give")]
		public static bool Give(object accountName, object value)
		{
			if (accountName == null || !(accountName is string))
				return false;

			long parsedValue = 0;
			if (value == null || !Int64.TryParse(value as string, out parsedValue))
				return false;

			// Value must amount to a positive influx
			if (Math.Sign(parsedValue) != 1)
				return false;

			BankAccount account = BankMain.Bank.Get(accountName as string);
			if (account == null)
				return false;

			try
			{
				Task.Run(() => BankMain.Bank.ChangeByAsync(account.AccountName, parsedValue));
				return true;
			}
			catch
			{
				// This and a few sanity checks above could take proper error handling
				return false;
			}
		}

		/// <summary>
		/// Takes a given amount of currency from target bank account.
		/// </summary>
		/// <param name="accountName">The bank account name.</param>
		/// <br/>
		/// <param name="value">The value to be taken.</param>
		/// <br/>
		/// <returns>
		/// True if the transaction completed successfully.
		/// False is returned in one of the following situations:
		/// * <paramref	name="accountName"/> is null or not a string;
		/// * <paramref name="value"/> is null, negative or may not be parsed to an Int64;
		/// * A bank account could not be found by the given name.
		/// </returns>
		[JavascriptFunction("currencybank_take")]
		public static bool Take(object accountName, object value)
		{
			if (accountName == null || !(accountName is string))
				return false;

			long parsedValue = 0;
			if (value == null || !Int64.TryParse(value as string, out parsedValue))
				return false;

			// Value must amount to a positive influx
			if (Math.Sign(parsedValue) != 1)
				return false;

			BankAccount account = BankMain.Bank.Get(accountName as string);
			if (account == null)
				return false;

			try
			{
				Task.Run(() => BankMain.Bank.ChangeByAsync(account.AccountName, -parsedValue));
				return true;
			}
			catch
			{
				// This and a few sanity checks above could take proper error handling
				return false;
			}
		}
	}
}