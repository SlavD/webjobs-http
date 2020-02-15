using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace RunFuncEmulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"AzureFunctionsTools\Releases\2.45.1\cli_x64\func.exe");

            /*
            using (var runspace = RunspaceFactory.CreateRunspace())
            {
                System.Environment.CurrentDirectory = @"C:\Users\sdorz\source\repos\WebJobSDKSample\FunctionApp1";
                runspace.Open();

                using (Pipeline pipeline = runspace.CreatePipeline())
                {
                    pipeline.Commands.Add(fileName + " start");
                    var result = pipeline.Invoke();
                }
                runspace.Close();

            }*/

            using (var ps = PowerShell.Create())
            {
                var results = ps.AddScript(fileName + " start").Invoke();
                foreach (var result in results)
                {
                    Debug.Write(result.ToString());
                }
            }

            //Console.WriteLine(result);
        }
    }
}
