dotnet build Silverback.slnx --configuration Release
# The coverage reports have a timestamp in their name, so remove those of the previous runs
Remove-Item -Recurse -Force TestResults/coverage -ErrorAction SilentlyContinue
dotnet test --solution Silverback.slnx --no-build --no-restore --configuration Release --report-trx --coverlet --coverlet-output-format opencover --coverlet-exclude-by-attribute GeneratedCodeAttribute --coverlet-exclude "[*]*.Migrations.*" --coverlet-exclude-assemblies-without-sources MissingAll --results-directory TestResults/coverage --results-directory-layout per-module
dotnet tool restore
dotnet tool run reportgenerator -reports:"TestResults/coverage/**/*.opencover.*.xml" -targetdir:coveragereport -filefilters:"-*LoggerMessage.g.cs" -reporttypes:HTMLInline
. ./coveragereport/index.htm
