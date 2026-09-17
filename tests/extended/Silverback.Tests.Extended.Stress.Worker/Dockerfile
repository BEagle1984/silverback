ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
FROM ${SDK_IMAGE} AS tools
RUN dotnet tool install dotnet-dump --tool-path /tools && dotnet tool install dotnet-stack --tool-path /tools

FROM ${SDK_IMAGE} AS build
WORKDIR /src
COPY . .
RUN mkdir -p /src/nuget
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish tests/extended/Silverback.Tests.Extended.Stress.Worker/Silverback.Tests.Extended.Stress.Worker.csproj -c Release -o /app /p:RunAnalyzers=false

FROM ${SDK_IMAGE}
WORKDIR /app
COPY --from=build /app .
COPY --from=tools /tools /tools
ENV PATH="${PATH}:/tools"
ENTRYPOINT ["dotnet", "Silverback.Tests.Extended.Stress.Worker.dll"]

