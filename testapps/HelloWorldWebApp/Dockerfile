FROM mcr.microsoft.com/dotnet/aspnet:3.1
ARG source
WORKDIR /app
EXPOSE 80
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "HelloWorldWebApp.dll"]
