using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EOSCPPManagerLib
{
    public static class DockerHelper
    {
        static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static Uri dockerServer = new Uri("tcp://localhost:2375");
        static DockerClient client = new DockerClientConfiguration(dockerServer).CreateClient();
        //static string dockerBuildContainerPrefix = "/EOSCDT";
        //static string dockerBuildContainerName = string.Empty;
        public static string sourceCodePath = string.Empty;


        public static void init(String path, String dockerImage, bool watch)
        {
            sourceCodePath = path;
        }


        public static async Task<bool> dockerCleanup()
        {
            var containers = ListContainersAsync().Result;

            foreach (var container in containers)
            {

                foreach (var containerName in container.Names)
                {
                    //logger.Info("NAME {0}", containerName);
                    if (containerName.ToUpper().Contains("EOSCDT"))
                    {
                        logger.Info("Remove container {0}", containerName);

                        ContainerRemoveParameters removeSettings = new ContainerRemoveParameters()
                        {
                            Force = true,
                            RemoveLinks = false,
                            RemoveVolumes = false
                        };

                        await client.Containers.StopContainerAsync(container.ID, new ContainerStopParameters { });
                        await client.Containers.RemoveContainerAsync(container.ID, removeSettings, default(CancellationToken));
                        logger.Info("done");

                    }

                }
                
            }

            return true;
        }


        public static async Task<bool> getImageAsync(string imageName)
        {
            bool success = false;

            // create progress 
            var report = new Progress<JSONMessage>(msg =>
            {
                logger.Info( msg.Status);
            });

            await client.Images.CreateImageAsync(new ImagesCreateParameters
            {
                FromImage = imageName
            },
            new AuthConfig(),
            report
            );


            return success;
        }

        public static async Task<bool> CheckImageExistsAsync(string imageNameToCheck)
        {
            bool exists = false;

            logger.Info("Checking if required docker image exists");

            IList<ImagesListResponse> images = null;
            try
            {
                images = await client.Images.ListImagesAsync(
                new ImagesListParameters()
                {
                    MatchName = imageNameToCheck
                });

                if (images.Count > 0)
                    exists = true;

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }


            return exists;
        }

        public static async Task<bool> CheckContainerExistsAsync(string containerNameToCheck)
        {
            containerNameToCheck = "/" + containerNameToCheck;  // Not sure why the container names are prefixed with /, but we'll add it so that we match. 
            bool exists = false;
            var containers = ListContainersAsync().Result;
            foreach (var container in containers)
            {
                foreach (var containerName in container.Names)
                {
                    //logger.Info("Container found: {0} in {1} state", containerName, container.State );
                    if (containerName.ToUpper() == containerNameToCheck)
                    {
                        //logger.Info("container MATCH: {0} == {1}", containerName.ToUpper(), containerNameToCheck);
                        exists = true;

                        if (container.State != "running")
                        {

                            IConfiguration config = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json", true, true)
                            .Build();
                            var eosiocppDockerImage = config["eosiocppDockerImage"];


                            logger.Info("The container is NOT RUNNING, it's in a {0} state. Start it", container.State);
                            var started = StartDockerAsync(eosiocppDockerImage, containerNameToCheck, true).Result;
                        }
                        if (exists)
                            break;
                    }

                }

            }
            return exists;
        }

        public static async Task<IList<ContainerListResponse>> ListContainersAsync()
        {
            IList<ContainerListResponse> containers = null;
            try
            {
                containers = await client.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    Limit = 100,
                });

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
            return containers;
        }

        public static async Task<bool> StartDockerAsync(string dockerImageName, string containerName, bool exists)
        {
            try
            {
                var m = new List<Mount>
                            {
                                new Mount
                                {
                                    Source = sourceCodePath,
                                    Target = "/data",
                                    Type = "bind"
                                }
                                /*
                                ,
                                new Mount
                                {
                                    Source = "C:/eosincludes",
                                    Target = "/host_eosinclude",
                                    Type = "bind"
                                }
                                */
                            };

                HostConfig h = new HostConfig();
                h.Mounts = m;

                if (!exists)
                {
                    logger.Info("Creating container {0} from image {1}", containerName, dockerImageName);
                    CreateContainerParameters create = new CreateContainerParameters()
                    {
                        Image = dockerImageName,
                        Tty = true,
                        Cmd = new List<string> { "/bin/bash" },
                        Name = containerName,
                        HostConfig = h,
                        WorkingDir = "/data"
                    };
                    var container = client.Containers.CreateContainerAsync(create, default(CancellationToken)).Result;
                }
                else
                {
                    logger.Info("Container {0} already exists", containerName);
                }

                logger.Info("AttachContainerAsync: {0}", containerName);
                var u = client.Containers.AttachContainerAsync(containerName, false, new ContainerAttachParameters() { }, default(CancellationToken)).Result;
                logger.Info("StartContainerAsync: {0}", containerName);
                var t = client.Containers.StartContainerAsync(containerName, new ContainerStartParameters() { }).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return true;
        }

        public static async Task<bool> RunCommandAsync(string command)
        {
            //const string id = "EOSCDT";

            try
            {
                var echo = Encoding.UTF8.GetBytes("ls -al");
                //var ping = Encoding.UTF8.GetBytes("/bin/ping -c 3 127.0.0.1");
                //var cmdToExecuteCP = Encoding.UTF8.GetBytes("cp -R /data /eosio.contracts/ \n");
                var cmdToExecute = Encoding.UTF8.GetBytes(command + "\n");

                var config = new ContainerExecCreateParameters
                {
                    AttachStderr = true,
                    AttachStdin = true,
                    AttachStdout = true,
                    Cmd = new string[] { "env", "TERM=xterm-256color", "bash" },
                    Detach = false,
                    Tty = false,
                    User = "root",
                    Privileged = true
                };

                var execId = await client.Containers.ExecCreateContainerAsync(Util.getContainerName(sourceCodePath), config);

                logger.Info("ExecCreateContainerAsync {0}", execId.ID);

                var configStart = new ContainerExecStartParameters
                {
                    AttachStderr = true,
                    AttachStdin = true,
                    AttachStdout = true,
                    Cmd = new string[] { "env", "TERM=xterm-256color", "bash" },
                    Detach = false,
                    Tty = false,
                    User = "root",
                    Privileged = true
                };

                var buffer = new byte[1024];
                using (var stream = await client.Containers.StartWithConfigContainerExecAsync(execId.ID, configStart, default(CancellationToken)))
                {
                    //await stream.WriteAsync(cmdToExecuteCP, 0, cmdToExecuteCP.Length, default(CancellationToken));
                    await stream.WriteAsync(cmdToExecute, 0, cmdToExecute.Length, default(CancellationToken));

                    stream.CloseWrite();

                    var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, default(CancellationToken));
                    do
                    {

                        String printMe = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        // Strip the local docker path from the output so that when VSCode get it, it can treat it as a relative path
                        logger.Info(printMe.Replace("/data/",""));
                        result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, default(CancellationToken));

                    }
                    while (!result.EOF);

                    logger.Info("End EOSIO contract build");
                }

            }
            catch (Exception ex)
            {
                logger.Error("x"  + ex.Message);
                logger.Error("x" + ex.InnerException);
                logger.Error(ex.StackTrace);
            }

            return true;

        }
    }
}
