FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KvStore.Api/KvStore.Api.csproj", "KvStore.Api/"]
COPY ["KvStore.Core/KvStore.Core.csproj", "KvStore.Core/"]
COPY ["KvStore.Infrastructure/KvStore.Infrastructure.csproj", "KvStore.Infrastructure/"]
RUN dotnet restore "KvStore.Api/KvStore.Api.csproj"
COPY . .
WORKDIR "/src/KvStore.Api"
RUN dotnet publish "KvStore.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KvStore.Api.dll"]

