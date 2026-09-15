FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY src/MaxTelegramBridge/MaxTelegramBridge.csproj src/MaxTelegramBridge/
RUN dotnet restore src/MaxTelegramBridge/MaxTelegramBridge.csproj

COPY . .

RUN dotnet publish \
    src/MaxTelegramBridge/MaxTelegramBridge.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "MaxTelegramBridge.dll"]