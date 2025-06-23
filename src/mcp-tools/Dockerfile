FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /McpTools

COPY ./*.sln ./
COPY ./*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done
RUN dotnet restore

COPY . ./

RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /McpTools
COPY --from=build /McpTools/out .
ENTRYPOINT ["dotnet", "McpTools.dll"]
