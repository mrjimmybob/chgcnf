using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EncryptorLibrary;

namespace chgcnf3
{
	class Program
	{
		static int versionMajor = 3;
		static int versionMinor = 1;
		static int versionRevision = 0;
		static string strFind = "";
		static string strReplace = "";

		static string undoFile = "chgcnf3_undo.bat";
		static string logFileName = "chgcnf3.log";

		static bool fileContainsString(string filename, string strToFind)
		{
			string contents = System.IO.File.ReadAllText(filename);
			if (contents.ToUpper().Contains(strToFind.ToUpper()))
			{
				return true;
			}
			return false;
		}

		static bool FileContainsStringUTF16(string filename, string strToFind)
		{
			return false;
			
			string contents = System.IO.File.ReadAllText(filename);
			if (contents.ToUpper().Contains(strToFind.ToUpper()))
			{
				return true;
			}
			return false;
		}

		static void debug(string msg)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("<");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(msg);
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(">");
			Console.ForegroundColor = ConsoleColor.White;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="error"></param>
		/// <param name="detail"></param>
		static void printError(string name, string error, string detail)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("" + error + ": ");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("\'" + name + "\' ");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("(" + detail + ")");
			Console.ForegroundColor = ConsoleColor.White;
		}

		static void printInfo(string title, string data)
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(title);
			Console.ForegroundColor = ConsoleColor.Magenta; 
			Console.WriteLine(data);
		}

		static string processLineEncDec(string filename, string line, string strOld, string strNew)
		{
			// You are processing a connectioon line, if it has a C{atalog} string, it is not encrypted
			if (isEncrypted(line))
			{
				// If you find at least one connection string without a C{atalog} it is encrypted
				Encryptor enc = new Encryptor();
				string cryptLine = "";
				try
				{
					cryptLine = enc.Decrypt(line, true);
				}
				catch
                {
					return line;
                }
				if (cryptLine.ToUpper().Contains(strOld.ToUpper()))
				{
					printInfo("Replaced '" + strOld + "' with '" + strNew + "' in encrypted file: ", filename);
					writeToLog("Replaced '" + strOld + "' with '" + strNew + "' in encrypted file: " + filename);

					// String is found, replace and encrypt string again 
					cryptLine = cryptLine.Replace(strOld.ToLower(), strNew.ToUpper());
					cryptLine = cryptLine.Replace(strOld.ToUpper(), strNew.ToUpper());
					cryptLine = cryptLine.Replace(toCamelCase(strOld), strNew.ToUpper());
					
					return enc.Encrypt(cryptLine, true);
				}
			}
			return line; // if you are here, just return the line that came in.
		}

		static void replaceFileContentsEncrypted(string newFile, string filename, string strOld, string strNew)
		{
			string fileContents;
			try
			{
				fileContents = System.IO.File.ReadAllText(filename);
			}
			catch (Exception ex)
			{
				printError(filename, "Error reading file", ex.Message);
				writeToLog("The file '" + filename + "' could not be read: " + ex.Message);
				return;
			}
			int i1, i2, iSearch, iCopy = 0;
			string newFileContents = "";
			
			if (fileContents.IndexOf("xml") > 0)
			{
				// It is an xml config file with connectionStrings
				iSearch = 0;
				iCopy = 0;
				while (true)
				{
					i1 = fileContents.IndexOf("connectionString", iSearch);
					i2 = fileContents.IndexOf("connectionStrings", iSearch);
					if (i1 != i2 && i1 >= 0)
					{
						// you have a connectionString section
						iSearch = i1 + "connectionString".Length;
						// get first "
						while (fileContents[iSearch] != '\"' && iSearch < fileContents.Length)
						{
							// got first "
							iSearch++;
						}
						if (iSearch >= fileContents.Length) break; // security
						iSearch++;
						newFileContents += fileContents.Substring(iCopy, iSearch - iCopy); // Copy from where you had last time to where you searched
						iCopy = iSearch;
						while (fileContents[iSearch] != '\"' && iSearch < fileContents.Length)
						{
							// got second "
							iSearch++;
						}
						if (iSearch >= fileContents.Length) break; // security
						newFileContents += processLineEncDec(newFile, fileContents.Substring(iCopy, iSearch - iCopy), strOld, strNew);
						iCopy = iSearch;
					}
					else
					{
						if (i1 >= 0)
						{
							// you have a connectionStrings main section, keep going
							iSearch = i1 + "connectionStrings".Length;
						}
						else {
							// No connection String is found
							//iSearch = fileContents.Length;
							newFileContents += fileContents.Substring(iCopy); // Copy from where you had last time to where you searched
							break;
						}
					}
				}

				try
				{
					System.IO.File.WriteAllText(newFile, newFileContents);
				}
				catch (Exception ex)
				{
					writeToLog("The file '" + newFile + "' could not be writen: " + ex.Message);
					printError(filename, "Error writing file", ex.Message);
					return;
				}
			}
			else {
				// no connectionStrings found, get out.
				return;
			}
			
			return;
		}

		static bool isEncrypted(string line)
		{
			Encryptor enc = new Encryptor();
			string cryptLine;
			try
			{
				cryptLine = enc.Decrypt(line, true);
				if (cryptLine.Contains("atalog") || cryptLine.Contains("atabase") || cryptLine.Contains("DSN"))
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
			return false;
		}

		static bool fileHasEncriptedConnectionString(string path)
		{
			string substring = "";
			try
			{
				substring = System.IO.File.ReadAllText(path);
			}
			catch (Exception ex)
			{
				printError(path, "Error reading file", ex.Message);
				writeToLog("The file '" + path + "' could not be read: " + ex.Message);
				return false;
			}

			int from = substring.IndexOf("connectionStrings") + "connectionStrings".Length + 1; // skip closing "
			int to = substring.IndexOf("</connectionStrings");
			int i1, i2, i3;
			string line;

			
			if (from > 0 && to > 0)
			{
				substring = substring.Substring(from, to - from); // these are the connection strings (all)
				// It is an xml config file with connectionStrings
				while (true)
				{
					// Find first connectionString in substring 
					i1 = substring.IndexOf("connectionString");
					//debug("1: [" + i1.ToString() + "]" + substring);
					if (i1 < 0) break;
					i1 += "connectionString".Length + 1; // go past the connectionString=
														 //debug("2: i1= " + i1.ToString());
					// found a connectionString, get the limits
					i2 = substring.IndexOf("\"", i1); // find first "
													  //debug("3: i2= " + i2.ToString());					 
					if (i2 < 0) break;
					i2++; // pass the double comma
					i3 = substring.IndexOf("/>", i2); // find end of connectionString
													  //debug("4: i3= " + i3.ToString());
					if (i3 < 0) break;
					i3 += 2; // pass the /> bit
					line = substring.Substring(i2, i3 - i2 - 4); // Minus string beginning and '/>'
																 //debug("Line: " + line);
					if (isEncrypted(line))
					{
						return true;
					}
					if (i2 > i3) break;
					substring = substring.Substring(i3 - i2); // get from after last '/>'
				}
			}
			return false;
		}

		static void createUndoHeader(string path)
		{
			string undoText = "ECHO Restoring " + path + Environment.NewLine;
			undoText += "@ECHO OFF" + Environment.NewLine;
			undoText += "ECHO Restoring last batch of changes" + Environment.NewLine;
			undoText += "PAUSE" + Environment.NewLine;

			try
			{
				File.WriteAllText(undoFile, undoText);
			}
			catch (Exception ex)
			{
				printError(undoFile, "Error creating undo file", ex.Message);
				writeToLog("Error creating undo file: " + ex.Message);
			} 
			return;
		}

		static void createUndo(string currentPath, string backupPath)
		{
			string undoText = "ECHO Restoring " + currentPath + Environment.NewLine;
			undoText += "MOVE /Y \"" + currentPath + "\" \"" + currentPath + "_delete\"" + Environment.NewLine;
			undoText += "MOVE /Y \"" + backupPath + "\" \"" + currentPath + "\"" + Environment.NewLine;

			try
			{
				File.AppendAllText(undoFile, undoText);
				// writeToLog("Created undo file: " + undoFile);
				// printInfo("Created undo file: ", undoFile);
			}
			catch (Exception ex)
			{
				printError(undoFile, "Error creating undo file", ex.Message);
				writeToLog("Error creating undo file: " + ex.Message);
			}
		}

		static string createBackup(string path) {
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string backupFile = path + "_" + timestamp;

			try
			{
				System.IO.File.Move(path, backupFile);
				// writeToLog("Created backup file: " + backupFile);
				// printInfo("Created backup file: ", backupFile);
			}
			catch (Exception ex)
			{
				printError(backupFile, "Error creating backup file", ex.Message);
				writeToLog("Error creating backup file: " + ex.Message);
				return null;
			}

			return backupFile;
		}
		
		static void replaceFileContentsUTF16(string newFile, string filename, string strOld, string strNew)
		{
			return;
		}

		public static string toCamelCase(string input)
		{  
			if (String.IsNullOrEmpty(input))
				throw new ArgumentException("ARGH!");
			return input.First().ToString().ToUpper() + input.Substring(1).ToLower();
		}


		static void replaceFileContents(string newFile, string filename, string strOld, string strNew)
		{
			string newContents = "";

			try
			{
				using (var fileContents = new StreamReader(filename))
				{
					newContents = Convert.ToString(fileContents.ReadToEnd());
					newContents = newContents.Replace(strOld.ToLower(), strNew.ToUpper());
					newContents = newContents.Replace(strOld.ToUpper(), strNew.ToUpper());
					newContents = newContents.Replace(toCamelCase(strOld), strNew.ToUpper());
				}
			}
			catch (IOException ex)
			{
				printError(filename, "Error reading file", ex.Message);
				writeToLog("Error reading file: '" + filename + "': " + ex.Message);
			}
			try
			{
				using (StreamWriter outputFile = new StreamWriter(newFile))
				{
					outputFile.Write(newContents);
				}
			}
			catch (IOException ex)
			{
				printError(newFile, "Error reading file", ex.Message);
				writeToLog("Error reading file '" + newFile + "': " + ex.Message);
			}
		}

		static void processFile(FileInfo file)
		{
			string backupFile = "";
			 
			if (fileContainsString(file.FullName, strFind)) {
				backupFile = createBackup(file.FullName);
				if (backupFile is null) {
					printError(file.FullName, "Error creating backup file", "Execution halted");
					System.Environment.Exit(0);
				}
				replaceFileContents(file.FullName, backupFile, strFind, strReplace);
				createUndo(file.FullName, backupFile);

				printInfo("Replaced '" + strFind + "' with '" + strReplace + "' in text file: ", file.FullName);
				writeToLog("Replaced '" + strFind + "' with '" + strReplace + "' in text file: " + file.FullName); 
			} 
			else
			{
				if (FileContainsStringUTF16(file.FullName, strFind))
				{
					backupFile = createBackup(file.FullName);
					if (backupFile is null)
					{
						printError(file.FullName, "Error creating backup file", "Execution halted");
						System.Environment.Exit(0);
					}
					replaceFileContentsUTF16(file.FullName, backupFile, strFind, strReplace);
					createUndo(file.FullName, backupFile);

					printInfo("Replaced '" + strFind + "' with '" + strReplace + "' in UTF16 file: ", file.FullName);
					writeToLog("Replaced '" + strFind + "' with '" + strReplace + "' in UTF16 file: " + file.FullName);
				}
				else
				{
					if (fileHasEncriptedConnectionString(file.FullName))
					{
						backupFile = createBackup(file.FullName);
						if (backupFile is null)
						{
							printError(file.FullName, "Error creating backup file", "Execution halted");
							System.Environment.Exit(0);
						}
						replaceFileContentsEncrypted(file.FullName, backupFile, strFind, strReplace);
						createUndo(file.FullName, backupFile);

						// writeToLog("Encrypted connectionString found in: " + file.FullName);
						// printInfo("Processed encrypted file: ", file.FullName);
					}
					/*
					else
					{
						writeToLog("Ignoring file: '" + file.FullName + "' ('" + strFind + "' not found)");
					}
					*/
				}
			}
		}

		internal static void EnumerateFiles(string sFullPath)
		{
			try
			{
				DirectoryInfo di = new DirectoryInfo(sFullPath);
				FileInfo[] files = di.GetFiles();

				foreach (FileInfo file in files)
				{
					if (file.Extension.ToUpper().Equals(".CONFIG") || file.Extension.ToUpper().Equals(".UDL"))
					{
						// writeToLog("Processing file: " + file.FullName);
						processFile(file);
					}
				}
				// Scan recursively
				DirectoryInfo[] dirs = di.GetDirectories();
				if (dirs == null || dirs.Length < 1)
					return;
				foreach (DirectoryInfo dir in dirs)
					EnumerateFiles(dir.FullName);
			}
			catch (Exception ex)
			{
				writeToLog("Error processing directory: '" + sFullPath + "' " + ex.Message);
				printError(sFullPath, "Error processing directory", ex.Message);
			}
		}

		static void writeToLog(string logMessage)
		{
			try
			{
				// Console.WriteLine(logMessage);
				File.AppendAllText(logFileName, logMessage + Environment.NewLine);
			}
			catch (Exception ex)
            {
				printError(logFileName, "Error opening log file for create/append", ex.Message);
			}
		}

		static void writeLogHeader()
		{
			writeToLog("Execution: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
		}

		static void printUsageAndExit()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Usage: ");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("chgcnf3.exe");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" <");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("path");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(">");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" <");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("STR1");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(">");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" <");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("STR2");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(">");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("As from <path>, replace STR1 with STR2 in config (.config, .udl) files.");
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("Third ");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("3");
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine("ye Software Inc. (c) 2021");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Version: {0}.{1}.{2}. ", versionMajor, versionMinor, versionRevision);
			System.Environment.Exit(0);
		}

		static void Main(string[] args)
		{
			if (args.Length != 3)
			{
				printUsageAndExit();
			}
			else
			{
				string path = args[0];
				strFind = args[1];
				strReplace = args[2];

				writeToLog("Replacing '" + strFind + "' for '" + strReplace.ToUpper() + "' in config files (.config and .udl) in '" + path + "'.");
				Console.ForegroundColor = ConsoleColor.Green; Console.Write("Replacing '");
				Console.ForegroundColor = ConsoleColor.Cyan;  Console.Write(strFind);
				Console.ForegroundColor = ConsoleColor.Green; Console.Write("' for '");
				Console.ForegroundColor = ConsoleColor.Cyan;  Console.Write(strReplace.ToUpper()); 
				Console.ForegroundColor = ConsoleColor.Green; Console.Write("' in config files (.config and .udl) in '");
				Console.ForegroundColor = ConsoleColor.Cyan;  Console.Write(path);
				Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("'.");
				Console.ForegroundColor = ConsoleColor.White;


				if (Directory.Exists(path))
				{
					createUndoHeader(path);
					writeLogHeader();

					var watch = System.Diagnostics.Stopwatch.StartNew();

					EnumerateFiles(path);

					watch.Stop();
					var elapsedMs = watch.ElapsedMilliseconds;
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write("Finished processing file in ");
					Console.ForegroundColor = ConsoleColor.Yellow;
					if (elapsedMs > 1000)
					{
						Console.Write(Convert.ToString(elapsedMs/1000) + " s");
					}
					else {
						Console.Write(Convert.ToString(elapsedMs) + " ms");
					}
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(".");
					Console.ForegroundColor = ConsoleColor.White;
				}
				else
				{
					printError(path, "Directory does not exist", "path not found");
					System.Environment.Exit(0);
				}


			}

		}
	}
}
 