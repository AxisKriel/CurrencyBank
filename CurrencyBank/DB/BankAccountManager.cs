using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace CurrencyBank.DB
{
	public class BankAccountManager
	{
		private IDbConnection db;
		private object syncLock = new object();

		public static readonly int MaxAccounts = 999999;

		private List<BankAccount> bankAccounts = new List<BankAccount>();

		public BankAccountManager(IDbConnection db)
		{
			this.db = db;

			var sql = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ?
				(IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			bool table = sql.EnsureTableStructure(new SqlTable("BankAccounts",
				new SqlColumn("ID", MySqlDbType.Int32) { Primary = true },
				new SqlColumn("AccountName", MySqlDbType.Text),
				new SqlColumn("Balance", MySqlDbType.Int64)));

			if (table)
				TShock.Log.ConsoleInfo("currencybank-db: created table 'BankAccounts'");

			Task.Run(() => Reload());
		}

		public Task<bool> AddAsync(BankAccount account)
		{
			return Task.Run(() =>
				{
					if (bankAccounts.Count >= MaxAccounts)
						return false;

					lock (syncLock)
					{
						bankAccounts.Add(account);
						return db.Query("INSERT INTO `BankAccounts` (`ID`, `AccountName`, `Balance`) VALUES (@0, @1, @2)",
							account.ID, account.AccountName, account.Balance) == 1;
					}
				});
		}

		public async Task ChangeByAsync(string accountIdent, long value)
		{
			BankAccount account = await GetAsync(accountIdent);
			if (account == null)
				throw new NullReferenceException();

			await Task.Run(() =>
				{
					lock (syncLock)
					{
						// Balances can't be negative at the moment
						account.Balance = Math.Max(0, account.Balance + value);
						if (db.Query("UPDATE `BankAccounts` SET `Balance` = @1 WHERE `ID` = @0", account.ID, account.Balance) != 1)
							throw new InvalidOperationException();
					}
				});
		}

		public Task<bool> DelAsync(string accountName)
		{
			return Task.Run(() =>
				{
					lock (syncLock)
					{
						bankAccounts.RemoveAll(a => a.AccountName == accountName);
						return db.Query("DELETE FROM `BankAccounts` WHERE `AccountName` = @0", accountName) == 1;
					}
				});
		}

		public Task<bool> Reload()
		{
			return Task.Run(() =>
				{
					try
					{
						bankAccounts.Clear();
						using (var result = db.QueryReader("SELECT * FROM BankAccounts"))
						{
							while (result.Read())
							{
								BankAccount account = new BankAccount(result.Get<int>("ID"));
								account.AccountName = result.Get<string>("AccountName");
								account.Balance = result.Get<long>("Balance");
								bankAccounts.Add(account);
							}
						}
						return true;
					}
					catch (Exception ex)
					{
						TShock.Log.ConsoleError("Error while loading CurrencyBank's database. Check logs for details.");
						TShock.Log.Error(ex.ToString());
						return false;
					}
				});
		}

		public int GenID()
		{
			var used = bankAccounts.Select(a => a.ID).ToList();

			// If the account limit is reached, -1 is returned as an error code
			if (bankAccounts.Count >= MaxAccounts)
				return -1;

			Random rand = new Random();
			int container = 0;
			int maxtries = 10000;
			for (int i = 0; i < maxtries; i++)
			{
				container = rand.Next(0, MaxAccounts) + 1;
				if (!used.Contains(container))
					return container;
			}

			// If for whatever reason maxtries triggers, return -2 as an error code
			return -2;
		}

		public Task<BankAccount> GetAsync(string accountIdent)
		{
			if (String.IsNullOrWhiteSpace(accountIdent))
				return null;
			int id;
			if (Int32.TryParse(accountIdent, out id))
				return Task.Run(() => bankAccounts.Find(a => a.ID == id));
			else
				return Task.Run(() => bankAccounts.Find(a => a.AccountName == accountIdent));
		}
	}
}
