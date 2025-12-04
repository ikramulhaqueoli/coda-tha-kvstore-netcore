FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 7001
ENV ASPNETCORE_URLS=http://+:7001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KvStore.Router/KvStore.Router.csproj", "KvStore.Router/"]
RUN dotnet restore "KvStore.Router/KvStore.Router.csproj"
COPY . .
WORKDIR "/src/KvStore.Router"
RUN dotnet publish "KvStore.Router.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KvStore.Router.dll"]

