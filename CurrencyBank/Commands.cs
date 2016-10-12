using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CurrencyBank.DB;
using TShockAPI;
using Wolfje.Plugins.Jist;
using Wolfje.Plugins.Jist.Framework;
using Wolfje.Plugins.Jist.stdlib;
using static CurrencyBank.BankMain;

namespace CurrencyBank
{
	public class Commands
	{
		private static string specifier = TShockAPI.Commands.Specifier;

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
				args.Player.SendErrorMessage($"{Tag} Invalid syntax! Proper syntax: {specifier}cbank [switch] [params...]");
				args.Player.SendInfoMessage($"{Tag} Available cbank switches: bal, give, help, info, pay, take");
			}
			else
			{
				if (String.IsNullOrWhiteSpace(match.Groups[1].Value))
					SendInfo(args.Player, await Bank.GetAsync(args.Player.User?.Name));
				else
				{
					switch (match.Groups[1].Value)
					{
						#region Balance

						case "bal":
						case "balance":
							if ((account = await Bank.GetAsync(args.Player.User?.Name)) == null)
								args.Player.SendErrorMessage($"{Tag} You must have a bank account to use this command.");
							else
								args.Player.SendInfoMessage($"{Tag} ID: {account.ID:000000} | Balance: {FormatMoney(account.Balance)}.");
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
							string accountName;
							accountName = String.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[3].Value : match.Groups[2].Value;
							ulong value = 0;
							if (String.IsNullOrWhiteSpace(accountName))
								args.Player.SendErrorMessage($"{Tag} Syntax: {specifier}cbank give <account name or ID> <amount> [msg]");
							else if ((recipient = await Bank.GetAsync(accountName)) == null)
								args.Player.SendErrorMessage($"{Tag} Invalid bank account!");
							else if (!UInt64.TryParse(match.Groups[4].Value, out value) || value == 0 || value > Int64.MaxValue)
								args.Player.SendErrorMessage($"{Tag} Invalid amount!");
							else
							{
								string message = match.Groups[5].Value;
								try
								{
									await Bank.ChangeByAsync(recipient.AccountName, (long)value);
									Log.Gain(recipient, (long)value, message);
									// If the command is silent, don't send the self message, so that this command can be used in scripts
									if (!args.Silent)
										args.Player.SendSuccessMessage($"{Tag} Gave {FormatMoney((long)value)} to {recipient.AccountName}. " +
											$"{recipient.AccountName}'s balance: {FormatMoney(recipient.Balance)}.");

									// Notify the recipient
									SendNotice((account = await Bank.GetAsync(args.Player.User?.Name)) ??
										BankAccount.Server, recipient, (long)value, message, false);
								}
								catch (NullReferenceException)
								{
									args.Player.SendErrorMessage($"{Tag} Invalid bank account!");
								}
								catch (InvalidOperationException)
								{
									args.Player.SendErrorMessage($"{Tag} Error performing transaction. Possible database corruption.");
									args.Player.SendInfoMessage($"{Tag} Double check if there are multiple accounts with the same ID.");
									args.Player.SendInfoMessage($"{Tag} You can try syncing the server with the database by using the reload command.");
								}
								catch (BankLogException ex)
								{
									TShock.Log.Error(ex.ToString());
								}
							}
							return;

						#endregion

						#region Help

						case "help":
							string command = match.Groups[3].Value;
							if (String.IsNullOrWhiteSpace(command))
							{
								args.Player.SendSuccessMessage($"{Tag} Help - Subcommands:");
								args.Player.SendInfoMessage($"{Tag} bal - Displays your account ID and value stored.");
								args.Player.SendInfoMessage($"{Tag} pay <account> <x> [msg] - Transfers {BankMain.Config.CurrencyNameShort}x from your account to another.");
								args.Player.SendInfoMessage($"{Tag} give <account> <x> [msg] - Gives {BankMain.Config.CurrencyNameShort}x to target account.");
								args.Player.SendInfoMessage($"{Tag} take <account> <x> [msg] - Takes {BankMain.Config.CurrencyNameShort}x from target account.");
								args.Player.SendInfoMessage($"{Tag} info <account> - Displays information regarding target account.");
							}
							else
							{
								command = command.ToLowerInvariant();
								if (command == "bal" || command == "balance")
								{
									args.Player.SendInfoMessage($"{Tag} Syntax: {specifier}cbank bal");
									args.Player.SendInfoMessage($"{Tag} Description: Displays your account ID and your current {BankMain.Config.CurrencyName} balance.");
								}
								else if (command == "give")
								{
									args.Player.SendInfoMessage($"{Tag} Syntax: {specifier}cbank give <account name or ID> <amount> [msg]");
									args.Player.SendInfoMessage($"{Tag} Description: Gives target account the selected amount of {BankMain.Config.CurrencyNamePlural}." +
										" Optionally attach a message to be seen by the recipient, in case they're online.");
								}
								else if (command == "info")
								{
									args.Player.SendInfoMessage($"{Tag} Syntax: {specifier}cbank info <account name or ID>");
									args.Player.SendInfoMessage($"{Tag} Description: Displays target account's ID, AccountName and Balance.");
								}
								else if (command == "pay")
								{
									args.Player.SendInfoMessage($"{Tag} Syntax: {specifier}cbank pay <account name or ID> <amount> [msg]");
									args.Player.SendInfoMessage($"{Tag} Description: Transfers selected amount of {BankMain.Config.CurrencyNamePlural} from your account" +
										$" to target account. Fails if you don't have enough {BankMain.Config.CurrencyNamePlural}. Optionally attach a message to be" +
										" seen by the recipient, in case they're online.");
								}
								else if (command == "take")
								{
									args.Player.SendInfoMessage($"{Tag} Syntax: {specifier}cbank take <account name or ID> <amount> [msg]");
									args.Player.SendInfoMessage($"{Tag} Description: Removes selected amount of {BankMain.Config.CurrencyNamePlural} from target account." +
										" Optionally attaches a message to be seen by the recipient, in case they're online.");
								}
								else
								{
									args.Player.SendErrorMessage($"{Tag} Invalid subcommand! Type {specifier}cbank help for a list of subcommands.");
								}
							}
							return;

						#endregion

						#region Info

						case "info":
							if (!args.Player.Group.HasPermission(Permissions.Info))
							{
								args.Player.SendErrorMessage("You do not have access to this command.");
								return;
							}

							accountName = String.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[3].Value : match.Groups[2].Value;
							if (String.IsNullOrWhiteSpace(accountName))
								args.Player.SendInfoMessage($"Syntax: {specifier}cbank info <account name or ID>");
							else if ((account = await Bank.GetAsync(accountName)) == null)
								args.Player.SendErrorMessage($"{Tag} Invalid bank account!");
							else
							{
								args.Player.SendInfoMessage($"{Tag} ID: {account.ID:000000}");
								args.Player.SendInfoMessage($"{Tag} AccountName: {account.AccountName}");
								args.Player.SendInfoMessage($"{Tag} Balance: {FormatMoney(account.Balance)}");
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
							accountName = String.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[3].Value : match.Groups[2].Value;
							if ((account = await Bank.GetAsync(args.Player.User?.Name)) == null)
								args.Player.SendErrorMessage($"{Tag} You must have a bank account to use this command.");
							else if (String.IsNullOrWhiteSpace(accountName))
								args.Player.SendErrorMessage($"{Tag} Syntax: {specifier}cbank pay <account name or ID> <amount> [msg]");
							else if ((recipient = await Bank.GetAsync(accountName)) == null)
								args.Player.SendErrorMessage($"{Tag} Invalid bank account!");
							else if (!UInt64.TryParse(match.Groups[4].Value, out value) || value == 0 || value > Int64.MaxValue)
								args.Player.SendErrorMessage($"{Tag} Invalid amount!");
							else if (account.Balance < (long)value)
								args.Player.SendErrorMessage($"{Tag} You are {FormatMoney((long)value - account.Balance)} short!");
							else
							{
								string message = match.Groups[5].Value;
								try
								{
									await Bank.ChangeByAsync(account.AccountName, -(long)value);
									await Bank.ChangeByAsync(recipient.AccountName, (long)value);
									Log.Payment(account, recipient, (long)value, message);
									if (!args.Silent)
										args.Player.SendInfoMessage($"{Tag} Paid {FormatMoney((long)value)} to {recipient.AccountName}." +
											$" Your balance: {FormatMoney(account.Balance)}.");

									// Notify the recipient
									SendNotice(account, recipient, (long)value, message);
								}
								catch (NullReferenceException)
								{
									args.Player.SendErrorMessage($"{Tag} Invalid bank account!");
								}
								catch (InvalidOperationException)
								{
									args.Player.SendErrorMessage($"{Tag} Error performing transaction. Possible database corruption.");
									args.Player.SendInfoMessage($" {Tag} Double check if there are multiple accounts with the same ID.");
									args.Player.SendInfoMessage($"{Tag} You can try syncing the server with the database by using the reload command.");
								}
								catch (BankLogException ex)
								{
									TShock.Log.Error(ex.ToString());
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
							accountName = String.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[3].Value : match.Groups[2].Value;
							if (String.IsNullOrWhiteSpace(accountName))
								args.Player.SendErrorMessage($"{Tag} Syntax: {specifier}cbank take <account name or ID> <amount> [msg]");
							else if ((target = await Bank.GetAsync(accountName)) == null)
								args.Player.SendErrorMessage($"{Tag} Invalid bank account!");
							else if (!UInt64.TryParse(match.Groups[4].Value, out value) || value == 0 || value > Int64.MaxValue)
								args.Player.SendErrorMessage($"{Tag} Invalid amount!");
							else
							{
								string message = match.Groups[5].Value;
								try
								{
									await Bank.ChangeByAsync(target.AccountName, -(long)value);
									Log.Loss(target, (long)value, message);
									if (!args.Silent)
										args.Player.SendSuccessMessage($"Took {FormatMoney((long)value)} from {target.AccountName}." +
											$" {target.AccountName}'s balance: {FormatMoney(target.Balance)}.");

									// Notify the target
									SendNotice((account = await Bank.GetAsync(args.Player.User?.Name)) ??
										BankAccount.Server, target, -(long)value, message);
								}
								catch (NullReferenceException)
								{
									args.Player.SendErrorMessage($"{Tag} Invalid bank account!");
								}
								catch (InvalidOperationException)
								{
									args.Player.SendErrorMessage($"{Tag} Error performing transaction. Possible database corruption.");
									args.Player.SendInfoMessage($"{Tag} Double check if there are multiple accounts with the same ID.");
									args.Player.SendInfoMessage($"{Tag} You can try syncing the server with the database by using the reload command.");
								}
								catch (BankLogException ex)
								{
									TShock.Log.Error(ex.ToString());
								}
							}
							return;

							#endregion
					}
				}
			}
		}

		private static void SendInfo(TSPlayer player, BankAccount account)
		{
			player.SendSuccessMessage($"{TShock.Utils.ColorTag("CurrencyBank", new Color(137, 73, 167))}" +
				$" v{Assembly.GetExecutingAssembly().GetName().Version}" +
				$" by {TShock.Utils.ColorTag("Enerdy", new Color(0, 127, 255))}");
			if (account != null)
				player.SendInfoMessage($"You currently have {FormatMoney(account.Balance)}.");
			player.SendInfoMessage($" - You may check your {BankMain.Config.CurrencyName} balance at anytime using {specifier}cbank bal.");
			if (player.Group.HasPermission(Permissions.Pay))
				player.SendInfoMessage($" - You can pay other players with {specifier}cbank pay.");
			player.SendInfoMessage($"Type {specifier}cbank help or {specifier}cbank help <command> for more.");
		}

		private static void SendNotice(BankAccount sender, BankAccount target, long value, string message = "", bool showSender = true)
		{
			var players = TShock.Utils.FindPlayer(target.AccountName);
			if (players.Count < 1)
				return;

			TSPlayer player = players[0];
			bool payment = Math.Sign(value) == -1;
			var sb = new StringBuilder();
			sb.Append($"{Tag} ");
			sb.Append(payment ? "Paid" : "Received").Append(' ');
			sb.Append(FormatMoney(Math.Abs(value)));
			if (showSender)
				sb.Append(' ').Append(payment ? "to" : "from").Append(' ').Append(sender.AccountName);
			if (!String.IsNullOrWhiteSpace(message))
				sb.Append(' ').Append('(').Append(message).Append(')');

			player.SendInfoMessage(sb.Append('.').ToString());
		}
	}	
}
