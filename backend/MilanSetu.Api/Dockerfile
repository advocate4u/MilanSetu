# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . ./
RUN dotnet restore MilanSetu.Api.csproj
RUN dotnet publish MilanSetu.Api.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

RUN useradd --create-home --uid 10001 appuser
COPY --from=build /app/publish ./
RUN mkdir -p /app/private-verification-documents && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "MilanSetu.Api.dll"]
