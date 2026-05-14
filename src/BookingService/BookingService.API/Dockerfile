FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.slnx .
COPY src/BookingService/BookingService.API/*.csproj ./src/BookingService/BookingService.API/
COPY src/BookingService/BookingService.Application/*.csproj ./src/BookingService/BookingService.Application/
COPY src/BookingService/BookingService.Domain/*.csproj ./src/BookingService/BookingService.Domain/
COPY src/BookingService/BookingService.Infrastructure/*.csproj ./src/BookingService/BookingService.Infrastructure/

COPY src/Shared/*.csproj ./src/Shared/
COPY src/dormitory.ServiceDefaults/*.csproj ./src/dormitory.ServiceDefaults/
COPY Directory.Packages.props .
RUN dotnet restore ./src/BookingService/BookingService.API/BookingService.API.csproj

# copy everything else and build app
COPY . .
WORKDIR /source/src/BookingService/BookingService.API
RUN dotnet build "BookingService.API.csproj" -c release -o /app/build
RUN dotnet publish "BookingService.API.csproj" -c release -o /app/publish /p:UseAppHost=false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
USER app
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "BookingService.API.dll"]