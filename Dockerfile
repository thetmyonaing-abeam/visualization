FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Visualization.API/Visualization.API.csproj", "Visualization.API/"]
RUN dotnet restore "Visualization.API/Visualization.API.csproj"
COPY src/ .
WORKDIR "/src/Visualization.API"
RUN dotnet build "Visualization.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Visualization.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Visualization.API.dll"]
