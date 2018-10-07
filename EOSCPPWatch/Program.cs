using PowerArgs;
using System;
using System.IO;
using System.Threading;
using NLog;

namespace EOSCPPWatch
{
    class Program
    {
        static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            try
            {
                var parsedWatchArgs = Args.Parse<WatchArgs>(args);
                DockerHelper.init(parsedWatchArgs);

                try
                {
                    if (!Directory.Exists(parsedWatchArgs.Path))
                    {
                        throw new Exception(string.Format("The path provided does not exist of can not be accessed: {0}",parsedWatchArgs.Path));
                    }

                    logger.Info("Begin watching '{0}'", parsedWatchArgs.Path);
                    
                    Watcher.start(parsedWatchArgs);
                    while(1 < 2)
                    {
                        Thread.Sleep(500);
                        // Every 500ms we chech the build queue and trigger a build in the Docker container. 
                        // The purpose of the queue is to make sure that we don't end up with multiple builds in parallel. 
                        Watcher.checkQueue();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }
            }
            catch (ArgException ex)
            {
                logger.Error(ex.Message);
                logger.Error(ArgUsage.GenerateUsageFromTemplate<WatchArgs>());
            }           
        }
    }

    public class WatchArgs
    {        
        //[ArgRequired(PromptIfMissing = true)]
        [ArgRequired, ArgDescription("The path the folder you'd like to watch for changes")]
        [ArgExample(@"c:\dev\mycontract", "Example description")]
        public string Path { get; set; }

        [ArgDescription("The name of the docker images containing the eosiocpp tool")]
        [ArgDefaultValue("binaryfocus/eosio_wasm_1.2.6")]
        public string DockerImage { get; set; }
    }


    
}
