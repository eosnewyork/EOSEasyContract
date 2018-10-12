using EOSCPPManagerLib;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Threading;

namespace EOSEasyContract
{
    class Program
    {
        static Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static int Main(string[] args)
        {

            var app = new CommandLineApplication
            {
                Name = "EOSCPPManager",
                Description = "A EOS Smart Contract helper tool",
            };

            app.ThrowOnUnexpectedArgument = false;
            app.HelpOption(inherited: true);

            app.Command("util", configCmd =>
            {
                configCmd.OnExecute(() =>
                {
                    //Console.WriteLine("Specify a subcommand");
                    configCmd.ShowHelp();
                    return 1;
                });


                configCmd.Command("cleanDocker", setCmd =>
                {
                    setCmd.Description = "Remove unused Docker containers that have been used to compile";

                    setCmd.OnExecute(() =>
                    {
                        logger.Info($"Start cleanup");

                        try
                        {
                            var x = DockerHelper.dockerCleanup().Result;
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex.Message);
                        }

                    });
                });

            });

            app.Command("init", configCmd =>
            {
                configCmd.OnExecute(() =>
                {
                    //Console.WriteLine("Specify a subcommand");
                    configCmd.ShowHelp();
                    return 1;
                });


                configCmd.Command("docker", setCmd =>
                {
                    setCmd.Description = "Check that the required docker images exist on the machine and download any missing";

                    setCmd.OnExecute(() =>
                    {

                        IConfiguration config = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json", true, true)
                            .Build();
                        var eosiocppDockerImage = config["eosiocppDockerImage"];

                        logger.Info("Check connection to local docker instance and confirm that image \"{0}\" exists", eosiocppDockerImage);

                        var exists = DockerHelper.CheckImageExistsAsync(eosiocppDockerImage).Result;
                        if (!exists)
                        {
                            var y = DockerHelper.getImageAsync(eosiocppDockerImage).Result;
                        }
                        else
                        {
                            logger.Info("Connection = good. Image exists. You're good to go.");
                        }
                        //logger.Info(list);
                    });
                });

                configCmd.Command("windows", setCmd =>
                {
                    setCmd.Description = "Initialize windows PATH variable - Must be run as Administartor";

                    setCmd.OnExecute(() =>
                    {
                        logger.Info($"Start Initialize windows env");
                        //if(DirectoryExistsAttribute.e)
                        var lib = new EOSCPPManagerCore();
                        try
                        {
                            lib.initializeWindowEnv();
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex.Message);
                        }

                    });
                });

            });

            app.Command("template", configCmd =>
            {
                configCmd.OnExecute(() =>
                {
                    //Console.WriteLine("Specify a subcommand");
                    configCmd.ShowHelp();
                    return 1;
                });

                configCmd.Command("new", setCmd =>
                {
                    setCmd.Description = "Create a new smart contract template";
                    var pathOption = setCmd.Option("-p|--path <PATH>", "The path to the parent folder, which will contain the new template", CommandOptionType.SingleValue).IsRequired();
                    var nameOption = setCmd.Option("-n|--name <NAME>", "The path to the parent folder, which will contain the new template", CommandOptionType.SingleValue).IsRequired();
                    var overwriteOption = setCmd.Option("-o|--overwrite", "If a project exists, delete and create new", CommandOptionType.NoValue);

                    setCmd.OnExecute(() =>
                    {
                        String path = pathOption.Value();
                        String name = nameOption.Value();
                        bool overwrite = false;
                        if (overwriteOption.Values.Count > 0)
                            overwrite = true;

                        //Console.WriteLine($"Create new project in {path}");
                        logger.Info($"Begin create of new smart contract {name} in parent folder {path}");
                        //if(DirectoryExistsAttribute.e)
                        var lib = new EOSCPPManagerCore();
                        try
                        {
                            lib.createNewSmartContract(path, name, overwrite);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex.Message);
                        }

                    });
                });

            });

            app.Command("build", configCmd =>
            {

                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true, true)
                    .Build();
                var eosiocppDockerImage = config["eosiocppDockerImage"];

                var pathOption = configCmd.Option("-p|--path <PATH>", "The path to the parent folder, which will contain the new template (*Required)", CommandOptionType.SingleValue).IsRequired();
                var dockerImageOption = configCmd.Option("-d|--dockerImage <ImageName>", string.Format("The name of the docker image to use (Default: {0})", eosiocppDockerImage), CommandOptionType.SingleValue);
                var watchOption = configCmd.Option("-w|--watch", "Continue watching this folder for changes (*Currently Required)", CommandOptionType.NoValue);


                configCmd.OnExecute(() =>
                {
                    var path = pathOption.Value();
                    //var dockerImage = "binaryfocus/eosio_wasm_1.2.6";
                    var dockerImage = eosiocppDockerImage;
                    var watch = false;
                    if (dockerImageOption.Values.Count > 0)
                        dockerImage = dockerImageOption.Value();
                    if (watchOption.Values.Count > 0)
                        watch = true;

                    if(watch)
                        Console.WriteLine("Begin watching {0}. Build using docker image {1}.", path, dockerImage, watch);
                    else
                        Console.WriteLine("Compile {0} using docker image {1}. Continue watching = {2}", path, dockerImage, watch);


                    //logger.Info("Begin watching '{0}'", parsedWatchArgs.Path);

                    Watcher.start(path,dockerImage);
                    //It doesn't matter what file name we push onto the list, as long as it's a cpp or hpp it'll trigger a build.
                    DockerHelper.init(path, dockerImage, watch);
                    Watcher.addToQueue("test.cpp");
                    

                    if (watch)
                    {
                        while (1 < 2)
                        {
                            Thread.Sleep(500);
                            // Every 500ms we chech the build queue and trigger a build in the Docker container. 
                            // The purpose of the queue is to make sure that we don't end up with multiple builds in parallel. 
                            Watcher.checkQueue();
                        }

                    } else
                    {
                        //logger.Info("We're still experiencing some problems with the one off build command and docker. Please use the --watch option to watch the folder instead.");
                        Watcher.checkQueue();
                    }

                    //configCmd.ShowHelp();
                    return 1;
                });
            });

            app.OnExecute(() =>
            {
                Console.WriteLine("Specify a subcommand");
                app.ShowHelp();
                return 1;
            });

            int result = -1;
            try
            {
                result = app.Execute(args);
            }
            catch (Exception ex)
            {
                app.ShowHelp();
            }

            return result;
            

        }

    }
}
