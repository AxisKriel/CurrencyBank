using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CurrencyBank.DB;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Text.RegularExpressions;

namespace CurrencyBank
{
	public class Commands
	{
		public static async void CBank(CommandArgs args)
		{
			BankAccount account;

			var regex = new Regex(@"^(?:\w+|\w+ (bal(?:ance)?|give|help|info|pay|take)(?: ""(.+)""| ([^\s]+?))?(?: (\d*))?(?: (.+))?)$");
			// Regex Groups:
			// 0 - The entire match
			// 1 - The switch
			// 2 - AccountIdent if using quotes
			// 3 - AccountIdent if using non-whitespace
			// 4 - The numeric value (currency amount)
			// 5 - The custom message
			Match match = regex.Match(args.Message);
			if (!match.Success)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}cbank [switch] [params...]",
					TShock.Config.CommandSpecifier);
				args.Player.SendInfoMessage("Available cbank switches: bal, give, help, info, pay, take");
			}
			else
			{
				if (string.IsNullOrWhiteSpace(match.Groups[1].Value))
					SendInfo(args.Player, await BankMain.Bank.FindAccount(args.Player.UserAccountName));
				else
				{
					switch (match.Groups[1].Value)
					{
						#region Balance

						case "bal":
						case "balance":
							if (!args.Player.RealPlayer)
								args.Player.SendErrorMessage("You must use this command in-game.");
							else if ((account = await BankMain.Bank.FindAccount(args.Player.UserAccountName)) == null)
								args.Player.SendErrorMessage("You must have a bank account to use this command.");
							else
								args.Player.SendInfoMessage("[CurrencyBank] ID: {0:000000} | Balance: {1}", account.ID,
									FormatMoney(account.Balance));
							return;

						#endregion

						#region Give

						case "give":
							if (!args.Player.Group.HasPermission(Permissions.Give))
							{
								args.Player.SendErrorMessage("You do not have access to this command.");
								return;
							}

							BankAccount recipient;
							string accountName = string.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[3].Value :
								match.Groups[2].Value;
							ulong value = 0;
							if (string.IsNullOrWhiteSpace(accountName))
								args.Player.SendErrorMessage("Syntax: {0}cbank give <account name or ID> <amount> [msg]",
									TShock.Config.CommandSpecifier);
							else if ((recipient = await BankMain.Bank.FindAccount(accountName)) == null)
								args.Player.SendErrorMessage("Invalid bank account!");
							else if (!ulong.TryParse(match.Groups[4].Value, out value) || value == 0 || value > long.MaxValue)
								args.Player.SendErrorMessage("Invalid amount!");
							else
							{
								string message = match.Groups[5].Value;
								try
								{
									await BankMain.Bank.ChangeByAsync(recipient.AccountName, (long)value);
									// Reminder: Once silent specifiers are out, make silent exclude this message for self givings
									args.Player.SendSuccessMessage("Gave {0} to {1}. New balance: {0}.",
										FormatMoney((long)value), recipient.AccountName, FormatMoney(recipient.Balance));

									// Notify the recipient
									SendNotice((account = await BankMain.Bank.FindAccount(args.Player.UserAccountName)) ??
										BankAccount.Server, recipient, (long)value, message, false);
								}
								catch (NullReferenceException)
								{
									args.Player.SendErrorMessage("Invalid bank account!");
								}
								catch (InvalidOperationException)
								{
									args.Player.SendErrorMessage("Error performing transaction. Possible database corruption.");
									args.Player.SendInfoMessage("Double check if there are multiple accounts with the same ID.");
									args.Player.SendInfoMessage("You can try syncing the server with the database by using the reload command.");
								}
							}
							return;

						#endregion

						#region Help

						case "help":
							string command = match.Groups[3].Value;
							if (string.IsNullOrWhiteSpace(command))
							{
								args.Player.SendSuccessMessage("CurrencyBank Help - Subcommands:");
								args.Player.SendInfoMessage("bal - Displays your account ID and value stored.");
								args.Player.SendInfoMessage("pay <account> <x> [msg] - Transfers {0}x from your account to another.",
									BankMain.Config.CurrencyNameShort);
								args.Player.SendInfoMessage("give <account> <x> [msg] - Gives {0}x to target account.",
									BankMain.Config.CurrencyNameShort);
								args.Player.SendInfoMessage("take <account> <x> [msg] - Takes {0}x from target account.",
									BankMain.Config.CurrencyNameShort);
								args.Player.SendInfoMessage("info <account> - Displays information regarding target account.");
							}
							else
							{
								command = command.ToLowerInvariant();
								if (command == "bal" || command == "balance")
								{
									args.Player.SendInfoMessage("Syntax: {0}cbank bal", TShock.Config.CommandSpecifier);
									args.Player.SendInfoMessage("Description: Displays your account ID and your current {0} balance.",
										BankMain.Config.CurrencyName);
								}
								else if (command == "give")
								{
									args.Player.SendInfoMessage("Syntax: {0}cbank give <account name or ID> <amount> [msg]",
										TShock.Config.CommandSpecifier);
									args.Player.SendInfoMessage("Description: Gives target account the selected amount of {0}." +
										" Optionally attach a message to be seen by the recipient, in case they're online.",
										BankMain.Config.CurrencyNamePlural);
								}
								else if (command == "info")
								{
									args.Player.SendInfoMessage("Syntax: {0}cbank info <account name or ID>",
										TShock.Config.CommandSpecifier);
									args.Player.SendInfoMessage("Description: Displays target account's ID, AccountName and Balance.");
								}
								else if (command == "pay")
								{
									args.Player.SendInfoMessage("Syntax: {0}cbank pay <account name or ID> <amount> [msg]",
										TShock.Config.CommandSpecifier);
									args.Player.SendInfoMessage("Description: Transfers selected amount of {0} from your account" +
										" to target account. Fails if you don't have enough {0}. Optionally attach a message to be" +
										" seen by the recipient, in case they're online.", BankMain.Config.CurrencyNamePlural);
								}
								else if (command == "take")
								{
									args.Player.SendInfoMessage("Syntax: {0}cbank take <account name or ID> <amount> [msg]",
										TShock.Config.CommandSpecifier);
									args.Player.SendInfoMessage("Description: Removes selected amount of {0} from target account." +
										" Optionally attaches a message to be seen by the recipient, in case they're online.",
										BankMain.Config.CurrencyNamePlural);
								}
								else
								{
									args.Player.SendErrorMessage(
										"Invalid subcommand! Type {0}cbank help for a list of subcommands.",
										TShock.Config.CommandSpecifier);
								}
							}
							return;

						#endregion

						#region Info

						case "info":
							accountName = string.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[3].Value :
								match.Groups[2].Value;
							if (string.IsNullOrWhiteSpace(accountName))
								args.Player.SendInfoMessage("Syntax: {0}cbank info <account name or ID>",
									TShock.Config.CommandSpecifier);
							else if ((account = await BankMain.Bank.FindAccount(accountName)) == null)
								args.Player.SendErrorMessage("Invalid bank account!");
							else
							{
								args.Player.SendInfoMessage("ID: {0:000000}", account.ID);
								args.Player.SendInfoMessage("AccountName: {0}", account.AccountName);
								args.Player.SendInfoMessage("Balance: {0}", FormatMoney(account.Balance));
							}
							return;

						#endregion

						#region Pay

						case "pay":
							if (!args.Player.Group.HasPermission(Permissions.Pay))
							{
								args.Player.SendErrorMessage("You do not have access to this command.");
								return;
							}

							//BankAccount recipient;
							accountName = string.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[3].Value :
								match.Groups[2].Value;
							if (!args.Player.RealPlayer)
								args.Player.SendErrorMessage("You must use this command in-game.");
							else if ((account = await BankMain.Bank.FindAccount(args.Player.UserAccountName)) == null)
								args.Player.SendErrorMessage("You must have a bank account to use this command.");
							else if (string.IsNullOrWhiteSpace(accountName))
								args.Player.SendErrorMessage("Syntax: {0}cbank pay <account name or ID> <amount> [msg]");
							else if ((recipient = await BankMain.Bank.FindAccount(accountName)) == null)
								args.Player.SendErrorMessage("Invalid bank account!");
							else if (!ulong.TryParse(match.Groups[4].Value, out value) || value == 0 || value > long.MaxValue)
								args.Player.SendErrorMessage("Invalid amount!");
							else if (account.Balance < (long)value)
								args.Player.SendErrorMessage("You are {0} short!", FormatMoney((long)value - account.Balance));
							else
							{
								string message = match.Groups[5].Value;
								try
								{
									await BankMain.Bank.ChangeByAsync(account.AccountName, -(long)value);
									await BankMain.Bank.ChangeByAsync(recipient.AccountName, (long)value);
									args.Player.SendInfoMessage("Paid {0} to {1}. Your balance: {2}.",
										FormatMoney((long)value), recipient.AccountName, FormatMoney(account.Balance));

									// Notify the recipient
									SendNotice(account, recipient, (long)value, message);
								}
								catch (NullReferenceException)
								{
									args.Player.SendErrorMessage("Invalid bank account!");
								}
								catch (InvalidOperationException)
								{
									args.Player.SendErrorMessage("Error performing transaction. Possible database corruption.");
									args.Player.SendInfoMessage("Double check if there are multiple accounts with the same ID.");
									args.Player.SendInfoMessage("You can try syncing the server with the database by using the reload command.");
								}
							}
							return;

						#endregion

						#region Take

						case "take":
							if (!args.Player.Group.HasPermission(Permissions.Take))
							{
								args.Player.SendErrorMessage("You do not have access to this command.");
								return;
							}

							BankAccount target;
							accountName = string.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[3].Value :
								match.Groups[2].Value;
							if (string.IsNullOrWhiteSpace(accountName))
								args.Player.SendErrorMessage("Syntax: {0}cbank take <account name or ID> <amount> [msg]",
									TShock.Config.CommandSpecifier);
							else if ((target = await BankMain.Bank.FindAccount(accountName)) == null)
								args.Player.SendErrorMessage("Invalid bank account!");
							else if (!ulong.TryParse(match.Groups[4].Value, out value) || value == 0 || value > long.MaxValue)
								args.Player.SendErrorMessage("Invalid amount!");
							else
							{
								string message = match.Groups[5].Value;
								try
								{
									await BankMain.Bank.ChangeByAsync(target.AccountName, -(long)value);
									args.Player.SendSuccessMessage("Took {0} from {1}. New balance: {0}.",
										FormatMoney((long)value), target.AccountName, FormatMoney(target.Balance));

									// Notify the target
									SendNotice((account = await BankMain.Bank.FindAccount(args.Player.UserAccountName)) ??
										BankAccount.Server, target, -(long)value, message);
								}
								catch (NullReferenceException)
								{
									args.Player.SendErrorMessage("Invalid bank account!");
								}
								catch (InvalidOperationException)
								{
									args.Player.SendErrorMessage("Error performing transaction. Possible database corruption.");
									args.Player.SendInfoMessage("Double check if there are multiple accounts with the same ID.");
									args.Player.SendInfoMessage("You can try syncing the server with the database by using the reload command.");
								}
							}
							return;

						#endregion
					}
				}
			}
			#region old
			/// <todo>
			/// Implement the below with REGEX
			/// Add an info/find command for checking on account's balance
			/// </todo>
			//// Only do if executed from an in-game player
			//if (args.Player.RealPlayer)
			//{
			//	if (!args.Player.IsLoggedIn)
			//	{
			//		args.Player.SendInfoMessage("Please login to use CurrencyBank.");
			//		return;
			//	}

			//	account = BankMain.Bank.FindAccount(args.Player.UserAccountName);
			//	if (account == null && args.Player.Group.HasPermission(Permissions.Permit))
			//	{
			//		account = new BankAccount(args.Player.UserAccountName);
			//		BankMain.Bank.Add(account);
			//		args.Player.SendWarningMessage("As this is your first access, a bank account has been created for you.");
			//		args.Player.SendWarningMessage("Your ID: {0:000000}", account.ID);
			//		return;
			//	}
			//}

			//if (args.Parameters.Count < 1)
			//{
			//	SendInfo(args.Player, account);
			//}
			//else
			//{
			//	string cmd = args.Parameters[0];
			//	switch (cmd)
			//	{
			//		#region Balance
			//		case "bal":
			//		case "balance":
			//			if (account == null)
			//			{
			//				args.Player.SendErrorMessage("You must have a bank account to use this command.");
			//				return;
			//			}
			//			if (!args.Player.RealPlayer)
			//				args.Player.SendErrorMessage("You must use this command in-game.");
			//			else
			//				args.Player.SendInfoMessage("[CurrencyBank] ID: {0:000000} | Balance: {1}", account.ID,
			//					FormatMoney(account.Balance));
			//			return;
			//		#endregion
			//		#region Create
			//		case "create":
			//			if (!args.Player.Group.HasPermission(Permissions.Create))
			//			{
			//				args.Player.SendErrorMessage("You do not have access to this command.");
			//				return;
			//			}

			//			if (args.Parameters.Count < 2)
			//				args.Player.SendInfoMessage("Syntax: {0}cbank create <account name> [starting money]",
			//					TShock.Config.CommandSpecifier);
			//			else
			//			{
			//				string accountName = args.Parameters[1];
			//				long startMoney = 0;
			//				if (args.Parameters.Count == 3)
			//					long.TryParse(args.Parameters[2], out startMoney);

			//				BankAccount newAccount = new BankAccount(accountName, startMoney);
			//				if (BankMain.Bank.Add(newAccount))
			//				{
			//					args.Player.SendInfoMessage("Bank account '{0}' created. ID: {1:n6} | Balance: {2}",
			//						accountName, newAccount.ID, FormatMoney(newAccount.Balance));
			//				}
			//				else
			//					args.Player.SendErrorMessage("Unable to create a new bank account. Check logs for details.");
			//			}
			//			return;
			//		#endregion
			//		#region Delete
			//		case "del":
			//		case "delete":
			//			if (!args.Player.Group.HasPermission(Permissions.Delete))
			//			{
			//				args.Player.SendErrorMessage("You do not have access to this command.");
			//				return;
			//			}

			//			if (args.Parameters.Count < 2)
			//				args.Player.SendInfoMessage("Syntax: {0}cbank del <account name or ID>");
			//			else
			//			{
			//				string accountName = args.Parameters[1];
			//				BankAccount accToDel = BankMain.Bank.FindAccount(accountName);
			//				if (accToDel == null)
			//					args.Player.SendErrorMessage("Invalid bank account!");
			//				else
			//				{
			//					accountName = accToDel.AccountName;
			//					if (BankMain.Bank.Del(accountName))
			//						args.Player.SendSuccessMessage("Successfully deleted '{0}'.", accountName);
			//					else
			//						args.Player.SendErrorMessage("Unable to delete '{0}'. Check logs for details.",
			//							accountName);
			//				}
			//			}
			//			return;
			//		#endregion
			//		#region Give
			//		case "give":
			//			if (!args.Player.Group.HasPermission(Permissions.Give))
			//			{
			//				args.Player.SendErrorMessage("You do not have access to this command.");
			//				return;
			//			}

			//			if (args.Parameters.Count < 3)
			//				args.Player.SendInfoMessage("Syntax: {0}cbank give <account name or ID> <amount> [msg]",
			//					TShock.Config.CommandSpecifier);
			//			else
			//			{
			//				string accountName = args.Parameters[1];
			//				BankAccount accToGive = BankMain.Bank.FindAccount(accountName);
			//				if (accToGive == null)
			//				{
			//					args.Player.SendErrorMessage("Invalid bank account!");
			//					return;
			//				}

			//				ulong amount;
			//				if (!ulong.TryParse(args.Parameters[2], out amount) || amount > long.MaxValue)
			//				{
			//					args.Player.SendErrorMessage("Invalid amount!");
			//					return;
			//				}

			//				if (BankMain.Bank.ChangeBy(accToGive.AccountName, (long)amount))
			//				{
			//					args.Player.SendSuccessMessage("Gave {0} to '{1}'. New balance: {0}.",
			//						FormatMoney((long)amount), accToGive.AccountName, FormatMoney(accToGive.Balance));

			//					// Notify the recipient
			//					args.Parameters.RemoveRange(0, 3);
			//					SendNotice(BankAccount.Server, accToGive, (long)amount, string.Join(" ", args.Parameters));
			//				}
			//				else
			//					args.Player.SendErrorMessage("Error performing transaction. Check logs for details.");
			//			}
			//			return;
			//		#endregion
			//		#region Help
			//		case "?":
			//		case "help":
			//			if (args.Parameters.Count < 2)
			//			{
			//	args.Player.SendSuccessMessage("CurrencyBank Help - Subcommands:");
			//	args.Player.SendInfoMessage("bal - Displays your account ID and value stored.");
			//	args.Player.SendInfoMessage("pay <account> <x> [msg] - Transfers x{0} from your account to another.",
			//		BankMain.Config.CurrencyNameShort);
			//	args.Player.SendInfoMessage("give <account> <x> [msg] - Gives x{0} to target account.",
			//		BankMain.Config.CurrencyNameShort);
			//	args.Player.SendInfoMessage("take <account> <x> [msg] - Takes x{0} from target account.",
			//		BankMain.Config.CurrencyNameShort);
			//	args.Player.SendInfoMessage("create <name> [x] - Creates a new bank account with x{0}.",
			//		BankMain.Config.CurrencyNameShort);
			//	args.Player.SendInfoMessage("delete <account> - Deletes an existing bank account.");
			//}
			//else
			//{
			//	string help = args.Parameters[1];
			//	if (help == "bal" || help == "balance")
			//	{
			//		args.Player.SendInfoMessage("Syntax: {0}cbank bal", TShock.Config.CommandSpecifier);
			//		args.Player.SendInfoMessage("Description: Displays your account ID and your current {0} balance.",
			//			BankMain.Config.CurrencyNameShort);
			//	}
			//	else if (help == "create")
			//	{
			//		args.Player.SendInfoMessage("Syntax: {0}cbank create <account name> [starting money]",
			//			TShock.Config.CommandSpecifier);
			//		args.Player.SendInfoMessage(
			//			"Description: Creates a new bank account. Optionally set its starting balance.");
			//	}
			//	else if (help == "del" || help == "delete")
			//	{
			//		args.Player.SendInfoMessage("Syntax: {0}cbank del <account name or ID>",
			//			TShock.Config.CommandSpecifier);
			//		args.Player.SendInfoMessage("Description: Deletes an existing bank account.");
			//	}
			//	else if (help == "give")
			//	{
			//		args.Player.SendInfoMessage("Syntax: {0}cbank give <account name or ID> <amount> [msg]",
			//			TShock.Config.CommandSpecifier);
			//		args.Player.SendInfoMessage("Description: Gives target account the selected amount of {0}." +
			//			" Optionally attach a message to be seen by the recipient, in case they're online.",
			//			BankMain.Config.CurrencyNamePlural);
			//	}
			//	else if (help == "pay")
			//	{
			//		args.Player.SendInfoMessage("Syntax: {0}cbank pay <account name or ID> <amount> [msg]",
			//			TShock.Config.CommandSpecifier);
			//		args.Player.SendInfoMessage("Description: Transfers selected amount of {0} from your account" +
			//			" to target account. Fails if you don't have enough {0}. Optionally attach a message to be" +
			//			" seen by the recipient, in case they're online.", BankMain.Config.CurrencyNamePlural);
			//	}
			//	else if (help == "take")
			//	{
			//		args.Player.SendInfoMessage("Syntax: {0}cbank take <account name or ID> <amount> [msg]",
			//			TShock.Config.CommandSpecifier);
			//		args.Player.SendInfoMessage("Description: Removes selected amount of {0} from target account." +
			//			" Optionally attaches a message to be seen by the recipient, in case they're online.",
			//			BankMain.Config.CurrencyNamePlural);
			//	}
			//	else
			//	{
			//		args.Player.SendErrorMessage(
			//			"Invalid subcommand! Type {0}cbank help for a list of subcommands.",
			//			TShock.Config.CommandSpecifier);
			//	}
			//			}
			//			return;
			//		#endregion
			//		#region Pay
			//		case "pay":
			//			{
			//				if (account == null)
			//				{
			//					args.Player.SendErrorMessage("You must have a bank account to use this command.");
			//					return;
			//				}

			//				if (!args.Player.RealPlayer)
			//				{
			//					args.Player.SendErrorMessage("You must use this command in-game.");
			//					return;
			//				}

			//				if (!args.Player.Group.HasPermission(Permissions.Pay))
			//				{
			//					args.Player.SendErrorMessage("You do not have access to this command.");
			//					return;
			//				}

			//				if (args.Parameters.Count < 3)
			//					args.Player.SendInfoMessage("Syntax: {0}cbank pay <account name or ID> <amount> [msg]",
			//						TShock.Config.CommandSpecifier);
			//				else
			//				{
			//					string accountName = args.Parameters[1];
			//					BankAccount accToPay = BankMain.Bank.FindAccount(accountName);
			//					if (accToPay == null)
			//					{
			//						args.Player.SendErrorMessage("Invalid bank account!");
			//						return;
			//					}

			//					ulong amount;
			//					if (!ulong.TryParse(args.Parameters[2], out amount) || amount > long.MaxValue)
			//					{
			//						args.Player.SendErrorMessage("Invalid amount!");
			//						return;
			//					}

			//					if ((long)amount > account.Balance)
			//					{
			//						args.Player.SendErrorMessage("You are {0} short!",
			//							FormatMoney((long)amount - account.Balance));
			//						return;
			//					}

			//					if (BankMain.Bank.ChangeBy(account.AccountName, -(long)amount) &&
			//						BankMain.Bank.ChangeBy(accToPay.AccountName, (long)amount))
			//					{
			//						args.Player.SendInfoMessage("Paid {0} to '{1}'. Your balance: {2}.",
			//							FormatMoney((long)amount), accToPay.AccountName, FormatMoney(account.Balance));

			//						// Notify the recipient
			//						args.Parameters.RemoveRange(0, 3);
			//						SendNotice(account, accToPay, (long)amount, string.Join(" ", args.Parameters));
			//					}
			//					else
			//						args.Player.SendErrorMessage("Error performing transaction. Check logs for details.");
			//				}
			//			}
			//			return;
			//		#endregion
			//		#region Take
			//		case "take":
			//			if (!args.Player.Group.HasPermission(Permissions.Take))
			//			{
			//				args.Player.SendErrorMessage("You do not have access to this command.");
			//				return;
			//			}

			//			if (args.Parameters.Count < 3)
			//				args.Player.SendInfoMessage("Syntax: {0}cbank take <account name or ID> <amount>",
			//					TShock.Config.CommandSpecifier);
			//			else
			//			{
			//				string accountName = args.Parameters[1];
			//				BankAccount accToTake = BankMain.Bank.FindAccount(accountName);
			//				if (accToTake == null)
			//				{
			//					args.Player.SendErrorMessage("Invalid bank account!");
			//					return;
			//				}

			//				ulong amount;
			//				if (!ulong.TryParse(args.Parameters[2], out amount) || amount > long.MaxValue)
			//				{
			//					args.Player.SendErrorMessage("Invalid amount!");
			//					return;
			//				}

			//				if (BankMain.Bank.ChangeBy(accToTake.AccountName, (long)amount))
			//				{
			//					args.Player.SendSuccessMessage("Took {0} from '{1}'. New balance: {2}.",
			//						FormatMoney((long)amount), accToTake.AccountName, FormatMoney(accToTake.Balance));

			//					// Notify the taken
			//					args.Parameters.RemoveRange(0, 3);
			//					SendNotice(BankAccount.Server, accToTake, (long)amount, string.Join(" ", args.Parameters));
			//				}
			//				else
			//					args.Player.SendErrorMessage("Error performing transaction. Check logs for details.");
			//			}
			//			return;
			//		#endregion
			//		default:
			//			break;
			//	}
			//}
			#endregion
		}

		private static void SendInfo(TSPlayer player, BankAccount account)
		{
			player.SendSuccessMessage("CurrencyBank v{0} by Enerdy",
				System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
			if (account != null)
				player.SendInfoMessage("You currently have {0}.", FormatMoney(account.Balance));
			player.SendInfoMessage(" - You may check your {0} balance at anytime using {1}cbank bal",
				BankMain.Config.CurrencyName, TShock.Config.CommandSpecifier);
			if (player.Group.HasPermission(Permissions.Pay))
				player.SendInfoMessage(" - You can pay other players with {0}cbank pay",
					TShock.Config.CommandSpecifier);
			player.SendInfoMessage("Type {0}cbank help or {0}cbank help <command> for more.",
				TShock.Config.CommandSpecifier);
		}

		private static void SendNotice(BankAccount sender, BankAccount target, long value, string message = "", bool showSender = true)
		{
			var players = TShock.Utils.FindPlayer(target.AccountName);
			if (players.Count < 1)
				return;

			TSPlayer player = players[0];
			bool payment = Math.Sign(value) == -1;
			var sb = new StringBuilder();
			sb.Append("[CurrencyBank] ");
			sb.Append(payment ? "Paid" : "Received").Append(' ');
			sb.Append(FormatMoney(Math.Abs(value)));
			if (showSender)
				sb.Append(' ').Append(payment ? "to" : "from").Append(' ').Append(sender.AccountName);
			if (!string.IsNullOrWhiteSpace(message))
				sb.Append(' ').Append('(').Append(message).Append(')');

			player.SendInfoMessage(sb.ToString());
		}

		private static string FormatMoney(long money)
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
