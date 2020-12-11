---
AIOMiner Application 

# Adding in your own mining application

AIOMiner allows for anyone to add in your own mining application.  AIOMiner will look for two files to work together to find out how to execute and monitor the application.

You will need a few things
1) JSON Editor
  * Online Editor - [jsoneditoronline.org](https://jsoneditoronline.org/)
  * Standalone Windows Application - [Notepad++](https://notepad-plus-plus.org/downloads/)
  
2) ZIP/RAR/7zip of an online location to download the miner from.  an example of this is a github link with a .zip
    ethminer - https://github.com/ethereum-mining/ethminer/releases/download/v0.18.0/ethminer-0.18.0-cuda10.0-windows-amd64.zip

3) Knowledge of how it's being run and the options you would like to use.  An example with ethminer is the following
   * ethminer.exe --api-port -3333 -RH -U -S eu2.ethermine.org:4444 -O <YOUR-ACCOUNT>.rig*
  


With your choice of JSON editor, open up the following file *~/Settings/Miners.json* folder. 
Here you will see a list of miners that are currently being supported by aiominer.  for you to add
in a new miner, you just need to simply copy one of the existing blocks, paste, and fill in with your settings.

## Example of Current Miners.json 
```
{
  "miners": [
    {
      "NAME": "ccminer_polytimos",
      "WEBLOCATION": "https://github.com/nemosminer/ccminerpolytimos/releases/download/Alexis78-1.0/ccminer-polytimos.7z",
      "PATH": "~\\Miners\\NVIDIA\\ccminer_polytimos\\"
    },
    {
      "NAME": "ccminer",
      "WEBLOCATION": "https://github.com/tpruvot/ccminer/releases/download/2.2.4-tpruvot/ccminer-x64-2.2.4-cuda9.7z",
      "PATH": "~\\Miners\\NVIDIA\\ccminer\\"
    }
      ]
}


 ```
 
 
## New Current Miners.json, with ethminer or any other mining application
```
{
  "miners": [
    {
      "NAME": "ccminer_polytimos",
      "WEBLOCATION": "https://github.com/nemosminer/ccminerpolytimos/releases/download/Alexis78-1.0/ccminer-polytimos.7z",
      "PATH": "~\\Miners\\NVIDIA\\ccminer_polytimos\\"
    },
    {
      "NAME": "ccminer",
      "WEBLOCATION": "https://github.com/tpruvot/ccminer/releases/download/2.2.4-tpruvot/ccminer-x64-2.2.4-cuda9.7z",
      "PATH": "~\\Miners\\NVIDIA\\ccminer\\"
    },
        {
      "NAME": "ethminer",
      "WEBLOCATION": "https://github.com/ethereum-mining/ethminer/releases/download/v0.18.0/ethminer-0.18.0-cuda10.0-windows-amd64.zip",
      "PATH": "~\\Miners\\any\\ethminer\\"
    }
      ]
}
 ```

`Tips:` Make sure you have in the double slashes for each directory path as exemplified above.  Also, make sure you can download the file yourself, and if it is to extract, what does that folder structure look like.  you will need to know the full folder structure for the next part. 

## MAKE SURE WHEN YOU SAVE THE FILE YOU HAVE CONFIRMED YOUR JSON EDIT IS CORRECT, VISIT A JSON LINT WEBSITE AND PAST THE WHOLE FILE IN TO ENSURE IT IS CORRECT!


Next, With your choice of JSON editor, open up the following file *~/Settings/MinerProcessInfo.json*

'TIP' - This file is separated by the algorithm.  if you are adding in a new algorithm, you will need to make a new section.  If the algorithm already exists you can simply copy a new block inside of the algo.  Review below for an example

AIOMiner uses keyword replacement, this allows for a generic setting to be passed to the ARGS section.  
 _ip = pool ip or FQDN (exp: https://aiominer.com/pool111 or 10.10.10.10)
 _port = pool port (exp: 8008)
 _worker = wallet address or username for a pool (exp:aiominer or 0x07bC4d2a376b770b69156f6f1616dbc033a94395)
 _pass = wallet password if needed
 _PubShared.GPUTOUSE = gpu's that you want to use while mining.  this will import 0 1 2 3 4 5 6 7 etc,etc,etc.  If a user disables a GPU, we will try to disable it here.
 

## Example of MinerProcessInfo.json
```
{
  "Process": [
    {
      "Algo": "ethash",
      "Info": [
        {
          "GPUx": "NVIDIA",
          "MINER": "nvidia-phoenix",
          "PATH": "~\\Miners\\phoenix\\PhoenixMiner_5.1c_Windows\\",
          "EXECUTABLE": "PhoenixMiner.exe",
          "PREFERED": "1",
          "API": "phoenix",
          "ARGS": "-pool _ip:_port -wal _worker"
        },
        {
          "GPUx": "AMD",
          "MINER": "amd-phoenix",
          "PATH": "~\\Miners\\phoenix\\PhoenixMiner_5.1c_Windows\\",
          "EXECUTABLE": "PhoenixMiner.exe",
          "PREFERED": "",
          "API": "phoenix",
          "ARGS": "-pool _ip:_port -wal _worker"
        }
      ]
    }
  ]
}

```

## Example of MinerProcessInfo.json with ethminer in the ethash algorithm group

```
{
  "Process": [
    {
      "Algo": "ethash",
      "Info": [
        {
          "GPUx": "NVIDIA",
          "MINER": "nvidia-phoenix",
          "PATH": "~\\Miners\\phoenix\\PhoenixMiner_5.1c_Windows\\",
          "EXECUTABLE": "PhoenixMiner.exe",
          "PREFERED": "1",
          "API": "phoenix",
          "ARGS": "-pool _ip:_port -wal _worker"
        },
        {
          "GPUx": "AMD",
          "MINER": "amd-phoenix",
          "PATH": "~\\Miners\\phoenix\\PhoenixMiner_5.1c_Windows\\",
          "EXECUTABLE": "PhoenixMiner.exe",
          "PREFERED": "",
          "API": "phoenix",
          "ARGS": "-pool _ip:_port -wal _worker"
        },
        {
          "GPUx": "AMD",
          "MINER": "ethminer",
          "PATH": "~\\Miners\\any\\ethminer\\",
          "EXECUTABLE": "ethminer.exe",
          "PREFERED": "1",
          "API": "ethminer",
          "ARGS": "--api-port -3333 -RH -U -S _ip:_port -O _wal"
        }
      ]
    }
  ]
}

  ```
  `tip` - Noticed how PREFERED changed.  If you have two prefered at the same time with this manual edit, only the first in the list will be used and you will run into issues later on if you want to swap from one miner to another.  ensure that AMD/NVIDIA both only have one prefered.
  
 
Make sure you verify your changes in a JSON LINTER.  Once both files have been updated.  You can go back to your AIOMiner application and open up the Settings/Benchmarking.  It should download your file, extract it and allow for it to be used by AIOMiner.  You will need to do this before you can start usings the application. 


  After testing, if everything works out. please consider a push request to update and give this miner to other users.  note that all tests are verified with CRC checks.  Anyone caught trying to push a virus or a modified copy of a miner in any way will be prosecuted to the fullest of the law.  Don't be a jerk.


