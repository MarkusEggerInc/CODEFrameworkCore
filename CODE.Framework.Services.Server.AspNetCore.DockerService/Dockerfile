FROM microsoft/aspnetcore:2.0.0
#ARG source

ENV ASPNETCORE_URLS=http://*:80
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80
EXPOSE 1433

WORKDIR /app

# copy publish folder contents to web root
COPY ./bin/Release/netcoreapp2.0/publish .

#ENTRYPOINT ["dotnet", "CODE.Framework.Core.ServiceHandler.Web.dll"]
CMD ["/bin/sh", "-c", "dotnet 'CODE.Framework.Core.ServiceHandler.Web.Generic.dll'"]