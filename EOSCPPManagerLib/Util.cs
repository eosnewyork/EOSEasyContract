using System;
using System.Collections.Generic;
using System.Text;

namespace EOSCPPManagerLib
{
    public static class Util
    {
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static string getContainerName(string sourceCodePath)
        {
            string pathMD5 = Util.CreateMD5(sourceCodePath);
            return "EOSCDT" + "-" + pathMD5;
        }

        public static void copyLibs(string destinationPath)
        {

        }
    }
}
