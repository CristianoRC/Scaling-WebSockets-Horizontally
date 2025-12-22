# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copia o arquivo do projeto e restaura dependências
COPY ChatApi.csproj .
RUN dotnet restore

# Copia o resto do código e builda
COPY . .
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Copia a aplicação buildada
COPY --from=build /app .

# Expõe a porta
EXPOSE 8080

# Define variáveis de ambiente padrão
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ChatApi.dll"]

