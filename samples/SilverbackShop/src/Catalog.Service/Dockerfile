FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["Catalog.Service/Catalog.Service.csproj", "Catalog.Service/"]
COPY ["Common.Api/Common.Api.csproj", "Common.Api/"]
COPY ["Common.Domain/Common.Domain.csproj", "Common.Domain/"]
COPY ["Common.Infrastructure/Common.Infrastructure.csproj", "Common.Infrastructure/"]
COPY ["Catalog.Integration/Catalog.Integration.csproj", "Catalog.Integration/"]
COPY ["Catalog.Infrastructure/Catalog.Infrastructure.csproj", "Catalog.Infrastructure/"]
COPY ["Catalog.Domain/Catalog.Domain.csproj", "Catalog.Domain/"]
COPY ["Common.Data/Common.Data.csproj", "Common.Data/"]
RUN dotnet restore "Catalog.Service/Catalog.Service.csproj"
COPY . .
WORKDIR "/src/Catalog.Service"
RUN dotnet build "Catalog.Service.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Catalog.Service.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SilverbackShop.Catalog.Service.dll"]