FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HyperControlPanel.API.csproj", "./"]
RUN dotnet restore "./HyperControlPanel.API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "HyperControlPanel.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HyperControlPanel.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HyperControlPanel.API.dll"]