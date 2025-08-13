# STAGE 1: Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# STAGE 2: Build and publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy solution and project files for restore
COPY DeveloperStore.sln .
COPY src/Sales/Sales.Api/Sales.Api.csproj src/Sales/Sales.Api/
COPY src/Sales/Sales.Infrastructure/Sales.Infrastructure.csproj src/Sales/Sales.Infrastructure/
COPY src/Sales/Sales.Application/Sales.Application.csproj src/Sales/Sales.Application/
COPY src/Sales/Sales.Domain/Sales.Domain.csproj src/Sales/Sales.Domain/
COPY src/Shared/Shared.Common/Shared.Common.csproj src/Shared/Shared.Common/

RUN dotnet restore src/Sales/Sales.Api/Sales.Api.csproj

# copy the rest of the source
COPY . .

# publish
WORKDIR /src/src/Sales/Sales.Api
RUN dotnet publish -c Release -o /app/publish

# STAGE 3: Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sales.Api.dll"]