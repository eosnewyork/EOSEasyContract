using NLog;
using System;
using System.IO;

namespace EOSCPPManagerLib
{
    public class EOSCPPManagerCore
    {
        static Logger logger = NLog.LogManager.GetCurrentClassLogger();
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
                var buildFile = Path.Combine(fullPath, "build.sh");
                File.Copy(Path.Combine(baseDir,"templateFiles\\build.sh"), buildFile);
                var cmakelistFile = Path.Combine(fullPath, "CMakeLists.txt");
                File.Copy(Path.Combine(baseDir, "templateFiles\\CMakeLists.txt"), cmakelistFile);

                // Replace the test references with the name of the template. 
                string cmakeText = File.ReadAllText(cmakelistFile);
                cmakeText = cmakeText.Replace("test.cpp", contractName+".cpp");
                cmakeText = cmakeText.Replace("test.wasm", contractName + ".wasm");
                File.WriteAllText(cmakelistFile, cmakeText);

                // Copy the cpp and hpp file - the template itself. 
                var cppFile = Path.Combine(fullPath, contractName+".cpp");
                File.Copy(Path.Combine(baseDir, "templateFiles\\test.cpp"), cppFile);
                var hppFile = Path.Combine(fullPath, contractName+".hpp");
                File.Copy(Path.Combine(baseDir, "templateFiles\\test.hpp"), hppFile);

                // Create the .vscode folder and content so that vscode knows how to handle the contents
                String vscodeFolder = Path.Combine(fullPath, ".vscode");
                Directory.CreateDirectory(vscodeFolder);
                foreach (var srcPath in Directory.GetFiles(Path.Combine(baseDir, "templateFiles\\.vscode")))
                {
                    FileInfo fileInfo = new FileInfo(srcPath);
                    File.Copy(srcPath, Path.Combine(vscodeFolder,fileInfo.Name), true);
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
