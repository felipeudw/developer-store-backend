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
COPY src/Auth/Auth.Api/Auth.Api.csproj src/Auth/Auth.Api/
COPY src/Shared/Shared.Common/Shared.Common.csproj src/Shared/Shared.Common/

RUN dotnet restore src/Auth/Auth.Api/Auth.Api.csproj

# copy the rest of the source
COPY . .

# publish
WORKDIR /src/src/Auth/Auth.Api
RUN dotnet publish -c Release -o /app/publish

# STAGE 3: Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Auth.Api.dll"]