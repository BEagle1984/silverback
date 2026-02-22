dotnet build -c Release ../Silverback.sln --no-incremental -o build

docfx metadata
docfx --serve --port 4242
