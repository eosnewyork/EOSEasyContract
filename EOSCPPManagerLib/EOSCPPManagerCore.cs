using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EOSCPPManagerLib
{
    public class EOSCPPManagerCore
    {
        static Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        public void createNewSmartContract(string folder, string contractName, bool overwriteExisting)
        {

            //logger.Info("Create new smart contract template \"{0}\" in \"{1}\"", contractName, folder);
            if(!Directory.Exists(folder))
            {
                throw new Exception("The folder \"{0}\", does not exist of can not be accessed");
            }

            string fullPath = Path.Combine(folder, contractName);
            logger.Info("Create new smart contract template \"{0}\" in \"{1}\"", contractName, fullPath);

            if (Directory.Exists(fullPath) && !overwriteExisting)
            {
                throw new Exception(string.Format("The folder \"{0}\", already exist. Please delete the existing template if you'd like to create a new template at this location", fullPath));
            }
            else
            {
                if(Directory.Exists(fullPath))
                {
                    if(!String.IsNullOrEmpty(fullPath))
                    {
                        logger.Warn("By request, deleting {1} before creating new template at this path.", overwriteExisting, fullPath);
                        Directory.Delete(fullPath,true);
                    }
                    
                }

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                logger.Info("Creatind directory \"{0}\"", fullPath);
                Directory.CreateDirectory(fullPath);

                // Copy the build and cmakelist
                var buildFileDestinationPath = Path.Combine(fullPath, "build.sh");
                File.Copy(Path.Combine(baseDir,"templateFiles\\build.sh"), buildFileDestinationPath);
                var cmakelistFileDestinationPath = Path.Combine(fullPath, "CMakeLists.txt");
                File.Copy(Path.Combine(baseDir, "templateFiles\\CMakeLists.txt"), cmakelistFileDestinationPath);
                var gitIgnoreDestinationPath = Path.Combine(fullPath, ".gitignore");
                File.Copy(Path.Combine(baseDir, "templateFiles\\_gitignore"), gitIgnoreDestinationPath);

                

                // Replace the test references with the name of the template. 
                string updatedCmakeText = File.ReadAllText(cmakelistFileDestinationPath);
                updatedCmakeText = updatedCmakeText.Replace("hello", contractName);
                File.WriteAllText(cmakelistFileDestinationPath, updatedCmakeText);

                // Copy the cpp and hpp file - the template itself. 
                var cppFileDestinationPath = Path.Combine(fullPath, contractName+".cpp");
                File.Copy(Path.Combine(baseDir, "templateFiles\\test.cpp"), cppFileDestinationPath);
                string updatedCPPText = File.ReadAllText(cppFileDestinationPath);
                updatedCPPText = updatedCPPText.Replace("hello", contractName);
                File.WriteAllText(cppFileDestinationPath, updatedCPPText);



                var hppFile = Path.Combine(fullPath, contractName+".hpp");
                File.Copy(Path.Combine(baseDir, "templateFiles\\test.hpp"), hppFile);

                // Create the .vscode folder and content so that vscode knows how to handle the contents
                String vscodeFolder = Path.Combine(fullPath, ".vscode");
                Directory.CreateDirectory(vscodeFolder);
                foreach (var srcPath in Directory.GetFiles(Path.Combine(baseDir, "templateFiles\\.vscode")))
                {
                    FileInfo fileInfo = new FileInfo(srcPath);
                    String destFilePath = Path.Combine(vscodeFolder, fileInfo.Name);
                    File.Copy(srcPath, destFilePath, true);

                    if(destFilePath.Contains("c_cpp_properties.json"))
                    {
                        string updatedPropertiesText = File.ReadAllText(destFilePath);
                        updatedPropertiesText = updatedPropertiesText.Replace("C:/eosincludes", Util.AppDataFolder().Replace(@"\","/"));
                        File.WriteAllText(destFilePath, updatedPropertiesText);

                    }
                }

                logger.Info("Template creation completed");

            }

        }

        public void initializeWindowEnv()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            logger.Info("Adding {0} to PATH", baseDir);
            
            const string name = "PATH";
            string pathvar = System.Environment.GetEnvironmentVariable(name);

            logger.Debug("Print path before change");
            printPath(pathvar);

            if(pathvar.ToUpper().Contains(baseDir.ToUpper()))
            {
                logger.Warn("Path ENV already contains {0}. Nothing changed.", baseDir);
            }
            else
            {
                var value = pathvar + @";" + baseDir;
                var target = EnvironmentVariableTarget.Machine;
                System.Environment.SetEnvironmentVariable(name, value, target);

                logger.Info("Please close and re-open any application (e.g. vscode) / console window that requires this new PATH");
            }

        }

        public void initializeInclude()
        {
            logger.Info("Begin include init");
            //var includeFolder = config["includeFolder"];
            var includeFolder = Util.AppDataFolder();
            logger.Info("Include Folder = {0}.", includeFolder);

            if(Directory.Exists(includeFolder))
            {
                logger.Info("Deleting existsing folder");
                Directory.Delete(includeFolder, true);
            }

            if(!Directory.Exists(includeFolder))
            {
                logger.Info("Create folder {0}", includeFolder);
                Directory.CreateDirectory(includeFolder);
                logger.Info("Pause for 2 seconds before starting the copy.");
                Thread.Sleep(2000);
            }

           
            Dictionary<String, String> mounts = new Dictionary<string, string>();
            mounts.Add(includeFolder, "/host_eosinclude");

            logger.Info("Check if container {0} exists", Util.getContainerName(includeFolder));

            var containerExists = DockerHelper.CheckContainerExistsAsync(Util.getContainerName(includeFolder), mounts).Result;
            if (!containerExists)
            {
                var eosiocppDockerImage = config["eosiocppDockerImage"];
                //logger.Info("Container {0} not found. Please run \"EOSEasyContract init docker\"", eosiocppDockerImage);
                //return;
                //var eosiocppDockerImage = config["eosiocppDockerImage"];
                logger.Info("Container did not exist. Creating new Container to copy include files from: {0}", Util.getContainerName(includeFolder));
                var n = DockerHelper.StartDockerAsync(eosiocppDockerImage, Util.getContainerName(includeFolder), false, mounts).Result;
                var containerExistsTake2 = DockerHelper.CheckContainerExistsAsync(Util.getContainerName(includeFolder), mounts).Result;
                logger.Info("Check if container {0} exists", Util.getContainerName(includeFolder));
                if (!containerExistsTake2)
                {
                    logger.Error("Container not found. We tried creating the container but something went wrong and we still can't access the container.");
                    logger.Error("Container {0} not found. Please run \"EOSEasyContract init docker\" and then try again.", eosiocppDockerImage);
                    return;
                }
            }
            else
            {
                logger.Info("Existing container found");
            }


            //string cmd = "cp -R /usr/local/eosio/include /host_eosinclude";
            string cmd = @"mkdir -p /host_eosinclude/usr/local/eosio; \
cp -v -R /usr/local/eosio /host_eosinclude/usr/local; \
mkdir -p /host_eosinclude/usr/local/eosio.cdt/include; \
cp -v -R /usr/local/eosio.cdt/include /host_eosinclude/usr/local/eosio.cdt/; \
mkdir -p /host_eosinclude/usr/local/include; \
cp -v -R /usr/local/eosio/include /host_eosinclude/usr/local; \
mkdir -p /host_eosinclude/usr/global/include; \
cp -v -R /usr/include /host_eosinclude/usr/global; 

".Replace("\r","");
            var asyncResult = DockerHelper.RunCommandAsync(cmd, Util.getContainerName(includeFolder)).Result;

            logger.Info("Include File written to {0}. This include path will be referened in any new projects created.", includeFolder);

        }

        private void printPath(string path)
        {
            var parts = path.Split(';');
            foreach (var part in parts)
            {
                logger.Debug(part);
            }
            //logger.Info("Adding {0} to PATH", baseDir);
        }
    }
}
