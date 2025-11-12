FROM hub.nexdev.net/base/dotnet/core/sdk:9.0-alpine AS builder
WORKDIR /src

COPY *.sln .
COPY ONEHub.GOTO.Consumer/ONEHub.GOTO.Consumer.csproj ./ONEHub.GOTO.Consumer/
COPY ONEHub.GOTO.Core/ONEHub.GOTO.Core.csproj ./ONEHub.GOTO.Core/
COPY ONEHub.Shared/ONEHub.Shared.csproj ./ONEHub.Shared/

RUN dotnet nuget add source https://nuget.nexdev.net/api/v2

RUN --mount=type=cache,target=/root/.nuget/packages \
	dotnet restore -v n ./ONEHub.GOTO.Consumer/ONEHub.GOTO.Consumer.csproj

COPY ONEHub.GOTO.Consumer/. ./ONEHub.GOTO.Consumer/
COPY ONEHub.GOTO.Core/. ./ONEHub.GOTO.Core/
COPY ONEHub.Shared/. ./ONEHub.Shared/

WORKDIR /src/ONEHub.GOTO.Consumer

RUN --mount=type=cache,target=/root/.nuget/packages \
  dotnet publish -v n -r linux-x64 --self-contained false -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:9.0-alpine AS base

RUN adduser --disabled-password \
  --home /app \
  --gecos '' dotnetuser && chown -R dotnetuser /app

USER dotnetuser

ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000


WORKDIR /app
COPY --from=builder /app .

ENV TZ="America/La_Paz"

ENTRYPOINT ["dotnet", "ONEHub.GOTO.Consumer.dll"]