using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CurrencyBank.DB;
using TShockAPI;

namespace CurrencyBank
{
	public class BankLog
	{
		private string path;
		private FileStream _fs;
		private StreamWriter _writer;
		private object writeLock = new object();

		public static string GetLogName()
		{
			return "BankLog_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".log";
		}

		public BankLog(string path)
		{
			this.path = path;
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			_fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
			_writer = new StreamWriter(_fs);
		}

		/// <summary>
		/// Writes to the log file.
		/// </summary>
		/// <param name="data">The data to write.</param>
		public void Write(string data)
		{
			try
			{
				lock (writeLock)
				{
					var sb = new StringBuilder();
					// Date of the transaction
					sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
					sb.Append(" - ");

					sb.Append(data);
					if (sb[sb.Length - 1] != '.')
						sb.Append('.');

					_writer.WriteLine(sb.ToString());
					_writer.Flush();
				}
			}
			catch (Exception inner)
			{
				throw new BankLogException("An exception was thrown at 'BankLog.Write'.", inner);
			}
		}

		/// <summary>
		/// Writes to the log file using a formatted string.
		/// </summary>
		/// <param name="format">The string to be formatted.</param>
		/// <param name="args">The parameters to be used to format the string.</param>
		public void Write(string format, params object[] args)
		{
			Write(string.Format(format, args));
		}

		/// <summary>
		/// Logs an account's gain.
		/// </summary>
		/// <param name="account">The receiver's account.</param>
		/// <param name="amount">The amount of currency.</param>
		/// <param name="message">An optional message for the transaction.</param>
		public void Gain(BankAccount account, long amount, string message = "")
		{
			Write("{0} received {1}{2}", account.AccountName, BankMain.FormatMoney(amount),
				string.IsNullOrWhiteSpace(message) ? "" : " with the message \"" + message + "\"");
		}

		/// <summary>
		/// Logs an account's loss.
		/// </summary>
		/// <param name="account">The sender's account.</param>
		/// <param name="amount">The amount of currency.</param>
		/// <param name="message">An optional message for the transaction.</param>
		public void Loss(BankAccount account, long amount, string message = "")
		{
			Write("{0} lost {1}{2}", account.AccountName, BankMain.FormatMoney(amount),
				string.IsNullOrWhiteSpace(message) ? "" : " with the message \"" + message + "\"");
		}

		/// <summary>
		/// Logs a payment transaction to the log.
		/// </summary>
		/// <param name="sender">The sender's account.</param>
		/// <param name="receiver">The receiver's account.</param>
		/// <param name="amount">The amount of currency.</param>
		/// <param name="message">An optional message for the transaction.</param>
		public void Payment(BankAccount sender, BankAccount receiver, long amount, string message = "")
		{
			Write("{0} paid {1} to {2}{3}", sender.AccountName, BankMain.FormatMoney(amount), receiver.AccountName,
				string.IsNullOrWhiteSpace(message) ? "" : " with the message \"" + message + "\"");
		}
	}

	[Serializable]
	public class BankLogException : Exception
	{
		public BankLogException()
		{
			
		}

		public BankLogException(string message)
			: base(message)
		{

		}

		public BankLogException(string message, Exception inner)
			: base(message, inner)
		{

		}
	}
}
