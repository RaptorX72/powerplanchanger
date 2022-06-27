This small Windows service checks the running applications every 10 seconds, and sets the Power Mode to whatever we defined in the settings file.

If nothing is specified, the service will default to the first power plan added when detecting a process.

Installation:
1.  Go to PowerSetter.cs and change the AppDataPath variable to your own AppData/Roaming path. 
2.  Build the solution
3.  Move the created executable where it will be stored
4.  Open a Command Prompt or any other terminal with admin priviliges
5.  If a previous version of the service exists, perform the following command:
    ```sc delete yourservicename```
    ```sc delete "your service name"```
6.  Go to the .NET Framework folder
    ```cd C:\Windows\Microsoft.NET\Framework\v4.0.30319```
7. Perform the following command:
    ```.\installutil.exe "fullpathtoyourexe.exe"```
The service should now be installed and ready to start using services.msc.



To add/remove a power plan & process:
1.  Go to %appData%
2.  Go to Power Changer folder
3.  Open settings.xml

Power plan:
1.  Go to Choose a power plan in Windows, and select the plan you want
2.  Open terminal of choice, and type in:
    ```powercfg /GetActiveScheme```
3.  Copy the GUID
4.  Add a new node in the Power Plans node with your custom name and the copied ID.

Process:
1.  Start Task Manager
2.  Go to Details tab and find the process
3.  Add a new node in the Processes node, the value should be the process's name (without .exe, not case sensitive) and type in the desired plan's name in the plan attribute.