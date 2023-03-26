using System;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FileLibrary;

namespace ConsoleUploadFile2OneDrive // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string parameterFolderPath = "--path";
            const string parameterClientID = "--clientid";
            const string parameterClientSecret = "--clientsecret";
            const string parameterUPN = "--upn";
            const string parameterTenantId = "--tenantid";
            const string parameterOutput = "--output";

            Console.WriteLine("Working");
           
            string path = String.Empty;
            string clientID = string.Empty;
            string upn = String.Empty;
            string clientSecret = string.Empty;
            string output = String.Empty;
            string tenantid = string.Empty;


            var length = args.Length;

            for (int i = 0; i < length; i++)
            {
                switch (args[i].Trim().ToLower())
                {
                    case parameterClientID:
                        clientID = args[i + 1];
                        break;
                    case parameterClientSecret:
                        clientSecret = args[i + 1];
                        break;
                    case parameterFolderPath:
                        path = args[i + 1];
                        break;
                    case parameterOutput:
                        output = args[i + 1];
                        break;
                    case parameterTenantId:
                        tenantid = args[i + 1];
                        break;
                    case parameterUPN:
                        upn = args[i + 1];
                        break;

                    default:
                        break;
                }
            }
            if ( string.IsNullOrEmpty(clientID) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(path)
                || string.IsNullOrEmpty(output) || string.IsNullOrEmpty(tenantid) || string.IsNullOrEmpty(upn))
            {
                Console.WriteLine("Empty parameters!");
                return;
            }
            (new FileUtils(clientID, clientSecret, tenantid, path, upn, output)).Upload();
        }
    }
}