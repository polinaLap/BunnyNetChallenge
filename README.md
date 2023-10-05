# BunnyNetChallenge

## Task description

Implement integration with a local container runtime API: containerd or Docker.
 
The integration should support the following functionalities:

-   create/start new container
-   stop/delete container
 

This functionality should be provided through REST API (so we can call create new container endpoint - with image name, and the backend code should deploy the container to one of the selected container runtimes). We would also like that you use Channels in your implementation.  

So the request from the REST API should be put into the channel and then consumed and processed via worker thread (demonstrate multithreading). The state - which containers are deployed at some moment and their status (created/running/stopped) should be stored in a shared dictionary and the status for the containers should be possible to be obtained via API endpoint.

## To launch

To launch the application locally:

- pull files from the repository
- make sure your Docker Engine is running
- update `appsettings.json`  with Docker base URL: 
``````csharp
Default for Windows: "npipe://./pipe/docker_engine" 
Default for Unix: "unix:///var/run/docker.sock"
``````
- run solution with `dotnet run` or IDE
- open `http://localhost:5183/swagger/index.html` to access Swagger
