FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY SilverbackShop.sln ./
COPY Baskets.Service/Baskets.Service.csproj Baskets.Service/
COPY Common.Domain/Common.Domain.csproj Common.Domain/
COPY Baskets.Domain/Baskets.Domain.csproj Baskets.Domain/
COPY Baskets.Integration/Baskets.Integration.csproj Baskets.Integration/
COPY Catalog.Integration/Catalog.Integration.csproj Catalog.Integration/
COPY Common.Data/Common.Data.csproj Common.Data/
COPY Common.Infrastructure/Common.Infrastructure.csproj Common.Infrastructure/
COPY Baskets.Infrastructure/Baskets.Infrastructure.csproj Baskets.Infrastructure/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/Baskets.Service
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Baskets.Service.dll"]
