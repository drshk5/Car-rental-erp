FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CarRentalERP.sln ./
COPY src/backend/CarRentalERP.Shared/CarRentalERP.Shared.csproj src/backend/CarRentalERP.Shared/
COPY src/backend/CarRentalERP.Domain/CarRentalERP.Domain.csproj src/backend/CarRentalERP.Domain/
COPY src/backend/CarRentalERP.Application/CarRentalERP.Application.csproj src/backend/CarRentalERP.Application/
COPY src/backend/CarRentalERP.Infrastructure/CarRentalERP.Infrastructure.csproj src/backend/CarRentalERP.Infrastructure/
COPY src/backend/CarRentalERP.Api/CarRentalERP.Api.csproj src/backend/CarRentalERP.Api/

RUN dotnet restore src/backend/CarRentalERP.Api/CarRentalERP.Api.csproj

COPY . .
RUN dotnet publish src/backend/CarRentalERP.Api/CarRentalERP.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CarRentalERP.Api.dll"]
