FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["Baskets.Service/Baskets.Service.csproj", "Baskets.Service/"]
COPY ["Common.Domain/Common.Domain.csproj", "Common.Domain/"]
COPY ["Common.Api/Common.Api.csproj", "Common.Api/"]
COPY ["Common.Infrastructure/Common.Infrastructure.csproj", "Common.Infrastructure/"]
COPY ["Baskets.Domain/Baskets.Domain.csproj", "Baskets.Domain/"]
COPY ["Baskets.Integration/Baskets.Integration.csproj", "Baskets.Integration/"]
COPY ["Catalog.Integration/Catalog.Integration.csproj", "Catalog.Integration/"]
COPY ["Common.Data/Common.Data.csproj", "Common.Data/"]
COPY ["Baskets.Infrastructure/Baskets.Infrastructure.csproj", "Baskets.Infrastructure/"]
RUN dotnet restore "Baskets.Service/Baskets.Service.csproj"
COPY . .
WORKDIR "/src/Baskets.Service"
RUN dotnet build "Baskets.Service.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Baskets.Service.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SilverbackShop.Baskets.Service.dll"]