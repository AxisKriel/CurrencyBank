using System;
using CurrencyBank.DB;
using Jint.Native;
using Wolfje.Plugins.Jist;
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
		public bool AccountExists(object accountName)
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
		public long GetBalance(object accountName)
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
		/// <param name="callback">A callback function to be run after the transaction is completed.</param>
		[JavascriptFunction("currencybank_give_async")]
		public async void GiveAsync(object accountName, object value, JsValue callback)
		{
			if (accountName == null || !(accountName is string))
				return;

			long parsedValue = 0;
			if (value == null || !Int64.TryParse(value as string, out parsedValue))
				return;

			// Value must amount to a positive influx
			if (Math.Sign(parsedValue) != 1)
				return;

			BankAccount account = BankMain.Bank.Get(accountName as string);
			if (account == null)
				return;

			bool success;
			try
			{
				await BankMain.Bank.ChangeByAsync(account.AccountName, parsedValue);
				success = true;
			}
			catch
			{
				// This and a few sanity checks above could take proper error handling
				success = false;
			}

			JistPlugin.Instance.CallFunction(callback, null, success);
		}

		/// <summary>
		/// Takes a given amount of currency from target bank account.
		/// </summary>
		/// <param name="accountName">The bank account name.</param>
		/// <br/>
		/// <param name="value">The value to be taken.</param>
		/// <br/>
		/// <param name="callback">A callback function to be run after the transaction is completed.</param>
		[JavascriptFunction("currencybank_take_async")]
		public async void TakeAsync(object accountName, object value, JsValue callback)
		{
			if (accountName == null || !(accountName is string))
				return;

			long parsedValue = 0;
			if (value == null || !Int64.TryParse(value as string, out parsedValue))
				return;

			// Value must amount to a positive influx
			if (Math.Sign(parsedValue) != 1)
				return;

			BankAccount account = BankMain.Bank.Get(accountName as string);
			if (account == null)
				return;

			bool success;
			try
			{
				await BankMain.Bank.ChangeByAsync(account.AccountName, -parsedValue);
				success = true;
			}
			catch
			{
				// This and a few sanity checks above could take proper error handling
				success = false;
			}

			JistPlugin.Instance.CallFunction(callback, null, success);
		}
	}
}