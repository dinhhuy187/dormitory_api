FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.slnx .
COPY src/RoomService.API/*.csproj ./src/RoomService.API/
COPY src/Shared/*.csproj ./src/Shared/
COPY src/dormitory.ServiceDefaults/*.csproj ./src/dormitory.ServiceDefaults/
COPY Directory.Packages.props .
RUN dotnet restore ./src/RoomService.API/RoomService.API.csproj

# copy everything else and build app
COPY . .
WORKDIR /source/src/RoomService.API
RUN dotnet build -c release -o /app/build
RUN dotnet publish -c release -o /app/publish /p:UseAppHost=false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
USER app
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "RoomService.API.dll"]