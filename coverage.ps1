dotnet build Silverback.sln --configuration Release
dotnet test Silverback.sln --no-build --no-restore --configuration Release --logger:trx --verbosity minimal /p:CollectCoverage=true /p:ExcludeByAttribute=GeneratedCodeAttribute /p:Exclude="[*]*.Migrations.*" /p:CoverletOutputFormat=opencover
dotnet tool restore
dotnet tool run reportgenerator -reports:"tests/**/*.opencover.xml" -targetdir:coveragereport -filefilters:"-*LoggerMessage.g.cs" -reporttypes:HTMLInline
. ./coveragereport/index.htm
