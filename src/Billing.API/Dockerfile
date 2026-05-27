FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.slnx .
COPY src/Billing.API/*.csproj ./src/Billing.API/
COPY src/Shared/*.csproj ./src/Shared/
COPY src/dormitory.ServiceDefaults/*.csproj ./src/dormitory.ServiceDefaults/
COPY Directory.Packages.props .
RUN dotnet restore ./src/Billing.API/Billing.API.csproj

# copy everything else and build app
COPY . .
WORKDIR /source/src/Billing.API
RUN dotnet build -c release -o /app/build
RUN dotnet publish -c release -o /app/publish /p:UseAppHost=false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
USER app
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Billing.API.dll"]
