:'
Build script for Arauco Otimizador API.
Usage: ./build.sh [Debug|Release]
'

CONFIGURATION="${1:-Release}"

dotnet restore arauco-otimizador-api.sln

dotnet build arauco-otimizador-api.sln --configuration "$CONFIGURATION" --no-restore
