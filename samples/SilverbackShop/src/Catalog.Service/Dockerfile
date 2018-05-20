FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY SilverbackShop.sln ./
COPY Catalog.Service/Catalog.Service.csproj Catalog.Service/
COPY Catalog.Integration/Catalog.Integration.csproj Catalog.Integration/
COPY Catalog.Infrastructure/Catalog.Infrastructure.csproj Catalog.Infrastructure/
COPY Catalog.Domain/Catalog.Domain.csproj Catalog.Domain/
COPY Common.Domain/Common.Domain.csproj Common.Domain/
COPY Common.Data/Common.Data.csproj Common.Data/
COPY Common.Infrastructure/Common.Infrastructure.csproj Common.Infrastructure/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/Catalog.Service
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Catalog.Service.dll"]
