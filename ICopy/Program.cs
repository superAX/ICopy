using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ICopy
{
    class Program
    {
        private string copyType = "Copy";   // Default Operation : Copy (Will Not re-copy up-to-date items)
        private bool encrypt = false;       // Default: No encrypt  
        private bool decrypt = false;       // Default: No decrypt    
        private bool noCache = false;       
        private bool containsSub = true;    // Default: Include sub directory    
        private string pattern = "*.*";
        private string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ICopy.log");
        private string tempDir = Path.GetTempPath();
        private CAesCrypt aes = new CAesCrypt();
        private List<string> excludeList = new List<string>();

        static void Main(string[] args)
        {
            Program prg = new Program();
            prg.Run(args);
        }

        public void Run(string[] args)
        {
            if (args.Length < 2)
            {
                WriteLog("Invalid Parameters: ICopy sourceDir targetDir [/full] [/incremental] [/differential] [/move] [/nocache] [/nosub] [/encrypt] [/decrypt] [/exclude=.ext]");
            }
            else
            {
                for (int i = 2; i < args.Length; i++)
                {
                    string arg = args[i].ToLower();
                    if (arg == "/full")
                    {
                        copyType = "Full";
                    }
                    else if (arg == "/incremental")
                    {
                        copyType = "Incremental";
                    }
                    else if (arg == "/differential")
                    {
                        copyType = "Differential";
                    }
                    else if (arg == "/move")
                    {
                        copyType = "Move";
                    }
                    else if (arg == "/encrypt")
                    {
                        encrypt = true;
                    }
                    else if (arg == "/decrypt")
                    {
                        decrypt = true;
                    }
                    else if (arg == "/nocache")
                    {
                        noCache = true;
                    }
                    else if (arg == "/nosub")
                    {
                        containsSub = false;
                    }
                    else if (arg.Length >= 8 && arg.Substring(0, 8) == "/exclude")
                    {
                        if (arg.IndexOf('=') > 0)
                        {
                            string pattern = arg.Substring(arg.IndexOf('=') + 1);
                            pattern = "^" + pattern.Replace(".", @"\.").Replace("*", ".+") + "$";
                            excludeList.Add(pattern);
                        }
                    }
                }
                try
                {
                    Copy(genSrcPath(args[0]), genTarPath(args[1]));
                }
                catch (Exception ex)
                {
                    WriteError(ex.Message);
                }
                WriteInfo("The End.");
                WriteInfo(string.Empty);
            }
        }

        private string genSrcPath(string srcPath)
        {
            // Source path refers to a directory
            if (srcPath[srcPath.Length - 1] == '\\')
            {
                return srcPath;
            }
            // Source path refers to a directory which exists but without '\' sign
            else if (Directory.Exists(srcPath + "\\"))
            {
                return srcPath + "\\";
            }
            // Source path is a file
            else
            {
                containsSub = false;
                pattern = Path.GetFileName(srcPath);
                return Path.GetDirectoryName(srcPath);
            }
        }

        private string genTarPath(string tarPath)
        {
            if (tarPath[tarPath.Length - 1] == '\\')
            {
                return tarPath;
            }
            return tarPath + "\\";
        }

        private void Copy(string sourceDir, string targetDir)
        {
            string[] fileSet, dirSet;

            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            fileSet = Directory.GetFiles(sourceDir, pattern);
            dirSet = Directory.GetDirectories(sourceDir);

            foreach (string sourceFile in fileSet)
            {
                FileAttributes fa = File.GetAttributes(sourceFile);
                string targetFile = Path.Combine(targetDir, Path.GetFileName(sourceFile));

                if (copyType == "Full") //Full
                {
                    if (FileCopy(sourceFile, targetFile))
                    {   
                        // Clear the Archive from source file 
                        File.SetAttributes(sourceFile, fa & ~FileAttributes.Archive);
                        // Set the Archive for target file
                        File.SetAttributes(targetFile, FileAttributes.Archive);
                        WriteLog(sourceFile + " ------> " + targetFile);
                    }
                }
                else if (copyType == "Incremental")    //Incremental
                {
                    // Check whether this file is moidified or not
                    if ((fa & FileAttributes.Archive) != 0)
                    {
                        if (FileCopy(sourceFile, targetFile))
                        {
                            File.SetAttributes(targetFile, FileAttributes.Archive);
                            // Clear the Archive of the copyed file 
                            File.SetAttributes(sourceFile, fa & ~FileAttributes.Archive);
                            WriteLog(sourceFile + " ------> " + targetFile);
                        }
                    }
                }
                else if (copyType == "Differential")  //Differential
                {
                    if ((fa & FileAttributes.Archive) != 0)
                    {
                        if (FileCopy(sourceFile, targetFile))
                        {
                            File.SetAttributes(targetFile, FileAttributes.Archive);
                            WriteLog(sourceFile + " ------> " + targetFile);
                        }
                    }
                }
                else if (copyType == "Move")  //Move
                {
                    if (FileCopy(sourceFile, targetFile))
                    {
                        File.Delete(sourceFile);
                        WriteLog(sourceFile + " ------> " + targetFile);
                    }
                }
                else if (copyType == "Copy")  //Copy
                {
                    if (FileCopy(sourceFile, targetFile))
                    {
                        WriteLog(sourceFile + " ------> " + targetFile);
                    }
                }
            }

            // Copy the sub-directory recrusively
            if (containsSub && dirSet.Length > 0)
            {
                foreach (string subDir in dirSet) Copy(subDir, subDir.Replace(sourceDir, targetDir));              
            }
        }

        private bool FileCopy(string source, string target)
        {
            string srcFile = source;
            string file = Path.GetFileName(source);

            // If files are in exclude list, skip copy operation and return false
            foreach (string exclude in excludeList)
            {
                Regex rgx = new Regex(exclude);
                Console.WriteLine("exclude = " + rgx);
                if (rgx.IsMatch(file)) return false;
            }

            if (File.Exists(target)) File.Delete(target);   // Delete the files in target path if already exist
            
            // if encrypt/decrypt is needed 
            if (encrypt)
            {
                srcFile = Path.Combine(tempDir, Path.GetFileName(source));
                aes.EncryptFile(source, srcFile);
            }
            else if (decrypt)
            {
                srcFile = Path.Combine(tempDir, Path.GetFileName(source));
                aes.DecryptFile(source, srcFile);
            }
            
            if (noCache)
            {
                NoCacheCopy(srcFile, target);   // For extremely large file
            }
            else
            {
                if (copyType == "Move")
                    File.Move(srcFile, target);
                else
                    File.Copy(srcFile, target, true);
            }

            if (srcFile != source) File.Delete(srcFile);    // Delete the temp file
            if (copyType == "Move") File.Delete(source);

            return true;
        }

        private void NoCacheCopy(string source, string target)
        {
            int size;
            byte[] buf = new byte[134217728];      // 128 MB per time
            FileStream fs1 = new FileStream(source, FileMode.Open);
            FileStream fs2 = new FileStream(target, FileMode.Create);

            while ((size = fs1.Read(buf, 0, buf.Length)) > 0)
            {
                fs2.Write(buf, 0, size);
            }
            fs1.Close();
            fs2.Close();
        }

        private void WriteLog(string text)
        {
            string s = string.Empty;
            if (encrypt)
            {
                s = "<Encrypt> ";
            }
            else if (decrypt)
            {
                s = "<Decrypt> ";
            }
            StreamWriter sw = new StreamWriter(logPath, true);
            sw.WriteLine(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "<" + copyType + "> " + s + text);
            sw.Close();
            Console.WriteLine(text);
        }
        private void WriteError(string text)
        {
            StreamWriter sw = new StreamWriter(logPath, true);
            sw.WriteLine(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "<Error> " + text);
            sw.Close();
            Console.WriteLine(text);
        }
        private void WriteInfo(string text)
        {
            StreamWriter sw = new StreamWriter(logPath, true);
            if (text == string.Empty)
            {
                sw.WriteLine();
            }
            else
            {
                sw.WriteLine(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + text);
            }
            sw.Close();
            Console.WriteLine(text);
        }
    }
}