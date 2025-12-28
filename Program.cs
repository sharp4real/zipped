using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Zipped
{
    class Program
    {
        static string exePath = "";
        static string draggedPath = "";
        static string appDataPath = "";

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                exePath = Process.GetCurrentProcess().MainModule.FileName;
            }
            catch
            {
                exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            }

            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zipped");
            Directory.CreateDirectory(appDataPath);

            if (!IsAdmin())
            {
                RestartAsAdmin(args);
                return;
            }

            SetupWindowsIntegration();

            if (args.Length > 0)
            {
                draggedPath = args[0].Trim('"');
                HandleDragDrop(args);
            }
            else
            {
                ShowMainScreen();
            }
        }

        static bool IsAdmin()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        static void RestartAsAdmin(string[] args)
        {
            try
            {
                List<string> argList = new List<string>();
                foreach (string arg in args)
                {
                    argList.Add("\"" + arg + "\"");
                }

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = string.Join(" ", argList),
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(info);
            }
            catch
            {
            }
        }

        static void SetupWindowsIntegration()
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(".zipped"))
                {
                    if (key != null)
                    {
                        key.SetValue("", "ZippedArchive");
                    }
                }

                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey("ZippedArchive"))
                {
                    if (key != null)
                    {
                        key.SetValue("", "Zipped Archive");

                        using (RegistryKey iconKey = key.CreateSubKey("DefaultIcon"))
                        {
                            if (iconKey != null)
                            {
                                iconKey.SetValue("", exePath + ",0");
                            }
                        }

                        using (RegistryKey shellKey = key.CreateSubKey("shell"))
                        {
                            if (shellKey != null)
                            {
                                using (RegistryKey openKey = shellKey.CreateSubKey("open"))
                                {
                                    if (openKey != null)
                                    {
                                        openKey.SetValue("", "Extract with Zipped");
                                        using (RegistryKey cmdKey = openKey.CreateSubKey("command"))
                                        {
                                            if (cmdKey != null)
                                            {
                                                cmdKey.SetValue("", "\"" + exePath + "\" \"%1\"");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey("*\\shell\\ZippedCompress"))
                {
                    if (key != null)
                    {
                        key.SetValue("", "Compress with Zipped");
                        key.SetValue("Icon", exePath);
                        using (RegistryKey cmdKey = key.CreateSubKey("command"))
                        {
                            if (cmdKey != null)
                            {
                                cmdKey.SetValue("", "\"" + exePath + "\" \"%1\"");
                            }
                        }
                    }
                }

                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey("Directory\\shell\\ZippedCompress"))
                {
                    if (key != null)
                    {
                        key.SetValue("", "Compress with Zipped");
                        key.SetValue("Icon", exePath);
                        using (RegistryKey cmdKey = key.CreateSubKey("command"))
                        {
                            if (cmdKey != null)
                            {
                                cmdKey.SetValue("", "\"" + exePath + "\" \"%1\"");
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        static bool IsFirstRun()
        {
            string flagFile = Path.Combine(appDataPath, "tour_completed.flag");
            return !File.Exists(flagFile);
        }

        static void MarkTourCompleted()
        {
            string flagFile = Path.Combine(appDataPath, "tour_completed.flag");
            File.WriteAllText(flagFile, DateTime.Now.ToString());
        }

        static void HandleDragDrop(string[] paths)
        {
            Console.Clear();
            DrawHeader();

            string path = paths[0].Trim('"');

            if (path.EndsWith(".zipped", StringComparison.OrdinalIgnoreCase))
            {
                string outputFolder = Path.Combine(
                    Path.GetDirectoryName(path) ?? "",
                    Path.GetFileNameWithoutExtension(path)
                );
                ExtractMode(path, outputFolder);
            }
            else
            {
                string outputPath = path + ".zipped";
                CompressMode(path, outputPath);
            }

            Console.WriteLine("\n  Press any key to exit...");
            Console.ReadKey(true);
        }

        static void ShowMainScreen()
        {
            if (IsFirstRun())
            {
                Console.Clear();
                DrawHeader();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("    Welcome to Zipped! This is your first time here.\n");
                Console.ResetColor();

                Console.Write("    Would you like a quick tour? (Y/N): ");
                ConsoleKeyInfo key = Console.ReadKey(true);
                Console.WriteLine(key.KeyChar);

                if (key.Key == ConsoleKey.Y)
                {
                    ShowTour();
                }

                MarkTourCompleted();
            }

            while (true)
            {
                Console.Clear();
                DrawHeader();
                DrawMenu();

                ConsoleKeyInfo key = Console.ReadKey(true);

                bool shouldContinue = false;

                if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
                {
                    shouldContinue = CompressInteractive();
                }
                else if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
                {
                    shouldContinue = ExtractInteractive();
                }
                else if (key.Key == ConsoleKey.D3 || key.Key == ConsoleKey.NumPad3)
                {
                    ShowTour();
                    shouldContinue = true;
                }
                else if (key.Key == ConsoleKey.D4 || key.Key == ConsoleKey.NumPad4 || key.Key == ConsoleKey.Escape)
                {
                    return;
                }

                if (!shouldContinue)
                {
                    Console.WriteLine("\n  Press any key to continue...");
                    Console.ReadKey(true);
                }
            }
        }

        static void ShowTour()
        {
            Console.Clear();
            DrawHeader();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("    ╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("    ║            WELCOME TO THE ZIPPED TOUR!                   ║");
            Console.WriteLine("    ╚══════════════════════════════════════════════════════════╝\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    Press ENTER to go through each step...\n");
            Console.ResetColor();
            Console.ReadKey(true);

            Console.Clear();
            DrawHeader();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    STEP 1: THE MAIN MENU");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    ╔═════════════════════════════════════════╗");
            Console.WriteLine("    ║                                         ║");
            Console.WriteLine("    ║  [1] COMPRESS File or Folder            ║  <--- Use this to create archives");
            Console.WriteLine("    ║  [2] EXTRACT Archive                    ║  <--- Use this to open archives");
            Console.WriteLine("    ║  [3] Take the Tour                      ║  <--- You are here!");
            Console.WriteLine("    ║  [4] Exit                               ║  <--- Quit the app");
            Console.WriteLine("    ║                                         ║");
            Console.WriteLine("    ╚═════════════════════════════════════════╝");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n    Just press the number key to select an option!");
            Console.WriteLine("\n    Press ENTER to continue...");
            Console.ResetColor();
            Console.ReadKey(true);

            Console.Clear();
            DrawHeader();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    STEP 2: COMPRESSING FILES");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine("    Input path (or 'back'): C:\\MyFolder\\document.pdf");
            Console.WriteLine("                             ^^^^^^^^^^^^^^^^^^^^^^^^");
            Console.WriteLine("                             Type the file/folder path here");
            Console.WriteLine();
            Console.WriteLine("    Output file (or ENTER for default): ");
            Console.WriteLine("                                         ^^^^");
            Console.WriteLine("                                         Press ENTER for auto-naming");
            Console.WriteLine("                                         or type custom name");
            Console.WriteLine();
            Console.WriteLine("    Password (2+ chars or ENTER for none): ******");
            Console.WriteLine("              ^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
            Console.WriteLine("              Optional! Press ENTER to skip encryption");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("    TIP: You can also drag & drop files onto zipped.exe!");
            Console.WriteLine("         Or right-click any file and choose 'Compress with Zipped'");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n    Press ENTER to continue...");
            Console.ResetColor();
            Console.ReadKey(true);

            Console.Clear();
            DrawHeader();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("    STEP 3: EXTRACTING ARCHIVES");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine("    Archive path (or 'back'): C:\\Archives\\backup.zipped");
            Console.WriteLine("                               ^^^^^^^^^^^^^^^^^^^^^^^");
            Console.WriteLine("                               Type your .zipped file path here");
            Console.WriteLine();
            Console.WriteLine("    Output folder (or ENTER for default): ");
            Console.WriteLine("                                           ^^^^");
            Console.WriteLine("                                           Press ENTER for auto folder");
            Console.WriteLine("                                           or choose custom location");
            Console.WriteLine();
            Console.WriteLine("    Password (or ENTER if none): ");
            Console.WriteLine("              ^^^^^^^^^^^^^^^^^^");
            Console.WriteLine("              Enter the password you used (if any)");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("    TIP: Double-click any .zipped file to extract it quickly!");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n    Press ENTER to continue...");
            Console.ResetColor();
            Console.ReadKey(true);

            Console.Clear();
            DrawHeader();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    STEP 4: PROGRESS TRACKING");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine("    While compressing or extracting, you'll see:");
            Console.WriteLine();
            Console.WriteLine("    Compressing: [##########----------] 50%");
            Console.WriteLine("                  ^^^^^^^^^^");
            Console.WriteLine("                  Real-time progress bar");
            Console.WriteLine();
            Console.WriteLine("    Encrypting:  [####################] 100%");
            Console.WriteLine("                                         ^^^^");
            Console.WriteLine("                                         Shows percentage complete");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("    The app uses real compression (DEFLATE) to reduce file size");
            Console.WriteLine("    and military-grade AES-256 encryption for security!");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n    Press ENTER to continue...");
            Console.ResetColor();
            Console.ReadKey(true);

            Console.Clear();
            DrawHeader();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("    STEP 5: PRO TIPS & SHORTCUTS");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    → Type 'back' anytime to return to main menu");
            Console.WriteLine("               ^^^^");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    → Press ENTER for default options");
            Console.WriteLine("              ^^^^^");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    → Right-click any file/folder in Windows Explorer");
            Console.WriteLine("      and select 'Compress with Zipped'");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    → Double-click .zipped files to extract them");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    → Password minimum is only 2 characters");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    → 100% offline - no internet connection needed!");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n    Press ENTER to continue...");
            Console.ResetColor();
            Console.ReadKey(true);

            Console.Clear();
            DrawHeader();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    ╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("    ║              TOUR COMPLETE!                              ║");
            Console.WriteLine("    ╚══════════════════════════════════════════════════════════╝\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("    You're all set to use Zipped!");
            Console.WriteLine();
            Console.WriteLine("    Quick Recap:");
            Console.WriteLine("    • [1] Compress - Make encrypted archives");
            Console.WriteLine("    • [2] Extract - Open your archives");
            Console.WriteLine("    • Right-click files for quick compress");
            Console.WriteLine("    • Drag & drop for instant action");
            Console.WriteLine("    • Type 'back' anytime to go back");
            Console.WriteLine();
            Console.WriteLine("    Security Features:");
            Console.WriteLine("    • AES-256 Encryption");
            Console.WriteLine("    • Real DEFLATE Compression");
            Console.WriteLine("    • 100% Offline & Private");
            Console.WriteLine();
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    Press any key to return to the main menu...");
            Console.ResetColor();

            Console.ReadKey(true);
        }

        static void DrawHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
    ███████╗██╗██████╗ ██████╗ ███████╗██████╗ 
    ╚══███╔╝██║██╔══██╗██╔══██╗██╔════╝██╔══██╗
      ███╔╝ ██║██████╔╝██████╔╝█████╗  ██║  ██║
     ███╔╝  ██║██╔═══╝ ██╔═══╝ ██╔══╝  ██║  ██║
    ███████╗██║██║     ██║     ███████╗██████╔╝
    ╚══════╝╚═╝╚═╝     ╚═╝     ╚══════╝╚═════╝ 
");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("    SECURE FILE COMPRESSION & ENCRYPTION");
            Console.WriteLine("    AES-256 | DEFLATE | 100% Offline\n");
            Console.ResetColor();
        }

        static void DrawMenu()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    ╔═════════════════════════════════════════╗");
            Console.WriteLine("    ║                                         ║");
            Console.WriteLine("    ║  [1] COMPRESS File or Folder            ║");
            Console.WriteLine("    ║  [2] EXTRACT Archive                    ║");
            Console.WriteLine("    ║  [3] Take the Tour                      ║");
            Console.WriteLine("    ║  [4] Exit                               ║");
            Console.WriteLine("    ║                                         ║");
            Console.WriteLine("    ╚═════════════════════════════════════════╝");
            Console.ResetColor();

            Console.Write("\n    Select [1-4]: ");
        }

        static bool CompressInteractive()
        {
            Console.Clear();
            DrawHeader();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    [COMPRESS MODE]\n");
            Console.ResetColor();

            string path = "";

            if (!string.IsNullOrEmpty(draggedPath))
            {
                path = draggedPath;
                Console.WriteLine("    Input: " + path);
                draggedPath = "";
            }
            else
            {
                Console.Write("    Input path (or 'back'): ");
                string input = Console.ReadLine();

                if (input != null && input.Trim().ToLower() == "back")
                    return true;

                path = input != null ? input.Trim('"') : "";
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                ShowError("Path not found!");
                return false;
            }

            Console.Write("    Output file (or ENTER for default): ");
            string outputInput = Console.ReadLine();
            string outputPath = "";

            if (string.IsNullOrWhiteSpace(outputInput))
            {
                outputPath = path + ".zipped";
            }
            else
            {
                outputPath = outputInput.Trim('"');
                if (!outputPath.EndsWith(".zipped", StringComparison.OrdinalIgnoreCase))
                {
                    outputPath += ".zipped";
                }
            }

            CompressMode(path, outputPath);
            return false;
        }

        static bool ExtractInteractive()
        {
            Console.Clear();
            DrawHeader();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("    [EXTRACT MODE]\n");
            Console.ResetColor();

            string path = "";

            if (!string.IsNullOrEmpty(draggedPath))
            {
                path = draggedPath;
                Console.WriteLine("    Archive: " + path);
                draggedPath = "";
            }
            else
            {
                Console.Write("    Archive path (or 'back'): ");
                string input = Console.ReadLine();

                if (input != null && input.Trim().ToLower() == "back")
                    return true;

                path = input != null ? input.Trim('"') : "";
            }

            if (!File.Exists(path))
            {
                ShowError("File not found!");
                return false;
            }

            Console.Write("    Output folder (or ENTER for default): ");
            string outputInput = Console.ReadLine();
            string outputFolder = "";

            if (string.IsNullOrWhiteSpace(outputInput))
            {
                outputFolder = Path.Combine(
                    Path.GetDirectoryName(path) ?? "",
                    Path.GetFileNameWithoutExtension(path)
                );
            }
            else
            {
                outputFolder = outputInput.Trim('"');
            }

            ExtractMode(path, outputFolder);
            return false;
        }

        static void CompressMode(string path, string outputPath)
        {
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("    Password (2+ chars or ENTER for none): ");
            Console.ResetColor();
            string password = ReadPassword();

            if (!string.IsNullOrEmpty(password) && password.Length < 2)
            {
                ShowError("Password too short (minimum 2 characters)");
                return;
            }

            if (!string.IsNullOrEmpty(password))
            {
                Console.Write("    Confirm password:                       ");
                string confirm = ReadPassword();

                if (password != confirm)
                {
                    ShowError("Passwords don't match!");
                    return;
                }
            }

            Console.WriteLine();

            try
            {
                Compress(path, outputPath, password);
                Console.WriteLine();
                ShowSuccess("Archive created!");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("    Location: " + outputPath);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        static void ExtractMode(string archivePath, string outputFolder)
        {
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("    Password (or ENTER if none): ");
            Console.ResetColor();
            string password = ReadPassword();

            Console.WriteLine();

            try
            {
                Extract(archivePath, outputFolder, password);
                Console.WriteLine();
                ShowSuccess("Extraction complete!");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("    Location: " + outputFolder);
                Console.ResetColor();
            }
            catch (CryptographicException)
            {
                ShowError("Wrong password or corrupted archive!");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        static void Compress(string inputPath, string outputPath, string password)
        {
            string tempZip = Path.GetTempFileName();

            try
            {
                long totalSize = 0;
                long processedSize = 0;

                if (File.Exists(inputPath))
                {
                    totalSize = new FileInfo(inputPath).Length;
                }
                else
                {
                    foreach (string file in Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories))
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                }

                Console.Write("    Compressing: [");
                int progressPos = Console.CursorLeft;
                int progressTop = Console.CursorTop;
                Console.Write("                    ] 0%   ");

                using (FileStream fs = new FileStream(tempZip, FileMode.Create))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    if (File.Exists(inputPath))
                    {
                        zip.CreateEntryFromFile(inputPath, Path.GetFileName(inputPath), CompressionLevel.Optimal);
                        processedSize = totalSize;
                        UpdateProgress(progressPos, progressTop, 100);
                    }
                    else
                    {
                        processedSize = AddDirectoryWithProgress(zip, inputPath, "", totalSize, processedSize, progressPos, progressTop);
                    }
                }

                Console.WriteLine();

                if (!string.IsNullOrEmpty(password))
                {
                    Console.Write("    Encrypting:  [");
                    progressPos = Console.CursorLeft;
                    progressTop = Console.CursorTop;
                    Console.Write("                    ] 0%   ");

                    EncryptFile(tempZip, outputPath, password);

                    UpdateProgress(progressPos, progressTop, 100);
                    Console.WriteLine();
                }
                else
                {
                    File.Copy(tempZip, outputPath, true);
                }

                File.Delete(tempZip);
            }
            catch
            {
                if (File.Exists(tempZip)) File.Delete(tempZip);
                if (File.Exists(outputPath)) File.Delete(outputPath);
                throw;
            }
        }

        static long AddDirectoryWithProgress(ZipArchive zip, string dirPath, string relativePath, long totalSize, long processedSize, int progressPos, int progressTop)
        {
            foreach (string file in Directory.GetFiles(dirPath))
            {
                string entryName = Path.Combine(relativePath, Path.GetFileName(file));
                zip.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);

                processedSize += new FileInfo(file).Length;
                int percent = totalSize > 0 ? (int)((processedSize * 100) / totalSize) : 100;
                UpdateProgress(progressPos, progressTop, percent);
            }

            foreach (string dir in Directory.GetDirectories(dirPath))
            {
                string entryName = Path.Combine(relativePath, Path.GetFileName(dir));
                processedSize = AddDirectoryWithProgress(zip, dir, entryName, totalSize, processedSize, progressPos, progressTop);
            }

            return processedSize;
        }

        static void UpdateProgress(int progressPos, int progressTop, int percent)
        {
            Console.SetCursorPosition(progressPos, progressTop);
            int blocks = percent / 5;
            string bar = new string('#', blocks) + new string('-', 20 - blocks);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(bar);
            Console.ResetColor();
            Console.Write("] " + percent + "%   ");
        }

        static void Extract(string archivePath, string outputFolder, string password)
        {
            string tempZip = Path.GetTempFileName();

            try
            {
                if (IsEncrypted(archivePath))
                {
                    Console.Write("    Decrypting:  [");
                    int progressPos = Console.CursorLeft;
                    int progressTop = Console.CursorTop;
                    Console.Write("                    ] 0%   ");

                    DecryptFile(archivePath, tempZip, password);

                    UpdateProgress(progressPos, progressTop, 100);
                    Console.WriteLine();
                }
                else
                {
                    File.Copy(archivePath, tempZip, true);
                }

                Console.Write("    Extracting:  [");
                int extractPos = Console.CursorLeft;
                int extractTop = Console.CursorTop;
                Console.Write("                    ] 0%   ");

                Directory.CreateDirectory(outputFolder);

                using (ZipArchive zip = ZipFile.OpenRead(tempZip))
                {
                    int totalFiles = zip.Entries.Count(e => !string.IsNullOrEmpty(e.Name));
                    int processedFiles = 0;

                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        string destPath = Path.Combine(outputFolder, entry.FullName);

                        string destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(destPath, true);
                            processedFiles++;
                            int percent = totalFiles > 0 ? (processedFiles * 100) / totalFiles : 100;
                            UpdateProgress(extractPos, extractTop, percent);
                        }
                    }
                }

                Console.WriteLine();
                File.Delete(tempZip);
            }
            catch
            {
                if (File.Exists(tempZip)) File.Delete(tempZip);
                throw;
            }
        }

        static bool IsEncrypted(string path)
        {
            try
            {
                using (FileStream fs = File.OpenRead(path))
                {
                    byte[] header = new byte[8];
                    fs.Read(header, 0, 8);
                    return Encoding.ASCII.GetString(header) == "ZIPPED20";
                }
            }
            catch
            {
                return false;
            }
        }

        static void EncryptFile(string inputPath, string outputPath, string password)
        {
            byte[] salt = new byte[32];
            byte[] iv = new byte[16];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
                rng.GetBytes(iv);
            }

            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            using (Aes aes = Aes.Create())
            {
                aes.Key = pbkdf2.GetBytes(32);
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (FileStream inputStream = File.OpenRead(inputPath))
                using (FileStream outputStream = File.Create(outputPath))
                {
                    outputStream.Write(Encoding.ASCII.GetBytes("ZIPPED20"), 0, 8);
                    outputStream.Write(salt, 0, 32);
                    outputStream.Write(iv, 0, 16);

                    using (CryptoStream cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        inputStream.CopyTo(cryptoStream);
                    }
                }
            }
        }

        static void DecryptFile(string inputPath, string outputPath, string password)
        {
            using (FileStream inputStream = File.OpenRead(inputPath))
            {
                byte[] header = new byte[8];
                inputStream.Read(header, 0, 8);

                if (Encoding.ASCII.GetString(header) != "ZIPPED20")
                    throw new InvalidDataException("Invalid archive format");

                byte[] salt = new byte[32];
                byte[] iv = new byte[16];
                inputStream.Read(salt, 0, 32);
                inputStream.Read(iv, 0, 16);

                using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                using (Aes aes = Aes.Create())
                {
                    aes.Key = pbkdf2.GetBytes(32);
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (FileStream outputStream = File.Create(outputPath))
                    using (CryptoStream cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        cryptoStream.CopyTo(outputStream);
                    }
                }
            }
        }

        static string ReadPassword()
        {
            StringBuilder password = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            Console.WriteLine();
            return password.ToString();
        }

        static void ShowSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    [SUCCESS] " + message);
            Console.ResetColor();
        }

        static void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("    [ERROR] " + message);
            Console.ResetColor();
            Thread.Sleep(2000);
        }
    }
}