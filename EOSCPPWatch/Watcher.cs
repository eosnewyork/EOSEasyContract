using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace EOSCPPWatch
{
    static class Watcher
    {
        static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static Queue<string> buildQueue = new Queue<string>();
        static bool building = false;
        static WatchArgs args = null;
        static string dockerBuildContainerName = "/EOSCDT";

        public static void start(WatchArgs watchArgs)
        {
            args = watchArgs;
            FileSystemWatcher watcher_cpp = new FileSystemWatcher(args.Path);
            //watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher_cpp.IncludeSubdirectories = true;
            //watcher_cpp.Filter = "*.cpp";
            watcher_cpp.Renamed += new RenamedEventHandler(renamed);
            watcher_cpp.Deleted += new FileSystemEventHandler(changed);
            watcher_cpp.Changed += new FileSystemEventHandler(changed);
            watcher_cpp.Created += new FileSystemEventHandler(changed);
            watcher_cpp.EnableRaisingEvents = true;


        }

        private static void renamed(object sender, RenamedEventArgs e)
        {
            //logger.Info("rename" + DateTime.Now + ": " + e.ChangeType + " " + e.FullPath);
            addToQueue(e.FullPath);
        }

        private static void changed(object sender, FileSystemEventArgs e)
        {
            //logger.Info("change" + DateTime.Now + ": " + e.ChangeType + " " + e.FullPath);
            addToQueue(e.FullPath);
        }

        private static void addToQueue(string fullPath)
        {
            var buildPath = Path.Combine(args.Path, "build");
            if (!fullPath.StartsWith(buildPath))
            {
                FileInfo info = new FileInfo(fullPath);
                if (info.Extension.Contains("cpp") || info.Extension.Contains("hpp"))
                    buildQueue.Enqueue(DateTime.Now.ToString());
            }

        }

        public static void checkQueue()
        {
            if (buildQueue.Count > 0)
            {
                buildQueue.Clear();
                if (!building)
                {
                    logger.Info("Begin EOSIO contract build");
                    build();
                }
                else
                {
                    logger.Info("Build already in progress. Save again once this build has completed");
                }
            }
        }

        private static void build()
        {
            //If we're already building then exit
            if (building)
                return;

            building = true;
            try
            {
                logger.Info("Building ... ");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                /*
                                IConfiguration config = new ConfigurationBuilder()
                                    .AddJsonFile("appsettings.json", true, true)
                                    .Build();

                                var eosiocppDockerImage = config["eosiocppDockerImage"];
                */
                var containerExists = DockerHelper.CheckContainerExistsAsync(dockerBuildContainerName).Result;
                if (!containerExists)
                {
                    var n = DockerHelper.StartDockerAsync(args.DockerImage, dockerBuildContainerName, false).Result;
                }

                //string cmd = "eosiocpp";
                string cmd = "./build.sh";
                var asyncResult = DockerHelper.RunCommandAsync(cmd).Result;

                sw.Stop();
                logger.Info("Done Building. Build Duration = " + sw.Elapsed);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
            finally
            {
                building = false;
            }
        }

    }
}
