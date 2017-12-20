# publish the application first
dotnet publish -c Release

# clean up old image and any containers (running or not)
docker stop servicehandler
docker rm servicehandler-f 
docker rmi markuseggerinc/servicehandler:servicehandler

# create new image
docker build -t rickstrahl/servicehandler:servicehandler .

# immediately start running the container in the background (-d) (no console)
docker run  -it -p 5004:80 --name servicehandler  rickstrahl/servicehandler:servicehandler 

# Map host IP to a domain - so we can access local SQL server
# $localIpAddress=((ipconfig | findstr [0-9].\.)[0]).Split()[-1]
#--add-host dev.west-wind.com:$localIpAddress

#docker stop servicehandler
#docker rm servicehandler

# docker exec -it servicehandler  /bin/bash

# # if above doesn't work
# docker exec -it servicehandler  /bin/sh

#docker push 
