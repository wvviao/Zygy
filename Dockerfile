FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

ARG GIT_HASH=dev-build

COPY ["global.json", "./"]
COPY ["NuGet.Config", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["Zygy.slnx", "./"]
COPY ["Zygy.Api/Zygy.Api.csproj", "Zygy.Api/"]

RUN dotnet restore "Zygy.Api/Zygy.Api.csproj"

COPY . .
WORKDIR "/src/Zygy.Api"
RUN dotnet build "Zygy.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Zygy.Api.csproj" -c Release \
    -o /app/publish \
    /p:UseAppHost=true \
    /p:GitHash=${GIT_HASH} \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS final
WORKDIR /app

USER $APP_UID

COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["/app/Zygy.Api"]
