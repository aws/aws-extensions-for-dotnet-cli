FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG source
WORKDIR /app
EXPOSE 80
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "HelloWorldWebApp.dll"]
