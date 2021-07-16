#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-focal AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src
COPY ["RebaseWorkflowStarterAction.csproj", "."]
RUN dotnet restore "./RebaseWorkflowStarterAction.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "RebaseWorkflowStarterAction.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RebaseWorkflowStarterAction.csproj" -c Release -o /app/publish

FROM base AS final
COPY --from=publish /app/publish /app
ENTRYPOINT ["dotnet", "/app/RebaseWorkflowStarterAction.dll"]