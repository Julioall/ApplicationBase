FROM node:20-alpine AS client-build
WORKDIR /app
COPY Application.Client/package.json Application.Client/package-lock.json ./Application.Client/
WORKDIR /app/Application.Client
RUN npm ci
COPY Application.Client/ .
RUN npm run build -- --configuration production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Application.Web/Application.Api.csproj Application.Web/
COPY Application.Domain/Application.Domain.csproj Application.Domain/
COPY Application.Shared/Application.Shared.csproj Application.Shared/
COPY Application.Infrastructure/Application.Infrastructure.csproj Application.Infrastructure/
COPY Application.Service/Application.Service.csproj Application.Service/
COPY Application.Client/Application.Client.esproj Application.Client/
RUN dotnet restore Application.Web/Application.Api.csproj

COPY . .
RUN dotnet publish Application.Web/Application.Api.csproj -c Release -o /app/publish
COPY --from=client-build /app/Application.Client/dist/Application.Client/browser /app/publish/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Application.Api.dll"]
