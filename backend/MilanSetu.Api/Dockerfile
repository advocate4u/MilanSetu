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
ENV VerificationDocuments__RootPath=/var/lib/milansetu/private-verification-documents
ENV VerificationDocuments__ClamAv__ExecutablePath=/usr/bin/clamscan
ENV VerificationDocuments__ClamAv__TimeoutSeconds=30
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends clamav clamav-freshclam \
    && rm -rf /var/lib/apt/lists/*
RUN useradd --create-home --uid 10001 appuser
COPY --from=build /app/publish ./
COPY entrypoint.sh freshclam.conf ./
RUN chmod 0555 /app/entrypoint.sh \
    && mkdir -p /var/lib/milansetu/private-verification-documents /var/lib/clamav \
    && chown -R appuser:appuser /app /var/lib/milansetu /var/lib/clamav
USER appuser

ENTRYPOINT ["/app/entrypoint.sh"]
