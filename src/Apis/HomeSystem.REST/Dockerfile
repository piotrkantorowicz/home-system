# Home System API image. Build context is the repository root:
#   docker build -f src/Apis/HomeSystem.REST/Dockerfile -t home-system-api .
# Configuration is runtime-only (environment variables / mounted appsettings); no secrets are baked in.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig HomeSystem.slnx ./
COPY src src
RUN dotnet publish src/Apis/HomeSystem.REST/HomeSystem.REST.csproj \
    --configuration Release \
    --output /app \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# The base image ships an unprivileged `app` user; listen on 8080 (non-root cannot bind 80).
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HomeSystem.REST.dll"]
