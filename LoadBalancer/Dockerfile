FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LoadBalancer/LoadBalancer.API.csproj LoadBalancer/
RUN dotnet restore LoadBalancer/LoadBalancer.API.csproj

COPY LoadBalancer/ LoadBalancer/
RUN dotnet publish LoadBalancer/LoadBalancer.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LoadBalancer.API.dll"]
