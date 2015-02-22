using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace CurrencyBank.DB
{
	public class BankAccountManager
	{
		private IDbConnection db;

		public static readonly int MaxAccounts = 999999;

		public List<BankAccount> BankAccounts = new List<BankAccount>();

		public BankAccountManager(IDbConnection db)
		{
			this.db = db;

			var sql = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ?
				(IQueryBuilder)new SqliteQueryCreator() : (IQueryBuilder)new MysqlQueryCreator());

			sql.EnsureExists(new SqlTable("BankAccounts",
				new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true },
				new SqlColumn("AccountName", MySqlDbType.Text),
				new SqlColumn("Balance", MySqlDbType.Int64)));

			Task.Run(() => Reload());
		}

		public async Task<bool> AddAsync(BankAccount account)
		{
			try
			{
				if (BankAccounts.Count >= MaxAccounts)
					return false;

				return await Task.Run(() =>
					{
						BankAccounts.Add(account);
						return db.Query("INSERT INTO BankAccounts (ID, AccountName, Balance) VALUES (@0, @1, @2)",
							account.ID, account.AccountName, account.Balance) == 1;
					});
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return false;
			}
		}

		public async void ChangeByAsync(string accountIdent, long value)
		{
			BankAccount account = await FindAccount(accountIdent);
			if (account == null)
				throw new NullReferenceException();

			await Task.Run(() =>
				{
					// Balances can't be negative at the moment
					account.Balance = Math.Max(0, account.Balance + value);
					if (db.Query("UPDATE BankAccounts SET Balance = @1 WHERE ID = @0", account.ID, value) != 1)
						throw new InvalidOperationException();
				});
		}

		public Task<bool> DelAsync(string accountName)
		{
			return Task.Run(() =>
				{
					try
					{
						BankAccounts.RemoveAll(a => a.AccountName == accountName);
						return db.Query("DELETE FROM BankAccounts WHERE AccountName = @0", accountName) == 1;
					}
					catch (Exception ex)
					{
						Log.Error(ex.ToString());
						return false;
					}
				});
		}

		public Task<bool> Reload()
		{
			return Task.Run(() =>
				{
					try
					{
						BankAccounts.Clear();
						using (var result = db.QueryReader("SELECT * FROM BankAccounts"))
						{
							while (result.Read())
							{
								BankAccount account = new BankAccount(result.Get<int>("ID"));
								account.AccountName = result.Get<string>("AccountName");
								account.Balance = result.Get<long>("Balance");
								BankAccounts.Add(account);
							}
						}
						return true;
					}
					catch (Exception ex)
					{
						Log.ConsoleError("Error while loading CurrencyBank's database.\nCheck logs for details.");
						Log.Error(ex.ToString());
						return false;
					}
				});
		}

		public int GenID()
		{
			var used = BankAccounts.Select(a => a.ID).ToList();

			// If the account limit is reached, -1 is returned as an error code
			if (BankAccounts.Count >= MaxAccounts)
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

		public Task<BankAccount> FindAccount(string accountIdent)
		{
			int id;
			if (int.TryParse(accountIdent, out id))
				return Task.Run(() => BankAccounts.Find(a => a.ID == id));
			else
				return Task.Run(() => BankAccounts.Find(a => a.AccountName == accountIdent));
		}
	}
}
