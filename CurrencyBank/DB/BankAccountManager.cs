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
		}

		public async Task<bool> AddAsync(BankAccount account)
		{
			if (await GetCountAsync() >= MaxAccounts)
				return false;

			lock (syncLock)
			{
				return db.Query("INSERT INTO `BankAccounts` (`ID`, `AccountName`, `Balance`) VALUES (@0, @1, @2)",
					account.ID, account.AccountName, account.Balance) == 1;
			}
		}

		public async Task ChangeByAsync(string accountIdent, long value)
		{
			BankAccount account = await GetAsync(accountIdent);
			if (account == null)
				throw new NullReferenceException();

			await Task.Run(() =>
			{
				// Balances can't be negative at the moment
				account.Balance = Math.Max(0, account.Balance + value);
				if (db.Query("UPDATE `BankAccounts` SET `Balance` = @1 WHERE `ID` = @0", account.ID, account.Balance) != 1)
					throw new InvalidOperationException();
			});
		}

		public Task<bool> DelAsync(string accountName)
		{
			return Task.Run(() =>
			{
				return db.Query("DELETE FROM `BankAccounts` WHERE `AccountName` = @0", accountName) == 1;
			});
		}

		public Task<BankAccount> GetAsync(string accountIdent)
		{
			if (String.IsNullOrWhiteSpace(accountIdent))
				return null;

			return Task.Run(() =>
			{
				int id;
				bool isNum = Int32.TryParse(accountIdent, out id);
				if (isNum)
				{
					using (var result = db.QueryReader("SELECT * FROM `BankAccounts` WHERE `ID` = @0", id))
					{
						if (result.Read())
						{
							return new BankAccount(id)
							{
								AccountName = result.Get<string>("AccountName"),
								Balance = result.Get<long>("Balance")
							};
						}
					}
				}
				else
				{
					using (var result = db.QueryReader("SELECT * FROM `BankAccounts` WHERE `AccountName` = @0", accountIdent))
					{
						if (result.Read())
						{
							return new BankAccount(result.Get<int>("ID"))
							{
								AccountName = accountIdent,
								Balance = result.Get<long>("Balance")
							};
						}
					}
				}
				return null;
			});
		}

		public Task<int> GetCountAsync()
		{
			return Task.Run(() =>
			{
				using (var result = db.QueryReader("SELECT COUNT(ID) AS Count FROM `BankAccounts`"))
				{
					if (result.Read())
						return result.Get<int>("Count");
					else
						throw new InvalidOperationException();
				}
			});
		}

		public Task<List<int>> GetUsedIDs()
		{
			return Task.Run(() =>
			{
				using (var result = db.QueryReader("SELECT `ID` FROM `BankAccounts`"))
				{
					List<int> values = new List<int>();
					while (result.Read())
					{
						values.Add(result.Get<int>("ID"));
					}
					return values;
				}
			});
		}

		public async Task<int> GenID()
		{
			List<int> used = await GetUsedIDs();
			// If the account limit is reached, -1 is returned as an error code
			if (used.Count >= MaxAccounts)
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
	}
}
