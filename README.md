EOS Easy Contract
============

EOS Easy Contract allows EOS developers to get started writing smart contracts in minutes, not hours! 

EOS Easy Contract allows you to create a smart contract without having to compile any software and does not require knowledge of the complex EOS ecosystem used to build its contracts. 

EOS Smart Contracts are written in C++, and configuring a development environment to compile those smart contracts can be time consuming and much of the tooling is not yet available for Windows developers. 

How does EOS Easy Contract pull this together? First, all of the tooling needed is packaged into a Docker container. When it comes time to compile your code, EOS Easy Contract runs all the needed commands inside the docker container and gives you back the result (.wasm file) which can then be uploaded to the EOS network. 

EOS Easy Contract provides tooling to quickly:
1. Create a starting Visual Studio Code template. 
2. A watcher application that will build your code every time you save. 
3. Allow Visual Studio Code to report errors back in a helpful way. 

Suported Platforms
------------
Currently Supported on: __Windows__, ~~OSX~~ (coming soon), ~~Linux~~ (coming soon)

Requirements
------------
- Docker (free) - https://www.docker.com/get-started
- Visual Studio Code (free) - https://code.visualstudio.com/download

Getting Started
------------

<iframe width="560" height="315" src="https://www.youtube.com/embed/h-9qDKCSN1g" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

#### 1. Get the software
Download ( https://github.com/eosnewyork/EOSEasyContract/releases ) and decompress the software (in this example we'll decompress the .zip file to a folder c:\tools\EOSEasyContract)


#### 2. Initialize you environment

This step added the exe folder to your PATH variable. 
```
# Your command prompt must have Admin rights as this will try to add a variable to your PATH
> cd c:\tools\EOSEasyContract
> EOSEasyContract.exe init windows
````

#### 3. Test docker and download missing images

Before executing the following commands, ensure that you this following setting is set correctly in your docker settings. 


![Docker Settings - Enable daemon ](DockerSettings.png)

![Docker Settings - Share drive](ShareDrive.png)

The following command will check that you have the required Docker image. If not, it'll be downloaded .. it is a large image (~3GB) and takes some time to download. 

```
> cd c:\tools\EOSEasyContract
> EOSEasyContract.exe init docker
````

#### 4. Test docker and download missing images

By executing the following, the header files in the docker container are copied to the local machine (C:\eosincludes folder) and will be referenced in the generated templates. This allows Visual studio code completion and other functions to act correctly in context.

```
> EOSEasyContract.exe init include
```


#### 5. Generate a project from template

To generate a new project use the the following command.

In the below example we will create a project called "EOSTemplate1" in the C:\temp folder. Note that c:\temp must exist and the project folder (EOSTemplte1 in this case) will be created. 

```
> EOSEasyContract.exe template new --path c:\temp --name EOSTemplate1
````


#### 6. Open the project in Visual Studio Code, using the below instruction or simply launch the GUI and open the folder

```
> cd c:\temp\EOSTemplate1
> code .
````

#### 7. Once in Visual Studio code, run the Build task by pressing CTL+SHIFT+B

You should see output that looks as follows in your Visual Studio termnal window. 

```
> Executing task: EOSEasyContract.exe build --path "C:\temp\EOSTemplate1" --watch <

Begin watching C:\temp\EOSTemplate1. Build using docker image XXXXXXXXXXXXXXXXX.
```

#### 8. Save the .cpp or .hpp file in the project, and watch the magic happen. 

You should see somethin like this in the terminal window:

```
        =========== Building eosio.contracts ===========


-- Setting up Eosio Wasm Toolchain

-- Configuring done

-- Generating done

-- Build files have been written to: build

Scanning dependencies of target EOSTemplate1.wasm

[ 50%] Building CXX object CMakeFiles/EOSTemplate1.wasm.dir/EOSTemplate1.cpp.o

[100%] Linking CXX executable buddy1.wasm

[100%] Built target EOSTemplate1.wasm

End EOSIO contract build
Done Building. Build Duration = 00:00:08.0230652
```

#### 9. The result of the build will be placed into a sub folder called "build" 

```
C:\temp\EOSTemplate1\EOSTemplate1.wasm
```


Uploading your contract to the blockchain
------------

Getting a test EOS Network up and running is currently outside the scope of this readme. 

The following steps can however be used to push your newly compiled contract to a testnet or mainnet of your choice. 

Check the Terminal output which compiled your contact and you'll find a container name that looks as follows: __EOSCDT-XXXXX__ (Example: EOSCDT-7BB4BB304C3DE98597913F1FDD1386EC) - This is the name of the docker container that was created to compile your contract. 

Run the following command to gain shell access to the running container. 
```
docker exec -it {Container Name} /bin/bash
```

Next create a wallet and import the private key for the account you'd like to publish the contract to. 
```
cleos wallet create --to-console

cleos wallet import --private-key {Account Private Key}
```
Upload the contract to your EOS account 
```
cleos -u {API URL}:{API Port} set contract {EOS Account Name} /data/build {Contract Name}.wasm {Contract Name}.abi -p {EOS Account Name}
```
Call an action on your contract. This example assumes you've uploaded the default template and that a method name 'Hi' exists. 
```
cleos -u {API URL}:{API Port} push action {EOS Account Name} hi '["username1"]' --permission {EOS Account Name}@active
```

Upgrading
------------

In order to upgrade: 

1. Delete the current binaries from your inital install locations. 
2. The following command will stop any running containers. 
```
> EOSEasyContract.exe util cleanDocker
```

3. The docker image may have been upgraded, so run the following commadn to download any new image. 
```
> EOSEasyContract.exe init docker
```

4. The include files may have changed, so run the following to download the latest include files. 
```
> EOSEasyContract.exe init include
```
