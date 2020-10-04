using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using Ionic.Zip;
using Sentry;

namespace SQLAutoBackup
{
	internal class Program
	{
		private static bool SentryEnabled
			=> !string.IsNullOrEmpty(ConfigurationManager.AppSettings["SentryDSN"]);

		private static bool SuccessMessages
			=> !string.IsNullOrEmpty(ConfigurationManager.AppSettings["SuccessMessages"]) && Convert.ToBoolean(ConfigurationManager.AppSettings["SuccessMessages"]);

		private static string MachineName
			=> Environment.MachineName;

		private static void Main()
		{
			//"The underlying connection was closed: An unexpected error occurred on a send." error fix
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

			Console.WriteLine($"CONFIG {{ MachineName: {MachineName}, SentryEnabled: {SentryEnabled}, SuccessMessages: {SuccessMessages} }}");

			using (SentryEnabled ? SentrySdk.Init(ConfigurationManager.AppSettings["SentryDSN"]) : null)
			{
				if (SentryEnabled)
				{
					SentrySdk.ConfigureScope(scope =>
					{
						scope.SetTag("Environment.MachineName", System.Environment.MachineName);
						scope.SetTag("BackupDir", ConfigurationManager.AppSettings["BackupDir"]);
						scope.SetTag("ExcludedDBs", ConfigurationManager.AppSettings["ExcludedDBs"]);
					});
				}

				var timer = Stopwatch.StartNew();

				#region Settings

				var backupSet = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");

				var tempDir = Path.GetTempPath() + backupSet + "\\";
				var backupDir = ConfigurationManager.AppSettings["BackupDir"];

				var tempZip = $"{tempDir}{backupSet}.zip";
				var backupZip = $"{backupDir}{backupSet}.zip";

				List<string> databasesToBackup = ConfigurationManager.AppSettings["DBs"].Split(';').ToList();
				List<string> databasesToExclude = ConfigurationManager.AppSettings["ExcludedDBs"].Split(';').ToList();

				#endregion

				try
				{
					#region Create backup dir
					if (!Directory.Exists(backupDir))
					{
						Directory.CreateDirectory(backupDir);
						Console.WriteLine($"Created backup dir: {backupDir}");
					}
					#endregion

					#region Create temporary backup dir

					if (!Directory.Exists(tempDir))
					{
						Directory.CreateDirectory(tempDir);
						Console.WriteLine($"Created temporary dir: {tempDir}");
					}

					var secInfo = Directory.GetAccessControl(tempDir);

					secInfo.AddAccessRule(
						new FileSystemAccessRule(
							new SecurityIdentifier(WellKnownSidType.WorldSid, null),
							FileSystemRights.Modify | FileSystemRights.Synchronize,
							InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
							PropagationFlags.None,
							AccessControlType.Allow
						)
					);

					Directory.SetAccessControl(tempDir, secInfo);
					Console.WriteLine($"Set permissions for dir: {tempDir} @ Everyone");

					#endregion

					#region Backup specified databases

					using (var cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["default"].ConnectionString))
					{
						cnn.Open();

						using (var cmd = new SqlCommand("", cnn))
						{
							if (databasesToBackup.Contains("*"))
							{
								cmd.CommandText = "SELECT Name FROM sys.databases;";
								using (var rdr = cmd.ExecuteReader())
								{
									databasesToBackup.Clear();

									while (rdr.Read())
									{
										var dbName = rdr["Name"].ToString();
										if (!databasesToExclude.Contains(dbName))
										{
											databasesToBackup.Add(dbName);
										}
									}
								}
							}

							foreach (var database in databasesToBackup.Where(database => database.Length > 0))
							{
								Console.WriteLine($"BACKUP DATABASE [{database}]");

								cmd.CommandText = $"BACKUP DATABASE [{database}] TO DISK = '{tempDir}{database}.bak' WITH FORMAT;";
								cmd.ExecuteNonQuery();
							}

							if (SentryEnabled)
							{
								SentrySdk.ConfigureScope(scope =>
								{
									scope.SetTag("DBsToBackup", string.Join(";", databasesToBackup));
								});
							}
						}
					}

					#endregion

					#region Zip backup set to tempDir and move to backupDir

					using (var zip = new ZipFile())
					{
						Console.WriteLine($"Compressing backup set to: {tempZip}");
						zip.AddDirectory(tempDir);

						zip.Save(tempZip);
					}

					File.Copy(tempZip, backupZip, true);

					#endregion

					timer.Stop();
					var elapsedMs = timer.ElapsedMilliseconds;
					decimal elapsedS = (decimal)elapsedMs / 1000;

					var message = $"SQL DB backup on '{MachineName}' completed successfully in {elapsedS.ToString("0.00")}s!";
					if (SuccessMessages && SentryEnabled)
					{
						SentrySdk.CaptureMessage(message);
					}
					Console.WriteLine(message);
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"Error: {ex.Message}");

					if (SentryEnabled)
					{
						SentrySdk.CaptureException(ex);
					}
				}
				finally
				{
					#region Remove temporary backup dir

					if (Directory.Exists(tempDir))
					{
						Directory.Delete(tempDir, true);
					}

					#endregion
				}
			}
		}
	}
}