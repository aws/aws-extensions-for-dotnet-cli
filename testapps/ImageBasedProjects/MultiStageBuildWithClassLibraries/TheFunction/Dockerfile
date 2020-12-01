#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM public.ecr.aws/lambda/dotnet:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["TheFunction/TheFunction.csproj", "TheFunction/"]
COPY ["Supportlibrary/Supportlibrary.csproj", "Supportlibrary/"]
RUN dotnet restore "TheFunction/TheFunction.csproj"
COPY . .
WORKDIR "/src/TheFunction"
RUN dotnet build "TheFunction.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TheFunction.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /var/task
COPY --from=publish /app/publish .