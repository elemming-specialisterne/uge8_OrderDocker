FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5028

ENV ASPNETCORE_URLS=http://+:5028

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /OrderService
COPY ["OrderService/OrderService.csproj", "OrderService/"]
RUN dotnet restore "OrderService\OrderService.csproj"
COPY . .
WORKDIR "/OrderService"
RUN dotnet build "OrderService/OrderService.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "OrderService/OrderService.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]

