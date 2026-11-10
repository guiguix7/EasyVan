# Build and publish the ASP.NET Core MVC application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Gerenciamento de Van.csproj", "./"]
RUN dotnet restore "Gerenciamento de Van.csproj"
COPY . .
RUN dotnet publish "Gerenciamento de Van.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Run the published application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Npgsql may load the GSSAPI native library during authentication negotiation.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
# Railway injeta PORT; o fallback 8080 mantém o comportamento local/Docker.
ENTRYPOINT ["sh", "-c", "exec dotnet \"Gerenciamento de Van.dll\" --urls \"http://0.0.0.0:${PORT:-8080}\""]
