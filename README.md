# Dormitory Management System

This project is a microservices-based Dormitory Management System built with **.NET 10**, **Entity Framework Core**, **PostgreSQL**, **RabbitMQ**, and orchestrated with **.NET Aspire**.

---

## System Architecture

The project consists of the following microservices:
1. **Identity.API**: Manages user authentication, token generation (JWT), user roles, and security using ASP.NET Core Identity.
2. **Profile.API**: Stores detailed user profile records (specifically for students), emergency contacts, and personal information.
3. **RoomService.API**: Manages buildings, rooms, room types, capacities, amenities, and room bookings/reservations.
4. **BookingService.API**: Manages student booking requests, academic terms, and workflows via MassTransit Saga State Machine.
5. **Community.API**: Handles posts, lost-and-found announcements, likes, comments, and reports inside the dormitory.
6. **Billing.API**: Generates invoices (first-time deposits and monthly utility bills calculated progressively), progressive tiers, and tracks payment status.
7. **Chat.API**: Facilitates real-time direct and group messaging between students and administration.
8. **Incident.API**: Processes reports of facility damage/issues and manages the resolution workflow.
9. **Gateway.API**: The API gateway routing all incoming HTTP requests to their appropriate services.

---

## Prerequisites

Before running the application, make sure you have the following installed on your machine:
* **Docker & Docker Desktop**: Required to run containers for PostgreSQL, RabbitMQ, and microservices.
* **.NET 10 SDK** (Optional, but required if you want to run the project locally without Docker or build the images from source code).
* **WSL 2** (For Windows users, to run Docker containers efficiently).
* **pgAdmin** or **DBeaver** (Optional, to connect to and browse the database schemas).

---

## How to Run Using Docker Compose

The configuration for Docker Compose is located in the `docker-compose-artifacts` folder. The databases are by default configured to connect to Neon Serverless PostgreSQL in the cloud (credentials are partially preset), or you can use your own local PostgreSQL instance.

### Step 1: Build the Docker Images
From the root directory of the project, build the docker images for each service by running:

```bash
docker build -t identity-api:latest -f src/Identity.API/Dockerfile .
docker build -t profile-api:latest -f src/Profile.API/Dockerfile .
docker build -t room-api:latest -f src/RoomService.API/Dockerfile .
docker build -t booking-api:latest -f src/BookingService/BookingService.API/Dockerfile .
docker build -t community-api:latest -f src/Community.API/Dockerfile .
docker build -t billing-api:latest -f src/Billing.API/Dockerfile .
docker build -t chat-api:latest -f src/Chat.API/Dockerfile .
docker build -t incident-api:latest -f src/Incident.API/Dockerfile .
docker build -t gateway-api:latest -f src/Gateway.API/Dockerfile .
```

### Step 2: Configure the Environment Variables
1. Go to the `docker-compose-artifacts` directory:
   ```bash
   cd docker-compose-artifacts
   ```
2. Open the `.env` file and configure the empty variables. Here is a recommended configuration template for local execution:

```env
# Database Settings (Using your own local PostgreSQL or cloud Neon Postgres connection strings)
# Note: EF Core Migrations will automatically run and create tables on startup.
IDENTITYDB=Host=localhost;Port=5432;Database=identitydb;Username=postgres;Password=postgres;
PROFILEDB=Host=localhost;Port=5432;Database=profiledb;Username=postgres;Password=postgres;
ROOMDB=Host=localhost;Port=5432;Database=roomdb;Username=postgres;Password=postgres;
BOOKINGDB=Host=localhost;Port=5432;Database=bookingdb;Username=postgres;Password=postgres;
COMMUNITYDB=Host=localhost;Port=5432;Database=communitydb;Username=postgres;Password=postgres;
BILLINGDB=Host=localhost;Port=5432;Database=billingdb;Username=postgres;Password=postgres;
CHATDB=Host=localhost;Port=5432;Database=chatdb;Username=postgres;Password=postgres;
INCIDENTDB=Host=localhost;Port=5432;Database=incidentdb;Username=postgres;Password=postgres;

# RabbitMQ configuration
RABBITMQ_PASSWORD=

# JWT Configuration (For Token Authentication)
JWT_SECRET=
JWT_ISSUER=DormitoryIdentityService
JWT_AUDIENCE=DormitoryIdentityServiceClient
JWT_EXPIRY_MINUTES=15
JWT_REFRESH_EXPIRY_DAYS=7

# Docker Image tags built in Step 1
GATEWAY_API_IMAGE=gateway-api:latest
IDENTITY_API_IMAGE=identity-api:latest
PROFILE_API_IMAGE=profile-api:latest
ROOM_API_IMAGE=room-api:latest
BOOKING_API_IMAGE=booking-api:latest
COMMUNITY_API_IMAGE=community-api:latest
BILLING_API_IMAGE=billing-api:latest
CHAT_API_IMAGE=chat-api:latest
INCIDENT_API_IMAGE=incident-api:latest

# Port mappings to expose services
GATEWAY_API_PORT=8000
IDENTITY_API_PORT=8080
PROFILE_API_PORT=5001
ROOM_API_PORT=5002
BOOKING_API_PORT=5003
COMMUNITY_API_PORT=5004
BILLING_API_PORT=5005
CHAT_API_PORT=5006
INCIDENT_API_PORT=5007
```

### Step 3: Run Docker Compose
Start all containers in detached mode:
```bash
docker compose up -d
```

To view logs or stop the containers, use:
```bash
# View service logs
docker compose logs -f

# Stop and remove containers, networks, and volumes
docker compose down -v
```

---

## How to Run Locally with .NET Aspire (Recommended for Development)

The project includes built-in support for **.NET Aspire Orchestrator** which handles service discovery, RabbitMQ setup, logging, and connection strings out-of-the-box.

1. Ensure **Docker Desktop** is running.
2. From the root directory of the project, run:
   ```bash
   dotnet run --project src/dormitory.AppHost/dormitory.AppHost.csproj
   ```
3. The terminal will print a link to the **Aspire Dashboard** (usually on `http://localhost:18888`). Open this link in your browser to view logs, metrics, service health, and access service ports.

---

## Verifying the Services

* **API Gateway**: Access the primary entry point via the Gateway API at `http://localhost:8000` (or the port defined under `GATEWAY_API_PORT`).
* **RabbitMQ Console**: Access RabbitMQ management panel at `http://localhost:15672` (using username `guest` and the password configured in `RABBITMQ_PASSWORD`).
* **Aspire Dashboard**: Access dashboard at `http://localhost:18888` (token required, visible in console logs on startup).
