FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CareerGoat.Api/CareerGoat.Api.csproj", "src/CareerGoat.Api/"]
RUN dotnet restore "src/CareerGoat.Api/CareerGoat.Api.csproj"

COPY . .
WORKDIR /src/src/CareerGoat.Api
RUN dotnet publish "CareerGoat.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "CareerGoat.Api.dll"]
