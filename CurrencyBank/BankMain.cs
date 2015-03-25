using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CurrencyBank.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Wolfje.Plugins.Jist;
using Wolfje.Plugins.Jist.Framework;

namespace CurrencyBank
{
	[ApiVersion(1, 17)]
	public class BankMain : TerrariaPlugin
	{
		public static BankAccountManager Bank { get; set; }

		public static Config Config { get; set; }

		public static IDbConnection Db { get; set; }

		public static BankLog Log { get; private set; }

		public BankMain(Main game)
			: base(game)
		{
			Order = 2;
		}

		public override string Author
		{
			get { return "Enerdy"; }
		}

		public override string Description
		{
			get { return "SBPlanet Economy Bank Module"; }
		}

		public override string Name
		{
			get { return "CurrencyBank"; }
		}

		public override Version Version
		{
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				AccountHooks.AccountDelete -= OnAccountDelete;
				GeneralHooks.ReloadEvent -= OnReload;
				PlayerHooks.PlayerPostLogin -= OnPlayerPostLogin;
				JistPlugin.JavascriptFunctionsNeeded -= OnJsFunctionsNeeded;
			}
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			AccountHooks.AccountDelete += OnAccountDelete;
			GeneralHooks.ReloadEvent += OnReload;
			PlayerHooks.PlayerPostLogin += OnPlayerPostLogin;
			JistPlugin.JavascriptFunctionsNeeded += OnJsFunctionsNeeded;

		}

		void OnJsFunctionsNeeded(object sender, JavascriptFunctionsNeededEventArgs e)
		{
			JistCommands commands = new JistCommands(e.Engine);
			e.Engine.LoadLibrary(commands);
		}

		void OnInitialize(EventArgs e)
		{
			#region Config

			string cpath = Path.Combine(TShock.SavePath, "CurrencyBank", "Config.json");
			Config = Config.Read(cpath);

			#endregion

			#region Commands

			TShockAPI.Commands.ChatCommands.Add(new Command(Commands.CBank, "cbank", "currencybank")
				{
					HelpText = "Perform payments and manage bank accounts."
				});

			#endregion

			#region DB

			if (Config.StorageType.Equals("mysql", StringComparison.OrdinalIgnoreCase))
			{
				string[] host = Config.MySqlHost.Split(':');
				Db = new MySqlConnection()
				{
					ConnectionString = String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
					host[0],
					host.Length == 1 ? "3306" : host[1],
					Config.MySqlDbName,
					Config.MySqlUsername,
					Config.MySqlPassword)
				};
			}
			else if (Config.StorageType.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
				Db = new SqliteConnection(String.Format("uri=file://{0},Version=3",
					Path.Combine(TShock.SavePath, "CurrencyBank", "Database.sqlite")));
			else
				throw new InvalidOperationException("Invalid storage type!");

			#endregion

			Bank = new BankAccountManager(Db);
			Log = new BankLog(Path.Combine(TShock.SavePath, "CurrencyBank", "Logs", BankLog.GetLogName()));
		}

		async void OnAccountDelete(AccountDeleteEventArgs e)
		{
			if (await Bank.DelAsync(e.User.Name))
				TShock.Log.ConsoleInfo("[CurrencyBank] Deleted bank account for " + e.User.Name);
		}

		async void OnPlayerPostLogin(PlayerPostLoginEventArgs e)
		{
			BankAccount account;

			if ((account = await Bank.GetAsync(e.Player.UserAccountName)) == null && e.Player.Group.HasPermission(Permissions.Permit))
			{
				if (!(await Bank.AddAsync(new BankAccount(e.Player.UserAccountName))))
					TShock.Log.ConsoleError("[CurrencyBank] Unable to create bank account for " + e.Player.UserAccountName);
				else
					TShock.Log.ConsoleInfo("[CurrencyBank] Bank account created for " + e.Player.UserAccountName);
			}
		}

		async void OnReload(ReloadEventArgs e)
		{
			string cpath = Path.Combine(TShock.SavePath, "CurrencyBank", "Config.json");
			Config = Config.Read(cpath);

			if (await Bank.Reload())
				e.Player.SendSuccessMessage("[CurrencyBank] Database reloaded!");
			else
				e.Player.SendErrorMessage("[CurrencyBank] Database reload failed! Check logs for details.");

		}

		public static string FormatMoney(long money)
		{
			var sb = new StringBuilder();
			if (BankMain.Config.UseShortName)
				sb.Append(BankMain.Config.CurrencyNameShort);

			sb.Append(money);

			if (!BankMain.Config.UseShortName)
			{
				sb.Append(" ");
				if (money != 1)
					sb.Append(BankMain.Config.CurrencyNamePlural);
				else
					sb.Append(BankMain.Config.CurrencyName);
			}

			return sb.ToString();
		}
	}
}
